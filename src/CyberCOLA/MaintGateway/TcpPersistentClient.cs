using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.Service.WebAPI.MaintGateway.Gateway.Core;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Provides a persistent TCP client for maintaining a connection to a remote server with heartbeat and automatic reconnection capabilities.
/// </summary>
public class TcpPersistentClient : IDisposable
{
    private readonly string _ip = default!;
    private readonly int _port;
    private readonly int _localPort;
    private readonly int _receiveTimeout;
    private readonly int _heartbeatInterval;
    private readonly ILogger<TcpPersistentClient> _logger;
    private NetworkStream? _stream;
    private bool _isConnected = false;
    private DateTime _lastActivityTime = DateTime.Now;
    private readonly string _instanceId = Guid.NewGuid().ToString().Substring(0, 8);

    private static int _globalTaskRunning = 0;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _waitingTasks = new();
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpPersistentClient"/> class.
    /// </summary>
    /// <param name="config">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public TcpPersistentClient(IConfiguration config, ILogger<TcpPersistentClient> logger)
    {
        _logger = logger;
        try
        {
            string rawIp = config["ConnectionSettings:RemoteServer:RemoteIp"] ?? "127.0.0.1";
            _ip = (rawIp.ToLower() == "localhost") ? "127.0.0.1" : rawIp;
            _port = int.Parse(config["ConnectionSettings:RemoteServer:RemotePortNum"] ?? "14801");
            _localPort = int.Parse(config["ConnectionSettings:LocalServer:LocalPortNum"] ?? "24802");

            _receiveTimeout = int.Parse(config["ConnectionSettings:LocalServer:TimeoutMilliSeconds"] ?? "90000");
            _heartbeatInterval = int.Parse(config["ConnectionSettings:LocalServer:HeartbeatSeconds"] ?? "30") * 1000;

            _logger.LogInformation("[{ID}] TcpPersistentClient Initializing. Heartbeat: {HB}ms, Timeout: {TO}ms", _instanceId, _heartbeatInterval, _receiveTimeout);

            if (Interlocked.CompareExchange(ref _globalTaskRunning, 1, 0) == 0)
            {
                Task.Run(() => MaintainConnectionAsync(_cts.Token));
                Task.Run(() => HeartbeatLoopAsync(_cts.Token));
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{ID}] Config Error.", _instanceId);
        }
    }

    /// <summary>
    /// Sends a raw payload through the TCP stream after packing it with the binary protocol.
    /// </summary>
    /// <param name="payload">The raw ISO8583 payload bytes.</param>
    /// <param name="description">A description of the message for logging purposes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendRawAsync(byte[] payload, string description)
    {
        if (_stream == null || !_isConnected) throw new Exception("Stream is null or disconnected.");

        byte[] packet = BinaryProtocol.Pack(payload).ToArray();

        // Log outgoing bytes in HEX format
        _logger.LogDebug("[{ID}] >>> SEND ({Desc}): {Hex}", _instanceId, description, BitConverter.ToString(packet));

        await _stream.WriteAsync(packet, _cts.Token);
    }

    /// <summary>
    /// Background loop that triggers an echo test if the connection has been idle for longer than the heartbeat interval.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(5000, ct);
            if (_isConnected && (DateTime.Now - _lastActivityTime).TotalMilliseconds > _heartbeatInterval)
            {
                _ = Task.Run(async () => await SendEchoTestAsync(), ct);
            }
        }
    }

    /// <summary>
    /// Sends an ISO8583 0800 Echo Test request to the remote server.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendEchoTestAsync()
    {
        try
        {
            var echoGenerator = new NcccMessage0800("301");
            var ncccMsg = echoGenerator.GetNcccMessage();
            // Directly access Field 11 instead of property because property is only filled during Decode
            string? traceNo = ncccMsg.Core().Field(FIELD.F_11)?.ToString();
            if (string.IsNullOrEmpty(traceNo)) traceNo = DateTime.Now.ToString("HHmmss");

            var tcs = new TaskCompletionSource<byte[]>();
            _waitingTasks[traceNo] = tcs;

            byte[]? body = ncccMsg.Encode().Val();
            if (body != null)
            {
                await SendRawAsync(body, $"EchoTest-{traceNo}");
                var result = await Task.WhenAny(tcs.Task, Task.Delay(10000, _cts.Token));
                if (result == tcs.Task)
                {
                    _logger.LogDebug("[{ID}] Echo Test OK.", _instanceId);
                    _lastActivityTime = DateTime.Now;
                }
                else
                {
                    _logger.LogWarning("[{ID}] Echo Test TIMEOUT.", _instanceId);
                    _waitingTasks.TryRemove(traceNo, out _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ID}] Heartbeat failed.", _instanceId);
        }
    }

    /// <summary>
    /// Background task that establishes and maintains the TCP connection, including reading responses and handling reconnections.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task MaintainConnectionAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = new TcpClient(AddressFamily.InterNetwork);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.ReceiveTimeout = _receiveTimeout;

                var localEndPoint = new IPEndPoint(IPAddress.Any, _localPort);
                client.Client.Bind(localEndPoint);

                await client.ConnectAsync(_ip, _port, ct);
                _stream = client.GetStream();
                _isConnected = true;
                _lastActivityTime = DateTime.Now;
                _logger.LogInformation("[{ID}] TCP Connected. Local: {LPort} -> Remote: {RPort}", _instanceId, _localPort, _port);

                _ = Task.Run(async () => await SendInitialSignOnAsync(), ct);

                var reader = PipeReader.Create(_stream);
                while (!ct.IsCancellationRequested)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(_receiveTimeout);

                    ReadResult result;
                    try
                    {
                        result = await reader.ReadAsync(timeoutCts.Token);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        if ((DateTime.Now - _lastActivityTime).TotalMilliseconds > _receiveTimeout)
                        {
                            _logger.LogWarning("[{ID}] TCP Dead Line reached. Reconnecting...", _instanceId);
                            break;
                        }
                        continue;
                    }

                    _lastActivityTime = DateTime.Now;
                    var buffer = result.Buffer;
                    SequencePosition consumed = buffer.Start;

                    try
                    {
                        while (BinaryProtocol.TryParse(ref buffer, out var message))
                        {
                            consumed = buffer.Start;
                            // Log incoming bytes in HEX format (using BitConverter)
                            _logger.LogDebug("[{ID}] <<< RECV: {Hex}", _instanceId, BitConverter.ToString(message!));
                            await ProcessIncomingMessageAsync(message!);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[{ID}] Parse Error.", _instanceId);
                    }
                    finally
                    {
                        reader.AdvanceTo(consumed, buffer.End);
                    }

                    if (result.IsCompleted) break;
                }
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    _logger.LogError("[{ID}] Connection Error: {Msg}", _instanceId, ex.Message);
            }
            finally
            {
                _isConnected = false;
                _stream?.Dispose();
                _stream = null;
                client?.Dispose();
                if (!ct.IsCancellationRequested) await Task.Delay(5000, ct);
            }
        }
        Interlocked.Exchange(ref _globalTaskRunning, 0);
    }

    /// <summary>
    /// Sends an initial ISO8583 0800 Sign-On request to the remote server upon connection establishment.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendInitialSignOnAsync()
    {
        if (_stream == null || !_isConnected || _cts.IsCancellationRequested) return;
        try
        {
            var signOnGenerator = new NcccMessage0800("001");
            var ncccMsg = signOnGenerator.GetNcccMessage();
            // Directly access Field 11 instead of property because property is only filled during Decode
            string? traceNo = ncccMsg.Core().Field(FIELD.F_11)?.ToString();
            if (string.IsNullOrEmpty(traceNo)) traceNo = DateTime.Now.ToString("HHmmss");

            var tcs = new TaskCompletionSource<byte[]>();
            _waitingTasks[traceNo] = tcs;

            byte[]? body = ncccMsg.Encode().Val();
            if (body != null)
            {
                await SendRawAsync(body, $"SignOn-{traceNo}");
                var result = await Task.WhenAny(tcs.Task, Task.Delay(5000, _cts.Token));
                if (result == tcs.Task)
                {
                    var resp = await tcs.Task;
                    var ncccRsp = new NcccIso8583();
                    ncccRsp.Decode(resp);
                    _logger.LogInformation("[{ID}] Sign-On SUCCESS. RspCode: {Code}", _instanceId, ncccRsp.Core().ResponseCode);
                    _lastActivityTime = DateTime.Now;
                }
                else
                {
                    _logger.LogWarning("[{ID}] Sign-On TIMEOUT.", _instanceId);
                    _waitingTasks.TryRemove(traceNo, out _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ID}] Sign-On Exception.", _instanceId);
        }
    }

    /// <summary>
    /// Processes incoming messages from the TCP stream, handling both requests (auto-replies) and responses to waiting tasks.
    /// </summary>
    /// <param name="message">The raw message bytes received from the remote server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessIncomingMessageAsync(byte[] message)
    {
        try
        {
            if (message.Length < 12) return;
            var ncccReceiveMsg = new NcccIso8583();
            ncccReceiveMsg.Decode(message);
            var mti = ncccReceiveMsg.Mti();
            var traceNo = ncccReceiveMsg.Core().SystemTraceAuditNumber;

            bool isRequest = (mti.Length >= 4) && ((mti[2] - '0') % 2 == 0);
            if (isRequest)
            {
                if (mti.StartsWith("08") && _stream != null)
                {
                    var ncccSendMsg = new NcccMessage0800(ncccReceiveMsg).GetNcccMessage();
                    byte[]? sendBytes = ncccSendMsg.Encode().Val();
                    if (sendBytes != null)
                    {
                        await SendRawAsync(sendBytes, $"AutoReply-{mti}-{traceNo}");
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(traceNo) && _waitingTasks.TryRemove(traceNo, out var tcs))
                {
                    tcs.SetResult(message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ID}] ProcessIncoming Error.", _instanceId);
        }
    }

    /// <summary>
    /// Sends a payload asynchronously and waits for a corresponding response based on the provided correlation key.
    /// </summary>
    /// <param name="key">The correlation key (typically STAN/Field 11).</param>
    /// <param name="payload">The packed ISO8583 message bytes.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <returns>The response message bytes received from the remote server.</returns>
    /// <exception cref="Exception">Thrown if the client is offline.</exception>
    /// <exception cref="TimeoutException">Thrown if the response is not received within the specified timeout.</exception>
    public async Task<byte[]> SendAsync(string key, byte[] payload, int timeoutMs = 30000)
    {
        if (!_isConnected || _stream == null || _cts.IsCancellationRequested) throw new Exception("Offline");
        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waitingTasks[key] = tcs;
        try
        {
            await SendRawAsync(payload, $"Transaction-{key}");
            using var internalCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            internalCts.CancelAfter(timeoutMs);
            using (internalCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        catch (OperationCanceledException)
        {
            _waitingTasks.TryRemove(key, out _);
            throw new TimeoutException($"Timeout:{key}");
        }
        catch
        {
            _waitingTasks.TryRemove(key, out _);
            throw;
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="TcpPersistentClient"/> instance.
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("[{ID}] Disposing TcpPersistentClient...", _instanceId);
        _cts.Cancel();
        _cts.Dispose();
        _stream?.Dispose();
        _waitingTasks.Clear();
    }
}

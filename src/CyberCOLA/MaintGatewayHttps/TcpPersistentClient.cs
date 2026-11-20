using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.Service.WebAPI.MaintGatewayHttps.Gateway.Core;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

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

            _logger.LogInformation("[{ID}] TcpPersistentClient (HTTPS) Initializing. Heartbeat: {HB}ms, Timeout: {TO}ms", _instanceId, _heartbeatInterval, _receiveTimeout);

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

    // Centralized Send Method for HEX logging (using BitConverter)
    private async Task SendRawAsync(byte[] payload, string description)
    {
        if (_stream == null || !_isConnected) throw new Exception("Stream is null or disconnected.");

        byte[] packet = BinaryProtocol.Pack(payload).ToArray();

        // Log outgoing bytes in HEX format
        _logger.LogDebug("[{ID}] >>> SEND ({Desc}): {Hex}", _instanceId, description, BitConverter.ToString(packet));

        await _stream.WriteAsync(packet, _cts.Token);
    }

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

    public void Dispose()
    {
        _logger.LogInformation("[{ID}] Disposing TcpPersistentClient...", _instanceId);
        _cts.Cancel();
        _cts.Dispose();
        _stream?.Dispose();
        _waitingTasks.Clear();
    }
}

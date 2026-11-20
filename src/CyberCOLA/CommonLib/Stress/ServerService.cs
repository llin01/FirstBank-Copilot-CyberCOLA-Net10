using System.IO;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Network.Utilis.CarrierKeys;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.MCOLA;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.TM.MasterCard;
using Com.Cybersoft4u.TM.VISA;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;

using System.Net.Sockets;
using System.IO.Pipelines;
using System.Buffers;
using System.Threading.Tasks;
using Experimental.System.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Com.Cybersoft4u.CommonLib.Stress
{
    public class ServerService
    {
        private static string? _tranType;
        private static string? _ipAddressString;
        private static int _portNumber;
        private static IPayloadQueue[]? _messageQueue;
        private static int _sendInterval;
        private static int _clientIndex = 0;
        private static string[]? _visaCardTrack2Array;
        private static string[]? _masterCardTrack2Array;
        private static string[]? _jcbCardTrack2Array;
        private static bool _isWaitingSignOn;
        private static List<string>? _maintRemotePortList;
        private static bool _isReplaceCardNumber;
        private static bool _isReplaceOriginalData;
        private static int _sendMessageRepeatCount;
        private static bool _IsAutoEcho;
        private static Logger? Logger = null;

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private static readonly ConcurrentDictionary<ClientConnectionContext, ChannelInfo> ChannelsStatus = new ConcurrentDictionary<ClientConnectionContext, ChannelInfo>();
        private static readonly ConcurrentDictionary<string, ClientConnectionContext> ActiveConnections = new ConcurrentDictionary<string, ClientConnectionContext>();

        private static readonly Random StaticTraceRnd = new();
        private static readonly Random StaticAmtRnd = new();
        private static readonly Random StaticRefRnd = new();
        private static readonly Random StaticVisaCardIndex = new();
        private static readonly Random StaticMasterCardIndex = new();
        private static readonly Random StaticJcbCardIndex = new();
        private static readonly object DelayTimeLock = new();
        private static int delayMilliseconds = 0;

        // Thread-safe statistics
        public static StressMetrics Metrics { get; } = new StressMetrics();

        public class ClientConnectionContext
        {
            public Socket Socket { get; }
            public string Key { get; }
            public IPEndPoint RemoteEndPoint { get; }
            public IPEndPoint LocalEndPoint { get; }
            public Timer? SendTimer { get; set; }

            public ClientConnectionContext(Socket socket, string key)
            {
                Socket = socket;
                Key = key;
                RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint!;
                LocalEndPoint = (IPEndPoint)socket.LocalEndPoint!;
            }
        }



        public ServerService(string? tranType,
                             string? ipAddressString,
                             int portNumber,
                             IPayloadQueue[]? messageQueue,
                             int sendInterval,
                             string? cardXmlFilePath,
                             bool isWaitingSignOn,
                             List<string>? maintRemotePortList,
                             bool isReplaceCardNumber,
                             bool isReplaceOriginalData,
                             int sendMessageRepeatCount,
                             bool IsAutoEcho = true)
        {
            _tranType = tranType;
            _ipAddressString = ipAddressString;
            _portNumber = portNumber;
            _messageQueue = messageQueue;
            _sendInterval = sendInterval;
            GetCardArray(cardXmlFilePath);
            _isWaitingSignOn = isWaitingSignOn;
            _maintRemotePortList = maintRemotePortList?.ToList() ?? new List<string>();
            _isReplaceCardNumber = isReplaceCardNumber;
            _isReplaceOriginalData = isReplaceOriginalData;
            _sendMessageRepeatCount = sendMessageRepeatCount;
            _IsAutoEcho = IsAutoEcho;
        }

        public void StartService()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[ServerService]");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Parse(_ipAddressString ?? "127.0.0.1"), _portNumber);
            _listener.Start(_messageQueue?.Length ?? 10);
            Logger.Info($"Server Service listening on port: {_portNumber}");

            Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public void StopService()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();

                // Close all active client sockets and cancel their per-client timers
                foreach (var conn in ActiveConnections.Values)
                {
                    try { conn.SendTimer?.Change(Timeout.Infinite, Timeout.Infinite); } catch { /* Ignore timer change error */ }
                    try { conn.SendTimer?.Dispose(); } catch { /* Ignore timer dispose error */ }
                    try { conn.Socket.Shutdown(SocketShutdown.Both); } catch { /* Ignore socket shutdown error */ }
                    try { conn.Socket.Close(); } catch { /* Ignore socket close error */ }
                }

                ActiveConnections.Clear();
                ChannelsStatus.Clear();
                Logger?.Info("Server Service stopped successfully.");
            }
            catch (Exception ex)
            {
                Logger?.Info(ex.Message, ex);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _listener = null;
            }
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            int connectionId = 0;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Socket socket = await _listener!.AcceptSocketAsync(ct);
                    string key = $"{socket.RemoteEndPoint}_{Interlocked.Increment(ref connectionId)}";
                    var ctx = new ClientConnectionContext(socket, key);

                    if (_clientIndex >= (_messageQueue?.Length ?? 0))
                    {
                        socket.Close();
                        continue;
                    }

                    int queueIndex = _clientIndex++;
                    var info = new ChannelInfo { IsSignOn = false, IsWriteTimer = false, QueueIndex = queueIndex };
                    ChannelsStatus.TryAdd(ctx, info);
                    ActiveConnections.TryAdd(key, ctx);

                    Metrics.IncrementConnections();

                    // Start background client handling pipeline
                    _ = Task.Run(() => HandleClientAsync(ctx, info, ct), ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger?.Error("Error accepting client socket connection.", ex);
                }
            }
        }

        private async Task HandleClientAsync(ClientConnectionContext ctx, ChannelInfo info, CancellationToken ct)
        {
            Logger?.Info($"Add {ctx.RemoteEndPoint}");

            // Start sending worker loop if not waiting for SignOn
            CancellationTokenSource workerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!_isWaitingSignOn)
            {
                string remotePort = ctx.RemoteEndPoint.Port.ToString();
                var maintChannel = _maintRemotePortList?.Find(c => c.Equals(remotePort));
                if (string.IsNullOrEmpty(maintChannel))
                {
                    _ = Task.Run(() => SendQueueWorkerAsync(ctx, info, workerCts.Token), ct);
                }
            }

            // Start Pipeline Socket parsing
            try
            {
                var pipe = new Pipe();
                Task writing = FillPipeAsync(ctx.Socket, pipe.Writer, workerCts.Token);
                Task reading = ReadPipeAsync(ctx, pipe.Reader, workerCts.Token);
                await Task.WhenAll(writing, reading);
            }
            catch (Exception ex)
            {
                Logger?.Error($"[Client {ctx.Key}] Pipeline processing error.", ex);
            }
            finally
            {
                workerCts.Cancel();
                Metrics.DecrementConnections();
                ActiveConnections.TryRemove(ctx.Key, out _);
                ChannelsStatus.TryRemove(ctx, out _);
                Logger?.Info($"Remove {ctx.RemoteEndPoint}");
                try { ctx.Socket.Close(); } catch { /* Ignore socket close error */ }
            }
        }

        private async Task SendQueueWorkerAsync(ClientConnectionContext ctx, ChannelInfo info, CancellationToken ct)
        {
            int localDelay = 0;
            lock (DelayTimeLock)
            {
                delayMilliseconds += 50;
                localDelay = delayMilliseconds;
            }

            uint readQueueCount = 0;
            int interval = _sendInterval + localDelay;

            // Auto Echo Timer Simulation if enabled
            Task? echoTask = null;
            if (_IsAutoEcho)
            {
                echoTask = Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(10000, ct); // 10 seconds auto-echo
                        try
                        {
                            byte[] echoBytes = Array.Empty<byte>();
                            if (_tranType == "NCCC")
                                echoBytes = CreateNcccEchoTestRequestMessage();

                            if (echoBytes.Length > 0)
                            {
                                await ctx.Socket.SendAsync(echoBytes, SocketFlags.None, ct);
                                Metrics.IncrementSuccess();
                            }
                        }
                        catch { break; }
                    }
                }, ct);
            }

            try
            {
                // Main queue push sender
                while (!ct.IsCancellationRequested)
                {
                    var payloadQueue = _messageQueue?[info.QueueIndex];
                    if (payloadQueue != null)
                    {
                        byte[]? messageData = ReceiveQueueData(payloadQueue, info.QueueIndex);
                        if (messageData != null)
                        {
                            Logger?.Info($"Read QueueIndex[{info.QueueIndex}], Length [{messageData.Length}] Count[{++readQueueCount}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                            Logger?.Info($"RemoteEndPoint[{ctx.RemoteEndPoint}],Write Data[{Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(messageData, 2, messageData.Length - 2))}]");
                            var traceNumber = GetTraceNumber(messageData);
                            Logger?.Info($"Send Message{ctx.RemoteEndPoint}_{traceNumber}");

                            await ctx.Socket.SendAsync(messageData, SocketFlags.None, ct);
                            Metrics.IncrementSuccess();
                        }
                    }
                    await Task.Delay(interval, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger?.Fatal($"SendQueueWorkerAsync Exception : [{ex.Message}]");
            }
        }

        private async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken ct)
        {
            const int minimumBufferSize = 512;
            while (!ct.IsCancellationRequested)
            {
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                try
                {
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, ct);
                    if (bytesRead == 0) break;
                    writer.Advance(bytesRead);
                }
                catch
                {
                    Metrics.IncrementErrors();
                    break;
                }

                FlushResult result = await writer.FlushAsync(ct);
                if (result.IsCompleted) break;
            }
            await writer.CompleteAsync();
        }

        private async Task ReadPipeAsync(ClientConnectionContext ctx, PipeReader reader, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(ct);
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryParseMessage(ref buffer, out ReadOnlySequence<byte> message))
                {
                    byte[] requestBytes = message.ToArray();
                    Logger?.Info($"Server Receive Data[{HexStringConverter.FromByteArray(requestBytes)}]");

                    byte[]? retnBytes = null;
                    List<byte[]>? listRetnBytes = null;

                    if (_isWaitingSignOn)
                    {
                        switch (_tranType)
                        {
                            case "FISC":
                                retnBytes = ReceiveMessageForFisc(ctx, requestBytes);
                                break;
                            case "NCCC":
                                retnBytes = ReceiveMessageForNccc(ctx, requestBytes);
                                break;
                            case "TWIN":
                                retnBytes = ReceiveMessageForTwin(ctx, requestBytes);
                                break;
                        }

                        if (retnBytes != null)
                        {
                            await ctx.Socket.SendAsync(retnBytes, SocketFlags.None, ct);
                            Metrics.IncrementSuccess();
                        }
                    }
                    else
                    {
                        switch (_tranType)
                        {
                            case "FISC":
                                listRetnBytes = ReceiveMultiMessageForFisc(ctx, requestBytes);
                                break;
                            case "NCCC":
                                listRetnBytes = ReceiveMultiMessageForNccc(ctx, requestBytes);
                                break;
                            case "TWIN":
                                listRetnBytes = ReceiveMultiMessageForTwin(ctx, requestBytes);
                                break;
                        }

                        if (listRetnBytes != null && listRetnBytes.Count > 0)
                        {
                            foreach (var sendBytes in listRetnBytes)
                            {
                                await ctx.Socket.SendAsync(sendBytes, SocketFlags.None, ct);
                                Metrics.IncrementSuccess();
                                await Task.Delay(10, ct);
                            }
                        }
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted) break;
            }
            await reader.CompleteAsync();
        }

        private bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> message)
        {
            message = default;
            if (buffer.Length < 2) return false;

            // 1. Read packet length (Big-Endian 2-byte header for FISC/NCCC/TWIN/MC, or Visa 4-byte)
            if (_tranType == "VISA")
            {
                if (buffer.Length < 4) return false;

                // Read 4 bytes length
                byte[] lenBytes = new byte[4];
                buffer.Slice(0, 4).CopyTo(lenBytes);
                // Visa length is either raw Big-Endian or 2-byte inside 4-byte
                int length = (lenBytes[2] << 8) | lenBytes[3]; // Visa standard length parsing

                if (buffer.Length < length + 4) return false;
                message = buffer.Slice(0, length + 4);
                buffer = buffer.Slice(length + 4);
                return true;
            }
            else
            {
                byte[] lenBytes = new byte[2];
                buffer.Slice(0, 2).CopyTo(lenBytes);
                int length = (lenBytes[0] << 8) | lenBytes[1];

                if (buffer.Length < length + 2) return false;
                message = buffer.Slice(0, length + 2);
                buffer = buffer.Slice(length + 2);
                return true;
            }
        }

        private static void GetCardArray(string? cardXmlFilePath)
        {
            if (string.IsNullOrEmpty(cardXmlFilePath) || !File.Exists(cardXmlFilePath))
            {
                _jcbCardTrack2Array = Array.Empty<string>();
                _visaCardTrack2Array = Array.Empty<string>();
                _masterCardTrack2Array = Array.Empty<string>();
                return;
            }
            var doc = XDocument.Load(cardXmlFilePath);
            var track2DataList = doc.Descendants("CardInformation")
                                    .Select(cardInformation => cardInformation.Element("Track2Data")?.Value)
                                    .Where(x => x != null)
                                    .ToList();

            var jcbList = new List<string>();
            var visaList = new List<string>();
            var masterCardList = new List<string>();

            foreach (var track2 in track2DataList)
            {
                if (track2 != null)
                {
                    if (track2.StartsWith('3'))
                        jcbList.Add(track2);
                    else if (track2.StartsWith('4'))
                        visaList.Add(track2);
                    else if (track2.StartsWith('5'))
                        masterCardList.Add(track2);
                }
            }

            _jcbCardTrack2Array = jcbList.ToArray();
            _visaCardTrack2Array = visaList.ToArray();
            _masterCardTrack2Array = masterCardList.ToArray();
        }

        private static byte[]? ReceiveQueueData(IPayloadQueue messageQueue, int queueIndex)
        {
            try
            {
                byte[] returnMessageBytes = null;
                byte[]? receiveData = messageQueue.Receive(TimeSpan.FromMilliseconds(100));
                //System.Messaging.Message receiveData = messageQueue.Receive(new TimeSpan(0, 2, 0));
                if (receiveData == null)
                {
                    Logger.Info("Read MessageQueue Data Null");
                    return null;
                }

                if (_tranType.Equals("FISC"))
                    returnMessageBytes = CreateFiscRequestMessage(receiveData, queueIndex);
                else if (_tranType.Equals("NCCC"))
                    returnMessageBytes = CreateNcccRequestMessage(receiveData, queueIndex);
                return returnMessageBytes;
            }
            catch (MessageQueueException mqex)
            {
                if (mqex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    Logger.Debug("Read MessageQueue IOTimeout");
                    //if (_tranType.Equals("NCCC"))
                    //    return CreateNcccEchoTestRequestMessage();
                    //if (_tranType.Equals("NCPS"))
                    //    return CreateNcccEchoTestRequestMessage();
                    //if (_tranType.Equals("TWMP"))
                    //    return CreateNcccEchoTestRequestMessage();
                    //if (_tranType.Equals("VISA"))
                    //    return CreateVisaEchoTestRequestMessage();
                    //if (_tranType.Equals("MASTERCARD"))
                    //    return CreateMasterCardEchoTestRequestMessage();
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveQueueData Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] CreateFiscRequestMessage(byte[] requestMessage, int queueIndex)
        {
            try
            {
                if (!_isReplaceOriginalData)
                {
                    return ColaCommUtility.AddBinaryLength2BytesArray(requestMessage);
                }
                var fisc = new FISC(requestMessage);
                var cardIndex = StaticVisaCardIndex.Next(0, _visaCardTrack2Array.Length - 1);
                var track2Data = _visaCardTrack2Array[cardIndex];
                //Check Type
                var fieldName = FIELD.F_2;
                if (fisc.Core().Field(fieldName) != null)
                {
                    string cardNumber = (string)fisc.Core().Field(fieldName);
                    var cardType = CreditCardUtility.FindCardType(cardNumber);
                    switch (cardType)
                    {
                        case CreditCardType.JCB:
                            if (_jcbCardTrack2Array.Length == 0) break;
                            cardIndex = StaticJcbCardIndex.Next(0, _jcbCardTrack2Array.Length - 1);
                            track2Data = _jcbCardTrack2Array[cardIndex];
                            break;
                        case CreditCardType.MasterCard:
                            if (_masterCardTrack2Array.Length == 0) break;
                            cardIndex = StaticMasterCardIndex.Next(0, _masterCardTrack2Array.Length - 1);
                            track2Data = _masterCardTrack2Array[cardIndex];
                            break;
                        default:
                            if (_visaCardTrack2Array.Length == 0) break;
                            cardIndex = StaticVisaCardIndex.Next(0, _visaCardTrack2Array.Length - 1);
                            track2Data = _visaCardTrack2Array[cardIndex];
                            break;
                    }
                }
                else
                {
                    fieldName = FIELD.F_35;
                    if (fisc.Core().Field(fieldName) != null)
                    {
                        string originalTrack2Data = (string)fisc.Core().Field(fieldName);
                        int startIndex = originalTrack2Data.IndexOfAny(new char[] { '=', 'D', '~', '^' });
                        string cardNumber;
                        if (startIndex > 0)
                            cardNumber = originalTrack2Data.Substring(0, startIndex);
                        else
                            cardNumber = originalTrack2Data;
                        var cardType = CreditCardUtility.FindCardType(cardNumber);
                        switch (cardType)
                        {
                            case CreditCardType.JCB:
                                if (_jcbCardTrack2Array.Length == 0) break;
                                cardIndex = StaticJcbCardIndex.Next(0, _jcbCardTrack2Array.Length - 1);
                                track2Data = _jcbCardTrack2Array[cardIndex];
                                break;
                            case CreditCardType.MasterCard:
                                if (_masterCardTrack2Array.Length == 0) break;
                                cardIndex = StaticMasterCardIndex.Next(0, _masterCardTrack2Array.Length - 1);
                                track2Data = _masterCardTrack2Array[cardIndex];
                                break;
                            default:
                                if (_visaCardTrack2Array.Length == 0) break;
                                cardIndex = StaticVisaCardIndex.Next(0, _visaCardTrack2Array.Length - 1);
                                track2Data = _visaCardTrack2Array[cardIndex];
                                break;
                        }
                    }
                }

                //Change Field Data
                fieldName = FIELD.F_2;
                if (fisc.Core().Field(fieldName) != null)
                {
                    if (_isReplaceCardNumber)
                        fisc.Core().Field(fieldName, track2Data.Substring(0, 16));
                }

                fieldName = FIELD.F_4;
                if (fisc.Core().Field(fieldName) != null)
                {
                    int amtMount = StaticAmtRnd.Next(1, 100);
                    fisc.Core().Field(fieldName, $"{amtMount:000000000000}");
                }
                fieldName = FIELD.F_7;
                if (fisc.Core().Field(fieldName) != null)
                {
                    fisc.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                }
                fieldName = FIELD.F_11;
                if (fisc.Core().Field(fieldName) != null)
                {
                    int traceNumber = StaticTraceRnd.Next(1, 9999);
                    fisc.Core().Field(fieldName, $"{queueIndex:D2}{traceNumber:0000}");
                }
                fieldName = FIELD.F_12;
                if (fisc.Core().Field(fieldName) != null)
                {
                    fisc.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
                }
                fieldName = FIELD.F_13;
                if (fisc.Core().Field(fieldName) != null)
                {
                    fisc.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_15;
                if (fisc.Core().Field(fieldName) != null)
                {
                    fisc.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_17;
                if (fisc.Core().Field(fieldName) != null)
                {
                    fisc.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_35;
                if (fisc.Core().Field(fieldName) != null)
                {
                    if (_isReplaceCardNumber)
                        fisc.Core().Field(fieldName, track2Data);
                }
                fieldName = FIELD.F_37;
                if (fisc.Core().Field(fieldName) != null)
                {
                    int refNumber = StaticRefRnd.Next(1, 999999);
                    fisc.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                }
                byte[] multiMessageBytes = new byte[(fisc.Encode().Val().Length + 2) * _sendMessageRepeatCount];
                int offset = 0;
                for (int index = 1; index <= _sendMessageRepeatCount; index++)
                {
                    fieldName = FIELD.F_4;
                    if (fisc.Core().Field(fieldName) != null)
                    {
                        int amtMount = StaticAmtRnd.Next(1, 100);
                        fisc.Core().Field(fieldName, $"{amtMount:000000000000}");
                    }
                    fieldName = FIELD.F_7;
                    if (fisc.Core().Field(fieldName) != null)
                    {
                        fisc.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                    }
                    fieldName = FIELD.F_11;
                    if (fisc.Core().Field(fieldName) != null)
                    {
                        int traceNumber = StaticTraceRnd.Next(1, 9999);
                        fisc.Core().Field(fieldName, $"{queueIndex:D2}{traceNumber:0000}");
                    }
                    fieldName = FIELD.F_37;
                    if (fisc.Core().Field(fieldName) != null)
                    {
                        int refNumber = StaticRefRnd.Next(1, 999999);
                        fisc.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                    }
                    var singleMessageBytes = ColaCommUtility.AddBinaryLength2BytesArray(fisc.Encode().Val());
                    Buffer.BlockCopy(singleMessageBytes, 0, multiMessageBytes, offset, singleMessageBytes.Length);
                    offset += singleMessageBytes.Length;
                }
                return multiMessageBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateFiscRequestMessage Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] ReceiveMessageForFisc(byte[] requestBytes)
        {
            try
            {
                var processBytes = new byte[requestBytes.Length - 2];
                Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                var requestFisc = new FISC(processBytes);
                Logger.Info("Fisc Parsing Receive Message");
                Logger.Info($"MTI [{requestFisc.Mti()}]");
                foreach (var pair in requestFisc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseFisc = new FISC(processBytes);
                switch (requestFisc.Mti())
                {
                    case "0100":
                        responseFisc.Mti("0110");
                        responseFisc.Core().Field(FIELD.F_38, "777777");
                        responseFisc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0120":
                        responseFisc.Mti("0130");
                        responseFisc.Core().Field(FIELD.F_38, "777777");
                        responseFisc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0320":
                        responseFisc.Mti("0330");
                        responseFisc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0420":
                    case "0421":
                        responseFisc.Mti("0430");
                        responseFisc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0800":
                        responseFisc.Mti("0810");
                        responseFisc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0820":
                    case "0821":
                        responseFisc.Mti("0830");
                        responseFisc.Core().Field(FIELD.F_39, "00");
                        break;
                    default:
                        Logger.Info($"Request Message");
                        return null;
                }
                Logger.Info("Fisc Create Response Message");
                foreach (var pair in responseFisc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseBytes = responseFisc.Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMessageForFisc Exception : [{ex.Message}]");
                return null;
            }
        }

        private static List<byte[]> ReceiveMultiMessageForFisc(ClientConnectionContext ctx, byte[] requestBytes)
        {
            try
            {
                var retnListBytes = new List<byte[]>();
                var listReceiveBytes = GetTransactionListBytes(ctx, requestBytes);
                if (listReceiveBytes.Count <= 0)
                {
                    Logger.Warn(
                        $"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], ReceiveMultiMessageForFisc No Process");
                    return null;
                }
                else
                {
                    Logger.Info(
                        $"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], ReceiveMultiMessageForFisc Process List Count[{listReceiveBytes.Count}]");
                }

                foreach (var dataBytes in listReceiveBytes)
                {
                    var processBytes = new byte[dataBytes.Length - 2];
                    Buffer.BlockCopy(dataBytes, 2, processBytes, 0, dataBytes.Length - 2);
                    var requestFisc = new FISC(processBytes);
                    Logger.Info("Fisc Parsing Receive Message");
                    Logger.Info($"MTI [{requestFisc.Mti()}]");
                    foreach (var pair in requestFisc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }

                    var responseFisc = new FISC(processBytes);
                    bool noProcessFlag = false;
                    switch (requestFisc.Mti())
                    {
                        case "0100":
                            responseFisc.Mti("0110");
                            responseFisc.Core().Field(FIELD.F_38, "777777");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0120":
                            responseFisc.Mti("0130");
                            responseFisc.Core().Field(FIELD.F_38, "777777");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0320":
                            responseFisc.Mti("0330");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0420":
                        case "0421":
                            responseFisc.Mti("0430");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0800":
                            responseFisc.Mti("0810");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0820":
                        case "0821":
                            responseFisc.Mti("0830");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                            break;
                        default:
                            Logger.Info($"Request Message");
                            noProcessFlag = true;
                            break;
                    }
                    if (noProcessFlag) break;
                    Logger.Info("Fisc Create Response Message");
                    foreach (var pair in responseFisc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }

                    var responseBytes = responseFisc.Encode().Val();
                    var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                    retnListBytes.Add(retnBytes);
                }

                return retnListBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMultiMessageForFisc Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] ReceiveMessageForFisc(ClientConnectionContext ctx, byte[] requestBytes)
        {
            try
            {
                var processBytes = new byte[requestBytes.Length - 2];
                Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                var requestFisc = new FISC(processBytes);
                //bool isProcessSignOn  = false;
                //bool isProcessSignOff = false;
                bool isError = false;
                Logger.Info("Fisc Parsing Receive Message");
                Logger.Info($"Receive Message{ctx.RemoteEndPoint}_{requestFisc.Val(FIELD.F_11)}");
                Logger.Info($"MTI [{requestFisc.Mti()}]");
                foreach (var pair in requestFisc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }

                bool isItemExists = ChannelsStatus.TryGetValue(ctx, out var currentChannelInfo);
                if (!isItemExists) return null;
                var updateChannelInfo = new ChannelInfo()
                {
                    IsSignOn = currentChannelInfo.IsSignOn,
                    IsWriteTimer = currentChannelInfo.IsWriteTimer,
                    QueueIndex = currentChannelInfo.QueueIndex
                };

                if (!currentChannelInfo.IsSignOn)
                {
                    if (requestFisc.Mti().Equals("0800") |
                        requestFisc.Mti().Equals("0820") |
                        requestFisc.Mti().Equals("0821"))
                    {
                        const FIELD field = FIELD.F_70;
                        if (!ObjectUtils.IsNull(requestFisc.RawVal(field)))
                        {
                            var informationCode = (string)requestFisc.Core().Field(field);
                            if (informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.SignOn))
                                updateChannelInfo.IsSignOn = true;
                            else
                            {
                                Logger.Info($"Information Code Invalid, Information Code [{informationCode}]");
                                isError = true;
                            }
                        }
                        else
                        {
                            Logger.Info($"Information Code No Data");
                            isError = true;
                        }
                    }
                    else
                    {
                        Logger.Info($"Must be First SignOn, MTI [{requestFisc.Mti()}]");
                        isError = true;
                    }
                }
                if (!isError)
                {
                    if (updateChannelInfo.IsSignOn)
                    {

                        if (requestFisc.Mti().Equals("0800") |
                            requestFisc.Mti().Equals("0820") |
                            requestFisc.Mti().Equals("0821"))
                        {
                            const FIELD field = FIELD.F_70;
                            if (!ObjectUtils.IsNull(requestFisc.RawVal(field)))
                            {
                                var informationCode = (string)requestFisc.Core().Field(field);
                                if (informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.SignOn))
                                {
                                    if (!updateChannelInfo.IsWriteTimer)
                                    {
                                        var remoteEndPoint = ctx.RemoteEndPoint.ToString();
                                        var remotePortNumber =
                                            remoteEndPoint.Substring(remoteEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                                        var maintChannel =
                                            _maintRemotePortList.Find(c => c.Equals(remotePortNumber));
                                        if (string.IsNullOrEmpty(maintChannel))
                                        {
                                            int queueIndex = currentChannelInfo.QueueIndex;
                                            uint readQueueCount = 0;
                                            ctx.SendTimer = (
                                                new Timer(
                                                    state =>
                                                    {
                                                        var messageData = ReceiveQueueData(_messageQueue[queueIndex], queueIndex);
                                                        if (messageData == null)
                                                            return;
                                                        Logger.Info($"Read QueueIndex[{queueIndex}], Length [{messageData?.Length}] Count[{++readQueueCount}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                                        Logger.Info($"RemoteEndPoint[{ctx.RemoteEndPoint}],Write Data[{Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(messageData, 2, messageData.Length - 2))}]");
                                                        var traceNumber = GetTraceNumber(messageData);
                                                        Logger.Info($"Send Message{ctx.RemoteEndPoint}_{traceNumber}");
                                                        ctx.Socket.Send(messageData);
                                                    },
                                                    null,
                                                    1000,
                                                    _sendInterval));

                                        }
                                        updateChannelInfo.IsWriteTimer = true;
                                    }
                                }
                                else if (informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.SignOff))
                                {
                                    if (updateChannelInfo.IsWriteTimer)
                                    {
                                        if (ctx.SendTimer != null)
                                        {
                                            ctx.SendTimer.Dispose(); ctx.SendTimer = null;
                                        }

                                        updateChannelInfo.IsWriteTimer = false;
                                        updateChannelInfo.IsSignOn = false;
                                    }
                                    updateChannelInfo.IsWriteTimer = false;
                                    updateChannelInfo.IsSignOn = false;

                                }
                            }
                            else
                            {
                                Logger.Info($"Information Code No Data");
                                isError = true;
                            }
                        }
                    }
                    ChannelsStatus.TryUpdate(ctx, updateChannelInfo, currentChannelInfo);
                }
                var responseFisc = new FISC(processBytes);
                switch (requestFisc.Mti())
                {
                    case "0100":
                        responseFisc.Mti("0110");
                        if (isError)
                            responseFisc.Core().Field(FIELD.F_39, "96");
                        else
                        {
                            responseFisc.Core().Field(FIELD.F_38, "777777");
                            responseFisc.Core().Field(FIELD.F_39, "00");
                        }
                        break;
                    case "0320":
                        responseFisc.Mti("0330");
                        responseFisc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0420":
                    case "0421":
                        responseFisc.Mti("0430");
                        responseFisc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0800":
                        responseFisc.Mti("0810");
                        responseFisc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0820":
                    case "0821":
                        responseFisc.Mti("0830");
                        responseFisc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    default:
                        Logger.Info($"Request Message");
                        return null;
                }
                Logger.Info($"Fisc Create Response Message");
                foreach (var pair in responseFisc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseBytes = responseFisc.Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMessageForFisc Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] CreateFiscEchoTestRequestMessage()
        {
            try
            {
                var updateArray = FISCTransCreator.FiscEchoTestRequest().Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(updateArray);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateFiscEchoTestRequestMessage Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] CreateNcccRequestMessage(byte[] requestMessage, int queueIndex)
        {
            //try
            //{
            //    var nccc = new NcccIso8583(requestMessage);
            //    nccc.Core().Field(FIELD.F_2, "1234123412341234");
            //    var updateArray = nccc.Encode().Val();
            //    byte[] retnBytes = new byte[2 + updateArray.Length];
            //    retnBytes[0] = (byte)(updateArray.Length >> 8);
            //    retnBytes[1] = (byte)(updateArray.Length);
            //    Buffer.BlockCopy(updateArray, 0, retnBytes, 2, updateArray.Length);
            //    return (retnBytes);
            //}
            //catch (Exception)
            //{
            //    return null;
            //}
            try
            {
                if (!_isReplaceOriginalData)
                {
                    return ColaCommUtility.AddBinaryLength2BytesArray(requestMessage);
                }
                var nccc = new NcccIso8583(requestMessage);
                var cardIndex = StaticVisaCardIndex.Next(0, _visaCardTrack2Array.Length - 1);
                var track2Data = _visaCardTrack2Array[cardIndex];
                //Check Type
                var fieldName = FIELD.F_2;
                if (nccc.Core().Field(fieldName) != null)
                {
                    string cardNumber = (string)nccc.Core().Field(fieldName);
                    var cardType = CreditCardUtility.FindCardType(cardNumber);
                    switch (cardType)
                    {
                        case CreditCardType.JCB:
                            if (_jcbCardTrack2Array.Length == 0) break;
                            cardIndex = StaticJcbCardIndex.Next(0, _jcbCardTrack2Array.Length - 1);
                            track2Data = _jcbCardTrack2Array[cardIndex];
                            break;
                        case CreditCardType.MasterCard:
                            if (_masterCardTrack2Array.Length == 0) break;
                            cardIndex = StaticMasterCardIndex.Next(0, _masterCardTrack2Array.Length - 1);
                            track2Data = _masterCardTrack2Array[cardIndex];
                            break;
                        default:
                            if (_visaCardTrack2Array.Length == 0) break;
                            cardIndex = StaticVisaCardIndex.Next(0, _visaCardTrack2Array.Length - 1);
                            track2Data = _visaCardTrack2Array[cardIndex];
                            break;
                    }
                }
                else
                {
                    fieldName = FIELD.F_35;
                    if (nccc.Core().Field(fieldName) != null)
                    {
                        string originalTrack2Data = (string)nccc.Core().Field(fieldName);
                        int startIndex = originalTrack2Data.IndexOfAny(new char[] { '=', 'D', '~', '^' });
                        string cardNumber;
                        if (startIndex > 0)
                            cardNumber = originalTrack2Data.Substring(0, startIndex);
                        else
                            cardNumber = originalTrack2Data;
                        var cardType = CreditCardUtility.FindCardType(cardNumber);
                        switch (cardType)
                        {
                            case CreditCardType.JCB:
                                if (_jcbCardTrack2Array.Length == 0) break;
                                cardIndex = StaticJcbCardIndex.Next(0, _jcbCardTrack2Array.Length - 1);
                                track2Data = _jcbCardTrack2Array[cardIndex];
                                break;
                            case CreditCardType.MasterCard:
                                if (_masterCardTrack2Array.Length == 0) break;
                                cardIndex = StaticMasterCardIndex.Next(0, _masterCardTrack2Array.Length - 1);
                                track2Data = _masterCardTrack2Array[cardIndex];
                                break;
                            default:
                                if (_visaCardTrack2Array.Length == 0) break;
                                cardIndex = StaticVisaCardIndex.Next(0, _visaCardTrack2Array.Length - 1);
                                track2Data = _visaCardTrack2Array[cardIndex];
                                break;
                        }
                    }
                }

                //Change Field Data
                fieldName = FIELD.F_2;
                if (nccc.Core().Field(fieldName) != null)
                {
                    if (_isReplaceCardNumber)
                        nccc.Core().Field(fieldName, track2Data.Substring(0, 16));
                }

                fieldName = FIELD.F_4;
                if (nccc.Core().Field(fieldName) != null)
                {
                    int amtMount = StaticAmtRnd.Next(1, 100);
                    nccc.Core().Field(fieldName, $"{amtMount:000000000000}");
                }
                fieldName = FIELD.F_7;
                if (nccc.Core().Field(fieldName) != null)
                {
                    nccc.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                }
                fieldName = FIELD.F_11;
                if (nccc.Core().Field(fieldName) != null)
                {
                    int traceNumber = StaticTraceRnd.Next(1, 9999);
                    nccc.Core().Field(fieldName, $"{queueIndex:D2}{traceNumber:0000}");
                }
                fieldName = FIELD.F_12;
                if (nccc.Core().Field(fieldName) != null)
                {
                    nccc.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
                }
                fieldName = FIELD.F_13;
                if (nccc.Core().Field(fieldName) != null)
                {
                    nccc.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_15;
                if (nccc.Core().Field(fieldName) != null)
                {
                    nccc.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_17;
                if (nccc.Core().Field(fieldName) != null)
                {
                    nccc.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_35;
                if (nccc.Core().Field(fieldName) != null)
                {
                    if (_isReplaceCardNumber)
                        nccc.Core().Field(fieldName, track2Data);
                }
                fieldName = FIELD.F_37;
                if (nccc.Core().Field(fieldName) != null)
                {
                    int refNumber = StaticRefRnd.Next(1, 999999);
                    nccc.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                }
                byte[] multiMessageBytes = new byte[(nccc.Encode().Val().Length + 2) * _sendMessageRepeatCount];
                int offset = 0;
                for (int index = 1; index <= _sendMessageRepeatCount; index++)
                {
                    fieldName = FIELD.F_4;
                    if (nccc.Core().Field(fieldName) != null)
                    {
                        int amtMount = StaticAmtRnd.Next(1, 100);
                        nccc.Core().Field(fieldName, $"{amtMount:000000000000}");
                    }
                    fieldName = FIELD.F_7;
                    if (nccc.Core().Field(fieldName) != null)
                    {
                        nccc.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                    }
                    fieldName = FIELD.F_11;
                    if (nccc.Core().Field(fieldName) != null)
                    {
                        int traceNumber = StaticTraceRnd.Next(1, 9999);
                        nccc.Core().Field(fieldName, $"{queueIndex:D2}{traceNumber:0000}");
                    }
                    fieldName = FIELD.F_37;
                    if (nccc.Core().Field(fieldName) != null)
                    {
                        int refNumber = StaticRefRnd.Next(1, 999999);
                        nccc.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                    }
                    var singleMessageBytes = ColaCommUtility.AddBinaryLength2BytesArray(nccc.Encode().Val());
                    Buffer.BlockCopy(singleMessageBytes, 0, multiMessageBytes, offset, singleMessageBytes.Length);
                    offset += singleMessageBytes.Length;
                }
                return multiMessageBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateNcccRequestMessage Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] ReceiveMessageForNccc(byte[] requestBytes)
        {
            try
            {
                var processBytes = new byte[requestBytes.Length - 2];
                Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                var requestNccc = new NcccIso8583(processBytes);
                Logger.Info("Nccc Parsing Receive Message");
                Logger.Info($"Header[{requestNccc.Header().Encode()}]");
                Logger.Info($"MTI [{requestNccc.Mti()}]");
                foreach (var pair in requestNccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseNccc = new NcccIso8583(processBytes);
                switch (requestNccc.Mti())
                {
                    case "0200":
                        responseNccc.Mti("0210");
                        responseNccc.Core().Field(FIELD.F_38, "888888");
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0300":
                        responseNccc.Mti("0310");
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0420":
                    case "0421":
                        responseNccc.Mti("0430");
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0800":
                        responseNccc.Mti("0810");
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        break;
                    case "0820":
                    case "0821":
                        responseNccc.Mti("0830");
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        break;
                    default:
                        Logger.Info($"Request Message");
                        return null;
                }
                Logger.Info("Nccc Create Response Message");
                foreach (var pair in responseNccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseBytes = responseNccc.Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMessageForNccc Exception : [{ex.Message}]");
                return null;
            }
        }

        private static List<byte[]> ReceiveMultiMessageForNccc(ClientConnectionContext ctx, byte[] requestBytes)
        {
            try
            {
                var retnListBytes = new List<byte[]>();
                var listReceiveBytes = GetTransactionListBytes(ctx, requestBytes);
                if (listReceiveBytes.Count <= 0)
                {
                    Logger.Warn(
                        $"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], ReceiveMultiMessageForNccc No Process");
                    return null;
                }
                else
                {
                    Logger.Info(
                        $"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], ReceiveMultiMessageForNccc Process List Count[{listReceiveBytes.Count}]");
                }

                foreach (var dataBytes in listReceiveBytes)
                {
                    var processBytes = new byte[dataBytes.Length - 2];
                    Buffer.BlockCopy(dataBytes, 2, processBytes, 0, dataBytes.Length - 2);
                    var requestNccc = new NcccIso8583(processBytes);
                    Logger.Info("Nccc Parsing Receive Message");
                    Logger.Info($"Header[{requestNccc.Header().Encode()}]");
                    Logger.Info($"MTI [{requestNccc.Mti()}]");
                    foreach (var pair in requestNccc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }

                    var responseNccc = new NcccIso8583(processBytes);
                    bool noProcessFlag = false;
                    switch (requestNccc.Mti())
                    {
                        case "0200":
                            responseNccc.Mti("0210");
                            responseNccc.Core().Field(FIELD.F_38, "888888");
                            responseNccc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0300":
                            responseNccc.Mti("0310");
                            responseNccc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0420":
                        case "0421":
                            responseNccc.Mti("0430");
                            responseNccc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0800":
                            responseNccc.Mti("0810");
                            responseNccc.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0820":
                        case "0821":
                            responseNccc.Mti("0830");
                            responseNccc.Core().Field(FIELD.F_39, "00");
                            break;
                        default:
                            Logger.Info($"Request Message, No Process");
                            noProcessFlag = true;
                            break;
                    }
                    if (noProcessFlag) break;
                    Logger.Info("Nccc Create Response Message");
                    foreach (var pair in responseNccc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }
                    var responseBytes = responseNccc.Encode().Val();
                    var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                    retnListBytes.Add(retnBytes);
                }

                return retnListBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMultiMessageForNccc Exception : [{ex.Message}]");
                return null;
            }
        }
        private static List<byte[]> ReceiveMultiMessageForTwin(ClientConnectionContext ctx, byte[] requestBytes)
        {
            try
            {
                var retnListBytes = new List<byte[]>();
                var listReceiveBytes = GetTransactionListBytes(ctx, requestBytes);
                if (listReceiveBytes.Count <= 0)
                {
                    Logger.Warn(
                        $"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], ReceiveMultiMessageForTwin No Process");
                    return null;
                }
                else
                {
                    Logger.Info(
                        $"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], ReceiveMultiMessageForTwin Process List Count[{listReceiveBytes.Count}]");
                }

                foreach (var dataBytes in listReceiveBytes)
                {
                    var processBytes = new byte[dataBytes.Length - 2];
                    Buffer.BlockCopy(dataBytes, 2, processBytes, 0, dataBytes.Length - 2);
                    var requestTwin = new TWINIso8583(processBytes);
                    Logger.Info("Twin Parsing Receive Message");
                    Logger.Info($"Header[{requestTwin.Header().Encode()}]");
                    Logger.Info($"MTI [{requestTwin.Mti()}]");
                    foreach (var pair in requestTwin.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }

                    var responseTwin = new TWINIso8583(processBytes);
                    bool noProcessFlag = false;
                    switch (requestTwin.Mti())
                    {
                        case "0200":
                            responseTwin.Mti("0210");
                            responseTwin.Core().Field(FIELD.F_38, "999999");
                            responseTwin.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0300":
                            responseTwin.Mti("0310");
                            responseTwin.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0420":
                        case "0421":
                            responseTwin.Mti("0430");
                            responseTwin.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0800":
                            responseTwin.Mti("0810");
                            responseTwin.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0820":
                        case "0821":
                            responseTwin.Mti("0830");
                            responseTwin.Core().Field(FIELD.F_39, "00");
                            break;
                        default:
                            Logger.Info($"Request Message, No Process");
                            noProcessFlag = true;
                            break;
                    }
                    if (noProcessFlag) break;
                    Logger.Info("Twin Create Response Message");
                    foreach (var pair in responseTwin.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }
                    var responseBytes = responseTwin.Encode().Val();
                    var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                    retnListBytes.Add(retnBytes);
                }

                return retnListBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMultiMessageForTwin Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] ReceiveMessageForNccc(ClientConnectionContext ctx, byte[] requestBytes)
        {
            try
            {
                var processBytes = new byte[requestBytes.Length - 2];
                Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                var requestNccc = new NcccIso8583(processBytes);
                //bool isProcessSignOn  = false;
                //bool isProcessSignOff = false;
                bool isError = false;
                Logger.Info("Nccc Parsing Receive Message");
                Logger.Info($"Receive Message{ctx.RemoteEndPoint}_{requestNccc.Val(FIELD.F_11)}");
                Logger.Info($"Header[{requestNccc.Header().Encode()}]");
                Logger.Info($"MTI [{requestNccc.Mti()}]");
                foreach (var pair in requestNccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }

                bool isItemExists = ChannelsStatus.TryGetValue(ctx, out var currentChannelInfo);
                if (!isItemExists) return null;
                var updateChannelInfo = new ChannelInfo() { IsSignOn = currentChannelInfo.IsSignOn, IsWriteTimer = currentChannelInfo.IsWriteTimer, QueueIndex = currentChannelInfo.QueueIndex };

                if (!currentChannelInfo.IsSignOn)
                {
                    if (requestNccc.Mti().Equals("0800") |
                        requestNccc.Mti().Equals("0820") |
                        requestNccc.Mti().Equals("0821"))
                    {
                        const FIELD field = FIELD.F_70;
                        if (!ObjectUtils.IsNull(requestNccc.RawVal(field)))
                        {
                            var informationCode = (string)requestNccc.Core().Field(field);
                            if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn))
                                updateChannelInfo.IsSignOn = true;
                            else
                            {
                                Logger.Info($"Information Code Invalid, Information Code [{informationCode}]");
                                isError = true;
                            }
                        }
                        else
                        {
                            Logger.Info($"Information Code No Data");
                            isError = true;
                        }
                    }
                    else
                    {
                        Logger.Info($"Must be First SignOn, MTI [{requestNccc.Mti()}]");
                        isError = true;
                    }
                }
                if (!isError)
                {
                    if (updateChannelInfo.IsSignOn)
                    {

                        if (requestNccc.Mti().Equals("0800") |
                            requestNccc.Mti().Equals("0820") |
                            requestNccc.Mti().Equals("0821"))
                        {
                            const FIELD field = FIELD.F_70;
                            if (!ObjectUtils.IsNull(requestNccc.RawVal(field)))
                            {
                                var informationCode = (string)requestNccc.Core().Field(field);
                                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn))
                                {
                                    if (!updateChannelInfo.IsWriteTimer)
                                    {
                                        var remoteEndPoint = ctx.RemoteEndPoint.ToString();
                                        var remotePortNumber =
                                            remoteEndPoint.Substring(remoteEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                                        var maintChannel =
                                            _maintRemotePortList.Find(c => c.Equals(remotePortNumber));
                                        if (string.IsNullOrEmpty(maintChannel))
                                        {
                                            int queueIndex = currentChannelInfo.QueueIndex;
                                            uint readQueueCount = 0;
                                            ctx.SendTimer = (
                                                new Timer(
                                                    state =>
                                                    {
                                                        var messageData = ReceiveQueueData(_messageQueue[queueIndex], queueIndex);
                                                        if (messageData == null)
                                                            return;
                                                        Logger.Info($"Read QueueIndex[{queueIndex}], Length [{messageData?.Length}] Count[{++readQueueCount}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                                        Logger.Info($"RemoteEndPoint[{ctx.RemoteEndPoint}],Write Data[{Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(messageData, 2, messageData.Length - 2))}]");
                                                        var traceNumber = GetTraceNumber(messageData);
                                                        Logger.Info($"Send Message{ctx.RemoteEndPoint}_{traceNumber}");
                                                        ctx.Socket.Send(messageData);
                                                    },
                                                    null,
                                                    1000,
                                                    _sendInterval));

                                        }
                                        updateChannelInfo.IsWriteTimer = true;
                                    }
                                }
                                else if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOff))
                                {
                                    if (updateChannelInfo.IsWriteTimer)
                                    {
                                        if (ctx.SendTimer != null)
                                        {
                                            ctx.SendTimer.Dispose(); ctx.SendTimer = null;
                                        }

                                        updateChannelInfo.IsWriteTimer = false;
                                        updateChannelInfo.IsSignOn = false;
                                    }
                                    updateChannelInfo.IsWriteTimer = false;
                                    updateChannelInfo.IsSignOn = false;

                                }
                            }
                            else
                            {
                                Logger.Info($"Information Code No Data");
                                isError = true;
                            }
                        }
                    }
                    ChannelsStatus.TryUpdate(ctx, updateChannelInfo, currentChannelInfo);
                }
                var responseNccc = new NcccIso8583(processBytes);
                switch (requestNccc.Mti())
                {
                    case "0200":
                        responseNccc.Mti("0210");
                        if (isError)
                            responseNccc.Core().Field(FIELD.F_39, "96");
                        else
                        {
                            responseNccc.Core().Field(FIELD.F_38, "888888");
                            responseNccc.Core().Field(FIELD.F_39, "00");
                        }
                        break;
                    case "0300":
                        responseNccc.Mti("0310");
                        responseNccc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0420":
                    case "0421":
                        responseNccc.Mti("0430");
                        responseNccc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0800":
                        responseNccc.Mti("0810");
                        responseNccc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0820":
                    case "0821":
                        responseNccc.Mti("0830");
                        responseNccc.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    default:
                        Logger.Info($"Request Message");
                        return null;
                }
                Logger.Info($"Nccc Create Response Message");
                foreach (var pair in responseNccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseBytes = responseNccc.Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMessageForNccc Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] ReceiveMessageForTwin(ClientConnectionContext ctx, byte[] requestBytes)
        {
            try
            {
                var processBytes = new byte[requestBytes.Length - 2];
                Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                var requestTwin = new TWINIso8583(processBytes);
                //bool isProcessSignOn  = false;
                //bool isProcessSignOff = false;
                bool isError = false;
                Logger.Info("Twin Parsing Receive Message");
                Logger.Info($"Receive Message{ctx.RemoteEndPoint}_{requestTwin.Val(FIELD.F_11)}");
                Logger.Info($"Header[{requestTwin.Header().Encode()}]");
                Logger.Info($"MTI [{requestTwin.Mti()}]");
                foreach (var pair in requestTwin.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }

                bool isItemExists = ChannelsStatus.TryGetValue(ctx, out var currentChannelInfo);
                if (!isItemExists) return null;
                var updateChannelInfo = new ChannelInfo() { IsSignOn = currentChannelInfo.IsSignOn, IsWriteTimer = currentChannelInfo.IsWriteTimer, QueueIndex = currentChannelInfo.QueueIndex };

                if (!currentChannelInfo.IsSignOn)
                {
                    if (requestTwin.Mti().Equals("0800") |
                        requestTwin.Mti().Equals("0820") |
                        requestTwin.Mti().Equals("0821"))
                    {
                        const FIELD field = FIELD.F_70;
                        if (!ObjectUtils.IsNull(requestTwin.RawVal(field)))
                        {
                            var informationCode = (string)requestTwin.Core().Field(field);
                            if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOn))
                                updateChannelInfo.IsSignOn = true;
                            else
                            {
                                Logger.Info($"Information Code Invalid, Information Code [{informationCode}]");
                                isError = true;
                            }
                        }
                        else
                        {
                            Logger.Info($"Information Code No Data");
                            isError = true;
                        }
                    }
                    else
                    {
                        Logger.Info($"Must be First SignOn, MTI [{requestTwin.Mti()}]");
                        isError = true;
                    }
                }
                if (!isError)
                {
                    if (updateChannelInfo.IsSignOn)
                    {

                        if (requestTwin.Mti().Equals("0800") |
                            requestTwin.Mti().Equals("0820") |
                            requestTwin.Mti().Equals("0821"))
                        {
                            const FIELD field = FIELD.F_70;
                            if (!ObjectUtils.IsNull(requestTwin.RawVal(field)))
                            {
                                var informationCode = (string)requestTwin.Core().Field(field);
                                if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOn))
                                {
                                    if (!updateChannelInfo.IsWriteTimer)
                                    {
                                        var remoteEndPoint = ctx.RemoteEndPoint.ToString();
                                        var remotePortNumber =
                                            remoteEndPoint.Substring(remoteEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                                        var maintChannel =
                                            _maintRemotePortList.Find(c => c.Equals(remotePortNumber));
                                        if (string.IsNullOrEmpty(maintChannel))
                                        {
                                            int queueIndex = currentChannelInfo.QueueIndex;
                                            uint readQueueCount = 0;
                                            ctx.SendTimer = (
                                                new Timer(
                                                    state =>
                                                    {
                                                        var messageData = ReceiveQueueData(_messageQueue[queueIndex], queueIndex);
                                                        if (messageData == null)
                                                            return;
                                                        Logger.Info($"Read QueueIndex[{queueIndex}], Length [{messageData?.Length}] Count[{++readQueueCount}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                                        Logger.Info($"RemoteEndPoint[{ctx.RemoteEndPoint}],Write Data[{Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(messageData, 2, messageData.Length - 2))}]");
                                                        var traceNumber = GetTraceNumber(messageData);
                                                        Logger.Info($"Send Message{ctx.RemoteEndPoint}_{traceNumber}");
                                                        ctx.Socket.Send(messageData);
                                                    },
                                                    null,
                                                    1000,
                                                    _sendInterval));

                                        }
                                        updateChannelInfo.IsWriteTimer = true;
                                    }
                                }
                                else if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOff))
                                {
                                    if (updateChannelInfo.IsWriteTimer)
                                    {
                                        if (ctx.SendTimer != null)
                                        {
                                            ctx.SendTimer.Dispose(); ctx.SendTimer = null;
                                        }

                                        updateChannelInfo.IsWriteTimer = false;
                                        updateChannelInfo.IsSignOn = false;
                                    }
                                    updateChannelInfo.IsWriteTimer = false;
                                    updateChannelInfo.IsSignOn = false;

                                }
                            }
                            else
                            {
                                Logger.Info($"Information Code No Data");
                                isError = true;
                            }
                        }
                    }
                    ChannelsStatus.TryUpdate(ctx, updateChannelInfo, currentChannelInfo);
                }
                var responseTwin = new TWINIso8583(processBytes);
                switch (requestTwin.Mti())
                {
                    case "0200":
                        responseTwin.Mti("0210");
                        if (isError)
                            responseTwin.Core().Field(FIELD.F_39, "96");
                        else
                        {
                            responseTwin.Core().Field(FIELD.F_38, "999999");
                            responseTwin.Core().Field(FIELD.F_39, "00");
                        }
                        break;
                    case "0300":
                        responseTwin.Mti("0310");
                        responseTwin.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0420":
                    case "0421":
                        responseTwin.Mti("0430");
                        responseTwin.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0800":
                        responseTwin.Mti("0810");
                        responseTwin.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    case "0820":
                    case "0821":
                        responseTwin.Mti("0830");
                        responseTwin.Core().Field(FIELD.F_39, !isError ? "00" : "96");
                        break;
                    default:
                        Logger.Info($"Request Message");
                        return null;
                }
                Logger.Info($"Twin Create Response Message");
                foreach (var pair in responseTwin.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                    }
                    else
                    {
                        Logger.Info($"[FIELD {key}] VALUE [{value}]");
                    }
                }
                var responseBytes = responseTwin.Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ReceiveMessageForTwin Exception : [{ex.Message}]");
                return null;
            }
        }
        private static byte[] CreateNcccEchoTestRequestMessage()
        {
            try
            {
                var updateArray = NcccTransCreator.EchoTestRequest().Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(updateArray);
                return retnBytes;
                //test add 0x03;
                //var updateArray = NcccTransCreator.EchoTestRequest().Encode().Val();
                //byte[] addETXUpdateArray = new byte[updateArray.Length + 1];
                //Buffer.BlockCopy(updateArray, 0, addETXUpdateArray, 0, updateArray.Length);
                //addETXUpdateArray[addETXUpdateArray.Length - 1] = 0x03;
                //var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(addETXUpdateArray);
                //return (retnBytes);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateNcccEchoTestRequestMessage Exception : [{ex.Message}]");
                return null;
            }
        }

        private static byte[] CreateTwinEchoTestRequestMessage()
        {
            try
            {
                var updateArray = TwinTransCreator.TwinEchoTestRequest().Encode().Val();
                var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(updateArray);
                return retnBytes;
                //test add 0x03;
                //var updateArray = NcccTransCreator.EchoTestRequest().Encode().Val();
                //byte[] addETXUpdateArray = new byte[updateArray.Length + 1];
                //Buffer.BlockCopy(updateArray, 0, addETXUpdateArray, 0, updateArray.Length);
                //addETXUpdateArray[addETXUpdateArray.Length - 1] = 0x03;
                //var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(addETXUpdateArray);
                //return (retnBytes);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateTwinEchoTestRequestMessage Exception : [{ex.Message}]");
                return null;
            }
        }

        private static string GetTraceNumber(byte[] sendBytes)
        {
            try
            {
                byte[] processBytes;
                if (_tranType.Equals("FISC"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new FISC(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("IAG"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new MCOLA(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("STIN"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new MCOLA(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("MASTERCARD"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new MasterCard(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("NCCC"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new NcccIso8583(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("NCPS"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new NcccIso8583(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("TWMP"))
                {
                    processBytes = new byte[sendBytes.Length - 2];
                    Buffer.BlockCopy(sendBytes, 2, processBytes, 0, sendBytes.Length - 2);
                    var sendMessage = new NcccIso8583(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }

                if (_tranType.Equals("VISA"))
                {
                    processBytes = new byte[sendBytes.Length - 4];
                    Buffer.BlockCopy(sendBytes, 4, processBytes, 0, sendBytes.Length - 4);
                    var sendMessage = new VISA(processBytes);
                    var fieldName = FIELD.F_11;
                    if (sendMessage.Core().Field(fieldName) != null)
                    {
                        return sendMessage.Val(FIELD.F_11);
                    }
                    return null;
                }
                else
                    return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Info($"getTraceNumber Exception:{ex.Message}");
                return string.Empty;
            }
        }

        private static List<byte[]> GetTransactionListBytes(ClientConnectionContext ctx, byte[] receiveBytes)
        {
            try
            {
                var listReceiveBytes = new List<byte[]>();
                if (receiveBytes != null)
                {
                    Logger.Info(
                        $"GetTransactionListBytes Process Original Byte Length[{receiveBytes.Length}]B\n{PrintBytesMap(receiveBytes)}");
                    var startIndexPoint = 0;
                    while ((startIndexPoint + 2) <= receiveBytes.Length)
                    {
                        int processDataLength = Convert.ToInt16(receiveBytes[startIndexPoint]) * 256 +
                                                Convert.ToInt16(receiveBytes[startIndexPoint + 1]);
                        Logger.Info($"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], Binary data length[{processDataLength}]");
                        if (receiveBytes.Length < (startIndexPoint + 2 + processDataLength))
                        {
                            Logger.Warn($"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}],ReceiveBytes data length is not enough, StartIndexPoint[{startIndexPoint}], ProcessDataLength[{processDataLength}], ReceiveBytesLength[{receiveBytes.Length}]");
                            break;
                        }

                        var processBytes = new byte[2 + processDataLength];
                        Buffer.BlockCopy(receiveBytes, startIndexPoint, processBytes, 0, 2 + processDataLength);
                        listReceiveBytes.Add(processBytes);
                        Logger.Info($"ListReceiveBytes CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}] , StartIndexPoint[{startIndexPoint}], ProcessDataLength[{processDataLength}]\nLength: {processBytes.Length}B\n{PrintBytesMap(processBytes)}");
                        startIndexPoint += (2 + processDataLength);
                    }

                    Logger.Info($"CHANNELREAD [{ctx.RemoteEndPoint} => {ctx.LocalEndPoint}], GetTransactionListBytes Process Transaction Count[{listReceiveBytes.Count}]");
                }

                return listReceiveBytes;

            }
            catch (Exception ex)
            {
                Logger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process GetTransactionListBytes Exception : [{ex.Message}]");
                return null;
            }
        }

        private static string PrintBytesMap(byte[] bytes)
        {
            var dataOut = new StringBuilder();
            dataOut.Append(
                "         +-------------------------------------------------+\n         |  0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f |\n+--------+-------------------------------------------------+----------------+");
            var length = bytes.Length;
            var index = 0;
            var byteContent = string.Empty;
            var dataContent = string.Empty;
            foreach (var b in bytes)
            {
                if (index % 16 == 0)
                {
                    dataOut.Append($"\n|{(index / 16).ToString().PadLeft(8, '0')}| ");
                    byteContent = string.Empty;
                    dataContent = string.Empty;
                }

                byteContent += $"{b:X2} ";
                dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);

                if (index % 16 == 15 | index + 1 == length)
                {
                    dataOut.AppendFormat("{0}|{1}|", byteContent.PadRight(48, ' '), dataContent.PadRight(16, ' '));
                }

                index++;
            }

            dataOut.Append("\n+--------+-------------------------------------------------+----------------+");
            return dataOut.ToString();
        }
    }
    class ChannelInfo
    {
        public bool IsSignOn { get; set; }
        public bool IsWriteTimer { get; set; }
        public int QueueIndex { get; set; }
    }
}

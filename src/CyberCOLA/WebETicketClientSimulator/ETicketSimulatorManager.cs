using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.CyberUtils.Utils;

namespace Com.Cybersoft4u.Simulator.Web.WebETicketClientSimulator
{
    // --- E-Ticket Transaction Data Model (WPF Parity) ---
    public class ETicketDataInfo
    {
        public string MessageType { get; set; } = string.Empty;
        public string ProcessCode { get; set; } = string.Empty;
        public string SystemTraceNumber { get; set; } = string.Empty;
    }

    // --- Web Log Broadcaster (Dual-panel redirection) ---
    public static class ETicketLogBroadcaster
    {
        private static readonly ConcurrentQueue<string> SystemLogQueue = new();
        private static readonly ConcurrentQueue<string> DetailLogQueue = new();
        private static readonly object LockObj = new();

        public static event Action<string>? OnSystemLogAdded;
        public static event Action<string>? OnDetailLogAdded;

        public static void LogSystem(string message)
        {
            var formatted = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            SystemLogQueue.Enqueue(formatted);
            OnSystemLogAdded?.Invoke(formatted);
            while (SystemLogQueue.Count > 400)
            {
                SystemLogQueue.TryDequeue(out _);
            }
        }

        public static void LogDetail(string message)
        {
            DetailLogQueue.Enqueue(message);
            OnDetailLogAdded?.Invoke(message);
            while (DetailLogQueue.Count > 600)
            {
                DetailLogQueue.TryDequeue(out _);
            }
        }

        public static IEnumerable<string> GetSystemHistory() => SystemLogQueue.ToArray();
        public static IEnumerable<string> GetDetailHistory() => DetailLogQueue.ToArray();

        public static void Clear()
        {
            SystemLogQueue.Clear();
            DetailLogQueue.Clear();
        }
    }

    // --- Simulator Service Manager ---
    public class ETicketSimulatorManager
    {
        private dynamic? _clientETicket;
        private UdpClient? _udpClient;
        private bool _isStopUDPClient = false;
        private bool _isSending = false;
        private CancellationTokenSource? _sendCts;
        private int _sentCount = 0;

        private readonly List<byte[]> _templateRawDataList = new();
        private readonly IConfiguration _config;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Random StaticTraceRnd = new();
        private static readonly Random StaticAmtRnd = new();
        private static readonly Random StaticRefRnd = new();

        // WPF Parity properties
        public List<ETicketDataInfo> LoadedETicketMessages { get; } = new();
        public bool IsRunning => _clientETicket != null;
        public bool IsSending => _isSending;
        public string ETicketType { get; private set; }
        public int Port { get; private set; }
        public int StartLocalPort { get; private set; }
        public int SessionCount { get; private set; }
        public int TimeInterval { get; private set; }
        public int RepeatCount { get; private set; }
        public int LoadedFilesCount => _templateRawDataList.Count;
        public int SentCount => _sentCount;

        public ETicketSimulatorManager(IConfiguration config)
        {
            _config = config;
            ETicketType = "SVCS";
            Port = 4500;
            StartLocalPort = 3100;
            SessionCount = 1;
            TimeInterval = 1000;
            RepeatCount = 1;
        }

        public string[] GetTemplateFiles(string txType)
        {
            var dir = MapETicketTypeToDirectory(txType);
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir).Select(Path.GetFileName).ToArray()!;
        }

        public void LoadTemplates(string eTicketType, List<string>? selectedFiles, int repeatCount)
        {
            ETicketType = eTicketType;
            RepeatCount = repeatCount;
            _templateRawDataList.Clear();
            LoadedETicketMessages.Clear();

            var templatesDir = MapETicketTypeToDirectory(eTicketType);
            if (!Directory.Exists(templatesDir))
            {
                throw new DirectoryNotFoundException($"Template directory not found: {templatesDir}");
            }

            var files = selectedFiles != null && selectedFiles.Count > 0
                ? selectedFiles.Select(f => Path.Combine(templatesDir, f)).Where(File.Exists).ToList()
                : Directory.GetFiles(templatesDir).ToList();

            if (files.Count == 0)
            {
                throw new InvalidOperationException($"No template files found in: {templatesDir}");
            }

            var rnd = new Random();
            for (int i = 0; i < repeatCount; i++)
            {
                var shuffledIndices = Enumerable.Range(0, files.Count).OrderBy(_ => rnd.Next()).ToList();
                foreach (var index in shuffledIndices)
                {
                    byte[] dataBytes = File.ReadAllBytes(files[index]);
                    _templateRawDataList.Add(dataBytes);

                    // Parse template content for grid visualization (WPF Parity)
                    dynamic? requestMessage = null;
                    switch (eTicketType.Trim().ToUpper())
                    {
                        case "ICASH": requestMessage = CreateICASHRequestMessage(dataBytes); break;
                        case "IPASS": requestMessage = CreateIPASSRequestMessage(dataBytes); break;
                        default: requestMessage = CreateSVCSRequestMessage(dataBytes); break;
                    }

                    if (requestMessage != null)
                    {
                        var dataInfo = new ETicketDataInfo { MessageType = requestMessage.Mti() };
                        var fieldName = FIELD.F_3;
                        if (requestMessage.Core().Field(fieldName) != null)
                        {
                            dataInfo.ProcessCode = requestMessage.Core().Field(fieldName).ToString();
                        }
                        fieldName = FIELD.F_11;
                        if (requestMessage.Core().Field(fieldName) != null)
                        {
                            dataInfo.SystemTraceNumber = requestMessage.Core().Field(fieldName).ToString();
                        }
                        LoadedETicketMessages.Add(dataInfo);
                    }
                }
            }

            Log.Info($"Loaded {LoadedFilesCount} templates ({files.Count} files x {repeatCount} repeat count) for {eTicketType}.");
        }

        public void StartClient(string eTicketType, int port, int startLocalPort, int sessionCount)
        {
            if (IsRunning) return;

            ETicketType = eTicketType;
            Port = port;
            StartLocalPort = startLocalPort;
            SessionCount = sessionCount;

            UpdateAppSettingsJson(eTicketType, port, startLocalPort, sessionCount);

            switch (eTicketType.Trim().ToUpper())
            {
                case "ICASH":
                    _clientETicket = new ClientICASH();
                    Task.Run(() => ((ClientICASH)_clientETicket).Start());
                    break;
                case "IPASS":
                    _clientETicket = new ClientIPASS();
                    Task.Run(() => ((ClientIPASS)_clientETicket).Start());
                    break;
                case "SVCS":
                    _clientETicket = new ClientSVCS();
                    Task.Run(() => ((ClientSVCS)_clientETicket).Start());
                    break;
                default:
                    throw new ArgumentException($"Invalid E-Ticket type: {eTicketType}");
            }

            Log.Info($"ETicket Client Simulator started successfully. Type: {eTicketType}, Target Port: {port}, Local Port: {startLocalPort}, Sessions: {sessionCount}");
            RestartUDPForTicket(eTicketType);
        }

        public void StopClient()
        {
            if (!IsRunning) return;
            StopSending();

            switch (ETicketType.Trim().ToUpper())
            {
                case "ICASH":
                    ((ClientICASH)_clientETicket)?.Stop();
                    break;
                case "IPASS":
                    ((ClientIPASS)_clientETicket)?.Stop();
                    break;
                case "SVCS":
                    ((ClientSVCS)_clientETicket)?.Stop();
                    break;
            }

            _clientETicket = null;
            Log.Info("ETicket Client Simulator connection stopped.");
        }

        public void SendPayloads(int timeInterval)
        {
            if (_isSending) return;
            if (_templateRawDataList.Count == 0) throw new InvalidOperationException("No payloads loaded. Please LOAD FILES first.");
            if (!IsRunning) throw new InvalidOperationException("Client connection is not active. Please START CLIENT first.");

            bool hasChannels = false;
            switch (ETicketType.Trim().ToUpper())
            {
                case "ICASH": hasChannels = ((ClientICASH)_clientETicket).IsHaveChannels(); break;
                case "IPASS": hasChannels = ((ClientIPASS)_clientETicket).IsHaveChannels(); break;
                case "SVCS": hasChannels = ((ClientSVCS)_clientETicket).IsHaveChannels(); break;
            }

            if (!hasChannels)
            {
                throw new InvalidOperationException("No active channels established. Please wait for connections to stabilize.");
            }

            TimeInterval = timeInterval;
            _isSending = true;
            _sendCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    foreach (var dataBytes in _templateRawDataList)
                    {
                        if (_sendCts.Token.IsCancellationRequested) break;

                        dynamic requestMessage;
                        switch (ETicketType.Trim().ToUpper())
                        {
                            case "ICASH":
                                requestMessage = CreateICASHRequestMessage(dataBytes);
                                if (requestMessage != null && _clientETicket != null)
                                {
                                    LogRequestDetails(requestMessage);
                                    await Task.Run(() => ((ClientICASH)_clientETicket).AllChannelSendMessageToGateway((ICASH)requestMessage));
                                }
                                break;
                            case "IPASS":
                                requestMessage = CreateIPASSRequestMessage(dataBytes);
                                if (requestMessage != null && _clientETicket != null)
                                {
                                    LogRequestDetails(requestMessage);
                                    await Task.Run(() => ((ClientIPASS)_clientETicket).AllChannelSendMessageToGateway((IPASS)requestMessage));
                                }
                                break;
                            case "SVCS":
                                requestMessage = CreateSVCSRequestMessage(dataBytes);
                                if (requestMessage != null && _clientETicket != null)
                                {
                                    LogRequestDetails(requestMessage);
                                    await Task.Run(() => ((ClientSVCS)_clientETicket).AllChannelSendMessageToGateway((SVCS)requestMessage));
                                }
                                break;
                        }

                        Interlocked.Increment(ref _sentCount);
                        await Task.Delay(timeInterval, _sendCts.Token);
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Payload transmission error: {ex.Message}");
                }
                finally
                {
                    _isSending = false;
                }
            }, _sendCts.Token);

            Log.Info($"Transmission thread started. Interval: {timeInterval} ms.");
        }

        public void StopSending()
        {
            if (!_isSending) return;
            _sendCts?.Cancel();
            _isSending = false;
            Log.Info("Transmission thread stopped.");
        }

        public void ResetMetrics()
        {
            Interlocked.Exchange(ref _sentCount, 0);
            Log.Info("Sent transaction counter reset.");
        }

        public void StartUDPFromConfig()
        {
            var autoStart = Convert.ToBoolean(_config.GetValue<string>("GeneralConfig:UDPAutoStartFlag") ?? "true");
            if (!autoStart) return;

            var defaultUdpPort = _config.GetValue<int>("GeneralConfig:SVCSUDPListenerPort");
            if (defaultUdpPort == 0) defaultUdpPort = 9919;
            StartUDP(defaultUdpPort);
        }

        private void RestartUDPForTicket(string eTicketType)
        {
            StopUDP();

            var key = eTicketType.Trim().ToUpper() switch
            {
                "ICASH" => "ICASHUDPListenerPort",
                "IPASS" => "IPASSUDPListenerPort",
                _ => "SVCSUDPListenerPort"
            };

            var udpPort = _config.GetValue<int>($"GeneralConfig:{key}");
            if (udpPort == 0) udpPort = 9919;

            StartUDP(udpPort);
        }

        public void StartUDP(int udpPort)
        {
            _isStopUDPClient = false;
            Task.Run(async () =>
            {
                try
                {
                    _udpClient = new UdpClient(udpPort);
                    IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, udpPort);
                    Log.Info($"UDP Log Server started on port {udpPort}.");
                    while (!_isStopUDPClient)
                    {
                        if (_udpClient.Available <= 0)
                        {
                            await Task.Delay(100);
                            continue;
                        }
                        byte[] readBytes = _udpClient.Receive(ref groupEP);
                        if (readBytes == null) continue;
                        var logStr = Encoding.Default.GetString(readBytes, 0, readBytes.Length);
                        ETicketLogBroadcaster.LogSystem($"[UDP] {logStr}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"UDP Server error: {ex.Message}");
                }
            });
        }

        public void StopUDP()
        {
            _isStopUDPClient = true;
            _udpClient?.Close();
            _udpClient = null;
            Log.Info("UDP Log Server stopped.");
        }

        private void UpdateAppSettingsJson(string eTicketType, int port, int startLocalPort, int sessionCount)
        {
            try
            {
                var listenPortKey = "svcs-cola-service-target-port";
                var startLocalPortKey = "svcs-cola-service-local-port-start";
                var sessionsKey = "svcs-cola-service-sessions";
                switch (eTicketType.Trim().ToUpper())
                {
                    case "ICASH":
                        listenPortKey = "icash-cola-service-target-port";
                        startLocalPortKey = "icash-cola-service-local-port-start";
                        sessionsKey = "icash-cola-service-sessions";
                        break;
                    case "IPASS":
                        listenPortKey = "ipass-cola-service-target-port";
                        startLocalPortKey = "ipass-cola-service-local-port-start";
                        sessionsKey = "ipass-cola-service-sessions";
                        break;
                }

                var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(jsonFilePath))
                {
                    string json = File.ReadAllText(jsonFilePath);
                    dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json)!;
                    jsonObj.ColaServiceConfig[listenPortKey] = port.ToString();
                    jsonObj.ColaServiceConfig[startLocalPortKey] = startLocalPort.ToString();
                    jsonObj.ColaServiceConfig[sessionsKey] = sessionCount.ToString();
                    string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(jsonFilePath, output);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to update appsettings.json: {ex.Message}");
            }
        }

        private string MapETicketTypeToDirectory(string txType)
        {
            // Found from the program's location rather than a fixed E:\ path, so a clone elsewhere still works.
            var baseDir = Com.Cybersoft4u.CommonLib.Resource.CyberCOLAPaths.ResolveDirectory(
                "Data", "src/CyberCOLA/WpfETicketClientSimulator/Data", "publish/Config/WpfETicketClientSimulator/Data");
            return txType.Trim().ToUpper() switch
            {
                "ICASH" => Path.Combine(baseDir, "ICASH"),
                "IPASS" => Path.Combine(baseDir, "IPASS"),
                _ => Path.Combine(baseDir, "SVCS")
            };
        }

        private void LogRequestDetails(dynamic req)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"==================== E-TICKET TRANSMISSION FIELD DETAILS ====================");
            sb.AppendLine($"Header: [{req.Header().Encode()}] | MTI: [{req.Mti()}]");
            foreach (var pair in req.Core().OrderedFields())
            {
                var key = pair.Key;
                var value = pair.Value;
                if (value is byte[] bytes)
                {
                    sb.AppendLine($"[FIELD {key}] VALUE [{HexStringConverter.FromByteArray(bytes)}]");
                }
                else
                {
                    sb.AppendLine($"[FIELD {key}] VALUE [{value}]");
                }
            }
            sb.AppendLine($"=============================================================================");
            ETicketLogBroadcaster.LogDetail(sb.ToString());
        }

        private ICASH CreateICASHRequestMessage(byte[] originalMessage)
        {
            var requestMessage = new ICASH(originalMessage);
            var fieldName = FIELD.F_4;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int amtMount = StaticAmtRnd.Next(1, 100);
                requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
            }
            fieldName = FIELD.F_7;
            requestMessage.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
            fieldName = FIELD.F_11;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int traceNumber = StaticTraceRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{traceNumber:000000}");
            }
            fieldName = FIELD.F_12;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
            }
            fieldName = FIELD.F_13;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_15;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_17;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_37;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int refNumber = StaticRefRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
            }
            return requestMessage;
        }

        private IPASS CreateIPASSRequestMessage(byte[] originalMessage)
        {
            var requestMessage = new IPASS(originalMessage);
            var fieldName = FIELD.F_4;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int amtMount = StaticAmtRnd.Next(1, 100);
                requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
            }
            fieldName = FIELD.F_7;
            requestMessage.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
            fieldName = FIELD.F_11;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int traceNumber = StaticTraceRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{traceNumber:000000}");
            }
            fieldName = FIELD.F_12;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
            }
            fieldName = FIELD.F_13;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_15;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_17;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_37;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int refNumber = StaticRefRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
            }
            return requestMessage;
        }

        private SVCS CreateSVCSRequestMessage(byte[] originalMessage)
        {
            var requestMessage = new SVCS(originalMessage);
            var fieldName = FIELD.F_4;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int amtMount = StaticAmtRnd.Next(1, 100);
                requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
            }
            fieldName = FIELD.F_7;
            requestMessage.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
            fieldName = FIELD.F_11;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int traceNumber = StaticTraceRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{traceNumber:000000}");
            }
            fieldName = FIELD.F_12;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
            }
            fieldName = FIELD.F_13;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_15;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_17;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_37;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int refNumber = StaticRefRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
            }
            return requestMessage;
        }
    }
}

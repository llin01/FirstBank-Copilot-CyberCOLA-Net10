using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using Experimental.System.Messaging;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NLog.Targets;
using Com.Cybersoft4u.CyberUtils.Utils;

namespace Com.Cybersoft4u.Simulator.Web.WebStressSimulator
{
    // ─── Data Transfer Object ────────────────────────────────────────────
    public class QueueInfo
    {
        public string? QueueName { get; set; }
        public int QueueCount { get; set; }
    }

    // ─── NLog → Blazor Bridge ────────────────────────────────────────────
    public static class StressLogBroadcaster
    {
        private static readonly ConcurrentQueue<string> LogQueue = new();
        public static event Action<string>? OnLogAdded;

        /// <summary>Redirects NLog output to the Blazor UI log panel.</summary>
        public static void SetupNLogRedirection()
        {
            var config = LogManager.Configuration ?? new LoggingConfiguration();
            if (config.FindTargetByName("webConsole") != null)
                config.RemoveTarget("webConsole");

            var customTarget = new MethodCallTarget("webConsole", (logEvent, _) =>
            {
                Log($"[SYSTEM] [{logEvent.Level.Name.ToUpper()}] {logEvent.FormattedMessage}");
            });
            config.AddTarget("webConsole", customTarget);
            config.AddRuleForAllLevels(customTarget);
            LogManager.Configuration = config;
        }

        public static void Log(string message)
        {
            LogQueue.Enqueue(message);
            OnLogAdded?.Invoke(message);
            while (LogQueue.Count > 300)
                LogQueue.TryDequeue(out _);
        }

        public static IEnumerable<string> GetHistory() => LogQueue.ToArray();
    }

    // ─── Stress Simulator Manager ────────────────────────────────────────
    public class StressSimulatorManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ServerService? _serverService;
        private UdpClient? _udpClient;
        private Thread? _udpThread;
        private bool _isStopUDPServer = false;

        private readonly List<byte[]> _templateRawDataList = new();
        private List<string> _loadedFilesPaths = new();
        private readonly IConfiguration _config;

        // Status & Telemetry
        public bool IsRunning => _serverService != null;
        public string TransactionType { get; set; } = "FISC";
        public int ListenPort { get; private set; } = 8100;
        public int ActiveConnections => ServerService.Metrics.ActiveConnections;
        public long SuccessCount => ServerService.Metrics.SuccessCount;
        public long ErrorCount => ServerService.Metrics.ErrorCount;

        private long _lastSuccessCount = 0;
        private DateTime _lastTpsTime = DateTime.Now;
        public int Tps
        {
            get
            {
                if (!IsRunning) return 0;
                var now = DateTime.Now;
                var elapsed = (now - _lastTpsTime).TotalSeconds;
                if (elapsed <= 0) return 0;
                long success = SuccessCount;
                double tps = (success - _lastSuccessCount) / elapsed;
                _lastSuccessCount = success;
                _lastTpsTime = now;
                return (int)Math.Round(tps);
            }
        }

        public bool IsWaitingSignOn { get; private set; } = false;
        public bool IsAutoEcho { get; private set; } = true;
        public int SendInterval { get; private set; } = 10000;
        public int LoadedFilesCount => _loadedFilesPaths.Count;
        public int UdpPort { get; private set; } = 9008;

        private string? _initialDirectory;
        private string? _serverName;
        private string? _prefixQueueName;
        private int _sessionCount;
        private int _sendMessageRepeatCount;
        private string[]? _queueNameArray;
        private MessageQueue[]? _messageQueueArrays;
        private List<string>? _maintRemotePortList;

        public StressSimulatorManager(IConfiguration config)
        {
            _config = config;
            LoadParameters();
        }

        private void LoadParameters()
        {
            _initialDirectory = _config["GeneralConfig:InitialDirectory"];
            _serverName = _config["GeneralConfig:ServerName"];
            _prefixQueueName = _config["GeneralConfig:PrefixQueueName"];

            int.TryParse(_config["GeneralConfig:SessionCount"], out _sessionCount);
            if (_sessionCount == 0) _sessionCount = 10;

            int.TryParse(_config["GeneralConfig:SendMessageRepeatCount"], out _sendMessageRepeatCount);
            if (_sendMessageRepeatCount == 0) _sendMessageRepeatCount = 1;

            int.TryParse(_config["GeneralConfig:UDPPortNumber"], out int udp);
            UdpPort = udp == 0 ? 9008 : udp;

            _maintRemotePortList = (_config["GeneralConfig:MaintRemotePortList"] ?? "")
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // Initialize MSMQ arrays
            _queueNameArray = new string[_sessionCount];
            _messageQueueArrays = new MessageQueue[_sessionCount];
            for (int i = 0; i < _sessionCount; i++)
            {
                _queueNameArray[i] = $"{_prefixQueueName}_{i:000}";
                if (MessageQueue.Exists(_queueNameArray[i]))
                {
                    _messageQueueArrays[i] = new MessageQueue(_queueNameArray[i])
                    {
                        Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) })
                    };
                    _messageQueueArrays[i].Purge();
                }
                else
                {
                    _messageQueueArrays[i] = MessageQueue.Create(_queueNameArray[i]);
                    _messageQueueArrays[i].Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
                }
            }
        }

        /// <summary>Returns template filenames under the transaction-type subfolder.</summary>
        public List<string> GetTemplateFiles()
        {
            string subFolder = MapTransactionTypeToFolder(TransactionType);
            string targetPath = Path.Combine(_initialDirectory ?? "", subFolder);
            if (!Directory.Exists(targetPath))
                throw new DirectoryNotFoundException($"Template directory not found: {targetPath}");

            return Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                            .Select(f => Path.GetFileName(f))
                            .ToList()!;
        }

        public void LoadTemplates(List<string> selectedFiles)
        {
            _loadedFilesPaths.Clear();
            string targetPath = Path.Combine(_initialDirectory ?? "", MapTransactionTypeToFolder(TransactionType));

            foreach (var f in selectedFiles)
            {
                var fullPath = Path.Combine(targetPath, f);
                if (File.Exists(fullPath))
                    _loadedFilesPaths.Add(fullPath);
            }
            Log.Info($"Loaded {_loadedFilesPaths.Count} templates for TransactionType {TransactionType}.");
        }

        public int IngestPayloads(int repeatCount)
        {
            if (_loadedFilesPaths.Count == 0)
                throw new InvalidOperationException("No template files loaded. Please load files first.");

            _templateRawDataList.Clear();
            var rnd = new Random();
            for (int i = 1; i <= repeatCount; i++)
            {
                var randomNumbers = Enumerable.Range(1, _loadedFilesPaths.Count).OrderBy(x => rnd.Next()).ToList();
                foreach (var idx in randomNumbers)
                {
                    var fileName = _loadedFilesPaths[idx - 1];
                    byte[] bytesData;
                    if (TransactionType.ToUpper() == "FISC")
                    {
                        var raw = File.ReadAllBytes(fileName);
                        bytesData = new byte[raw.Length - 2];
                        Buffer.BlockCopy(raw, 2, bytesData, 0, bytesData.Length);
                    }
                    else
                    {
                        bytesData = File.ReadAllBytes(fileName);
                    }
                    _templateRawDataList.Add(bytesData);
                }
            }

            WriteBytesDataToQueue();
            return _templateRawDataList.Count;
        }

        private void WriteBytesDataToQueue()
        {
            if (_messageQueueArrays == null) return;
            foreach (MessageQueue messageQueue in _messageQueueArrays)
            {
                int count = 0;
                foreach (byte[] bytesArray in _templateRawDataList)
                {
                    var sendData = new Message(bytesArray, new XmlMessageFormatter(new Type[] { typeof(byte[]) }));
                    sendData.Label = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}_{++count:000000}";
                    messageQueue.Send(sendData);
                }
                Log.Info($"Message Queue Path[{messageQueue.Path}], Write Count[{count}]");
                StressLogBroadcaster.Log($"[QUEUE] Path[{messageQueue.Path.Replace(".\\Private$\\", "")}], Write Count[{count}]");
            }
        }

        public void StartService(string txType, int port, int sendInt, int repeat, int interval, bool waitSignOn, bool autoEcho)
        {
            if (IsRunning) return;

            TransactionType = txType;
            ListenPort = port;
            SendInterval = sendInt;
            IsWaitingSignOn = waitSignOn;
            IsAutoEcho = autoEcho;

            string cardXmlPrefixFileDirectory = _config["GeneralConfig:CardXmlPrefixFileDirectory"] ?? "";
            string cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\{MapTransactionTypeToFolder(TransactionType)}\\CardXmlFilePath.xml";

            bool replaceCardNum = Convert.ToBoolean(_config["GeneralConfig:IsReplaceCardNumber"] ?? "true");
            bool replaceOrigData = Convert.ToBoolean(_config["GeneralConfig:IsReplaceOriginalData"] ?? "true");

            string ipStr = CommonUtils.GetIPAddressStringByHostName(_serverName, AddressFamily.InterNetwork);

            _serverService = new ServerService(
                TransactionType,
                ipStr,
                ListenPort,
                _messageQueueArrays,
                SendInterval,
                cardXmlFilePath,
                IsWaitingSignOn,
                _maintRemotePortList,
                replaceCardNum,
                replaceOrigData,
                _sendMessageRepeatCount,
                IsAutoEcho
            );

            _serverService.StartService();
            // Re-apply NLog redirection since ServerService.StartService may reset the configuration
            StressLogBroadcaster.SetupNLogRedirection();
            _lastSuccessCount = 0;
            _lastTpsTime = DateTime.Now;
            Log.Info($"Server Service started on port {ListenPort} (IP: {ipStr})");
            StressLogBroadcaster.Log($"[SERVICE] Server Service started on port {ListenPort}");

            StartUDP();
        }

        public void StopService()
        {
            if (_serverService == null) return;
            StopUDP();
            _serverService.StopService();
            _serverService = null;
            Log.Info("Server Service stopped.");
            StressLogBroadcaster.Log("[SERVICE] Server Service stopped.");
        }

        public void ResetQueues()
        {
            StopService();
            if (_messageQueueArrays == null || _queueNameArray == null) return;

            for (int i = 0; i < _messageQueueArrays.Length; i++)
            {
                _queueNameArray[i] = $"{_prefixQueueName}_{i:000}";
                if (MessageQueue.Exists(_queueNameArray[i]))
                {
                    _messageQueueArrays[i] = new MessageQueue(_queueNameArray[i])
                    {
                        Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) })
                    };
                    _messageQueueArrays[i].Purge();
                }
                else
                {
                    _messageQueueArrays[i] = MessageQueue.Create(_queueNameArray[i]);
                    _messageQueueArrays[i].Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
                }
                StressLogBroadcaster.Log($"[QUEUE] Purged & Reset: {_queueNameArray[i].Replace(".\\Private$\\", "")}");
            }
            _templateRawDataList.Clear();
        }

        public List<QueueInfo> GetQueueInfo()
        {
            var list = new List<QueueInfo>();
            if (_messageQueueArrays == null) return list;

            foreach (var mq in _messageQueueArrays)
            {
                int count = 0;
                try
                {
                    using var enumerator = mq.GetMessageEnumerator2();
                    while (enumerator.MoveNext()) count++;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, $"Failed to count messages in queue {mq.Path}");
                }

                list.Add(new QueueInfo
                {
                    QueueName = mq.Path.Replace(".\\Private$\\", ""),
                    QueueCount = count
                });
            }
            return list;
        }

        /// <summary>Starts the UDP log listener. Called at startup and when stress service starts.</summary>
        public void StartUDPFromConfig() => StartUDP();

        /// <summary>Checks command-line arguments and appsettings.json for auto-start port and flag.</summary>
        public void CheckAndAutoStart(string[]? args)
        {
            bool isAutoStart = false;
            int port = 0;
            string? txType = null;

            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (string.IsNullOrWhiteSpace(arg)) continue;

                    if (arg.Equals("AUTO", StringComparison.OrdinalIgnoreCase))
                    {
                        isAutoStart = true;
                    }
                    else if (int.TryParse(arg, out int p) && p > 1000)
                    {
                        port = p;
                    }
                    else if (!arg.StartsWith("-") && !arg.StartsWith("/") && !arg.Contains("="))
                    {
                        txType = arg;
                    }
                }
            }

            // Fallback to appsettings.json config if auto-start is not in args
            if (!isAutoStart)
            {
                bool.TryParse(_config["GeneralConfig:AutoStart"], out isAutoStart);
            }

            if (port == 0)
            {
                int.TryParse(_config["GeneralConfig:AutoStartPort"], out port);
            }

            if (port == 0)
            {
                var portListStr = _config["GeneralConfig:ListenPortList"];
                if (!string.IsNullOrEmpty(portListStr))
                {
                    var firstPortStr = portListStr.Split(',')[0].Trim();
                    int.TryParse(firstPortStr, out port);
                }
            }

            if (port == 0) port = 8100;

            if (string.IsNullOrEmpty(txType))
            {
                var configTx = _config["GeneralConfig:TransactionType"];
                if (!string.IsNullOrEmpty(configTx))
                {
                    txType = configTx.Replace("Item", "", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (string.IsNullOrEmpty(txType))
            {
                txType = port switch
                {
                    14801 => "NCCC",
                    14851 => "NCCC",
                    8100 => "FISC",
                    9100 => "FISC",
                    _ => "FISC"
                };
            }

            // Sync property values
            ListenPort = port;
            TransactionType = txType;

            if (isAutoStart)
            {
                Log.Info($"Auto-starting WebStressSimulator for {txType} on port {port}...");
                StressLogBroadcaster.Log($"[AUTO-START] Launching {txType} simulator service on port {port}");

                // Auto-load template files if directory exists
                try
                {
                    var files = GetTemplateFiles();
                    if (files != null && files.Count > 0)
                    {
                        LoadTemplates(files);
                        IngestPayloads(1);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Auto-load templates notice: {ex.Message}");
                }

                StartService(
                    txType: txType,
                    port: port,
                    sendInt: 10000,
                    repeat: 1,
                    interval: 1000,
                    waitSignOn: false,
                    autoEcho: true
                );
            }
        }

        private void StartUDP()
        {
            StopUDP();
            _isStopUDPServer = false;
            _udpThread = new Thread(ReadUDPLogMessage);
            _udpThread.Start();
        }

        private void StopUDP()
        {
            _isStopUDPServer = true;
            if (_udpClient != null)
            {
                try
                {
                    // Send a dummy packet to unblock the blocking Receive() call
                    using var c = new UdpClient();
                    string ipStr = CommonUtils.GetIPAddressStringByHostName(_serverName, AddressFamily.InterNetwork);
                    if (ipStr == "0.0.0.0") ipStr = "127.0.0.1";
                    var ep = new IPEndPoint(IPAddress.Parse(ipStr), UdpPort);
                    byte[] bytes = Encoding.ASCII.GetBytes("STOP");
                    c.Send(bytes, bytes.Length, ep);
                }
                catch { /* Ignore socket close errors during shutdown */ }
                _udpClient.Close();
                _udpClient = null;
            }
            _udpThread = null;
        }

        private void ReadUDPLogMessage()
        {
            try
            {
                _udpClient = new UdpClient(UdpPort);
                var groupEP = new IPEndPoint(IPAddress.Any, UdpPort);
                while (!_isStopUDPServer)
                {
                    if (_udpClient.Available <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    byte[] bytes = _udpClient.Receive(ref groupEP);
                    string msg = Encoding.Default.GetString(bytes, 0, bytes.Length);
                    if (msg != "STOP")
                        StressLogBroadcaster.Log($"[UDP-LOG] {msg}");
                }
            }
            catch (Exception ex)
            {
                if (!_isStopUDPServer)
                    StressLogBroadcaster.Log($"[UDP-ERROR] {ex.Message}");
            }
            finally
            {
                _udpClient?.Close();
            }
        }

        /// <summary>Maps transaction type string to the corresponding template subfolder name.</summary>
        private static string MapTransactionTypeToFolder(string txType) => txType.ToUpper() switch
        {
            "FISC" => "FISC",
            "IAG" => "IAG",
            "MASTERCARD" => "MasterCard",
            "NCCC" => "Nccc",
            "NCPS" => "Ncps",
            "STIN" => "STIN",
            "TWIN" => "Twin",
            "TWMP" => "Twmp",
            _ => "Visa"
        };
    }
}

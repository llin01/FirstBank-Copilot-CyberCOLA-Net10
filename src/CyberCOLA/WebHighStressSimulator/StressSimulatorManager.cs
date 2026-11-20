using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Microsoft.Extensions.Configuration;
using Com.Cybersoft4u.CommonLib.Stress;

namespace Com.Cybersoft4u.Simulator.Web.WebHighStressSimulator
{
    // ─── High Stress Simulator Manager ───────────────────────────────────
    public class StressSimulatorManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ServerService? _server;
        private IPayloadQueue[]? _queues;
        private readonly IConfiguration _config;
        private readonly List<Action<string>> _logCallbacks = new();
        private List<string> _loadedFilesPaths = new();

        public bool IsRunning => _server != null;
        public string TransactionType { get; private set; }
        public int Port { get; private set; }
        public int SendInterval { get; private set; }
        public int RepeatCount { get; private set; }
        public bool IsChannelsMode { get; private set; }
        public int SessionCount { get; private set; }
        public int LoadedFilesCount => _loadedFilesPaths.Count;

        // --- Metrics (WPF Feature parity) ---
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

        public StressSimulatorManager(IConfiguration config)
        {
            _config = config;
            TransactionType = "ItemNccc";
            Port = 9001;
            SendInterval = 100;
            RepeatCount = 10;
            IsChannelsMode = true;
            SessionCount = 10;
        }

        // ─── Real-time UI Logging Callbacks ───────────────────────────────
        public void RegisterLogCallback(Action<string> callback)
        {
            lock (_logCallbacks)
            {
                if (!_logCallbacks.Contains(callback))
                    _logCallbacks.Add(callback);
            }
        }

        public void UnregisterLogCallback(Action<string> callback)
        {
            lock (_logCallbacks)
            {
                _logCallbacks.Remove(callback);
            }
        }

        private void NotifyLog(string message)
        {
            List<Action<string>> snapshot;
            lock (_logCallbacks)
            {
                snapshot = new List<Action<string>>(_logCallbacks);
            }
            foreach (var cb in snapshot)
            {
                try { cb(message); }
                catch { /* Ignore disconnected page handlers */ }
            }
        }

        public void NotifyLogPublic(string message) => NotifyLog(message);

        // ─── Business Logic Methods ───────────────────────────────────────
        public string[] GetListenPorts()
        {
            var portListStr = _config.GetValue<string>("GeneralConfig:ListenPortList") ?? "9001";
            return portListStr.Split(',').Select(p => p.Trim()).ToArray();
        }

        public string[] GetTemplateFiles(string txType)
        {
            var dir = MapTransactionTypeToDirectory(txType);
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir).Select(Path.GetFileName).ToArray()!;
        }

        public void Load(string transactionType, List<string>? selectedFiles, int sessionCount, bool isChannelsMode)
        {
            TransactionType = transactionType;
            SessionCount = sessionCount;
            IsChannelsMode = isChannelsMode;

            try
            {
                // 1. Initialize queues
                if (_queues == null || _queues.Length != sessionCount)
                {
                    _queues = new IPayloadQueue[sessionCount];
                    var prefixName = _config.GetValue<string>("GeneralConfig:PrefixQueueName") ?? ".\\Private$\\NStressQueue";
                    for (int i = 0; i < sessionCount; i++)
                    {
                        string qName = $"{prefixName}_{i:000}";
                        _queues[i] = isChannelsMode
                            ? new ChannelPayloadQueue(qName)
                            : new MsmqPayloadQueue(qName);
                        _queues[i].Purge();
                    }
                }

                // 2. Load templates from directory
                var templatesDir = MapTransactionTypeToDirectory(transactionType);
                if (!Directory.Exists(templatesDir))
                {
                    throw new DirectoryNotFoundException($"Template directory not found: {templatesDir}");
                }

                var files = selectedFiles != null && selectedFiles.Count > 0
                    ? selectedFiles.Select(f => Path.Combine(templatesDir, f)).Where(File.Exists).ToList()
                    : Directory.GetFiles(templatesDir).ToList();

                _loadedFilesPaths = files;

                var successMsg = $"Successfully loaded {_loadedFilesPaths.Count} templates for TransactionType {transactionType}. Mode: {(isChannelsMode ? "Channels" : "MSMQ")}, Sessions: {sessionCount}.";
                Log.Info(successMsg);
                NotifyLog($"[INFO] {successMsg}");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to load templates: {ex.Message}";
                Log.Error(ex, errorMsg);
                NotifyLog($"[ERROR] {errorMsg}");
                throw;
            }
        }

        public int Ingest(int repeatCount)
        {
            if (_queues == null)
            {
                throw new InvalidOperationException("Queues are not initialized. Please load files first.");
            }
            if (_loadedFilesPaths.Count == 0)
            {
                throw new InvalidOperationException("No template files loaded. Please load files first.");
            }

            RepeatCount = repeatCount;
            var rnd = new Random();
            int totalInjected = 0;
            var ingestionTasks = new List<Task>();

            try
            {
                for (int r = 0; r < repeatCount; r++)
                {
                    var shuffled = Enumerable.Range(0, _loadedFilesPaths.Count).OrderBy(_ => rnd.Next()).ToList();
                    foreach (int index in shuffled)
                    {
                        byte[] rawBytes = File.ReadAllBytes(_loadedFilesPaths[index]);
                        byte[] payloadBytes;

                        // Strip FISC Big-Endian header length bytes for injection if FISC
                        if (TransactionType == "FISC" && rawBytes.Length > 2)
                        {
                            payloadBytes = new byte[rawBytes.Length - 2];
                            Buffer.BlockCopy(rawBytes, 2, payloadBytes, 0, payloadBytes.Length);
                        }
                        else
                        {
                            payloadBytes = rawBytes;
                        }

                        foreach (var q in _queues)
                        {
                            ingestionTasks.Add(q.SendAsync(payloadBytes));
                        }
                        totalInjected += _queues.Length;
                    }
                }
                Task.WaitAll(ingestionTasks.ToArray());

                var successMsg = $"Successfully ingested {totalInjected} payloads across {SessionCount} session queues.";
                Log.Info(successMsg);
                NotifyLog($"[INFO] {successMsg}");
                return totalInjected;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Ingestion failed: {ex.Message}";
                Log.Error(ex, errorMsg);
                NotifyLog($"[ERROR] {errorMsg}");
                throw;
            }
        }

        public void Start(int port, int sendInterval, bool isWaitingSignOn, bool isReplaceCardNumber, bool isReplaceOriginalData, bool isAutoEcho)
        {
            if (IsRunning) return;
            if (_queues == null)
            {
                throw new InvalidOperationException("Queues are not initialized. Please load files and ingest payloads first.");
            }

            Port = port;
            SendInterval = sendInterval;

            try
            {
                var cardXmlFilePath = Path.Combine(
                    _config.GetValue<string>("GeneralConfig:CardXmlPrefixFileDirectory")
                        ?? Com.Cybersoft4u.CommonLib.Resource.CyberCOLAPaths.ResolveDirectory("Config", "src/CyberCOLA/WpfStressSimulator/Config"),
                    "card.xml"
                );
                var maintRemotePortVal = _config.GetValue<string>("GeneralConfig:MaintRemotePortList") ?? "23501";
                var maintPorts = maintRemotePortVal.Split(',').ToList();

                _server = new ServerService(
                    TransactionType,
                    "0.0.0.0", // listen on all interfaces
                    port,
                    _queues,
                    sendInterval,
                    cardXmlFilePath,
                    isWaitingSignOn,
                    maintPorts,
                    isReplaceCardNumber,
                    isReplaceOriginalData,
                    _config.GetValue<int>("GeneralConfig:SendMessageRepeatCount"),
                    isAutoEcho
                );

                _server.StartService();

                var startMsg = $"High Stress Simulator Service started on port {port}";
                Log.Info(startMsg);
                NotifyLog($"[INFO] {startMsg}");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to start High Stress service: {ex.Message}";
                Log.Error(ex, errorMsg);
                NotifyLog($"[ERROR] {errorMsg}");
                throw;
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            try
            {
                _server?.StopService();
                _server = null;
                _queues = null;

                var stopMsg = "High Stress Simulator Service stopped.";
                Log.Info(stopMsg);
                NotifyLog($"[INFO] {stopMsg}");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to stop High Stress service: {ex.Message}";
                Log.Error(ex, errorMsg);
                NotifyLog($"[ERROR] {errorMsg}");
                throw;
            }
        }

        public void PurgeQueues()
        {
            if (_queues == null) return;
            try
            {
                foreach (var q in _queues)
                {
                    q.Purge();
                }
                var purgeMsg = "All queues purged.";
                Log.Info(purgeMsg);
                NotifyLog($"[INFO] {purgeMsg}");
            }
            catch (Exception ex)
            {
                NotifyLog($"[ERROR] Purge failed: {ex.Message}");
            }
        }

        public Dictionary<string, int> GetQueueDepths()
        {
            var depths = new Dictionary<string, int>();
            if (_queues != null)
            {
                for (int i = 0; i < _queues.Length; i++)
                {
                    try
                    {
                        depths.Add($"Queue_{i:000}", _queues[i].GetCount());
                    }
                    catch
                    {
                        depths.Add($"Queue_{i:000}", -1);
                    }
                }
            }
            return depths;
        }

        private string MapTransactionTypeToDirectory(string tranType)
        {
            // Found from the program's location rather than a fixed E:\ path, so a clone elsewhere still works.
            var baseDir = Com.Cybersoft4u.CommonLib.Resource.CyberCOLAPaths.ResolveDirectory(
                "Data", "src/CyberCOLA/WpfStressSimulator/Data", "publish/Config/WpfStressSimulator/Data");
            return tranType switch
            {
                "FISC" => Path.Combine(baseDir, "Fisc"),
                "NCCC" or "ItemNccc" => Path.Combine(baseDir, "Nccc"),
                "TWIN" => Path.Combine(baseDir, "Twin"),
                "VISA" => Path.Combine(baseDir, "Visa"),
                "MASTERCARD" => Path.Combine(baseDir, "MasterCard"),
                _ => Path.Combine(baseDir, "Nccc")
            };
        }
    }
}

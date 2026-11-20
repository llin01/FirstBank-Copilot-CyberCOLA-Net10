using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.CommonLib.Stress;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfHighStressSimulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _metricsTimer;
        private ServerService? _serverService;
        private IPayloadQueue[]? _queues;
        private string? _originalConfigPrefixQueueName;
        private string[]? _lastSelectedFiles;

        private string _transactionType = "NCCC";
        private string _listenPort = "9001";
        private int _sendInterval = 1000;
        private int _repeatCount = 1;
        private int _sessionCount = 5;
        private bool _isAutoEcho = true;
        private bool _isWaitingSignOn;
        private bool _isReplaceCardNumber = true;
        private bool _isReplaceOriginalData = true;
        private bool _isServiceRunning;

        // Telemetry metrics
        private int _activeConnections;
        private int _tps;
        private long _successCount;
        private long _errorCount;
        private long _lastSuccessCount;

        private string _statusMessage = "Ready";
        private string _selectedDirectory = CyberCOLAPaths.ResolveDirectory("StressData", "publish/StressData");
        private string _prefixQueueName = "StresserQueue";
        private bool _isRetrievingQueue;
        private int _retrievalIntervalSeconds = 30;

        public event PropertyChangedEventHandler? PropertyChanged;

        // UI Bindings
        public ObservableCollection<string> TransactionTypes { get; } = new()
        { "FISC", "NCCC", "TWIN", "VISA", "MASTERCARD", "IAG", "NCPS", "STIN", "TWMP" };

        public ObservableCollection<string> AvailablePorts { get; } = new()
        { "9001", "9002", "9003", "9004", "9005", "10001", "10002" };

        public ObservableCollection<QueueRow> QueueList { get; } = new();

        public string TransactionType
        {
            get => _transactionType;
            set
            {
                if (_transactionType != value)
                {
                    _transactionType = value;
                    OnPropertyChanged();
                    UpdatePrefixQueueName();
                    ResetService();
                }
            }
        }

        public string ListenPort
        {
            get => _listenPort;
            set { _listenPort = value; OnPropertyChanged(); }
        }

        public int SendInterval
        {
            get => _sendInterval;
            set { _sendInterval = value; OnPropertyChanged(); }
        }

        public int RepeatCount
        {
            get => _repeatCount;
            set { _repeatCount = value; OnPropertyChanged(); }
        }

        public int SessionCount
        {
            get => _sessionCount;
            set { _sessionCount = value; OnPropertyChanged(); }
        }

        public bool IsAutoEcho
        {
            get => _isAutoEcho;
            set { _isAutoEcho = value; OnPropertyChanged(); }
        }

        public bool IsWaitingSignOn
        {
            get => _isWaitingSignOn;
            set { _isWaitingSignOn = value; OnPropertyChanged(); }
        }

        public bool IsReplaceCardNumber
        {
            get => _isReplaceCardNumber;
            set { _isReplaceCardNumber = value; OnPropertyChanged(); }
        }

        public bool IsReplaceOriginalData
        {
            get => _isReplaceOriginalData;
            set { _isReplaceOriginalData = value; OnPropertyChanged(); }
        }

        public bool IsServiceRunning
        {
            get => _isServiceRunning;
            set
            {
                if (_isServiceRunning != value)
                {
                    _isServiceRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsIngestionEnabled));
                }
            }
        }

        public bool IsIngestionEnabled => !_isServiceRunning;

        public int ActiveConnections
        {
            get => _activeConnections;
            set { _activeConnections = value; OnPropertyChanged(); }
        }

        public int Tps
        {
            get => _tps;
            set { _tps = value; OnPropertyChanged(); }
        }

        public long SuccessCount
        {
            get => _successCount;
            set { _successCount = value; OnPropertyChanged(); }
        }

        public long ErrorCount
        {
            get => _errorCount;
            set { _errorCount = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string SelectedDirectory
        {
            get => _selectedDirectory;
            set { _selectedDirectory = value; OnPropertyChanged(); }
        }

        public string PrefixQueueName
        {
            get => _prefixQueueName;
            set { _prefixQueueName = value; OnPropertyChanged(); }
        }

        public bool IsRetrievingQueue
        {
            get => _isRetrievingQueue;
            set { _isRetrievingQueue = value; OnPropertyChanged(); }
        }

        public int RetrievalIntervalSeconds
        {
            get => _retrievalIntervalSeconds;
            set { _retrievalIntervalSeconds = value; OnPropertyChanged(); }
        }

        // Channels vs MSMQ Engine mode (Default: true for Channels)
        private bool _isChannelsMode = true;
        public bool IsChannelsMode
        {
            get => _isChannelsMode;
            set
            {
                _isChannelsMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMsmqMode));
            }
        }

        public bool IsMsmqMode
        {
            get => !_isChannelsMode;
            set
            {
                _isChannelsMode = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChannelsMode));
            }
        }

        // Actions
        public Action<string>? LogAction { get; set; }
        public Func<string[], string[]?>? ShowFileSelector { get; set; }

        // Commands
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand LoadFilesCommand { get; }
        public ICommand IngestPayloadsCommand { get; }
        public ICommand ToggleRetrievalCommand { get; }
        public ICommand BrowseFolderCommand { get; }

        public MainViewModel()
        {
            StartCommand = new RelayCommand(StartService, () => !IsServiceRunning);
            StopCommand = new RelayCommand(StopService, () => IsServiceRunning);
            ResetCommand = new RelayCommand(ResetService, () => !IsServiceRunning);
            // LOAD FILES / INGEST PAYLOADS are always usable so templates can be selected/queued before service starts.
            LoadFilesCommand = new RelayCommand(LoadFiles);
            IngestPayloadsCommand = new RelayCommand(IngestPayloads);
            ToggleRetrievalCommand = new RelayCommand(ToggleRetrieval);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);

            // Configure dispatcher timer to safely update telemetry metrics at 500ms intervals
            _metricsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _metricsTimer.Tick += MetricsTimer_Tick;
            _metricsTimer.Start();

            // Set default directory
            var root = new ConfigurationLoaderRoot();
            SelectedDirectory = root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory") ?? SelectedDirectory;

            // Read and parse TransactionType from config to initialize the ViewModel property properly
            string? configTranType = root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TransactionType");
            if (!string.IsNullOrEmpty(configTranType))
            {
                configTranType = configTranType.Trim();
                if (configTranType.StartsWith("Item", StringComparison.OrdinalIgnoreCase) && configTranType.Length > 4)
                {
                    configTranType = configTranType.Substring(4);
                }
                string? matched = TransactionTypes.FirstOrDefault(t => string.Equals(t, configTranType, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    _transactionType = matched;
                }
            }

            PrefixQueueName = root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "PrefixQueueName") ?? PrefixQueueName;
            _originalConfigPrefixQueueName = PrefixQueueName;
            if (PrefixQueueName == "StresserQueue" || string.IsNullOrEmpty(PrefixQueueName))
            {
                UpdatePrefixQueueName();
            }

            int.TryParse(root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SessionCount"), out int sess);
            if (sess > 0) SessionCount = sess;

            int.TryParse(root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SendIntervalInMilliseconds"), out int interval);
            if (interval > 0) SendInterval = interval;

            // Load available ports from appsettings.json GeneralConfig:ListenPortList to strictly align with dynamic setups
            string? portListStr = root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ListenPortList");
            if (!string.IsNullOrWhiteSpace(portListStr))
            {
                var ports = portListStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(p => p.Trim())
                                      .Where(p => !string.IsNullOrEmpty(p))
                                      .ToList();
                if (ports.Count > 0)
                {
                    AvailablePorts.Clear();
                    foreach (var port in ports)
                    {
                        AvailablePorts.Add(port);
                    }
                    // Default to the first configured port in the list for friendly user onboarding
                    ListenPort = ports[0];
                }
            }
        }

        private void MetricsTimer_Tick(object? sender, EventArgs e)
        {
            // Gather atomic stats safely from the active C# pipelines communications engine
            ActiveConnections = ServerService.Metrics.ActiveConnections;
            SuccessCount = ServerService.Metrics.SuccessCount;
            ErrorCount = ServerService.Metrics.ErrorCount;

            // Instant TPS calculation (Difference over 500ms * 2)
            long currentSuccess = SuccessCount;
            long diff = currentSuccess - _lastSuccessCount;
            _lastSuccessCount = currentSuccess;
            Tps = (int)(diff * 2);

            // Periodically refresh Session Queue details if enabled
            if (IsRetrievingQueue && _queues != null)
            {
                UpdateQueueGrid();
            }

            // Write operation summary to JSON file periodically for external monitoring
            try
            {
                string summaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OperationSummary.json");
                string json = $"{{\n" +
                              $"  \"TransactionType\": \"{TransactionType}\",\n" +
                              $"  \"ListenPort\": \"{ListenPort}\",\n" +
                              $"  \"ActiveConnections\": {ActiveConnections},\n" +
                              $"  \"SuccessCount\": {SuccessCount},\n" +
                              $"  \"ErrorCount\": {ErrorCount},\n" +
                              $"  \"Tps\": {Tps},\n" +
                              $"  \"IsServiceRunning\": {(IsServiceRunning ? "true" : "false")},\n" +
                              $"  \"LastUpdated\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"\n" +
                              $"}}";
                File.WriteAllText(summaryPath, json);
            }
            catch
            {
                // Suppress exceptions to prevent crashing the simulator UI thread on file I/O errors
            }
        }

        private void StartService()
        {
            if (_queues == null)
            {
                StatusMessage = "Error: Please initialize queues first!";
                LogAction?.Invoke("Error: Attempted to start service but queues were not initialized.");
                return;
            }

            try
            {
                StatusMessage = "Starting service...";
                LogAction?.Invoke($"Starting TCP simulator service on port: {ListenPort}...");

                var root = new ConfigurationLoaderRoot();
                string cardXmlPrefixDir = root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "CardXmlPrefixFileDirectory") ?? @".\publish\CardData";
                string cardXmlPath = Path.Combine(cardXmlPrefixDir, TransactionType, "CardXmlFilePath.xml");

                string? serverName = root.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ServerName");
                string ipStr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                if (ipStr == "0.0.0.0") ipStr = "127.0.0.1";

                int port = int.Parse(ListenPort);

                // Initialize high performance server simulation engine
                _serverService = new ServerService(
                    TransactionType,
                    ipStr,
                    port,
                    _queues,
                    SendInterval,
                    cardXmlPath,
                    IsWaitingSignOn,
                    new List<string>(), // maintRemotePortList
                    IsReplaceCardNumber,
                    IsReplaceOriginalData,
                    1, // Repeat Send Count
                    IsAutoEcho
                );

                ServerService.Metrics.Reset();
                _serverService.StartService();

                IsServiceRunning = true;
                StatusMessage = "Service Running";
                LogAction?.Invoke($"[ServerService] Listening successfully on Port {port} for type {TransactionType}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Start failed: {ex.Message}";
                LogAction?.Invoke($"Error starting service: {ex.Message}");
            }
        }

        private void StopService()
        {
            try
            {
                StatusMessage = "Stopping service...";
                _serverService?.StopService();
                _serverService = null;
                IsServiceRunning = false;

                // Reset and purge all virtual queues data upon service shutdown for strict reliability
                if (_queues != null)
                {
                    foreach (var q in _queues)
                    {
                        q.Purge();
                    }
                    UpdateQueueGrid();
                }

                StatusMessage = "Service Stopped";
                LogAction?.Invoke("[ServerService] Stopped successfully. All session queues data reset.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Stop failed: {ex.Message}";
            }
        }

        private void ResetService()
        {
            try
            {
                StopService();
                _queues = null;
                _lastSelectedFiles = null;
                QueueList.Clear();
                ServerService.Metrics.Reset();
                StatusMessage = "Reset Complete";
                LogAction?.Invoke("Service reset, queues purged, and loaded file selection cleared.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Reset failed: {ex.Message}";
            }
        }

        private void LoadFiles()
        {
            // Case-Insensitive directory resolution to prevent folder loading failures under strict environments
            string workDir = Path.Combine(SelectedDirectory, TransactionType);
            if (Directory.Exists(SelectedDirectory))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(SelectedDirectory))
                    {
                        string dirName = Path.GetFileName(dir);
                        if (string.Equals(dirName, TransactionType, StringComparison.OrdinalIgnoreCase))
                        {
                            workDir = dir;
                            break;
                        }
                    }
                }
                catch
                {
                    // Fallback to default path combine
                }
            }

            if (!Directory.Exists(workDir))
            {
                StatusMessage = "Error: Selected directory does not exist!";
                LogAction?.Invoke($"Error: Directory not found: {workDir}");
                System.Windows.MessageBox.Show(
                    $"The selected template directory does not exist:\n{workDir}\n\nClick [Browse] to select the correct payload folder!",
                    "Directory Not Found",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                _lastSelectedFiles = null;
                return;
            }

            string[] fileEntries = Directory.GetFiles(workDir, "*.*", SearchOption.AllDirectories);
            if (fileEntries.Length == 0)
            {
                StatusMessage = "Error: No templates found in folder!";
                LogAction?.Invoke($"Error: No template payload files in {workDir}");
                System.Windows.MessageBox.Show(
                    $"No template payload files were found in the selected directory:\n{workDir}\n\nPlease verify that payload templates exist in this folder!",
                    "No Template Files",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                _lastSelectedFiles = null;
                return;
            }

            var selected = ShowFileSelector?.Invoke(fileEntries);
            if (selected == null || selected.Length == 0)
            {
                _lastSelectedFiles = null;
                StatusMessage = "No templates selected.";
                return;
            }

            _lastSelectedFiles = selected;
            StatusMessage = $"Loaded {selected.Length} template files. Ready to ingest.";
            LogAction?.Invoke($"Load Files: Selected {selected.Length} template files from {TransactionType}.");
        }

        private async void IngestPayloads()
        {
            if (_lastSelectedFiles == null || _lastSelectedFiles.Length == 0)
            {
                StatusMessage = "Error: Please load files first!";
                LogAction?.Invoke("Error: No template files loaded. Click LOAD FILES.");
                System.Windows.MessageBox.Show(
                    "No template payload files have been loaded yet.\n\nClick [LOAD FILES] to select templates before ingesting!",
                    "Templates Not Loaded",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var selected = _lastSelectedFiles;

            try
            {
                bool isAppending = (_queues != null && _queues.Length == SessionCount);

                if (isAppending)
                {
                    StatusMessage = "Appending payloads to active queues...";
                    LogAction?.Invoke($"Appending {selected.Length} template files to existing queues. RepeatCount = {RepeatCount}");
                }
                else
                {
                    StatusMessage = "Initializing payload queues...";
                    LogAction?.Invoke($"Selected {selected.Length} template files. RepeatCount = {RepeatCount}");

                    // Re-create high performance queues based on mode selection (Channels vs MSMQ)
                    _queues = new IPayloadQueue[SessionCount];
                    QueueList.Clear();

                    for (int i = 0; i < SessionCount; i++)
                    {
                        string qName = $"{PrefixQueueName}_{i:000}";
                        _queues[i] = IsChannelsMode
                            ? new ChannelPayloadQueue(qName)
                            : new MsmqPayloadQueue(qName);

                        _queues[i].Purge();
                    }
                }

                // Randomly disperse payloads evenly into session queues
                var rnd = new Random();
                int totalInjected = 0;
                var ingestionTasks = new List<Task>();

                for (int r = 0; r < RepeatCount; r++)
                {
                    var shuffled = Enumerable.Range(0, selected.Length).OrderBy(_ => rnd.Next()).ToList();
                    foreach (int index in shuffled)
                    {
                        byte[] rawBytes = File.ReadAllBytes(selected[index]);

                        // Strip FISC Big-Endian header length bytes for injection if FISC
                        byte[] payloadBytes;
                        if (TransactionType == "FISC" && rawBytes.Length > 2)
                        {
                            payloadBytes = new byte[rawBytes.Length - 2];
                            Buffer.BlockCopy(rawBytes, 2, payloadBytes, 0, payloadBytes.Length);
                        }
                        else
                        {
                            payloadBytes = rawBytes;
                        }

                        // Broadcast write to all session queues concurrently
                        foreach (var q in _queues)
                        {
                            ingestionTasks.Add(q.SendAsync(payloadBytes));
                        }
                        totalInjected += _queues.Length;
                    }
                }

                // Wait for all ingestion tasks to strictly complete before updating telemetry
                await Task.WhenAll(ingestionTasks);

                UpdateQueueGrid();

                if (isAppending)
                {
                    StatusMessage = $"Queues Updated (+{totalInjected} payloads appended)";
                    LogAction?.Invoke($"Success: Appended {totalInjected} packets dynamically to {SessionCount} Session queues (Engine: {(IsChannelsMode ? "System.Threading.Channels" : "MSMQ")}).");
                }
                else
                {
                    StatusMessage = $"Queues Ready ({totalInjected} payloads injected)";
                    LogAction?.Invoke($"Success: Injected {totalInjected} packets across {SessionCount} Session queues (Engine: {(IsChannelsMode ? "System.Threading.Channels" : "MSMQ")}).");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Queue setup failed: {ex.Message}";
                LogAction?.Invoke($"Error creating queues: {ex.Message}");
            }
        }

        private void ToggleRetrieval()
        {
            if (IsRetrievingQueue)
            {
                IsRetrievingQueue = false;
                StatusMessage = "Queue status auto-refresh stopped.";
            }
            else
            {
                if (RetrievalIntervalSeconds < 1) RetrievalIntervalSeconds = 1;
                IsRetrievingQueue = true;
                StatusMessage = $"Auto-refreshing queues every {RetrievalIntervalSeconds}s";
            }
        }

        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = Directory.Exists(SelectedDirectory) ? SelectedDirectory : Environment.CurrentDirectory
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedDirectory = dialog.SelectedPath;
                LogAction?.Invoke($"Selected root directory path: {SelectedDirectory}");
            }
        }

        private void UpdateQueueGrid()
        {
            if (_queues == null) return;

            QueueList.Clear();
            for (int i = 0; i < _queues.Length; i++)
            {
                QueueList.Add(new QueueRow
                {
                    SessionId = $"Session #{i + 1:000}",
                    QueuePath = _queues[i].Path,
                    Count = _queues[i].GetCount()
                });
            }
        }

        public void ShutdownCleanup()
        {
            try
            {
                // 1. Stop the TCP simulator service if running
                if (IsServiceRunning)
                {
                    StopService();
                }

                // 2. Dispose queue resources and clear pending payloads
                if (_queues != null)
                {
                    foreach (var q in _queues)
                    {
                        q.Purge();
                        (q as IDisposable)?.Dispose();
                    }
                    _queues = null;
                    UpdateQueueGrid();
                }

                _lastSelectedFiles = null;
                QueueList.Clear();
                LogAction?.Invoke("[Shutdown] All connections stopped and resources released.");
            }
            catch (Exception ex)
            {
                LogAction?.Invoke($"[Shutdown Cleanup Error] {ex.Message}");
            }
        }

        private void UpdatePrefixQueueName()
        {
            if (string.IsNullOrEmpty(TransactionType))
            {
                PrefixQueueName = "StressQueue";
                return;
            }

            string typeUpper = TransactionType.ToUpper();
            if (typeUpper == "NCCC")
            {
                if (_originalConfigPrefixQueueName != null && _originalConfigPrefixQueueName.Contains("QRCode", StringComparison.OrdinalIgnoreCase))
                {
                    PrefixQueueName = @".\Private$\QRCodeStressQueue";
                }
                else
                {
                    PrefixQueueName = @".\Private$\NStressQueue";
                }
            }
            else if (typeUpper == "FISC")
            {
                if (_originalConfigPrefixQueueName != null && _originalConfigPrefixQueueName.Contains("IDVerify", StringComparison.OrdinalIgnoreCase))
                {
                    PrefixQueueName = @".\Private$\IDVerifyStressQueue";
                }
                else
                {
                    PrefixQueueName = @".\Private$\FStressQueue";
                }
            }
            else
            {
                char prefixChar = char.ToUpper(TransactionType[0]);
                PrefixQueueName = $"{prefixChar}StressQueue";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class QueueRow
    {
        public string SessionId { get; set; } = string.Empty;
        public string QueuePath { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // High performance RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }
}

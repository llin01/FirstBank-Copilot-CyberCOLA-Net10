using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebTCPConnectionMonitor
{
    // --- Connection Info Model (WPF Parity) ---
    public class ConnectionInfo
    {
        public int Sequence { get; set; }
        public string RemoteEndPoint { get; set; } = string.Empty;
        public string LocalEndPoint { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    // --- Web Log Broadcaster for Connection Monitoring ---
    public static class TCPConnectionLogBroadcaster
    {
        private static readonly ConcurrentQueue<string> SystemLogQueue = new();
        private static readonly ConcurrentQueue<string> FatalLogQueue = new();

        public static event Action<string>? OnSystemLogAdded;
        public static event Action<string>? OnFatalLogAdded;

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

        public static void LogFatal(string message)
        {
            var formatted = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            FatalLogQueue.Enqueue(formatted);
            OnFatalLogAdded?.Invoke(formatted);
            while (FatalLogQueue.Count > 600)
            {
                FatalLogQueue.TryDequeue(out _);
            }
        }

        public static IEnumerable<string> GetSystemHistory() => SystemLogQueue.ToArray();
        public static IEnumerable<string> GetFatalHistory() => FatalLogQueue.ToArray();

        public static void Clear()
        {
            SystemLogQueue.Clear();
            FatalLogQueue.Clear();
        }
    }

    // --- TCP Connection Monitor Manager ---
    public class TCPConnectionMonitorManager : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _config;
        private TCPConnectionMonitorInfo? _tcpConnectionMonitorInfo;

        // Timer for polling
        private Timer? _pollingTimer;
        private readonly object _timerLock = new();
        private int _retrievalTimeIntervalSec = 30;

        // UDP Service State
        private UdpClient? _udpClient;
        private Task? _udpListenerTask;
        private CancellationTokenSource? _udpCts;

        // WPF Parity Descriptions
        public string Transaction2GatewayServiceDescription { get; private set; } = "Transaction to Gateway Service";
        public string ColaService2ClientAuthServiceDescription { get; private set; } = "ColaService to ClientAuth Service";
        public string ServerRouterService2ClientMonitorServiceDescription { get; private set; } = "ServerRouterService to Client Monitor Service";
        public string ClientAuthRouterService2TcpClientDescription { get; private set; } = "ClientAuth Service to TcpClient Service";

        public int UdpServerListenerPort { get; private set; } = 9910;
        public string UdpServerName { get; private set; } = "localhost";

        // Current Lists (Thread-safe binds)
        public List<ConnectionInfo> Nccc2NcccServiceList { get; } = new();
        public List<ConnectionInfo> ColaService2ClientAuthServiceList { get; } = new();
        public List<ConnectionInfo> RouterService2ClientMonitorServiceList { get; } = new();
        public List<ConnectionInfo> ClientAuthRouterService2TcpClientList { get; } = new();

        public bool IsPollingActive { get; private set; } = false;
        public bool IsUdpActive { get; private set; } = false;

        public event Action? OnStatusUpdated;

        public TCPConnectionMonitorManager(IConfiguration config)
        {
            _config = config;
            InitSystemConfig();
        }

        private void InitSystemConfig()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-connection-monitor-info-xml-file-path");

                if (string.IsNullOrEmpty(xmlFilePath) || !File.Exists(xmlFilePath))
                {
                    xmlFilePath = Path.Combine(AppContext.BaseDirectory, "Config", "TCPConnectionMonitorInfo.xml");
                }

                if (File.Exists(xmlFilePath))
                {
                    var serializer = new XmlSerializer(typeof(TCPConnectionMonitorInfo));
                    using var reader = XmlReader.Create(xmlFilePath);
                    _tcpConnectionMonitorInfo = (TCPConnectionMonitorInfo)serializer.Deserialize(reader)!;
                    TCPConnectionLogBroadcaster.LogSystem($"Loaded Connection Monitor config from: {xmlFilePath}");
                }
                else
                {
                    TCPConnectionLogBroadcaster.LogSystem($"Warning: Config file not found at: {xmlFilePath}. Polling requires active configuration.");
                }

                // Read Settings from configurationLoader
                var intervalStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "RetrievalTimeIntervalSec")?.Trim();
                if (int.TryParse(intervalStr, out var intervalSecs))
                {
                    _retrievalTimeIntervalSec = intervalSecs;
                }

                UdpServerName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPServerName")?.Trim() ?? "localhost";

                var udpPortStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPServerListenerPort")?.Trim();
                if (int.TryParse(udpPortStr, out var udpPort))
                {
                    UdpServerListenerPort = udpPort;
                }

                Transaction2GatewayServiceDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "Transaction2GatewayServiceDescription")?.Trim() ?? "Transaction to Gateway Service";
                ColaService2ClientAuthServiceDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ColaService2ClientAuthServiceDescription")?.Trim() ?? "ColaService to ClientAuth Service";
                ServerRouterService2ClientMonitorServiceDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ServerRouterService2ClientMonitorServiceDescription")?.Trim() ?? "ServerRouterService to Client Monitor Service";
                ClientAuthRouterService2TcpClientDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ClientAuthRouterService2TcpClientDescription")?.Trim() ?? "ClientAuth Service to TcpClient Service";

                var autoRetrieval = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "AutoRetrieval")?.Trim() ?? "false");
                var autoStartUDP = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "AutoStartUDPService")?.Trim() ?? "false");

                if (autoRetrieval)
                {
                    StartPolling(_retrievalTimeIntervalSec);
                }

                if (autoStartUDP)
                {
                    StartUdpService();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "InitSystemConfig failed");
                TCPConnectionLogBroadcaster.LogSystem($"InitConfig Error: {ex.Message}");
            }
        }

        public void StartPolling(int intervalSecs)
        {
            lock (_timerLock)
            {
                if (IsPollingActive) StopPolling();

                _retrievalTimeIntervalSec = intervalSecs;
                _pollingTimer = new Timer(PollConnections, null, 0, _retrievalTimeIntervalSec * 1000);
                IsPollingActive = true;
                TCPConnectionLogBroadcaster.LogSystem($"Started connection polling (Interval: {_retrievalTimeIntervalSec}s).");
            }
        }

        public void StopPolling()
        {
            lock (_timerLock)
            {
                if (!IsPollingActive) return;

                _pollingTimer?.Dispose();
                _pollingTimer = null;
                IsPollingActive = false;
                TCPConnectionLogBroadcaster.LogSystem("Stopped connection polling.");
            }
        }

        public void StartUdpService()
        {
            if (IsUdpActive) return;

            _udpCts = new CancellationTokenSource();
            _udpClient = new UdpClient(UdpServerListenerPort);
            IsUdpActive = true;

            _udpListenerTask = Task.Run(() => ReadServerLogMessage(_udpCts.Token));
            TCPConnectionLogBroadcaster.LogSystem($"UDPServer [{UdpServerListenerPort}] service started.");
        }

        public void StopUdpService()
        {
            if (!IsUdpActive) return;

            IsUdpActive = false;
            try
            {
                _udpCts?.Cancel();
            }
            catch { /* Ignore cancellation errors on shutdown */ }

            try
            {
                _udpClient?.Close();
                _udpClient?.Dispose();
            }
            catch { /* Ignore UDP client close/dispose errors on shutdown */ }
            _udpClient = null;

            TCPConnectionLogBroadcaster.LogSystem($"UDPServer [{UdpServerListenerPort}] service stopped.");
        }

        private void PollConnections(object? state)
        {
            try
            {
                if (_tcpConnectionMonitorInfo == null) return;

                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                TCPConnectionLogBroadcaster.LogSystem($"Retrieval connection status at: {dateTimeNow}");

                // 1. Nccc2NcccService
                lock (Nccc2NcccServiceList)
                {
                    Nccc2NcccServiceList.Clear();
                    int count = 0;
                    foreach (var ep in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.Nccc2NcccService.EndPoints.EndPoint)
                    {
                        var parts = ep.Split(',');
                        if (parts.Length < 2) continue;
                        var conns = GetTcpConnections(parts[0], parts[1]);
                        foreach (var conn in conns)
                        {
                            Nccc2NcccServiceList.Add(new ConnectionInfo
                            {
                                Sequence = ++count,
                                RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                                LocalEndPoint = conn.LocalEndPoint.ToString(),
                                State = conn.State.ToString()
                            });
                        }
                    }
                }

                // 2. ColaService2ClientAuthService
                lock (ColaService2ClientAuthServiceList)
                {
                    ColaService2ClientAuthServiceList.Clear();
                    int count = 0;
                    foreach (var ep in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.CloaServiceClientAuthService.EndPoints.EndPoint)
                    {
                        var parts = ep.Split(',');
                        if (parts.Length < 2) continue;
                        var conns = GetTcpConnections(parts[0], parts[1]);
                        foreach (var conn in conns)
                        {
                            ColaService2ClientAuthServiceList.Add(new ConnectionInfo
                            {
                                Sequence = ++count,
                                RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                                LocalEndPoint = conn.LocalEndPoint.ToString(),
                                State = conn.State.ToString()
                            });
                        }
                    }
                }

                // 3. RouterService2ClientMonitorService
                lock (RouterService2ClientMonitorServiceList)
                {
                    RouterService2ClientMonitorServiceList.Clear();
                    int count = 0;
                    foreach (var ep in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.RouterService2ClientMonitorService.EndPoints.EndPoint)
                    {
                        var parts = ep.Split(',');
                        if (parts.Length < 2) continue;
                        var conns = GetTcpConnections(parts[0], parts[1]);
                        foreach (var conn in conns)
                        {
                            RouterService2ClientMonitorServiceList.Add(new ConnectionInfo
                            {
                                Sequence = ++count,
                                RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                                LocalEndPoint = conn.LocalEndPoint.ToString(),
                                State = conn.State.ToString()
                            });
                        }
                    }
                }

                // 4. ClientAuthRouterService2TcpClient
                lock (ClientAuthRouterService2TcpClientList)
                {
                    ClientAuthRouterService2TcpClientList.Clear();
                    int count = 0;
                    foreach (var ep in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.ClientAuthRouterService2TcpClient.EndPoints.EndPoint)
                    {
                        var parts = ep.Split(',');
                        if (parts.Length < 2) continue;
                        var conns = GetTcpConnections(parts[0], parts[1]);
                        foreach (var conn in conns)
                        {
                            ClientAuthRouterService2TcpClientList.Add(new ConnectionInfo
                            {
                                Sequence = ++count,
                                RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                                LocalEndPoint = conn.LocalEndPoint.ToString(),
                                State = conn.State.ToString()
                            });
                        }
                    }
                }

                OnStatusUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred during connection polling");
                TCPConnectionLogBroadcaster.LogSystem($"Polling Error: {ex.Message}");
            }
        }

        private async Task ReadServerLogMessage(CancellationToken token)
        {
            var buffer = new byte[65535];
            while (IsUdpActive && !token.IsCancellationRequested)
            {
                try
                {
                    if (_udpClient == null) break;

                    var result = await _udpClient.ReceiveAsync(token);
                    string rawMsg = Encoding.Default.GetString(result.Buffer, 0, result.Buffer.Length);
                    TCPConnectionLogBroadcaster.LogFatal(rawMsg);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException sex) when (sex.SocketErrorCode == SocketError.OperationAborted)
                {
                    break; // Closed
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error reading UDP packet");
                    TCPConnectionLogBroadcaster.LogSystem($"UDP Receive Error: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        private TcpConnectionInformation[] GetTcpConnections(string remoteEndPoint, string localEndPoint)
        {
            try
            {
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

                var remoteIpArray = remoteEndPoint.Split(':');
                var connectionsFilteredByRemote = remoteIpArray.Length == 1
                    ? connections.Where(x => x.RemoteEndPoint.Address.ToString().Equals(remoteIpArray[0])).ToArray()
                    : connections.Where(x => x.RemoteEndPoint.ToString().Equals(remoteEndPoint)).ToArray();

                var localIpArray = localEndPoint.Split(':');
                var finalConnections = localIpArray.Length == 1
                    ? connectionsFilteredByRemote.Where(x => x.LocalEndPoint.Address.ToString().Equals(localIpArray[0])).ToArray()
                    : connectionsFilteredByRemote.Where(x => x.LocalEndPoint.ToString().Equals(localEndPoint)).ToArray();

                return finalConnections;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetTcpConnections failed");
                return Array.Empty<TcpConnectionInformation>();
            }
        }

        public void Dispose()
        {
            StopPolling();
            StopUdpService();
        }
    }
}

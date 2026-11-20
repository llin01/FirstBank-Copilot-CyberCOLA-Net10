using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.Test.Web.WebTCPClient
{
    public class TCPClientManager : IDisposable
    {
        private static readonly Com.Cybersoft4u.Utils.Logger Logger;

        static TCPClientManager()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WebTCPClient]");
        }

        private TCPClientService? _clientMonitorService;
        private bool _isTcpStarted = false;
        private bool _isInitialized = false;

        public event Action<string, string>? OnSystemLog; // message, color (e.g., "red", "green", "white")
        public event Action<string, string>? OnReceivedLog; // message, color

        public string RemoteHost { get; set; } = "CFLin-NB-Test";
        public string TargetIP { get; set; } = "CFLin-NB-Test";
        public int TargetPort { get; set; } = 9901;
        public string LocalIP { get; set; } = "CFLin-NB-Test";
        public int LocalPort { get; set; } = 10001;
        public bool AutoStart { get; set; } = true;
        public bool AutoEcho { get; set; } = false;

        public bool IsTcpConnected => _clientMonitorService?.IsConnected() ?? false;
        public bool IsTcpStarted => _isTcpStarted;

        public void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                Logger.Info("TCPClientManager initializing...");
                LogSystem("System initializing...", "cyan");

                var configurationLoaderRoot = new ConfigurationLoaderRoot();

                RemoteHost = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-remote-hostname")?.Trim() ?? "CFLin-NB-Test";
                TargetIP = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-target-ip")?.Trim() ?? "CFLin-NB-Test";

                if (int.TryParse(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-target-port"), out var tport))
                {
                    TargetPort = tport;
                }

                LocalIP = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-local-ip")?.Trim() ?? "CFLin-NB-Test";

                if (int.TryParse(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-local-port"), out var lport))
                {
                    LocalPort = lport;
                }

                AutoStart = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-auto-start-flag")?.Trim().ToUpper() == "TRUE";
                AutoEcho = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-auto-echo-flag")?.Trim().ToUpper() == "TRUE";

                if (AutoStart)
                {
                    StartTcpService();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Initialization failed: {ex.Message}");
                LogSystem($"Initialization Error: {ex.Message}", "red");
            }
        }

        public void StartTcpService()
        {
            try
            {
                if (_isTcpStarted) return;

                LogSystem($"Client Monitor Service starting at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}", "cyan");

                var remoteIPAddressString = CommonUtils.GetIPAddressStringByHostName(TargetIP, AddressFamily.InterNetwork);
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddressString), TargetPort);
                var localIPAddressString = CommonUtils.GetIPAddressStringByHostName(LocalIP, AddressFamily.InterNetwork);
                var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPAddressString), LocalPort);

                _clientMonitorService = new TCPClientService(RemoteHost, remoteEndPoint, localEndPoint);
                _clientMonitorService.EnableLogger("ClientAuthClientMonitor", Com.Cybersoft4u.Utils.LogLevel.Debug);

                _clientMonitorService.Start(OnReceivedBytes);
                _isTcpStarted = true;

                LogSystem("TCP Client Service started.", "green");
            }
            catch (Exception ex)
            {
                Logger.Error($"StartTcpService failed: {ex.Message}");
                LogSystem($"Start TCP Service Error: {ex.Message}", "red");
            }
        }

        public void StopTcpService()
        {
            try
            {
                if (_clientMonitorService != null && _isTcpStarted)
                {
                    _clientMonitorService.Stop();
                    _isTcpStarted = false;
                    LogSystem("TCP Client Service stopped.", "yellow");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"StopTcpService failed: {ex.Message}");
                LogSystem($"Stop TCP Service Error: {ex.Message}", "red");
            }
        }

        public bool SendMessage(string payload, bool isHex)
        {
            if (_clientMonitorService == null || !_clientMonitorService.IsConnected())
            {
                LogSystem("Cannot send: TCP Client is disconnected.", "red");
                return false;
            }

            try
            {
                byte[] bytes;
                if (isHex)
                {
                    // Clean hex string
                    var cleanHex = payload.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                    bytes = ColaCommUtility.HexString2HexBinary(cleanHex);
                }
                else
                {
                    bytes = Encoding.ASCII.GetBytes(payload);
                }

                LogSystem($"--- Sending data ({bytes.Length}B) ---", "gray");
                LogSystem($"HEX: {ColaCommUtility.PrintBytesHex(bytes)}", "gray");
                LogSystem($"ASCII: {Encoding.ASCII.GetString(bytes)}", "gray");

                var success = _clientMonitorService.Send(bytes);
                if (success)
                {
                    LogSystem("Data sent successfully.", "green");
                }
                else
                {
                    LogSystem("Failed to send data.", "red");
                }
                return success;
            }
            catch (Exception ex)
            {
                LogSystem($"Send failed: {ex.Message}", "red");
                Logger.Error($"Send failed: {ex.Message}");
                return false;
            }
        }

        private void OnReceivedBytes(byte[] receiveBytes)
        {
            if (receiveBytes == null || receiveBytes.Length == 0) return;

            try
            {
                var hexStr = ColaCommUtility.PrintBytesHex(receiveBytes);
                var asciiStr = Encoding.ASCII.GetString(receiveBytes);

                LogReceived($"--- Received Response ({receiveBytes.Length}B) ---", "gray");
                LogReceived($"HEX: {hexStr}", "green");
                LogReceived($"ASCII: {asciiStr}", "green");

                Logger.Info($"Received ({receiveBytes.Length}B): {hexStr}");
            }
            catch (Exception ex)
            {
                LogReceived($"Receive decode error: {ex.Message}", "red");
                Logger.Error($"Receive decode error: {ex.Message}");
            }
        }

        private void LogSystem(string msg, string color)
        {
            OnSystemLog?.Invoke(msg, color);
        }

        private void LogReceived(string msg, string color)
        {
            OnReceivedLog?.Invoke(msg, color);
        }

        public void Dispose()
        {
            StopTcpService();
        }
    }
}

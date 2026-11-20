using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.Simulator.Web.WebFiscMaintSimulator
{
    public class FiscMaintSimulatorManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ServerSocketListener? _serverSocketListener;
        private string _serverIPAddr = "127.0.0.1";
        private int _serverListenerPort = 8100;
        private int _maxConnection = 10;
        private int _bufferSize = 2048;
        private int _maxTimeoutMilliseconds = 10000;

        // Blazor real-time log callback support
        private readonly List<Action<string>> _logCallbacks = new();

        public bool IsRunning { get; private set; }
        public string ServerIPAddr => _serverIPAddr;
        public int ServerListenerPort => _serverListenerPort;
        public int MaxConnection => _maxConnection;
        public int BufferSize => _bufferSize;
        public int MaxTimeoutMilliseconds => _maxTimeoutMilliseconds;

        /// <summary>Registers a Blazor page log callback for real-time UI updates.</summary>
        public void RegisterLogCallback(Action<string> callback)
        {
            lock (_logCallbacks) _logCallbacks.Add(callback);
        }

        /// <summary>Unregisters a previously registered log callback.</summary>
        public void UnregisterLogCallback(Action<string> callback)
        {
            lock (_logCallbacks) _logCallbacks.Remove(callback);
        }

        /// <summary>Notifies all registered Blazor pages of a new log message.</summary>
        private void NotifyLog(string message)
        {
            List<Action<string>> snapshot;
            lock (_logCallbacks) snapshot = new List<Action<string>>(_logCallbacks);
            foreach (var cb in snapshot)
            {
                try { cb(message); }
                catch { /* ignore disconnected pages */ }
            }
        }

        /// <summary>Public entry point for NLog redirector in Program.cs.</summary>
        public void NotifyLogPublic(string message) => NotifyLog(message);

        public FiscMaintSimulatorManager()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                LoadSystemParameter();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize FiscMaintSimulatorManager: " + ex.Message);
            }
        }

        public void LoadSystemParameter()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var hostName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-server-name");
                _serverIPAddr = CommonUtils.GetIPAddressStringByHostName(hostName, AddressFamily.InterNetwork);

                var portStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-listen-port");
                if (!string.IsNullOrEmpty(portStr))
                {
                    _serverListenerPort = Convert.ToInt32(portStr.Trim());
                }

                var maxConnStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-max-connection");
                if (!string.IsNullOrEmpty(maxConnStr))
                {
                    _maxConnection = Convert.ToInt32(maxConnStr.Trim());
                }

                var bufStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-buffer-size");
                if (!string.IsNullOrEmpty(bufStr))
                {
                    _bufferSize = Convert.ToInt32(bufStr.Trim());
                }

                var timeoutStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-max-timeout-milliseconds");
                if (!string.IsNullOrEmpty(timeoutStr))
                {
                    _maxTimeoutMilliseconds = Convert.ToInt32(timeoutStr.Trim());
                }

                Log.Info($"System parameter loaded. Server IP: {_serverIPAddr}, Port: {_serverListenerPort}, MaxConn: {_maxConnection}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LoadSystemParameter error: " + ex.Message);
                throw;
            }
        }

        public void StartService(int? port = null)
        {
            if (IsRunning)
            {
                Log.Warn("Service is already running.");
                return;
            }

            if (port.HasValue)
            {
                _serverListenerPort = port.Value;
            }

            try
            {
                var serverServiceEndPoint = new IPEndPoint(IPAddress.Any, _serverListenerPort);
                _serverSocketListener = new ServerSocketListener(serverServiceEndPoint, _maxConnection, _bufferSize);
                _serverSocketListener.Start();
                IsRunning = true;
                var startMsg = $"Server Service started at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, IP Address[{_serverIPAddr}], Listener Port[{_serverListenerPort}]";
                Log.Info(startMsg);
                NotifyLog($"[INFO] {startMsg}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to start server service: {ex.Message}");
                IsRunning = false;
                NotifyLog($"[ERROR] Failed to start service: {ex.Message}");
                throw;
            }
        }

        public void StopService()
        {
            if (!IsRunning || _serverSocketListener == null)
            {
                Log.Warn("Service is not running.");
                return;
            }

            try
            {
                _serverSocketListener.Stop();
                _serverSocketListener = null;
                IsRunning = false;
                var stopMsg = $"Server Service stopped at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                Log.Info(stopMsg);
                NotifyLog($"[INFO] {stopMsg}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to stop server service: {ex.Message}");
                NotifyLog($"[ERROR] Failed to stop service: {ex.Message}");
                throw;
            }
        }

        public void AutoStartIfConfigured()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var autoStart = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-auto-start-flag");
                if (autoStart != null && autoStart.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Info("Auto-starting FiscMaintSimulator service...");
                    NotifyLog("[SYSTEM] Auto-start flag detected. Starting FISC Maint service...");
                    StartService();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AutoStartIfConfigured error: " + ex.Message);
                NotifyLog($"[ERROR] AutoStart error: {ex.Message}");
            }
        }
    }
}

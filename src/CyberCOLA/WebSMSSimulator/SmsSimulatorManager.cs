using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NLog;
using Microsoft.Extensions.Configuration;

namespace Com.Cybersoft4u.Simulator.Web.WebSMSSimulator
{
    // ─── SMS Simulator Manager ──────────────────────────────────────────
    public class SmsSimulatorManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private SMSSocketListener? _listener;
        private readonly IConfiguration _config;
        private readonly List<Action<string>> _logCallbacks = new();

        public bool IsRunning => _listener?.IsRunning ?? false;
        public string ServerIp { get; private set; }
        public int Port { get; private set; }
        public int MaxConnection { get; private set; }
        public int BufferSize { get; private set; }
        public int TimeoutMs { get; private set; }

        public SmsSimulatorManager(IConfiguration config)
        {
            _config = config;
            ServerIp = _config.GetValue<string>("GeneralConfig:server-service-server-name") ?? "0.0.0.0";
            Port = _config.GetValue<int>("GeneralConfig:server-service-listen-port");
            if (Port == 0) Port = 10000;
            MaxConnection = _config.GetValue<int>("GeneralConfig:server-service-max-connection");
            if (MaxConnection == 0) MaxConnection = 10;
            BufferSize = _config.GetValue<int>("GeneralConfig:server-service-buffer-size");
            if (BufferSize == 0) BufferSize = 2048;
            TimeoutMs = _config.GetValue<int>("GeneralConfig:server-service-max-timeout-milliseconds");
            if (TimeoutMs == 0) TimeoutMs = 10000;
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

        // ─── Service Lifecycle ────────────────────────────────────────────
        public void Start(int port, int maxConnection, int bufferSize)
        {
            if (IsRunning) return;

            Port = port;
            MaxConnection = maxConnection;
            BufferSize = bufferSize;

            try
            {
                var ipAddr = IPAddress.Parse(ServerIp);
                var endPoint = new IPEndPoint(ipAddr, port);
                _listener = new SMSSocketListener(endPoint, maxConnection, bufferSize);
                _listener.Start();

                var startMsg = $"SMS Simulator TCP Server started successfully. IP: {ServerIp}, Port: {port}";
                Log.Info(startMsg);
                NotifyLog($"[INFO] {startMsg}");
            }
            catch (Exception ex)
            {
                var errMsg = $"Failed to start SMS Simulator TCP Server: {ex.Message}";
                Log.Error(ex, errMsg);
                NotifyLog($"[ERROR] {errMsg}");
                throw;
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            try
            {
                _listener?.Stop();
                _listener = null;

                var stopMsg = "SMS Simulator TCP Server stopped.";
                Log.Info(stopMsg);
                NotifyLog($"[INFO] {stopMsg}");
            }
            catch (Exception ex)
            {
                var errMsg = $"Failed to stop SMS Simulator TCP Server: {ex.Message}";
                Log.Error(ex, errMsg);
                NotifyLog($"[ERROR] {errMsg}");
                throw;
            }
        }
    }
}

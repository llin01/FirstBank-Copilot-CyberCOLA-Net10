using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.ServiceProvider.QRCodeServerService;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using Logger = Com.Cybersoft4u.Utils.Logger;
using LogLevel = Com.Cybersoft4u.Utils.LogLevel;

namespace Com.Cybersoft4u.Service.NCCC.CyberAuthNCCC
{
    /// <summary>
    /// Background worker service for CyberAuthNCCC that manages NCCC authorization services.
    /// Initializes and coordinates multiple services including main authorization, connection monitoring, routing, and QR code services.
    /// </summary>
    public class CyberAuthNCCCWorker : BackgroundService
    {
        private readonly ILogger<CyberAuthNCCCWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberAuthNCCC";

        /// <summary>
        /// Initializes a new instance of the CyberAuthNCCCWorker class.
        /// </summary>
        /// <param name="logger">The logger instance for dependency injection.</param>
        public CyberAuthNCCCWorker(ILogger<CyberAuthNCCCWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Initialize NLog uniformly
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the background service startup sequence, initializing all configured services including:
        /// authorization service, connection monitoring, routing service, QR code server, and QR code client.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the service execution.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Start main authorization service (NCCC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthNCCC service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberAuthNCCC = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthNCCC();
                CyberAuthNCCCResource.CyberAuthNCCC = cyberAuthNCCC;
                
                // P0 Fix: Add startup timeout protection (2 minutes)
                try
                {
                    using (var startupCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
                    {
                        startupCts.CancelAfter(TimeSpan.FromMinutes(2));
                        await Task.Run(() => cyberAuthNCCC.Start(), startupCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    _appLogger.Fatal("CyberAuthNCCC service startup exceeded 2-minute timeout. Aborting startup.");
                    throw;
                }
                catch (Exception ex)
                {
                    _appLogger.Fatal($"CyberAuthNCCC service startup failed with exception: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _appLogger.Info($"CyberAuthNCCC service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Start connection monitoring (NCCC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var remoteIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-target-ip").Trim(), AddressFamily.InterNetwork);
                var remotePort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-target-port").Trim();
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), Convert.ToInt32(remotePort));

                var localHostName = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-local-hostname").Trim();
                var localIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-local-ip").Trim(), AddressFamily.InterNetwork);
                var localPort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-local-port").Trim();
                var localEndPoint = new IPEndPoint(IPAddress.Parse(localIP), Convert.ToInt32(localPort));
                var dummyEcho = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-dummy-echo-flag").Default("F").ToUpper().Equals("TRUE");

                _appLogger.Info($"ClientAuthClientMonitorService (NCCC) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthMonitorService = new ClientAuthClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.NCCC, dummyEcho);
                clientAuthMonitorService.EnableLogger("ClientAuthClientMonitor", LogLevel.Debug);
                CyberAuthNCCCResource.ClientAuthClientMonitorService = clientAuthMonitorService;
                clientAuthMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (NCCC) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Start routing service (NCCC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var localIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-local-ip").Trim(), AddressFamily.InterNetwork);
                var localPort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-listener-port").Trim();
                var backlog = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-backlog").Trim();

                _appLogger.Info($"ClientAuthRouterService (NCCC) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthRouterService = new ClientAuthRouterService(SystemConstantData.CommonData.ClientAuthNCCCRouterServiceName,
                                                                          localIP,
                                                                          Convert.ToInt32(localPort),
                                                                          Convert.ToInt32(backlog));
                CyberAuthNCCCResource.ClientAuthRouterService = clientAuthRouterService;
                clientAuthRouterService.StartService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (NCCC) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 4. Start QRCode server (Socket Listener)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeServerServiceConfigSectionName, "server-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var serverIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeServerServiceConfigSectionName, "server-service-server-ip").Trim(), AddressFamily.InterNetwork);
                var serverPort = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeServerServiceConfigSectionName, "server-service-listen-port").Trim());
                var maxConn = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeServerServiceConfigSectionName, "server-service-max-connection").Trim());
                var bufferSize = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeServerServiceConfigSectionName, "server-service-buffer-size").Trim());

                _appLogger.Info($"CyberAuthSocketListener (QRCode) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                var socketListener = new SocketListener(serverEndPoint, maxConn, bufferSize);
                CyberAuthNCCCResource.CyberAuthSocketListener = socketListener;
                socketListener.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthSocketListener (QRCode) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 5. Start QRCode client (NCCC QRCode)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeClientConfigSectionName, "nccc-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthNCCCQRCode service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberAuthNcccQRCode = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthNCCCQRCode();
                CyberAuthNCCCResource.CyberAuthNcccQRCode = cyberAuthNcccQRCode;
                cyberAuthNcccQRCode.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthNCCCQRCode service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // Keep running until cancellation signal is issued
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Stops the background service and gracefully shuts down all running services in reverse order of startup.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token for the stop operation.</param>
        /// <returns>A Task representing the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Stop main authorization service (NCCC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthNCCC service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthNCCCResource.CyberAuthNCCC?.Stop();
            }
            else
            {
                _appLogger.Info($"CyberAuthNCCC service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Stop connection monitoring (NCCC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthClientMonitorService (NCCC) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthNCCCResource.ClientAuthClientMonitorService?.Stop();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (NCCC) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Stop routing service (NCCC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthRouterService (NCCC) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthNCCCResource.ClientAuthRouterService?.StopService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (NCCC) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 4. Stop QRCode server (Socket Listener)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeServerServiceConfigSectionName, "server-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthSocketListener (QRCode) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthNCCCResource.CyberAuthSocketListener?.Stop();
            }
            else
            {
                _appLogger.Info($"CyberAuthSocketListener (QRCode) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 5. Stop QRCode client (NCCC QRCode)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.QRCodeClientConfigSectionName, "nccc-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthNCCCQRCode service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthNCCCResource.CyberAuthNcccQRCode?.Stop();
            }
            else
            {
                _appLogger.Info($"CyberAuthNCCCQRCode service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

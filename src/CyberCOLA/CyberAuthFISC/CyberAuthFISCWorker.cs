using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.ServiceProvider.IDVerifyServerService;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using Logger = Com.Cybersoft4u.Utils.Logger;
using LogLevel = Com.Cybersoft4u.Utils.LogLevel;

namespace Com.Cybersoft4u.Service.FISC.CyberAuthFISC
{
    /// <summary>
    /// Background worker service that manages the FISC (Financial Information Service Co., Ltd.) authorization system.
    /// Coordinates multiple services including authorization processing, connection monitoring, routing, and ID verification.
    /// </summary>
    public class CyberAuthFISCWorker : BackgroundService
    {
        private readonly ILogger<CyberAuthFISCWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberAuthFISC";

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberAuthFISCWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberAuthFISCWorker(ILogger<CyberAuthFISCWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Initialize NLog uniformly
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the FISC authorization worker service startup sequence.
        /// Starts multiple services based on configuration: main authorization service, connection monitoring,
        /// router service, ID verification server socket listener, and ID verification client service.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // Start main authorization service
            _appLogger.Info($"Data[{_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag")}]");
            _appLogger.Info($"Data[{_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper()}]");
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthFISC service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                // Use fully qualified name to avoid namespace conflicts
                var cyberAuthFISC = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthFISC();
                CyberAuthFISCResource.CyberAuthFISC = cyberAuthFISC;
                cyberAuthFISC.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthFISC service no start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // Start connection monitoring
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

                _appLogger.Info($"ClientAuthClientMonitorService service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthMonitorService = new ClientAuthClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.FISC, dummyEcho);
                clientAuthMonitorService.EnableLogger("ClientAuthClientMonitor", LogLevel.Debug);
                CyberAuthFISCResource.ClientAuthClientMonitorService = clientAuthMonitorService;
                clientAuthMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService service no start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // Start router service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var localIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-local-ip").Trim(), AddressFamily.InterNetwork);
                var localPort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-listener-port").Trim();
                var backlog = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-backlog").Trim();

                _appLogger.Info($"ClientAuthRouterService service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthRouterService = new ClientAuthRouterService(SystemConstantData.CommonData.ClientAuthFISCRouterServiceName,
                                                                          localIP,
                                                                          Convert.ToInt32(localPort),
                                                                          Convert.ToInt32(backlog));
                CyberAuthFISCResource.ClientAuthRouterService = clientAuthRouterService;
                clientAuthRouterService.StartService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService service no start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // Start ID verification server (Socket Listener)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyServerServiceConfigSectionName, "server-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var serverIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyServerServiceConfigSectionName, "server-service-server-ip").Trim(), AddressFamily.InterNetwork);
                var serverPort = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyServerServiceConfigSectionName, "server-service-listen-port").Trim());
                var maxConn = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyServerServiceConfigSectionName, "server-service-max-connection").Trim());
                var bufferSize = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyServerServiceConfigSectionName, "server-service-buffer-size").Trim());

                _appLogger.Info($"CyberAuthSocketListener service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                var socketListener = new SocketListener(serverEndPoint, maxConn, bufferSize);
                CyberAuthFISCResource.CyberAuthSocketListener = socketListener;
                socketListener.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthSocketListener service no start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }
            // Start ID verification client (FISC ID Verify)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthFISCIDVerify service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                // Use fully qualified name
                var cyberAuthFiscIDVerify = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthFISCIDVerify();
                CyberAuthFISCIDVerifyResource.CyberAuthFISCIDVerify = cyberAuthFiscIDVerify;
                cyberAuthFiscIDVerify.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthFISCIDVerify service no start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // Keep running until cancellation signal is received
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops all FISC authorization services.
        /// Stops the authorization service, connection monitor, router service, socket listener, and ID verification service in sequence.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            CyberAuthFISCResource.CyberAuthFISC?.Stop();
            CyberAuthFISCResource.ClientAuthClientMonitorService?.Stop();
            CyberAuthFISCResource.ClientAuthRouterService?.StopService();
            CyberAuthFISCResource.CyberAuthSocketListener?.Stop();
            CyberAuthFISCIDVerifyResource.CyberAuthFISCIDVerify?.Stop();

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

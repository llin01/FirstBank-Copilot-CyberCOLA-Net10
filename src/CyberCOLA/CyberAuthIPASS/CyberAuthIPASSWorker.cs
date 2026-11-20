using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using Logger = Com.Cybersoft4u.Utils.Logger;
using LogLevel = Com.Cybersoft4u.Utils.LogLevel;

namespace Com.Cybersoft4u.Service.IPASS.CyberAuthIPASS
{
    /// <summary>
    /// Background worker service that manages the iPASS (transit card) authorization system.
    /// Coordinates authorization processing, connection monitoring, and routing services for iPASS transactions.
    /// </summary>
    public class CyberAuthIPASSWorker : BackgroundService
    {
        private readonly ILogger<CyberAuthIPASSWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberAuthIPASS";

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberAuthIPASSWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberAuthIPASSWorker(ILogger<CyberAuthIPASSWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Initialize NLog
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the iPASS authorization worker service startup sequence.
        /// Starts the iPASS authorization service, connection monitoring, and router service based on configuration.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Start main authorization service (IPASS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthIPASS service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberAuthIPASS = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthIPASS();
                CyberAuthIPASSResource.CyberAuthIPASS = cyberAuthIPASS;
                cyberAuthIPASS.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthIPASS service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Start connection monitoring service (IPASS)
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

                _appLogger.Info($"ClientAuthClientMonitorService (IPASS) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthMonitorService = new ClientAuthClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.IPASS, dummyEcho);
                clientAuthMonitorService.EnableLogger("ClientAuthClientMonitor", LogLevel.Debug);
                CyberAuthIPASSResource.ClientAuthClientMonitorService = clientAuthMonitorService;
                clientAuthMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (IPASS) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Start router service (IPASS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var localIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-local-ip").Trim(), AddressFamily.InterNetwork);
                var localPort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-listener-port").Trim();
                var backlog = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-backlog").Trim();

                _appLogger.Info($"ClientAuthRouterService (IPASS) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthRouterService = new ClientAuthRouterService(SystemConstantData.CommonData.ClientAuthIPASSRouterServiceName,
                                                                          localIP,
                                                                          Convert.ToInt32(localPort),
                                                                          Convert.ToInt32(backlog));
                CyberAuthIPASSResource.ClientAuthRouterService = clientAuthRouterService;
                clientAuthRouterService.StartService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (IPASS) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops all iPASS authorization services.
        /// Stops the authorization service, connection monitor, and router service in sequence based on configuration.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Stop main authorization service (IPASS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthIPASS service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthIPASSResource.CyberAuthIPASS?.Stop();
            }
            else
            {
                _appLogger.Info($"CyberAuthIPASS service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Stop connection monitoring service (IPASS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthClientMonitorService (IPASS) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthIPASSResource.ClientAuthClientMonitorService?.Stop();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (IPASS) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Stop router service (IPASS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthRouterService (IPASS) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthIPASSResource.ClientAuthRouterService?.StopService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (IPASS) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

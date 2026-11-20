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

namespace Com.Cybersoft4u.Service.ICASH.CyberAuthICASH
{
    /// <summary>
    /// Background worker service that manages the ICASH (iCash stored-value card) authorization system.
    /// Coordinates authorization processing, connection monitoring, and routing services for ICASH transactions.
    /// </summary>
    public class CyberAuthICASHWorker : BackgroundService
    {
        private readonly ILogger<CyberAuthICASHWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberAuthICASH";

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberAuthICASHWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberAuthICASHWorker(ILogger<CyberAuthICASHWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Initialize NLog uniformly
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the ICASH authorization worker service startup sequence.
        /// Starts the ICASH authorization service, connection monitoring, and router service based on configuration.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Start main authorization service (ICASH)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthICASH service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberAuthICASH = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthICASH();
                CyberAuthICASHResource.CyberAuthICASH = cyberAuthICASH;
                cyberAuthICASH.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthICASH service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Start connection monitoring (ICASH)
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

                _appLogger.Info($"ClientAuthClientMonitorService (ICASH) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthMonitorService = new ClientAuthClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.ICASH, dummyEcho);
                clientAuthMonitorService.EnableLogger("ClientAuthClientMonitor", LogLevel.Debug);
                CyberAuthICASHResource.ClientAuthClientMonitorService = clientAuthMonitorService;
                clientAuthMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (ICASH) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Start routing service (ICASH)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var localIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-local-ip").Trim(), AddressFamily.InterNetwork);
                var localPort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-listener-port").Trim();
                var backlog = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-backlog").Trim();

                _appLogger.Info($"ClientAuthRouterService (ICASH) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthRouterService = new ClientAuthRouterService(SystemConstantData.CommonData.ClientAuthICASHRouterServiceName,
                                                                          localIP,
                                                                          Convert.ToInt32(localPort),
                                                                          Convert.ToInt32(backlog));
                CyberAuthICASHResource.ClientAuthRouterService = clientAuthRouterService;
                clientAuthRouterService.StartService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (ICASH) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops all ICASH authorization services.
        /// Stops the authorization service, connection monitor, and router service in sequence based on configuration.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Stop main authorization service (ICASH)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthICASH service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthICASHResource.CyberAuthICASH?.Stop();
            }
            else
            {
                _appLogger.Info($"CyberAuthICASH service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Stop connection monitoring (ICASH)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthClientMonitorService (ICASH) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthICASHResource.ClientAuthClientMonitorService?.Stop();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (ICASH) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Stop routing service (ICASH)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthRouterService (ICASH) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthICASHResource.ClientAuthRouterService?.StopService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (ICASH) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

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

namespace Com.Cybersoft4u.Service.SVCS.CyberAuthSVCS
{
    /// <summary>
    /// Background worker service that manages the SVCS (Stored Value Card System) authorization system.
    /// Coordinates authorization processing, connection monitoring, and routing services for SVCS transactions.
    /// </summary>
    public class CyberAuthSVCSWorker : BackgroundService
    {
        private readonly ILogger<CyberAuthSVCSWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberAuthSVCS";

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberAuthSVCSWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberAuthSVCSWorker(ILogger<CyberAuthSVCSWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the SVCS authorization worker service startup sequence.
        /// Starts the SVCS authorization service, connection monitoring, and router service based on configuration.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Start main authorization service (SVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthSVCS service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberAuthSVCS = new Com.Cybersoft4u.ServiceProvider.ClientAuth.CyberAuthSVCS();
                CyberAuthSVCSResource.CyberAuthSVCS = cyberAuthSVCS;
                cyberAuthSVCS.Start();
            }
            else
            {
                _appLogger.Info($"CyberAuthSVCS service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Start connection monitoring service (SVCS)
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

                _appLogger.Info($"ClientAuthClientMonitorService (SVCS) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthMonitorService = new ClientAuthClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.SVCS, dummyEcho);
                clientAuthMonitorService.EnableLogger("ClientAuthClientMonitor", LogLevel.Debug);
                CyberAuthSVCSResource.ClientAuthClientMonitorService = clientAuthMonitorService;
                clientAuthMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (SVCS) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Start router service (SVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                var localIP = CommonUtils.GetIPAddressStringByHostName(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-local-ip").Trim(), AddressFamily.InterNetwork);
                var localPort = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-listener-port").Trim();
                var backlog = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-backlog").Trim();

                _appLogger.Info($"ClientAuthRouterService (SVCS) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientAuthRouterService = new ClientAuthRouterService(SystemConstantData.CommonData.ClientAuthSVCSRouterServiceName,
                                                                          localIP,
                                                                          Convert.ToInt32(localPort),
                                                                          Convert.ToInt32(backlog));
                CyberAuthSVCSResource.ClientAuthRouterService = clientAuthRouterService;
                clientAuthRouterService.StartService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (SVCS) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops all SVCS authorization services.
        /// Stops the authorization service, connection monitor, and router service in sequence based on configuration.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Stop main authorization service (SVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"CyberAuthSVCS service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthSVCSResource.CyberAuthSVCS?.Stop();
            }
            else
            {
                _appLogger.Info($"CyberAuthSVCS service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Stop connection monitoring service (SVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthClientMonitorService (SVCS) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthSVCSResource.ClientAuthClientMonitorService?.Stop();
            }
            else
            {
                _appLogger.Info($"ClientAuthClientMonitorService (SVCS) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Stop router service (SVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientAuthRouterConfigSectionName, "clientauth-router-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"ClientAuthRouterService (SVCS) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberAuthSVCSResource.ClientAuthRouterService?.StopService();
            }
            else
            {
                _appLogger.Info($"ClientAuthRouterService (SVCS) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

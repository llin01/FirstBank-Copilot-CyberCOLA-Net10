using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using Logger = Com.Cybersoft4u.Utils.Logger;
using LogLevel = Com.Cybersoft4u.Utils.LogLevel;

namespace Com.Cybersoft4u.Service.SVCS.CyberGWSVCS
{
    /// <summary>
    /// Background worker service that manages the SVCS gateway system.
    /// Coordinates SVCS COLA service, main gateway service, and gateway client monitoring for routing transactions to the SVCS network.
    /// </summary>
    public class CyberGWSVCSWorker : BackgroundService
    {
        private readonly ILogger<CyberGWSVCSWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberGWSVCS";

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberGWSVCSWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberGWSVCSWorker(ILogger<CyberGWSVCSWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Unified NLog initialization
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the SVCS gateway worker service startup sequence.
        /// Starts the SVCS COLA service, main gateway service, and client monitoring service based on configuration.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Gateway Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Start SVCS COLA Service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"SvcsColaService start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var svcsColaService = new SvcsColaService();
                CyberGWSVCSResource.SvcsColaService = svcsColaService;
                svcsColaService.Start();
            }
            else
            {
                _appLogger.Info($"SvcsColaService service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Start Main Gateway Service (CyberGWSVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"{ServiceName} Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberGWSVCS = new Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.CyberGWSVCS();
                CyberGWSVCSResource.CyberGWSVCS = cyberGWSVCS;
                cyberGWSVCS.Start();
            }
            else
            {
                _appLogger.Info($"{ServiceName} service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Start Gateway Client Monitor Service
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

                _appLogger.Info($"GatewayClientMonitorService (SVCS) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientMonitorService = new GatewayClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.SVCS, dummyEcho);
                clientMonitorService.EnableLogger("GatewayClientMonitor", LogLevel.Debug);
                CyberGWSVCSResource.GatewayClientMonitorService = clientMonitorService;
                clientMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"GatewayClientMonitorService (SVCS) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // Keep running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops all SVCS gateway services.
        /// Stops the COLA service, main gateway service, and client monitor in sequence based on configuration.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Stop SVCS COLA Service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"SvcsColaService stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberGWSVCSResource.SvcsColaService?.Stop();
            }
            else
            {
                _appLogger.Info($"SvcsColaService service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Stop Main Gateway Service (CyberGWSVCS)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"{ServiceName} Service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberGWSVCSResource.CyberGWSVCS?.Stop();
            }
            else
            {
                _appLogger.Info($"{ServiceName} service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Stop Gateway Client Monitor Service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"GatewayClientMonitorService (SVCS) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberGWSVCSResource.GatewayClientMonitorService?.Stop();
            }
            else
            {
                _appLogger.Info($"GatewayClientMonitorService (SVCS) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

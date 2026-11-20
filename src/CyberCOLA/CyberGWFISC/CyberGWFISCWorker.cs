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

namespace Com.Cybersoft4u.Service.FISC.CyberGWFISC
{
    /// <summary>
    /// Background worker service that manages the FISC gateway system.
    /// Coordinates FISC COLA service, main gateway service, and gateway client monitoring for routing transactions to the FISC network.
    /// </summary>
    public class CyberGWFISCWorker : BackgroundService
    {
        private readonly ILogger<CyberGWFISCWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "CyberGWFISC";

        /// <summary>
        /// Initializes a new instance of the <see cref="CyberGWFISCWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public CyberGWFISCWorker(ILogger<CyberGWFISCWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Unified NLog initialization
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the FISC gateway worker service startup sequence.
        /// Starts the FISC COLA service, main gateway service, and client monitoring service based on configuration.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Gateway Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Start FISC COLA Service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"FiscColaService start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var fiscColaService = new FiscColaService();
                CyberGWFISCResource.FiscColaService = fiscColaService;
                fiscColaService.Start();
            }
            else
            {
                _appLogger.Info($"FiscColaService service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Start Main Gateway Service (CyberGWFISC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.FiscServiceConfigSectionName, "fisc-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"{ServiceName} Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var cyberGWFISC = new Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.CyberGWFISC();
                CyberGWFISCResource.CyberGWFISC = cyberGWFISC;
                cyberGWFISC.Start();
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

                _appLogger.Info($"GatewayClientMonitorService (FISC) service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var clientMonitorService = new GatewayClientMonitorService(localHostName, remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.FISC, dummyEcho);
                clientMonitorService.EnableLogger("GatewayClientMonitor", LogLevel.Debug);
                CyberGWFISCResource.GatewayClientMonitorService = clientMonitorService;
                clientMonitorService.Start();
            }
            else
            {
                _appLogger.Info($"GatewayClientMonitorService (FISC) service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // Keep running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Gracefully stops all FISC gateway services.
        /// Stops the COLA service, main gateway service, and client monitor in sequence based on configuration.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            // 1. Stop FISC COLA Service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"FiscColaService stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberGWFISCResource.FiscColaService?.Stop();
            }
            else
            {
                _appLogger.Info($"FiscColaService service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 2. Stop Main Gateway Service (CyberGWFISC)
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.FiscServiceConfigSectionName, "fisc-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"{ServiceName} Service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberGWFISCResource.CyberGWFISC?.Stop();
            }
            else
            {
                _appLogger.Info($"{ServiceName} service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            // 3. Stop Gateway Client Monitor Service
            if (_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ClientMonitorConfigSectionName, "client-monitor-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE"))
            {
                _appLogger.Info($"GatewayClientMonitorService (FISC) service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                CyberGWFISCResource.GatewayClientMonitorService?.Stop();
            }
            else
            {
                _appLogger.Info($"GatewayClientMonitorService (FISC) service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

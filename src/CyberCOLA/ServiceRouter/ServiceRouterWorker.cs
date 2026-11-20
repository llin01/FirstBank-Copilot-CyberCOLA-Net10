using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Serialization;
using Logger = Com.Cybersoft4u.Utils.Logger;

namespace Com.Cybersoft4u.Service.Router.ServiceRouter
{
    /// <summary>
    /// Background worker service that manages service routing infrastructure.
    /// Coordinates server-side and client-side router services for message routing between authorization systems.
    /// Loads routing configuration from XML and starts both ServerRouter and ClientRouter services.
    /// </summary>
    public class ServiceRouterWorker : BackgroundService
    {
        private readonly ILogger<ServiceRouterWorker> _logger;
        private readonly Logger _appLogger;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;
        private const string ServiceName = "ServiceRouter";
        private ServiceRouterInfo? _serviceRouterInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceRouterWorker"/> class.
        /// Sets up configuration loading and initializes the NLog logging infrastructure.
        /// </summary>
        /// <param name="logger">The logger instance provided by dependency injection for structured logging.</param>
        public ServiceRouterWorker(ILogger<ServiceRouterWorker> logger)
        {
            _logger = logger;
            _configurationLoaderRoot = new ConfigurationLoaderRoot();

            // Unified NLog initialization
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _appLogger = LoggerAdapter.GetLogger($"LoggerHandler[{ServiceName}]");
        }

        /// <summary>
        /// Executes the service router worker startup sequence.
        /// Loads router configuration from XML file, then starts ServerRouter and ClientRouter services based on configuration.
        /// Runs continuously until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the service should stop.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Unified Service Name({ServiceName}) execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            try
            {
                // Load Router Configuration
                var xmlFilePath = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "service-router-info-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(ServiceRouterInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _serviceRouterInfo = (ServiceRouterInfo?)serializer.Deserialize(reader);
                }

                if (_serviceRouterInfo == null)
                {
                    _appLogger.Fatal($"Create ServiceRouterInfo Error, Service Name({ServiceName}) not process execute at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return;
                }

                // 1. Start ServerRouter Service
                if (Convert.ToBoolean(_serviceRouterInfo.ServerServiceInfo.AutoStartFlag))
                {
                    _appLogger.Info($"ServerRouter service starting at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    var serverRouterService = new ServerRouterService(
                        CommonUtils.GetIPAddressStringByHostName(_serviceRouterInfo.ServerServiceInfo.ServerLocalIP.Trim(), AddressFamily.InterNetwork),
                        Convert.ToInt32(_serviceRouterInfo.ServerServiceInfo.ListenerPort.Trim()),
                        Convert.ToInt32(_serviceRouterInfo.ServerServiceInfo.BackLog.Trim()),
                        _serviceRouterInfo.ServerServiceInfo.ClientAuthServiceRemotePorts.Trim().Split(',').ToList());

                    RouterServiceResource.ServerRouterService = serverRouterService;
                    serverRouterService.StartService();
                }
                else
                {
                    _appLogger.Info($"ServerRouter service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }

                // 2. Start ClientRouter Service
                if (Convert.ToBoolean(_serviceRouterInfo.ClientServiceInfo.AutoStartFlag))
                {
                    _appLogger.Info($"ClientRouter service starting at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    var clientRouterService = new ClientRouterService(
                        CommonUtils.GetIPAddressStringByHostName(_serviceRouterInfo.ClientServiceInfo.ServerLocalIP.Trim(), AddressFamily.InterNetwork),
                        Convert.ToInt32(_serviceRouterInfo.ClientServiceInfo.ListenerPort.Trim()),
                        Convert.ToInt32(_serviceRouterInfo.ClientServiceInfo.BackLog.Trim()),
                        _serviceRouterInfo.ServerServiceInfo.ClientAuthServiceRemotePorts.Trim().Split(',').ToList());

                    RouterServiceResource.ClientRouterService = clientRouterService;
                    clientRouterService.StartService();
                }
                else
                {
                    _appLogger.Info($"ClientRouter service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }

                _appLogger.Info($"Service Name({ServiceName}) start completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

                // Keep running until cancellation is requested
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _appLogger.Fatal($"Service Name({ServiceName}) Exception: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gracefully stops all router services.
        /// Stops both ServerRouter and ClientRouter services in sequence based on configuration.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to signal when the stop operation should complete.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _appLogger.Info($"Service Name({ServiceName}) stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            if (_serviceRouterInfo != null)
            {
                // 1. Stop ServerRouter Service
                if (Convert.ToBoolean(_serviceRouterInfo.ServerServiceInfo.AutoStartFlag))
                {
                    _appLogger.Info($"ServerRouter service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    RouterServiceResource.ServerRouterService?.StopService();
                }
                else
                {
                    _appLogger.Info($"ServerRouter service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }

                // 2. Stop ClientRouter Service
                if (Convert.ToBoolean(_serviceRouterInfo.ClientServiceInfo.AutoStartFlag))
                {
                    _appLogger.Info($"ClientRouter service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    RouterServiceResource.ClientRouterService?.StopService();
                }
                else
                {
                    _appLogger.Info($"ClientRouter service not stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }
            }

            _appLogger.Info($"Service Name({ServiceName}) stop completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            await base.StopAsync(stoppingToken);
        }
    }
}

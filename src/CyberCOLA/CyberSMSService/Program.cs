using Com.Cybersoft4u.Service.SMS.CyberSMSService;

IHost host = Host.CreateDefaultBuilder(args)
                 .UseWindowsService(options =>
                 {
                     options.ServiceName = "FirstBank CyberSMS Short Message Service";
                 })
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                 })
                 .ConfigureServices(services =>
                 {
                     services.AddHostedService<CyberSMSWorker>();
                 })
                 .Build();

await host.RunAsync();

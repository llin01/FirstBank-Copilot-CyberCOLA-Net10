using Com.Cybersoft4u.Service.SMS.CyberSMSDaemon;

IHost host = Host.CreateDefaultBuilder(args)
                 .UseSystemd()
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

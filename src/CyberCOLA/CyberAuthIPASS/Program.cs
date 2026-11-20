/**
 * CyberAuthIPASS Unified Authorization Service Entry Point
 * 
 * This project adopts the modern .NET Generic Host architecture. 
 * A single project supports the following three execution modes:
 * 
 * 1. Console Mode (For development and debugging):
 *    - Execution: Directly execute the compiled .exe file or use the `dotnet run` command.
 *    - Description: The program will output logs directly to the console. Press `Ctrl + C` to gracefully shut down the service.
 * 
 * 2. Windows Service Mode (Production deployment - Windows):
 *    - Installation: Run the following command with administrator privileges:
 *      sc create CyberAuthIPASS binPath= "C:\path\to\CyberAuthIPASS.exe" start= auto
 *    - Description: The program automatically detects the Windows Service environment and integrates with the Service Control Manager (SCM).
 * 
 * 3. Linux Daemon Mode (Production deployment - Linux/Systemd):
 *    - Deployment: Create a configuration file at `/etc/systemd/system/cyberauth-ipass.service` with the following content:
 *      [Unit]
 *      Description=CyberAuthIPASS Authorization Service
 *      [Service]
 *      Type=notify
 *      ExecStart=/usr/bin/dotnet /path/to/CyberAuthIPASS.dll
 *    - Run publish to create linux-x64: dotnet publish -c Release -r linux-x64 --self-contained false
 *    - Description: Supports Linux systemd startup notifications and signal handling via `builder.Services.AddSystemd()`.
 */
using Com.Cybersoft4u.Service.IPASS.CyberAuthIPASS;
using System.Text;

// Register support for additional encodings such as Big5
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = Host.CreateApplicationBuilder(args);

// Configure log output to Console (very useful during development)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 1. Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CyberAuthIPASS Authorization Service";
});

// 2. Add Linux Systemd support
builder.Services.AddSystemd();

// 3. Register the core IPASS background Worker
builder.Services.AddHostedService<CyberAuthIPASSWorker>();

// 4. Configure background service exception behavior: 
//    Ignore exceptions to prevent the entire Host from crashing, allowing the Worker to handle retries internally.
builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

var host = builder.Build();

// Start the main program asynchronously
await host.RunAsync();

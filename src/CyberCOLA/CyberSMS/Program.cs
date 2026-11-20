/**
 * CyberSMS Unified Service Entry Point
 * 
 * This project adopts the modern .NET Generic Host architecture. 
 * A single project supports the following three execution modes:
 * 
 * 1. Console Mode (For development/debugging)
 * 2. Windows Service Mode (For production on Windows)
 * 3. Linux Daemon Mode (For production on Linux via Systemd)
 */

using Com.Cybersoft4u.Service.SMS.CyberSMS;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);

// Enable cross-platform support for Big5 and other traditional encodings
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Core Background Service
builder.Services.AddHostedService<CyberSMSWorker>();

// Infrastructure Support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CyberSMS";
});

builder.Services.AddSystemd();

// Global exception handling configuration
builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

var host = builder.Build();

// Start the main program asynchronously
await host.RunAsync();

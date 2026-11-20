using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Config;
using NLog.Targets;
using Com.Cybersoft4u.Simulator.Web.WebETicketClientSimulator;

var baseDir = AppContext.BaseDirectory;
string? wwwrootPath = Path.Combine(baseDir, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    var current = new DirectoryInfo(baseDir);
    for (int i = 0; i < 5; i++)
    {
        current = current?.Parent;
        if (current == null) break;
        var checkPath = Path.Combine(current.FullName, "wwwroot");
        if (Directory.Exists(checkPath))
        {
            wwwrootPath = checkPath;
            break;
        }
    }
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = baseDir,
    WebRootPath = Directory.Exists(wwwrootPath) ? wwwrootPath : null
});

// Enable static web assets (like blazor.web.js) in non-Development environments
builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register ETicketSimulatorManager as Singleton
builder.Services.AddSingleton<ETicketSimulatorManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Setup NLog redirection to Blazor Broadcaster
SetupNLogRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Auto-start UDP server from config
var mgr = app.Services.GetRequiredService<ETicketSimulatorManager>();
mgr.StartUDPFromConfig();

app.Run();

void SetupNLogRedirection()
{
    var config = LogManager.Configuration ?? new LoggingConfiguration();
    var customTarget = new MethodCallTarget("webConsole", (logEvent, parms) =>
    {
        ETicketLogBroadcaster.LogSystem($"[{logEvent.Level.Name.ToUpper()}] {logEvent.FormattedMessage}");
    });
    config.AddTarget("webConsole", customTarget);
    config.AddRuleForAllLevels(customTarget);
    LogManager.Configuration = config;
}

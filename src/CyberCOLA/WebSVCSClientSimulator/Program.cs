using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Com.Cybersoft4u.Simulator.Web.WebSVCSClientSimulator;
using NLog.Config;
using NLog.Targets;

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

// Register Singleton Manager
builder.Services.AddSingleton<SVCSClientSimulatorManager>();

// Redirect NLog outputs to dual Blazor Log Broadcaster
var nlogTarget = new MethodCallTarget("ETicketBlazorTarget", (logEvent, parms) =>
{
    var formattedMsg = logEvent.FormattedMessage;
    // Route logs to system connection logs (Message 2)
    ETicketLogBroadcaster.LogSystem(formattedMsg);
});
NLog.LogManager.Configuration?.AddTarget("ETicketBlazorTarget", nlogTarget);
NLog.LogManager.Configuration?.LoggingRules.Insert(0, new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, nlogTarget));
NLog.LogManager.ReconfigExistingLoggers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

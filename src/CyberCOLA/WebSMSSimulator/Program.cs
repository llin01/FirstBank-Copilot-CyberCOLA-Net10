using Microsoft.AspNetCore.Hosting;
using System.IO;
using Com.Cybersoft4u.Simulator.Web.WebSMSSimulator;
using NLog;
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

// Register SmsSimulatorManager as Singleton
builder.Services.AddSingleton<SmsSimulatorManager>();

// Add Blazor Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Setup NLog redirection to Manager's NotifyLog
SetupNLogRedirection(app);

app.UseStaticFiles();
app.UseAntiforgery();

// Map Blazor Hub and Razor Components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Auto-start if configured
var mgr = app.Services.GetRequiredService<SmsSimulatorManager>();
var autoStart = builder.Configuration.GetValue<string>("GeneralConfig:server-service-auto-start-flag") ?? "true";
if (autoStart.Equals("true", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        mgr.Start(mgr.Port, mgr.MaxConnection, mgr.BufferSize);
    }
    catch (Exception ex)
    {
        LogManager.GetCurrentClassLogger().Error(ex, "Failed to auto-start SMS Simulator service");
    }
}

app.Run();

// Redirect NLog output to Manager.NotifyLog for browser-side log display
static void SetupNLogRedirection(WebApplication app)
{
    var manager = app.Services.GetRequiredService<SmsSimulatorManager>();
    var config = LogManager.Configuration ?? new LoggingConfiguration();
    var customTarget = new MethodCallTarget("blazorConsole", (logEvent, _) =>
    {
        manager.NotifyLogPublic($"[{logEvent.Level.Name.ToUpper()}] {logEvent.FormattedMessage}");
    });
    config.AddTarget("blazorConsole", customTarget);
    config.AddRuleForAllLevels(customTarget);
    LogManager.Configuration = config;
}

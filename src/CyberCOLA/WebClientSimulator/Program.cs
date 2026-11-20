using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Collections.Concurrent;
using System.Net;
using NLog;
using NLog.Config;
using NLog.Targets;
using Com.Cybersoft4u.Simulator.Web.WebClientSimulator;

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

// Register ClientSimulatorManager as Singleton
builder.Services.AddSingleton<ClientSimulatorManager>();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Setup NLog redirection to Web Console
SetupNLogRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map Blazor Hub and Razor Components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

void SetupNLogRedirection()
{
    var config = LogManager.Configuration ?? new LoggingConfiguration();
    var customTarget = new MethodCallTarget("webConsole", (logEvent, parms) =>
    {
        ClientLogBroadcaster.Log($"[{logEvent.Level.Name.ToUpper()}] {logEvent.FormattedMessage}");
    });
    config.AddTarget("webConsole", customTarget);
    config.AddRuleForAllLevels(customTarget);
    LogManager.Configuration = config;
}

// SSE Log Broadcaster
public static class ClientLogBroadcaster
{
    private static readonly ConcurrentQueue<string> LogQueue = new();
    private const int MaxQueueSize = 300;
    public static event Action<string>? OnLogAdded;

    public static void Log(string message)
    {
        LogQueue.Enqueue(message);
        while (LogQueue.Count > MaxQueueSize)
        {
            LogQueue.TryDequeue(out _);
        }
        OnLogAdded?.Invoke(message);
    }

    public static List<string> GetRecentLogs()
    {
        return LogQueue.ToList();
    }
}

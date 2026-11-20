using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Collections.Concurrent;
using NLog;
using NLog.Config;
using NLog.Targets;
using Com.Cybersoft4u.CommonLib.Network.Server;
using Com.Cybersoft4u.Simulator.Web.WebHSMSimulator;

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

// Register HsmSimulatorManager as Singleton
builder.Services.AddSingleton<HsmSimulatorManager>();

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
        HsmLogBroadcaster.Log($"[{DateTime.Now:HH:mm:ss.fff}] [{logEvent.Level.Name.ToUpper()}] {logEvent.FormattedMessage}");
    });
    config.AddTarget("webConsole", customTarget);
    config.AddRuleForAllLevels(customTarget);
    LogManager.Configuration = config;
}

// Thread-safe live log queue
public static class HsmLogBroadcaster
{
    private static readonly ConcurrentQueue<string> LogQueue = new();
    public static event Action<string>? OnLogAdded;

    public static void Log(string message)
    {
        LogQueue.Enqueue(message);
        OnLogAdded?.Invoke(message);
        while (LogQueue.Count > 500)
        {
            LogQueue.TryDequeue(out _);
        }
    }

    public static IEnumerable<string> GetHistory()
    {
        return LogQueue.ToArray();
    }
}

// Background simulator runner
public class HsmSimulatorManager
{
    private AsynTcpServer? _server;
    private CancellationTokenSource? _cts;
    private readonly IConfiguration _config;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public HsmSimulatorManager(IConfiguration config)
    {
        _config = config;
        var portVal = _config.GetValue<int>("GeneralConfig:ListenPortNumber");
        Port = portVal == 0 ? 8500 : portVal;
    }

    public bool IsRunning => _server?.IsRunning ?? false;
    public int Port { get; private set; }

    public void Start()
    {
        if (IsRunning) return;

        var portVal = _config.GetValue<int>("GeneralConfig:ListenPortNumber");
        if (portVal == 0) portVal = 8500;

        var sessionNumber = _config.GetValue<int>("GeneralConfig:SessionNumber");
        if (sessionNumber == 0) sessionNumber = 20;

        Port = portVal;
        _cts = new CancellationTokenSource();

        _server = new AsynTcpServer(_cts.Token)
        {
            PortNumber = portVal,
            SessionNumber = sessionNumber
        };
        _server.StartServer();
        Log.Info($"HSM Simulator Service started successfully on port {portVal}");
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _server?.StopServer();
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _server = null;
        Log.Info("HSM Simulator Service stopped successfully.");
    }
}

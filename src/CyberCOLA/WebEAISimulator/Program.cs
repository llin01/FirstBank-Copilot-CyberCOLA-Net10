using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Collections.Concurrent;
using System.Net;
using NLog;
using NLog.Config;
using NLog.Targets;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Simulator.Web.WebEAISimulator;

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

// Register EaiSimulatorManager as Singleton
builder.Services.AddSingleton<EaiSimulatorManager>();

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
        EaiLogBroadcaster.Log($"[{DateTime.Now:HH:mm:ss.fff}] [{logEvent.Level.Name.ToUpper()}] {logEvent.FormattedMessage}");
    });
    config.AddTarget("webConsole", customTarget);
    config.AddRuleForAllLevels(customTarget);
    LogManager.Configuration = config;
}

// Thread-safe live log queue
public static class EaiLogBroadcaster
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
public class EaiSimulatorManager
{
    private EAISocketListener? _listener;
    private readonly IConfiguration _config;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EaiSimulatorManager(IConfiguration config)
    {
        _config = config;
        var portVal = _config.GetValue<int>("GeneralConfig:server-service-listen-port");
        Port = portVal == 0 ? 12290 : portVal;
    }

    public bool IsRunning => _listener?.IsRunning ?? false;
    public int Port { get; private set; }

    public void Start()
    {
        if (IsRunning) return;

        var ipStr = _config["GeneralConfig:server-service-server-name"] ?? "127.0.0.1";
        if (ipStr.Equals("CFLin-NB-Test", StringComparison.OrdinalIgnoreCase))
        {
            ipStr = "127.0.0.1"; // Compatibility fallback for cross-platform local run
        }
        var portVal = _config.GetValue<int>("GeneralConfig:server-service-listen-port");
        if (portVal == 0) portVal = 12290;

        var maxConn = _config.GetValue<int>("GeneralConfig:server-service-max-connection");
        if (maxConn == 0) maxConn = 10;

        var bufSize = _config.GetValue<int>("GeneralConfig:server-service-buffer-size");
        if (bufSize == 0) bufSize = 2048;

        Port = portVal;

        var ipAddress = CommonUtils.GetIPAddressStringByHostName(ipStr, System.Net.Sockets.AddressFamily.InterNetwork);
        var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), portVal);

        _listener = new EAISocketListener(endPoint, maxConn, bufSize);
        _listener.Start();
        Log.Info($"EAI Socket Listener started successfully on port {portVal}");
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _listener?.Stop();
        _listener?.Dispose();
        _listener = null;
        Log.Info("EAI Socket Listener stopped successfully.");
    }
}

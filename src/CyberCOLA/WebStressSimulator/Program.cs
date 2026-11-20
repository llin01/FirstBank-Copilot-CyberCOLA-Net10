using Microsoft.AspNetCore.Hosting;
using System.IO;
using Com.Cybersoft4u.Simulator.Web.WebStressSimulator;

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

// Register StressSimulatorManager as Singleton
builder.Services.AddSingleton<StressSimulatorManager>();

// Add Blazor Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Redirect NLog output to the Blazor UI log panel
StressLogBroadcaster.SetupNLogRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map Blazor Hub and Razor Components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Start UDP log listener & check auto-start CLI args at boot
var mgr = app.Services.GetRequiredService<StressSimulatorManager>();
mgr.StartUDPFromConfig();
mgr.CheckAndAutoStart(args);

app.Run();

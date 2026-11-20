using Microsoft.AspNetCore.Hosting;
using System.IO;
using Com.Cybersoft4u.Simulator.Web.WebServerSimulator;

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

// Register Singleton Manager
builder.Services.AddSingleton<ServerSimulatorManager>();

// Add Blazor services2
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Start UDP log collection at boot
var manager = app.Services.GetRequiredService<ServerSimulatorManager>();
manager.StartUdpService();

app.UseStaticFiles();
app.UseAntiforgery();

// Map Blazor Hub and Razor Components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

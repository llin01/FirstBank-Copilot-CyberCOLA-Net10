using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;

namespace Com.Cybersoft4u.Test.Web.WebSMSWebApiServer
{
    /// <summary>
    /// Singleton manager that hosts an embedded Kestrel SMS mock Web API server.
    /// Migrated from WpfSMSWebApiServer MainWindow.xaml.cs.
    /// </summary>
    public class SMSWebApiServerManager : IDisposable
    {
        // ── Status ──────────────────────────────────────────────────────────
        public bool IsRunning { get; private set; }
        public string StatusText { get; private set; } = "Stopped";
        public string ApiUrl { get; set; } = "http://localhost:7293/api/";

        // ── Log event hooks (system + request consoles) ─────────────────────
        public event Action<string, string>? OnSystemLog;   // msg, colorKey
        public event Action<string>? OnRequestLog;

        private WebApplication? _webApp;
        private CancellationTokenSource? _cts;

        public SMSWebApiServerManager()
        {
            LoadConfigUrl();
        }

        // ── Configuration ───────────────────────────────────────────────────
        private void LoadConfigUrl()
        {
            try
            {
                var info = SingletonOfSMSWebAPIInfo.Instance.GetSMSWebAPIInfo();
                if (info?.SMSWebAPI?.Urls?.Url != null && info.SMSWebAPI.Urls.Url.Count > 0)
                    ApiUrl = info.SMSWebAPI.Urls.Url[0];
            }
            catch (Exception ex)
            {
                Log($"[Config Warning] Cannot load SMSWebAPIInfo.xml: {ex.Message}. Using default URL.", "yellow");
            }
        }

        // ── Start ────────────────────────────────────────────────────────────
        public async Task StartAsync()
        {
            if (IsRunning) return;

            Log("Initializing embedded Kestrel SMS Web API Server...", "white");
            StatusText = "Starting...";

            try
            {
                var uri = new Uri(ApiUrl);
                string bindAddress = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
                // Build route path: ensure it ends with /Messages
                string routePath = uri.AbsolutePath.TrimEnd('/') + "/Messages";

                var serverBuilder = WebApplication.CreateBuilder();
                // Suppress all built-in console logging to keep UI clean
                serverBuilder.Logging.ClearProviders();
                serverBuilder.WebHost.UseUrls(bindAddress);
                serverBuilder.Services.ConfigureHttpJsonOptions(options =>
                    options.SerializerOptions.WriteIndented = true);

                var embeddedApp = serverBuilder.Build();

                // Register POST route for SMS simulation
                embeddedApp.MapPost(routePath, async (HttpContext context) =>
                {
                    try
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        string body = await reader.ReadToEndAsync();

                        string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fffff");
                        OnRequestLog?.Invoke($"[{timeStr}]\n{body}\n");

                        // Build simulated response
                        var responseModel = new SMSResponseModel
                        {
                            SYS_ID = "CyberCOLA",
                            SOURCE_ID = "#11",
                            MSG_ID = Guid.NewGuid().ToString(),
                            CELL_PHONE = "0912345678",
                            SMS_NAME = "OTP",
                            SMS_BODY = "Simulation",
                            SMS_PROVIDER = "EVERY8D",
                            SMS_STATUS = "0",
                            SMS_STATUS_STRING = "0000",
                            MNT_USER = "SIM",
                            CREATE_USER = "SIM"
                        };

                        Log($"[{timeStr}] SMS Success → Returned Status 0000", "green");
                        await context.Response.WriteAsJsonAsync(responseModel);
                    }
                    catch (Exception ex)
                    {
                        string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fffff");
                        Log($"[{timeStr}] Server Handler Error: {ex.Message}", "red");
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync(ex.Message);
                    }
                });

                _cts = new CancellationTokenSource();
                _webApp = embeddedApp;

                await embeddedApp.StartAsync(_cts.Token);

                IsRunning = true;
                StatusText = "Running";
                Log($"Server listening on: {bindAddress}", "green");
                Log($"Route registered: POST {routePath}", "cyan");
            }
            catch (Exception ex)
            {
                IsRunning = false;
                StatusText = "Failed to Start";
                Log($"[Fatal Error] {ex.Message}", "red");
            }
        }

        // ── Stop ─────────────────────────────────────────────────────────────
        public async Task StopAsync()
        {
            if (!IsRunning || _webApp == null) return;

            Log("Stopping embedded SMS Web API Server...", "white");
            StatusText = "Stopping...";
            try
            {
                _cts?.Cancel();
                await _webApp.StopAsync();
                _webApp = null;
                IsRunning = false;
                StatusText = "Stopped";
                Log("Server stopped successfully.", "yellow");
            }
            catch (Exception ex)
            {
                StatusText = "Error stopping";
                Log($"[Error during Stop]: {ex.Message}", "red");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void Log(string msg, string color) => OnSystemLog?.Invoke(msg, color);

        public void Dispose()
        {
            _cts?.Cancel();
            _webApp?.StopAsync().GetAwaiter().GetResult();
        }
    }
}

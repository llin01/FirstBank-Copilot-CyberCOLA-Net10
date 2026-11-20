#nullable enable
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;

namespace Com.Cybersoft4u.Test.Wpf.WpfSMSWebApiServer
{
    public partial class MainWindow : Window
    {
        private static readonly SingletonOfSMSWebAPIInfo SMSWebAPIInfo = SingletonOfSMSWebAPIInfo.Instance;
        private WebApplication? _webApp;
        private string _apiBaseUrl = "https://localhost:7293/api/";

        public MainWindow()
        {
            InitializeComponent();
            LoadConfigUrl();

            // English comments only: Auto-start if command-line argument contains "AUTO"
            string[] args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.Equals("AUTO", StringComparison.OrdinalIgnoreCase))
                {
                    ButtonStart_Click(this, new RoutedEventArgs());
                    break;
                }
            }
        }

        private void LoadConfigUrl()
        {
            try
            {
                // English comments only: Attempt to read configured SMS Web API endpoints from local XML config file
                var info = SMSWebAPIInfo.GetSMSWebAPIInfo();
                if (info?.SMSWebAPI?.Urls?.Url != null && info.SMSWebAPI.Urls.Url.Count > 0)
                {
                    _apiBaseUrl = info.SMSWebAPI.Urls.Url[0];
                }
            }
            catch (Exception ex)
            {
                LogToConsole(RichTextBoxLogs, $"[Config Warning] Could not load SMSWebAPIInfo.xml: {ex.Message}. Falling back to default URL.", Brushes.Yellow);
            }
            TextBoxUrl.Text = _apiBaseUrl;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            ButtonStart.IsEnabled = false;
            TextBlockStatus.Text = "Starting Web API Server...";
            LogToConsole(RichTextBoxLogs, "Initializing Kestrel Web API Server...", Brushes.White);

            Task.Run(async () =>
            {
                try
                {
                    var uri = new Uri(_apiBaseUrl);
                    string bindAddress = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
                    string routePath = uri.AbsolutePath.EndsWith("/") ? $"{uri.AbsolutePath}Messages" : $"{uri.AbsolutePath}/Messages";

                    var builder = WebApplication.CreateBuilder();

                    // English comments only: Shut down default logs to keep rendering clean
                    builder.Logging.ClearProviders();

                    builder.WebHost.UseUrls(bindAddress);
                    builder.Services.ConfigureHttpJsonOptions(options =>
                    {
                        options.SerializerOptions.WriteIndented = true;
                    });

                    var app = builder.Build();

                    // English comments only: Register HTTP POST route matching ColaCommUtility requests
                    app.MapPost(routePath, async (HttpContext context) =>
                    {
                        try
                        {
                            using var reader = new StreamReader(context.Request.Body);
                            string body = await reader.ReadToEndAsync();

                            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fffff");
                            Dispatcher.Invoke(() =>
                            {
                                LogToConsole(RichTextBoxRequests, $"[{timeStr} Request Received]:\n{body}\n", Brushes.LimeGreen);
                            });

                            // Construct a response matching the SMSResponseModel expectations
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

                            Dispatcher.Invoke(() =>
                            {
                                LogToConsole(RichTextBoxLogs, $"[{timeStr} SMS Success] Returned Status 0000", Brushes.GreenYellow);
                            });

                            await context.Response.WriteAsJsonAsync(responseModel);
                        }
                        catch (Exception ex)
                        {
                            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fffff");
                            Dispatcher.Invoke(() =>
                            {
                                LogToConsole(RichTextBoxLogs, $"[{timeStr} Server Error]: {ex.Message}", Brushes.Red);
                            });
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsync(ex.Message);
                        }
                    });

                    _webApp = app;
                    await app.StartAsync();

                    Dispatcher.Invoke(() =>
                    {
                        TextBlockStatus.Text = "Running";
                        LogToConsole(RichTextBoxLogs, $"Server successfully listening on: {bindAddress}", Brushes.Green);
                        LogToConsole(RichTextBoxLogs, $"Route registered: POST {routePath}", Brushes.Cyan);
                        ButtonStop.IsEnabled = true;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TextBlockStatus.Text = "Failed to Start";
                        LogToConsole(RichTextBoxLogs, $"[Fatal Error]: {ex.Message}", Brushes.Red);
                        ButtonStart.IsEnabled = true;
                    });
                }
            });
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            if (_webApp != null)
            {
                ButtonStop.IsEnabled = false;
                TextBlockStatus.Text = "Stopping server...";
                LogToConsole(RichTextBoxLogs, "Stopping Web API Server...", Brushes.White);

                Task.Run(async () =>
                {
                    try
                    {
                        await _webApp.StopAsync();
                        _webApp = null;
                        Dispatcher.Invoke(() =>
                        {
                            TextBlockStatus.Text = "Stopped";
                            LogToConsole(RichTextBoxLogs, "Server stopped successfully.", Brushes.Yellow);
                            ButtonStart.IsEnabled = true;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TextBlockStatus.Text = "Error stopping";
                            LogToConsole(RichTextBoxLogs, $"[Error during Stop]: {ex.Message}", Brushes.Red);
                            ButtonStop.IsEnabled = true;
                        });
                    }
                });
            }
        }

        private void LogToConsole(RichTextBox rtb, string message, Brush brush)
        {
            if (rtb.Dispatcher.CheckAccess())
            {
                var tr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                tr.Text = message + "\r";
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                rtb.ScrollToEnd();
            }
            else
            {
                rtb.Dispatcher.BeginInvoke(new Action<RichTextBox, string, Brush>(LogToConsole), rtb, message, brush);
            }
        }
    }
}

/*
 * How to Test (HTTPS Version):
 * 1. Start MaintGatewayHttps (Target: https://localhost:5101)
 * 2. Start MaintGatewayTesterHttps (Local: http://localhost:5102)
 * 3. Open Browser or use Curl: GET http://localhost:5102/api/test/send-xml
 * 4. Verify that MaintGatewayHttps received the request over HTTPS and responded via TCP.
 * Note: SSL errors are bypassed in this tester for development ease.
 */
using Com.Cybersoft4u.Interchange;
using NLog;
using NLog.Web;
using System.Xml.Serialization;

/// <summary>
/// The main program class for the MaintGatewayTester HTTPS API.
/// This application acts as a client to test the MaintGateway HTTPS service,
/// sending XML-based requests converted to JSON and logging responses.
/// It bypasses SSL certificate validation for development convenience.
/// </summary>
public partial class Program // Use partial to allow for top-level statements
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The entry point of the MaintGatewayTester HTTPS application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // NLog Setup
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Read Gateway settings (default points to new HTTPS version)
            var gatewayUrl = builder.Configuration["GatewaySettings:Url"] ?? "https://localhost:5101/";

            // Configure HttpClient to support self-signed certificates (development environment)
            builder.Services.AddHttpClient("MaintGatewayHttps", client =>
            {
                client.BaseAddress = new Uri(gatewayUrl);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

            var app = builder.Build();

            // Configure ASP.NET Core listening URLs, fallback to 5005 if not configured in appsettings
            var kestrelUrl = builder.Configuration["Kestrel:Endpoints:Http:Url"]
                             ?? builder.Configuration["Kestrel:Url"];
            if (!string.IsNullOrEmpty(kestrelUrl))
            {
                app.Urls.Add(kestrelUrl.Replace("0.0.0.0", "*"));
            }
            else
            {
                app.Urls.Add("http://*:5005");
            }

            /// <summary>
            /// Root endpoint of the MaintGatewayTester HTTPS API.
            /// </summary>
            app.MapGet("/", () => "MaintGatewayTesterHttps is running! Use /api/test/send-xml to test HTTPS flow.");

            /// <summary>
            /// Test endpoint to send an XML-based file maintenance request to the MaintGatewayHttps service.
            /// The XML data is read from a file, deserialized into a <see cref="WebFileMaintDataXml"/> object,
            /// and then sent as a JSON POST request to the MaintGatewayHttps.
            /// </summary>
            /// <param name="factory">The HTTP client factory.</param>
            /// <param name="config">The application configuration.</param>
            /// <param name="log">The logger instance.</param>
            /// <returns>An HTTP response containing the status of the test request.</returns>
            app.MapGet("/api/test/send-xml", async (IHttpClientFactory factory, IConfiguration config, ILogger<Program> log) =>
            {
                try
                {
                    string? xmlPath = config["GatewaySettings:TestDataFilePath"];
                    if (string.IsNullOrEmpty(xmlPath))
                    {
                        // Fallback sequence
                        var pathsToTry = new[] {
                            "TestData/WebFileMaintDataXml.xml",
                            "../MaintGatewayTester/TestData/WebFileMaintDataXml.xml",
                            "../../../../MaintGatewayTester/TestData/WebFileMaintDataXml.xml"
                        };
                        xmlPath = pathsToTry.FirstOrDefault(File.Exists) ?? pathsToTry[0];
                    }

                    if (!File.Exists(xmlPath))
                    {
                        log.LogError("XML Test file not found at: {Path}", xmlPath);
                        return Results.NotFound($"XML Test file not found at: {xmlPath}");
                    }

                    // 1. Read and parse XML
                    WebFileMaintDataXml? testData;
                    var serializer = new XmlSerializer(typeof(WebFileMaintDataXml));
                    using (var reader = new StreamReader(xmlPath))
                    {
                        testData = (WebFileMaintDataXml?)serializer.Deserialize(reader);
                    }

                    if (testData == null)
                    {
                        log.LogError("Deserialization failed for XML.");
                        return Results.BadRequest("Failed to deserialize XML.");
                    }

                    var txKey = testData.WebTxKeySeq ?? "UNKNOWN";
                    log.LogInformation("Sending XML Data to HTTPS Gateway. Url: {Url}, TxKey: {Key}", gatewayUrl, txKey);

                    // 2. Send JSON POST to MaintGatewayHttps
                    var client = factory.CreateClient("MaintGatewayHttps");
                    var response = await client.PostAsJsonAsync("api/interchange", testData);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<WebFileMaintDataXml>();
                        log.LogInformation("Successfully received response from HTTPS Gateway for TxKey: {Key}", txKey);
                        return Results.Ok(new
                        {
                            Status = "Success",
                            GatewayUrl = gatewayUrl,
                            Response = result
                        });
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        log.LogError("HTTPS Gateway returned error: {Code}. Details: {Msg}", response.StatusCode, error);
                        return Results.Problem($"HTTPS Gateway returned error: {response.StatusCode}. Details: {error}");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Unexpected error in send-xml route");
                    return Results.Problem(ex.Message);
                }
            });

            app.Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "MaintGatewayTesterHttps application start-up failed");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}

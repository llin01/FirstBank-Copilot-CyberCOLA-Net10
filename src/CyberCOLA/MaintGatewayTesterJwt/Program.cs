/*
 * How to Test (JWT Version):
 * 1. Start MaintGatewayJwt (Target: https://localhost:5103)
 * 2. Start MaintGatewayTesterJwt (Local: http://localhost:5104)
 * 3. Open Browser or use Curl: GET http://localhost:5104/api/test/send-xml
 * 4. Verify that:
 *    a. Tester calls /api/token to get JWT.
 *    b. Tester calls /api/interchange with Authorization: Bearer <token>.
 *    c. Gateway verifies JWT and processes the request.
 */
using Com.Cybersoft4u.Interchange;
using NLog;
using NLog.Web;
using System.Xml.Serialization;
using System.Net.Http.Headers;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;

/// <summary>
/// The main program class for the MaintGatewayTester JWT API.
/// This application acts as a client to test the MaintGateway JWT service,
/// handling JWT acquisition and sending authorized XML-based requests
/// converted to JSON, and logging responses.
/// </summary>
public partial class Program // Use partial to allow for top-level statements
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The entry point of the MaintGatewayTester JWT application.
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

            // Read Gateway settings
            var gatewayUrl = builder.Configuration["GatewaySettings:Url"] ?? "https://localhost:5103/";

            // Initialize SystemParameterInfo for ColaCommUtility decryption
            var systemParamPath = builder.Configuration["GeneralConfig:first-bank-system-parameter-info-xml-file-path"];
            if (!string.IsNullOrEmpty(systemParamPath) && System.IO.File.Exists(systemParamPath))
            {
                SingletonOfSystemParameterInfo.Instance.Load(systemParamPath);
            }

            // Configure HttpClient
            builder.Services.AddHttpClient("MaintGatewayJwt", client =>
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

            app.MapGet("/", () => "MaintGatewayTesterJwt is running! Use /api/test/send-xml to test JWT flow.");

            /// <summary>
            /// Test endpoint to acquire a JWT token and send an XML-based file maintenance request
            /// to the MaintGatewayJwt service with authorization.
            /// </summary>
            /// <param name="factory">The HTTP client factory.</param>
            /// <param name="config">The application configuration.</param>
            /// <param name="log">The logger instance.</param>
            /// <returns>An HTTP response containing the status of the test request, including token info and gateway response.</returns>
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

                    if (testData == null) return Results.BadRequest("Failed to deserialize XML.");

                    var txKey = testData.WebTxKeySeq ?? "UNKNOWN";
                    var client = factory.CreateClient("MaintGatewayJwt");

                    // 2. Acquire JWT Token
                    log.LogInformation("Acquiring JWT Token from {Url}api/token", gatewayUrl);
                    var clientUser = config["ClientCredentials:Username"] ?? "admin";
                    var encryptedPass = config["ClientCredentials:Password"];
                    /* 
                     * SECURITY NOTE: 
                     * Client password is encrypted in appsettings.json using AES-GCM.
                     * Decrypt it here before sending to the Gateway for authentication.
                     */
                    var clientPass = string.IsNullOrEmpty(encryptedPass) ? "password" : ColaCommUtility.DecryptAesGcm(encryptedPass);
                    var loginResponse = await client.PostAsJsonAsync("api/token", new { Username = clientUser, Password = clientPass });
                    if (!loginResponse.IsSuccessStatusCode)
                    {
                        log.LogError("Failed to acquire JWT. Status: {Code}", loginResponse.StatusCode);
                        return Results.Problem("JWT Acquisition Failed.");
                    }

                    var tokenData = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
                    if (string.IsNullOrEmpty(tokenData?.Token))
                    {
                        log.LogError("Received empty token.");
                        return Results.Problem("Empty JWT received.");
                    }

                    log.LogInformation("JWT Acquired successfully. Sending authorized request for TxKey: {Key}", txKey);

                    // 3. Send authorized JSON POST
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.Token);
                    var response = await client.PostAsJsonAsync("api/interchange", testData);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<WebFileMaintDataXml>();
                        log.LogInformation("Successfully received response from JWT Gateway for TxKey: {Key}", txKey);
                        return Results.Ok(new
                        {
                            Status = "Success",
                            Token = tokenData.Token,
                            Response = result
                        });
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        log.LogError("JWT Gateway returned error: {Code}. Details: {Msg}", response.StatusCode, error);
                        return Results.Problem($"JWT Gateway returned error: {response.StatusCode}. Details: {error}");
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
            logger.Error(ex, "MaintGatewayTesterJwt application start-up failed");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    /// <summary>
    /// Represents the response structure for JWT token acquisition.
    /// </summary>
    /// <param name="Token">The JWT token string.</param>
    public record TokenResponse(string Token);
}

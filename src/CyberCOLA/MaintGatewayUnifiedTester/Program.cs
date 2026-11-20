/*
 * MaintGatewayUnifiedTester — single project, three runtime modes (Http / Https / Jwt).
 *
 * Mode selection (two ways, either works):
 *   - CLI:            dotnet run --project MaintGatewayUnifiedTester -- --GatewayMode Jwt
 *   - Environment:    set ASPNETCORE_ENVIRONMENT=Jwt  (loads appsettings.Jwt.json)
 *
 * Default target gateway URL per mode:
 *   Http:  http://localhost:5000/    (tester listen: http://*:5005)
 *   Https: https://localhost:5101/   (tester listen: http://*:5006)
 *   Jwt:   https://localhost:5103/   (tester listen: http://*:5007)
 *
 * How to Test (Jwt mode):
 * 1. Start MaintGatewayUnified in Jwt mode (Target: https://localhost:5103)
 * 2. Start MaintGatewayUnifiedTester in Jwt mode (Local: http://localhost:5007)
 * 3. Open Browser or use Curl: GET http://localhost:5007/api/test/send-xml
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
/// The main program class for the MaintGatewayUnifiedTester API.
/// This application acts as a client to test the MaintGatewayUnified service,
/// acquiring a JWT (in Jwt mode) and sending authorized XML-based requests
/// converted to JSON, and logging responses.
/// </summary>
public partial class Program // Use partial to allow for top-level statements
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The entry point of the MaintGatewayUnifiedTester application.
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

            // Resolve runtime mode from configuration (appsettings.{Mode}.json or --GatewayMode CLI arg)
            var mode = Enum.TryParse<TesterMode>(builder.Configuration["GatewayMode"] ?? "Http", ignoreCase: true, out var parsedMode)
                ? parsedMode
                : TesterMode.Http;

            // Read Gateway URL; fall back to per-mode default when not configured
            var gatewayUrl = builder.Configuration["GatewaySettings:Url"];
            if (string.IsNullOrEmpty(gatewayUrl))
            {
                gatewayUrl = mode switch
                {
                    TesterMode.Https => "https://localhost:5001/",
                    TesterMode.Jwt => "https://localhost:5001/",
                    _ => "http://localhost:5000/"
                };
            }

            // Initialize SystemParameterInfo for ColaCommUtility decryption (required only in Jwt mode for /api/token)
            if (mode == TesterMode.Jwt)
            {
                var systemParamPath = builder.Configuration["GeneralConfig:first-bank-system-parameter-info-xml-file-path"];
                if (!string.IsNullOrEmpty(systemParamPath) && System.IO.File.Exists(systemParamPath))
                {
                    SingletonOfSystemParameterInfo.Instance.Load(systemParamPath);
                }
            }

            // Configure HttpClient (cert-bypass handler is harmless for plain HTTP)
            builder.Services.AddHttpClient("MaintGatewayUnified", client =>
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

            app.MapGet("/", () => $"MaintGatewayUnifiedTester ({mode}) is running! Use /api/test/send-xml to test the flow.");

            /// <summary>
            /// Test endpoint to read an XML-based file maintenance request, optionally acquire a JWT
            /// (in Jwt mode), and send it as JSON to the MaintGatewayUnified /api/interchange endpoint.
            /// </summary>
            /// <param name="factory">The HTTP client factory.</param>
            /// <param name="config">The application configuration.</param>
            /// <param name="log">The logger instance.</param>
            /// <returns>An HTTP response containing the status of the test request, including gateway response (and token in Jwt mode).</returns>
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
                            "../MaintGatewayUnifiedTester/TestData/WebFileMaintDataXml.xml",
                            "../../../../MaintGatewayUnifiedTester/TestData/WebFileMaintDataXml.xml"
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
                    var client = factory.CreateClient("MaintGatewayUnified");

                    // 2. Acquire JWT Token (Jwt mode only)
                    string? token = null;
                    if (mode == TesterMode.Jwt)
                    {
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

                        token = tokenData.Token;
                        log.LogInformation("JWT Acquired successfully. Sending authorized request for TxKey: {Key}", txKey);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    else
                    {
                        log.LogInformation("Sending request to {Url}api/interchange for TxKey: {Key}", gatewayUrl, txKey);
                    }

                    // 3. Send JSON POST
                    var response = await client.PostAsJsonAsync("api/interchange", testData);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<WebFileMaintDataXml>();
                        log.LogInformation("Successfully received response from Gateway for TxKey: {Key}", txKey);
                        return Results.Ok(mode == TesterMode.Jwt
                            ? new { Status = "Success", GatewayUrl = gatewayUrl, Token = token, Response = result }
                            : new { Status = "Success", GatewayUrl = gatewayUrl, Response = result });
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        log.LogError("Gateway returned error: {Code}. Details: {Msg}", response.StatusCode, error);
                        return Results.Problem($"Gateway returned error: {response.StatusCode}. Details: {error}");
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
            logger.Error(ex, "MaintGatewayUnifiedTester application start-up failed");
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

public enum TesterMode { Http, Https, Jwt }

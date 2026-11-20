/*
 * How to Test (HTTP Version):
 * 1. Start MaintGateway (Target: http://localhost:5000)
 * 2. Start MaintGatewayTester (Local: http://localhost:5001)
 * 3. Open Browser or use Curl: GET http://localhost:5001/api/test/send-xml
 * 4. Verify that MaintGateway received the request and responded via TCP.
 */
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.CommonLib.Resource;
using NLog;
using NLog.Web;

/// <summary>
/// The main program class for the MaintGateway HTTP API.
/// This application acts as a gateway for file maintenance transactions,
/// converting JSON requests to ISO8583 messages and communicating with a TCP backend.
/// </summary>
public partial class Program // Use partial to allow for top-level statements
{
    private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The entry point of the MaintGateway HTTP application.
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

            // Register services
            builder.Services.AddSingleton<TcpPersistentClient>();
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });

            var app = builder.Build();

            // Force start TCP Persistent Client (ensure single session establishment)
            app.Services.GetRequiredService<TcpPersistentClient>();

            /// <summary>
            /// Root endpoint of the MaintGateway HTTP API.
            /// </summary>
            app.MapGet("/", () => "MaintGateway (HTTP) is running! Please use POST /api/interchange for transactions.");

            /// <summary>
            /// Handles file maintenance interchange requests.
            /// Converts a JSON <see cref="WebFileMaintDataXml"/> request to an ISO8583 message,
            /// sends it to the backend TCP server, and converts the ISO8583 response back to JSON.
            /// </summary>
            /// <param name="data">The incoming JSON request data.</param>
            /// <param name="tcp">The persistent TCP client for backend communication.</param>
            /// <param name="log">The logger instance.</param>
            /// <returns>An HTTP response containing the processed data or an error.</returns>
            app.MapPost("/api/interchange", async (WebFileMaintDataXml data, TcpPersistentClient tcp, ILogger<Program> log) =>
            {
                try
                {
                    var txKey = data.WebTxKeySeq;
                    if (string.IsNullOrEmpty(txKey)) return Results.BadRequest("Missing WebTxKeySeq");

                    log.LogInformation(">>> [JSON -> ISO8583] Transforming Request. TxKey: {TxKey}", txKey);

                    // 1. Map JSON object to NCCC ISO8583 message
                    var ncccReq = new NcccIso8583();
                    ncccReq.Header().Decode("ISO006000060");
                    ncccReq.Mti("0420");

                    var core = ncccReq.Core();
                    if (data.WebIsoP2CardNbr != null) core.Field(FIELD.F_2, data.WebIsoP2CardNbr);
                    if (data.WebIsoP3ProcessCde != null) core.Field(FIELD.F_3, data.WebIsoP3ProcessCde);
                    if (data.WebIsoP7TxDteTime != null) core.Field(FIELD.F_7, data.WebIsoP7TxDteTime);
                    if (data.WebIsoP11TraceNo != null) core.Field(FIELD.F_11, data.WebIsoP11TraceNo);
                    if (data.WebIsoP13TransYear != null) core.Field(FIELD.F_13, data.WebIsoP13TransYear);
                    if (data.WebIsoP14ExpirDte != null) core.Field(FIELD.F_14, data.WebIsoP14ExpirDte);
                    if (data.WebIsoP33FwdIICode != null) core.Field(FIELD.F_33, data.WebIsoP33FwdIICode);
                    if (data.WebIsoP36NTID != null) core.Field(FIELD.F_36, data.WebIsoP36NTID);
                    if (data.WebIsoP37RefNbr != null) core.Field(FIELD.F_37, data.WebIsoP37RefNbr);
                    if (data.WebIsoP48AdditionalData != null) core.Field(FIELD.F_48, data.WebIsoP48AdditionalData);
                    if (data.WebIsoP49TranCurCode != null) core.Field(FIELD.F_49, data.WebIsoP49TranCurCode);
                    if (data.WebIsoP58T05TxDte != null) core.Field(FIELD.F_58, data.WebIsoP58T05TxDte);
                    if (data.WebIsoP60FromHostMaintData != null) core.Field(FIELD.F_60, data.WebIsoP60FromHostMaintData);
                    if (data.WebIsoP63SystemID != null) core.Field(FIELD.F_63, data.WebIsoP63SystemID);
                    if (data.WebIsoS73ActDte != null) core.Field(FIELD.F_73, data.WebIsoS73ActDte);
                    if (data.WebIsoS91FileUpdCode != null) core.Field(FIELD.F_91, data.WebIsoS91FileUpdCode);
                    if (data.WebIsoS92FileSecurityCode != null) core.Field(FIELD.F_92, data.WebIsoS92FileSecurityCode);
                    if (data.WebIsoS96MessageSecurity != null) core.Field(FIELD.F_96, data.WebIsoS96MessageSecurity);
                    if (data.WebIsoS101FileName != null) core.Field(FIELD.F_101, data.WebIsoS101FileName);
                    if (data.WebIsoS115AdditionalTraceData != null) core.Field(FIELD.F_115, data.WebIsoS115AdditionalTraceData);
                    if (data.WebIsoS120FileUpd != null) core.Field(FIELD.F_120, data.WebIsoS120FileUpd);
                    if (data.WebIsoS122HostMaint != null) core.Field(FIELD.F_122, data.WebIsoS122HostMaint);
                    if (data.WebIsoS124TokenData != null) core.Field(FIELD.F_124, data.WebIsoS124TokenData);

                    // Log full request details
                    Iso8583Logger.LogFields(ncccReq, "ncccReq (Request to NCCC)");

                    byte[]? isoBytes = ncccReq.Encode().Val();
                    if (isoBytes == null) return Results.Problem("ISO8583 Encoding Failed");

                    // 2. Send via persistent connection and wait for response
                    string correlationKey = core.SystemTraceAuditNumber;
                    if (string.IsNullOrEmpty(correlationKey)) correlationKey = data.WebIsoP11TraceNo ?? txKey;

                    log.LogInformation(">>> Sending ISO8583 to Server. Key: {Key}", correlationKey);
                    byte[] responseIsoBytes = await tcp.SendAsync(correlationKey, isoBytes);

                    // 3. Decode ISO8583 response and map back to JSON object
                    var ncccRsp = new NcccIso8583();
                    ncccRsp.Decode(responseIsoBytes);

                    // Log full response details
                    Iso8583Logger.LogFields(ncccRsp, "ncccRsp (Response from NCCC)");

                    var rspCore = ncccRsp.Core();
                    log.LogInformation("<<< [ISO8583 -> JSON] Received Response. RspCode: {Rsp}", rspCore.ResponseCode);

                    var responseObj = new WebFileMaintDataXml
                    {
                        WebTxKeySeq = correlationKey, // Keep the original request key
                        WebIsoP2CardNbr = rspCore.Field(FIELD.F_2)?.ToString(),
                        WebIsoP3ProcessCde = rspCore.Field(FIELD.F_3)?.ToString(),
                        WebIsoP7TxDteTime = rspCore.Field(FIELD.F_7)?.ToString(),
                        WebIsoP11TraceNo = rspCore.Field(FIELD.F_11)?.ToString(),
                        WebIsoP13TransYear = rspCore.Field(FIELD.F_13)?.ToString(),
                        WebIsoP14ExpirDte = rspCore.Field(FIELD.F_14)?.ToString(),
                        WebIsoP33FwdIICode = rspCore.Field(FIELD.F_33)?.ToString(),
                        WebIsoP36NTID = rspCore.Field(FIELD.F_36)?.ToString(),
                        WebIsoP37RefNbr = rspCore.Field(FIELD.F_37)?.ToString(),
                        WebIsoP39Rsp = rspCore.Field(FIELD.F_39)?.ToString(),
                        WebIsoP48AdditionalData = rspCore.Field(FIELD.F_48)?.ToString(),
                        WebIsoP49TranCurCode = rspCore.Field(FIELD.F_49)?.ToString(),
                        WebIsoP58T05TxDte = rspCore.Field(FIELD.F_58)?.ToString(),
                        WebIsoP60FromHostMaintData = rspCore.Field(FIELD.F_60)?.ToString(),
                        WebIsoP63SystemID = rspCore.Field(FIELD.F_63)?.ToString(),
                        WebIsoS73ActDte = rspCore.Field(FIELD.F_73)?.ToString(),
                        WebIsoS91FileUpdCode = rspCore.Field(FIELD.F_91)?.ToString(),
                        WebIsoS92FileSecurityCode = rspCore.Field(FIELD.F_92)?.ToString(),
                        WebIsoS96MessageSecurity = rspCore.Field(FIELD.F_96)?.ToString(),
                        WebIsoS101FileName = rspCore.Field(FIELD.F_101)?.ToString(),
                        WebIsoS115AdditionalTraceData = rspCore.Field(FIELD.F_115)?.ToString(),
                        WebIsoS120FileUpd = rspCore.Field(FIELD.F_120)?.ToString(),
                        WebIsoS122HostMaint = rspCore.Field(FIELD.F_122)?.ToString(),
                        WebIsoS124TokenData = rspCore.Field(FIELD.F_124)?.ToString()
                    };

                    return Results.Ok(responseObj);
                }
                catch (TimeoutException tex)
                {
                    log.LogWarning("!!! Gateway Timeout: {Msg}", tex.Message);
                    return Results.StatusCode(StatusCodes.Status408RequestTimeout);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "!!! Interchange Error.");
                    return Results.Problem(ex.Message);
                }
            });

            app.Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Gateway Application start-up failed");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}


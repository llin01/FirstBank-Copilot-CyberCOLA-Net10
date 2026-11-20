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
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.CommonLib.Resource;
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Com.Cybersoft4u.CommonLib.AuthUtils;

var logger = LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // JWT Configuration
    var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "CyberCOLA_Super_Secret_Key_2026_JWT";
    var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "CyberCOLA";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddAuthorization();

    // NLog Setup
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Register services
    builder.Services.AddSingleton<TcpPersistentClient>();
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
    });

    // Initialize SystemParameterInfo for ColaCommUtility decryption
    var systemParamPath = builder.Configuration["GeneralConfig:first-bank-system-parameter-info-xml-file-path"];
    if (!string.IsNullOrEmpty(systemParamPath) && System.IO.File.Exists(systemParamPath))
    {
        SingletonOfSystemParameterInfo.Instance.Load(systemParamPath);
    }

    var app = builder.Build();

    // Enable HTTPS Redirection
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    // Force start TCP Persistent Client
    app.Services.GetRequiredService<TcpPersistentClient>();

    app.MapGet("/", () => "MaintGatewayJwt (JWT) is running! Please use POST /api/interchange for transactions.");

    // Login Endpoint to issue JWT
    app.MapPost("/api/token", (LoginRequest request, IConfiguration config) =>
    {
        /* 
         * SECURITY NOTE: 
         * Credentials are managed in appsettings.json under 'GatewayCredentials'.
         * The password is stored using AES-GCM encryption (Base64 string).
         * Use ColaCommUtility.DecryptAesGcm() to decrypt the configured password 
         * before comparing it with the user's input.
         */
        var adminUser = config["GatewayCredentials:AdminUsername"] ?? "admin";
        var encryptedPass = config["GatewayCredentials:AdminPassword"];
        var adminPass = string.IsNullOrEmpty(encryptedPass) ? "password" : ColaCommUtility.DecryptAesGcm(encryptedPass);

        if (request.Username == adminUser && request.Password == adminPass)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, request.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(issuer, issuer, claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        return Results.Unauthorized();
    });

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

            // 2. Send via persistent connection
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
    })
    .RequireAuthorization()
    .Accepts<WebFileMaintDataXml>("application/json")
    .Produces<WebFileMaintDataXml>(StatusCodes.Status200OK, "application/json");

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

public record LoginRequest(string Username, string Password);

using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.Views
{
    public partial class MaintGatewayTesterJwtPage : Page
    {
        private static readonly HttpClient client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        public MaintGatewayTesterJwtPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            TxtRequest.Text = Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.ConfigHelper.GetXmlTestDataAsJson();
            var jwtUrl = Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.ConfigHelper.Configuration?["TesterSettings:JwtProxyUrl"];
            if (!string.IsNullOrEmpty(jwtUrl))
            {
                TxtUrl.Text = jwtUrl;
            }
        }

        private async void BtnTrigger_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text;
            try
            {
                var response = await client.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();
                TxtResponse.Text = $"Trigger Status: {response.StatusCode}\n\n{responseString}";
            }
            catch (Exception ex)
            {
                TxtResponse.Text = $"Trigger Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Why a token acquisition attempt failed. Kept distinct from the error text so the
        /// caller can decide which failures warrant which guidance -- e.g. a mode mismatch
        /// (wrong Gateway/Tester pair running) only explains ProxyErrorResponse, not
        /// ConnectionFailed (where the proxy port genuinely isn't reachable).
        /// </summary>
        private enum JwtTokenFailureKind
        {
            ConnectionFailed,
            ProxyErrorResponse,
            MissingToken,
            ParseFailed
        }

        /// <summary>
        /// Acquires a JWT token via the proxy endpoint. Returns a specific failure kind and
        /// reason (connection failure, non-success HTTP status with body, or a success response
        /// missing the "token" field) instead of collapsing every failure into a generic
        /// "port not running" message -- the proxy responding at all (even with an error)
        /// already proves the port is up, so that message was frequently misleading.
        /// </summary>
        private async Task<(string? Token, JwtTokenFailureKind? FailureKind, string? Error)> GetJwtTokenViaProxyAsync()
        {
            string proxyTriggerUrl = Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.ConfigHelper.Configuration?["TesterSettings:JwtProxyUrl"]
                                     ?? "http://localhost:5005/api/test/send-xml";

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(proxyTriggerUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Proxy Token acquisition failed: {ex.Message}");
                return (null, JwtTokenFailureKind.ConnectionFailed, $"Could not connect to {proxyTriggerUrl} ({ex.Message}).");
            }

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // The proxy answered (so its own port IS up); the failure is inside the proxy's
                // own token-acquisition logic (e.g. the JWT gateway it depends on isn't reachable
                // or rejected the login), most likely because a different mode's gateway/tester
                // pair is running instead of the JWT ones (mode mismatch), not a port problem.
                return (null, JwtTokenFailureKind.ProxyErrorResponse, $"Proxy at {proxyTriggerUrl} returned {(int)response.StatusCode} {response.StatusCode}: {responseString}");
            }

            try
            {
                var json = JObject.Parse(responseString);
                string? token = json["token"]?.ToString();
                if (!string.IsNullOrEmpty(token))
                {
                    return (token, null, null);
                }
                return (null, JwtTokenFailureKind.MissingToken, $"Proxy responded successfully but did not include a token. Raw response: {responseString}");
            }
            catch (Exception ex)
            {
                return (null, JwtTokenFailureKind.ParseFailed, $"Failed to parse proxy response as JSON: {ex.Message}. Raw response: {responseString}");
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text;
            string body = TxtRequest.Text;

            try
            {
                string proxyUrl = Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.ConfigHelper.Configuration?["TesterSettings:JwtProxyUrl"]
                                  ?? "http://localhost:5005/api/test/send-xml";
                int proxyPort = 5005;
                try
                {
                    var uri = new Uri(proxyUrl);
                    proxyPort = uri.Port;
                }
                catch { /* Fallback to default port on parse failure */ }

                TxtResponse.Text = $"Step 1: Acquiring FULL JWT Token via Proxy ({proxyPort})...";
                var (token, failureKind, tokenError) = await GetJwtTokenViaProxyAsync();

                if (token == null)
                {
                    TxtToken.Text = "TOKEN_ERROR";
                    string message = $"Step 1 Failed: {tokenError}";
                    // Only ProxyErrorResponse/MissingToken indicate the proxy answered but the
                    // JWT gateway it depends on rejected/never issued a token -- the classic sign
                    // of a mode mismatch. ConnectionFailed/ParseFailed are different failure
                    // classes (proxy genuinely unreachable, or an unexpected response shape) where
                    // this hint would be inaccurate.
                    if (failureKind is JwtTokenFailureKind.ProxyErrorResponse or JwtTokenFailureKind.MissingToken)
                    {
                        message += "\n\nThis is likely a mode mismatch -- make sure the JWT-mode Gateway/Tester pair " +
                                    "(not HTTP or HTTPS mode) is the one currently running.";
                    }
                    TxtResponse.Text = message;
                    return;
                }

                // DISPLAY FULL TOKEN: No truncation
                TxtToken.Text = token;
                TxtResponse.Text = $"Step 1 Success! Full Token acquired.\nStep 2: Sending request to {url}...";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                TxtResponse.Text = $"Final Result:\nAuth Status: {response.StatusCode}\n\n{responseString}";
            }
            catch (Exception ex)
            {
                TxtResponse.Text = $"Critical Error: {ex.Message}";
            }
        }
    }
}

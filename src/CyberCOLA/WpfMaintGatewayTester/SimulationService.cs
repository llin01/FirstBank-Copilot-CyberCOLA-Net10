using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester
{
    public enum SimulationMode
    {
        Basic,
        Https,
        Jwt
    }

    public class SimulationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ResponseBody { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
    }

    public class SimulationService
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public async Task<SimulationResult> RunSimulationAsync(SimulationMode mode, string url, string body, string token = "")
        {
            try
            {
                // English comments only: Log the start of simulation execution parameters
                Log.Info($"Starting simulation | Mode: {mode} | URL: {url}");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                if (mode == SimulationMode.Jwt && !string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Log.Info($"Simulation {mode} Success | StatusCode: {response.StatusCode} | Response Length: {responseString.Length}");
                }
                else
                {
                    Log.Warn($"Simulation {mode} Failed | StatusCode: {response.StatusCode} | Response: {responseString}");
                }

                return new SimulationResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = response.StatusCode,
                    ResponseBody = responseString,
                    Message = $"Simulation {mode} completed."
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Simulation {mode} Exception: {ex.Message}");
                return new SimulationResult
                {
                    Success = false,
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    Message = $"Simulation {mode} failed: {ex.Message}"
                };
            }
        }
    }
}

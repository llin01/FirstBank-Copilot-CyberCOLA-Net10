using Com.Cybersoft4u.CommonLib.Network.Client;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Core utility class providing common functions for authorization processing, database operations, 
    /// encryption/decryption, SMS messaging, and ISO 8583 message handling.
    /// </summary>
    public static partial class ColaCommUtility
    {

        /// <summary>
        /// Send SMS to SMS Server.
        /// </summary>
        /// <param name="sendBytes"></param>
        /// <param name="smsServer"></param>
        /// <returns></returns>
        public static async Task<string> SendSMSMessageToSMSServer(byte[] sendBytes, SMSServer smsServer)
        {
            byte[] retnBytes;
            SocketAsyncEventArgsClient socketAsyncEventArgsClient = null;
            try
            {
                Log.Info($"SendSMSMessageToSMSServer process start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var big5encode = Encoding.GetEncoding(950);
                retnBytes = await Task.Run(() =>
                {
                    try
                    {
                        socketAsyncEventArgsClient = new SocketAsyncEventArgsClient(smsServer.ServerName.Trim(), smsServer.ListenerPort, smsServer.ConnectionTimeoutMilliseconds, smsServer.SendReceiveTimeoutMilliseconds);
                        socketAsyncEventArgsClient.Connect();
                        //var receiveBytes = socketAsyncEventArgsClient.ProcessSendReceive(sendBytes);
                        var receiveBytes = socketAsyncEventArgsClient.SendReceive(sendBytes);
                        if (receiveBytes == null)
                            Log.Info($"Receive Message is null at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        else
                            // Log.Info($"Receive Message [{Encoding.ASCII.GetString(receiveBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                            Log.Info($"Receive Message [{big5encode.GetString(receiveBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        socketAsyncEventArgsClient.Disconnect(SocketShutdown.Both);
                        Log.Info($"SendSMSMessageToSMSServer process completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        return receiveBytes;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"SendSMSMessageToSMSServer process error occurred while sending/receiving SMS message to/from Server[{smsServer.ServerName.Trim()}]: {ex.Message}");
                        return null;
                    }
                });

                var check = retnBytes == null ? string.Empty : big5encode.GetString(retnBytes);
                if (!check.Equals("<OK>")) return "R001";
                return "S001";
            }
            catch (Exception ex)
            {
                Log.Error($"SendSMSMessageToSMSServer Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return "R002";
            }
        }

        /// <summary>
        /// Send SMS to SMS Server.
        /// </summary>
        /// <param name="sendBytes"></param>
        /// <param name="smsServerInfo"></param>
        /// <returns></returns>
        public static async Task<string> SendSMSMessageToSMSServer(byte[] sendBytes, SMSServerInfo smsServerInfo)
        {
            byte[] retnBytes = null;
            int retryCount = 1;
            SMSServer smsServer1 = null;
            SMSServer smsServer2 = null;
            SocketAsyncEventArgsClient socketAsyncEventArgsClient1 = null;
            SocketAsyncEventArgsClient socketAsyncEventArgsClient2 = null;
            try
            {

                var big5encode = Encoding.GetEncoding(950);
                if (sendBytes == null || smsServerInfo == null)
                {
                    Log.Error($"SendSMSMessageToSMSServer parameter invalid at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return "S001";
                }
                if (smsServerInfo.SMSServers.SMSServer.Count == 0)
                {
                    Log.Error($"SendSMSMessageToSMSServer parameter SingletonOfSMSServerInfo invalid at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return "S001";
                }
                else if (smsServerInfo.SMSServers.SMSServer.Count == 1)
                {
                    smsServer1 = smsServerInfo.SMSServers.SMSServer[0];
                    smsServer2 = smsServerInfo.SMSServers.SMSServer[0];
                }
                else
                {
                    smsServer1 = smsServerInfo.SMSServers.SMSServer[0];
                    smsServer2 = smsServerInfo.SMSServers.SMSServer[1];
                }
                Log.Info($"SendSMSMessageToSMSServer Server[{smsServer1.ServerName.Trim()}],[{smsServer2.ServerName.Trim()}]");
                if (int.TryParse(smsServerInfo.AppDataInfo.RetryCount.Trim(), out retryCount))
                {
                    Log.Info($"SendSMSMessageToSMSServer Retry Count[{retryCount}]");
                }
                else
                {
                    retryCount = 1;
                    Log.Info($"SendSMSMessageToSMSServer Retry Count[{retryCount}]");
                }
                int currentRetry = 0;
                while (currentRetry < retryCount)
                {
                    try
                    {

                        Log.Info($"SendSMSMessageToSMSServer process[{currentRetry + 1}] start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        retnBytes = await Task.Run(() =>
                        {
                            byte[] receiveBytes1, receiveBytes2;
                            try
                            {
                                socketAsyncEventArgsClient1 = new SocketAsyncEventArgsClient(smsServer1.ServerName.Trim(), smsServer1.ListenerPort, smsServer1.ConnectionTimeoutMilliseconds, smsServer1.SendReceiveTimeoutMilliseconds);
                                socketAsyncEventArgsClient1.Connect();
                                receiveBytes1 = socketAsyncEventArgsClient1.SendReceive(sendBytes);
                                if (receiveBytes1 == null)
                                {
                                    Log.Info($"Receive Message from Server[{smsServer1.ServerName.Trim()}] is null at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                }
                                else
                                {
                                    Log.Info($"Receive Message from Server[{smsServer1.ServerName.Trim()}]: [{big5encode.GetString(receiveBytes1)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                }
                                socketAsyncEventArgsClient1.Disconnect(SocketShutdown.Both);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"SendSMSMessageToSMSServer process error occurred while sending/receiving message to/from Server[{smsServer1.ServerName.Trim()}]: {ex.Message}");
                                receiveBytes1 = null;
                            }

                            if (receiveBytes1 == null)
                            {
                                try
                                {
                                    socketAsyncEventArgsClient2 = new SocketAsyncEventArgsClient(smsServer2.ServerName.Trim(), smsServer2.ListenerPort, smsServer2.ConnectionTimeoutMilliseconds, smsServer2.SendReceiveTimeoutMilliseconds);
                                    socketAsyncEventArgsClient2.Connect();
                                    receiveBytes2 = socketAsyncEventArgsClient2.SendReceive(sendBytes);
                                    if (receiveBytes2 == null)
                                    {
                                        Log.Info($"Receive Message from Server[{smsServer2.ServerName.Trim()}] is null at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                    }
                                    else
                                    {
                                        Log.Info($"Receive Message from Server[{smsServer2.ServerName.Trim()}]: [{big5encode.GetString(receiveBytes2)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                                    }
                                    socketAsyncEventArgsClient2.Disconnect(SocketShutdown.Both);
                                    return receiveBytes2;
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"SendSMSMessageToSMSServer process error occurred while sending/receiving message to/from Server[{smsServer2.ServerName.Trim()}]: {ex.Message}");
                                    receiveBytes2 = null;
                                    return null;
                                }
                            }
                            return receiveBytes1;
                        });
                        if (retnBytes == null)
                        {
                            Log.Info($"Receive Message is null at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        }
                        else
                        {
                            Log.Info($"Receive Message [{big5encode.GetString(retnBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                            break;
                        }
                        currentRetry++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"SendSMSMessageToSMSServer Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        currentRetry++;
                    }
                }
                var check = retnBytes == null ? string.Empty : big5encode.GetString(retnBytes);
                if (!check.Equals("<OK>")) return "R001";
                return "S001";
            }
            catch (Exception ex)
            {
                Log.Error($"SendSMSMessageToSMSServer Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return "R002";
            }
        }

        /// <summary>
        /// Send SMSWebAPI to SMS Server.
        /// </summary>
        /// <param name="sendBytes"></param>
        /// <param name="smsServer"></param>
        /// <returns></returns>
        public static async Task SendSMSMessageToWebAPI(StringContent stringContent)
        {
            try
            {
                if (stringContent == null)
                {
                    Log.Info($"SendSMSMessageToWebAPI no process(data invalid) start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return;
                }
                Log.Info($"SendSMSMessageToWebAPI process start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var correlationId = Guid.NewGuid().ToString();
                var _jsonOption = new JsonSerializerOptions() { WriteIndented = true };
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.Urls.Url[0]);
                httpClient.Timeout = new TimeSpan(0, 0, 0, Convert.ToInt32(SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.TimeoutMilliseconds) / 1000);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-correlation-id", correlationId);
                using var response = await httpClient.PostAsync($"{SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.Urls.Url[0]}Messages", stringContent);
                if (response.IsSuccessStatusCode) //range 200-299
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var sMSResponse = System.Text.Json.JsonSerializer.Deserialize<SMSResponseModel>(apiResponse, _jsonOption);
                    //Log.Info($"SendSMSMessageToWebAPI Return Message[{sMSResponse?.SMS_STATUS_STRING}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    Log.Info($"SendSMSMessageToWebAPI Success at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }
                else
                {
                    Log.Info($"SendSMSMessageToWebAPI Call[SMS BackEnd] Fail :{(int)response.StatusCode}:{response.StatusCode} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendSMSMessageToWebAPI Call[SMS BackEnd] Fail Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }
        }

        /// <summary>
        /// Send SMSWebAPI to SMS Server.
        /// </summary>
        /// <param name="sendBytes"></param>
        /// <param name="smsServer"></param>
        /// <returns></returns>
        public static async Task SendSMSMessageToWebAPINoCert(StringContent stringContent)
        {
            try
            {
                if (stringContent == null)
                {
                    Log.Info($"SendSMSMessageToWebAPINoCert no process(data invalid) start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    return;
                }
                Log.Info($"SendSMSMessageToWebAPINoCert process start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var correlationId = Guid.NewGuid().ToString();
                var _jsonOption = new JsonSerializerOptions() { WriteIndented = true };
                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors) => true
                };
                using (HttpClient httpClient = new HttpClient(handler))
                {
                    try
                    {
                        httpClient.BaseAddress = new Uri(SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.Urls.Url[0]);
                        httpClient.Timeout = new TimeSpan(0, 0, 0, Convert.ToInt32(SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.TimeoutMilliseconds) / 1000);
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-correlation-id", correlationId);
                        using var response = await httpClient.PostAsync($"{SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.Urls.Url[0]}Messages", stringContent);
                        if (response.IsSuccessStatusCode) //range 200-299
                        {
                            string apiResponse = await response.Content.ReadAsStringAsync();
                            var sMSResponse = System.Text.Json.JsonSerializer.Deserialize<SMSResponseModel>(apiResponse, _jsonOption);
                            //Log.Info($"SendSMSMessageToWebAPINoCert Return Message[{sMSResponse?.SMS_STATUS_STRING}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                            Log.Info($"SendSMSMessageToWebAPINoCert Success at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        }
                        else
                        {
                            Log.Info($"SendSMSMessageToWebAPINoCert Call[SMS BackEnd] Fail :{(int)response.StatusCode}:{response.StatusCode} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"SendSMSMessageToWebAPINoCert Call[SMS BackEnd] Fail Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendSMSMessageToWebAPINoCert Call[SMS BackEnd] Fail Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Com.Cybersoft4u.Simulator.Web.WebServerSimulator
{
    // Legacy HSM server simulator: answers on the ports of Config\HSMServerInfo.xml (12119-12123) with the
    // ASCII function id and the old reply layouts of its own Config\HSMRespConfig.xml. No current client uses
    // those ports: the Auth's HSM client (DLL\SocketClient.dll) connects to port 8500, answered by
    // WpfHSMSimulator / WebHSMSimulator (CommonLib AsynTcpServer). Do not use this one for the auth tests.
    public class ServerSimulatorManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Memory properties (No appsettings.json writes)
        public bool IsRunning { get; private set; }
        public string Ports { get; private set; } = "12119,12120,12121,12122,12123";
        public int SessionCount { get; private set; } = 20;
        public string HSMRespConfigPath { get; private set; } = "Config/HSMRespConfig.xml";

        private readonly List<AsynTcpServer> _activeServers = new();
        private readonly List<Thread> _serverThreads = new();
        private readonly SortedList<string, string> _hsmRespList = new();

        // SSE telemetry push
        private readonly List<Action<string>> _logCallbacks = new();
        private readonly object _callbackLock = new();
        private UdpClient? _udpListener;
        private CancellationTokenSource? _udpCts;
        public bool IsUdpRunning { get; private set; }
        public int UdpListenerPort => 9929;

        public ServerSimulatorManager(IConfiguration configuration)
        {
            // Initial configuration parameters
            var section = configuration.GetSection("GeneralConfig");
            if (section.Exists())
            {
                Ports = section["ListenPortNumber"] ?? Ports;
                if (int.TryParse(section["SessionNumber"], out var sessions))
                {
                    SessionCount = sessions;
                }
                HSMRespConfigPath = section["HSMRespConfig"] ?? HSMRespConfigPath;
            }
        }

        public void StartService(string ports, int? sessions)
        {
            if (IsRunning) return;

            Ports = ports;
            if (sessions.HasValue) SessionCount = sessions.Value;

            Logger.Info($"[ServerSimulatorManager] Starting Server service for ports: {Ports} with sessions: {SessionCount}");

            // Load HSM Response codes
            LoadHsmRespConfig();

            // Clear previous servers
            _activeServers.Clear();
            _serverThreads.Clear();

            var portList = Ports.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToList();

            foreach (var portStr in portList)
            {
                if (!int.TryParse(portStr, out var port))
                {
                    Logger.Warn($"[ServerSimulatorManager] Invalid port format: {portStr}");
                    continue;
                }

                var server = new AsynTcpServer(_hsmRespList)
                {
                    PortNumber = port,
                    SessionNumber = SessionCount
                };

                var thread = new Thread(server.StartServer)
                {
                    Name = $"WebServerSim_{port}",
                    IsBackground = true
                };

                _activeServers.Add(server);
                _serverThreads.Add(thread);
                thread.Start();

                Logger.Info($"[ServerSimulatorManager] Server thread started: {thread.Name}");
            }

            IsRunning = true;
        }

        public void StopService()
        {
            if (!IsRunning) return;

            Logger.Info("[ServerSimulatorManager] Stopping all simulated server threads...");

            foreach (var server in _activeServers)
            {
                server.StopServer();
            }

            _activeServers.Clear();
            _serverThreads.Clear();
            IsRunning = false;

            Logger.Info("[ServerSimulatorManager] All server threads stopped completely.");
        }

        private void LoadHsmRespConfig()
        {
            _hsmRespList.Clear();

            // Resolve path (support both absolute and relative)
            string fullPath = Path.IsPathRooted(HSMRespConfigPath)
                ? HSMRespConfigPath
                : Path.Combine(AppContext.BaseDirectory, HSMRespConfigPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"HSM response config file not found at: {fullPath}");
            }

            var doc = XDocument.Load(fullPath);
            var suipFuncQuery = from results in doc.Descendants("SuipFunc")
                                select new
                                {
                                    key = results.Attribute("Function-ID")?.Value,
                                    value = results.Attribute("RespCode")?.Value
                                };

            foreach (var suipFunc in suipFuncQuery)
            {
                if (suipFunc.key != null && suipFunc.value != null)
                {
                    if (!_hsmRespList.ContainsKey(suipFunc.key))
                    {
                        _hsmRespList.Add(suipFunc.key, suipFunc.value);
                    }
                }
            }

            Logger.Info($"[ServerSimulatorManager] Loaded {_hsmRespList.Count} response mapping codes from {HSMRespConfigPath}.");
        }

        // UDP capturing and forwarding callbacks
        public void StartUdpService()
        {
            if (IsUdpRunning) return;

            _udpCts = new CancellationTokenSource();
            try
            {
                _udpListener = new UdpClient();
                _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpListener.Client.Bind(new IPEndPoint(IPAddress.Loopback, UdpListenerPort));

                IsUdpRunning = true;
                var token = _udpCts.Token;

                Task.Run(async () =>
                {
                    Logger.Info($"[ServerSimulatorManager] UDP Server listening on loopback port {UdpListenerPort}");
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            var result = await _udpListener.ReceiveAsync(token);
                            string message = Encoding.UTF8.GetString(result.Buffer);
                            BroadcastLog(message);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"UDP log listener encountered error: {ex.Message}");
                            // Avoid flooding if socket gets closed
                            if (token.IsCancellationRequested) break;
                            await Task.Delay(500, token);
                        }
                    }
                }, token);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to launch UDP Log capture service: {ex.Message}");
            }
        }

        public void RegisterLogCallback(Action<string> callback)
        {
            lock (_callbackLock)
            {
                _logCallbacks.Add(callback);
            }
        }

        public void UnregisterLogCallback(Action<string> callback)
        {
            lock (_callbackLock)
            {
                _logCallbacks.Remove(callback);
            }
        }

        private void BroadcastLog(string message)
        {
            lock (_callbackLock)
            {
                foreach (var callback in _logCallbacks)
                {
                    try
                    {
                        callback(message);
                    }
                    catch
                    {
                        // Ignore dead callback failures
                    }
                }
            }
        }
    }

    public class AsynTcpServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly byte[] _dataBuffer = new byte[1024];
        private readonly SortedList<string, string> _respSortedList;
        private Socket? _asyncServer;

        public bool IsRunning { get; set; }
        public int PortNumber { get; set; }
        public int SessionNumber { get; set; }

        public AsynTcpServer(SortedList<string, string> sortedList)
        {
            _respSortedList = new SortedList<string, string>(sortedList);
            IsRunning = false;
        }

        public void StartServer()
        {
            try
            {
                _asyncServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var iep = new IPEndPoint(IPAddress.Any, PortNumber);
                _asyncServer.Bind(iep);
                _asyncServer.Listen(SessionNumber);
                _asyncServer.BeginAccept(AcceptConn, _asyncServer);
                IsRunning = true;
                Log.Info($"HSMServer Listener PortNumber[{PortNumber}] Waiting Connect ...");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to start TCP Server on Port [{PortNumber}]: {ex.Message}");
            }
        }

        private void AcceptConn(IAsyncResult pAsyncRes)
        {
            try
            {
                if (_asyncServer == null || !IsRunning) return;
                Socket client = _asyncServer.EndAccept(pAsyncRes);
                Log.Info("Connected From : {0} At {1:yyyy-MM-dd HH:mm:ss:fff}", client.RemoteEndPoint, DateTime.Now);
                client.BeginReceive(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, CallBackReceiveData, client);

                // Continue accepting other connections
                _asyncServer.BeginAccept(AcceptConn, _asyncServer);
            }
            catch (ObjectDisposedException)
            {
                // Expected when listener closed
            }
            catch (Exception ex)
            {
                if (IsRunning)
                {
                    Log.Error(ex, $"Accept Connection Error: {ex.Message}");
                }
            }
        }

        private void CallBackSendData(IAsyncResult pAsyncRes)
        {
            try
            {
                var client = (Socket)pAsyncRes.AsyncState!;
                int lBytesSend = client.EndSend(pAsyncRes);
                Log.Info("Send Data Length[{0}]", lBytesSend);
                client.Close();
            }
            catch (Exception ex)
            {
                Log.Warn($"SendData Callback failed: {ex.Message}");
            }
        }

        private void CallBackReceiveData(IAsyncResult pAsyncRes)
        {
            var client = (Socket)pAsyncRes.AsyncState!;

            try
            {
                int bytesRead = client.EndReceive(pAsyncRes);
                if (bytesRead <= 2)
                {
                    string rcvData = bytesRead > 0 ? Encoding.ASCII.GetString(_dataBuffer, 0, bytesRead).TrimEnd() : string.Empty;
                    Log.Info("Listen PortNumber[{0}] Receive Data Error, Data[{1}]", PortNumber, rcvData);

                    var errorMsg = Encoding.ASCII.GetBytes("9901");
                    client.BeginSend(errorMsg, 0, errorMsg.Length, SocketFlags.None, CallBackSendData, client);
                    Log.Info("Send Data[{0}], Length[{1}]", Encoding.ASCII.GetString(errorMsg), errorMsg.Length);
                    return;
                }

                string dataBufferString = Encoding.ASCII.GetString(_dataBuffer, 0, bytesRead).TrimEnd();
                Log.Info("Listen PortNumber[{0}] Receive Data[{1}]", PortNumber, dataBufferString);

                string funcID = dataBufferString.Substring(0, 2);
                var respCode = FindRespCodeByFuncID(funcID);
                var message = Encoding.ASCII.GetBytes(respCode);

                client.BeginSend(message, 0, message.Length, SocketFlags.None, CallBackSendData, client);
                Log.Info("Send Data[{0}], Length[{1}]", Encoding.ASCII.GetString(message), message.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"ReceiveData Callback failed ({ex.Message}). Closing connection.");
                try { client.Close(); } catch { /* Ignore socket close errors during error handling */ }
            }
        }

        private string FindRespCodeByFuncID(string funcID)
        {
            try
            {
                if (_respSortedList.TryGetValue(funcID, out var respCode))
                {
                    string returnString = respCode.Replace('x', '0');
                    returnString = returnString.Replace("-", string.Empty);
                    return returnString;
                }
                return "9901";
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FindRespCodeByFuncID Error: {ex.Message}");
                return "9901";
            }
        }

        public void StopServer()
        {
            IsRunning = false;
            try
            {
                if (_asyncServer != null)
                {
                    _asyncServer.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Failed to close server socket safely: {ex.Message}");
            }
        }
    }
}

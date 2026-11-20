using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Network.Server;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebServerMonitor
{
    public class ServerMonitorManager : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ServerMonitorService? _monitorServerService;
        private UdpClient? _udpListener;
        private bool _isStopUdpServer = true;

        public ServiceMonitorInfo? ServiceMonitorInfo { get; private set; }
        public List<string> ClientAuthPorts { get; private set; } = new();

        // --- Event Notifications ---
        public event Action<string, bool>? OnProcessLog; // Message, IsError
        public event Action<string>? OnUdpLogReceived;  // Log payload

        public ServerMonitorManager()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // --- Load Config XML ---
        public void LoadConfigurations()
        {
            try
            {
                var loader = new ConfigurationLoaderRoot();
                var configPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "service-monitor-info-xml-file-path")?.Trim();

                if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
                {
                    var serializer = new XmlSerializer(typeof(ServiceMonitorInfo));
                    using var reader = new StreamReader(configPath);
                    ServiceMonitorInfo = (ServiceMonitorInfo)serializer.Deserialize(reader)!;
                }

                var portsStr = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ClientAuthServiceLocalPorts")?.Trim();
                if (!string.IsNullOrEmpty(portsStr))
                {
                    ClientAuthPorts = portsStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                TriggerProcessLog($"Configurations loaded. Ports list: {string.Join(", ", ClientAuthPorts)}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load service monitor configurations");
                TriggerProcessLog($"Config Load Error: {ex.Message}", true);
            }
        }

        // --- Start Server Monitor Service ---
        public void StartMonitorService()
        {
            if (ServiceMonitorInfo == null)
            {
                TriggerProcessLog("Error: Configuration info is empty. Cannot start service.", true);
                return;
            }

            try
            {
                var serviceInfo = ServiceMonitorInfo.ServiceInfo;

                // Get IPAddress from host
                var hostEntry = Dns.GetHostEntry(serviceInfo.ServerName.Trim());
                var ipAddress = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                if (ipAddress == null)
                {
                    throw new Exception($"Cannot resolve IPv4 address for Host: {serviceInfo.ServerName}");
                }

                int port = Convert.ToInt32(serviceInfo.ListenerPort.Trim());
                int backlog = Convert.ToInt32(serviceInfo.BackLog.Trim());

                TriggerProcessLog($"Starting ServerMonitorService at {ipAddress}:{port}...");

                _monitorServerService = new ServerMonitorService(
                    ipAddress.ToString(),
                    port,
                    backlog,
                    "ServerMonitorService"
                );

                _monitorServerService.StartService();
                TriggerProcessLog($"ServerMonitorService running. Port: [{port}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Server service startup exception");
                TriggerProcessLog($"Service Startup Error: {ex.Message}", true);
            }
        }

        // --- Stop Server Monitor Service ---
        public void StopMonitorService()
        {
            try
            {
                _monitorServerService?.StopService();
                _monitorServerService = null;
                TriggerProcessLog("ServerMonitorService stopped.");
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to cleanly stop ServerMonitorService");
            }
        }

        // --- Get Client Connection States ---
        public System.Collections.Concurrent.ConcurrentDictionary<string, string> GetConnections()
        {
            if (_monitorServerService == null) return new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            return _monitorServerService.GetClientOfConnections() ?? new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        }

        // --- Send Command Message via Netty Channel ---
        public int SendCommandToClient(string clientIpAddress, string categoryCode, string commandCode)
        {
            if (_monitorServerService == null)
            {
                TriggerProcessLog("Error: Monitor service is not running.", true);
                return -1;
            }

            try
            {
                var channels = _monitorServerService.GetClientOfChannels();
                if (channels == null || !channels.TryGetValue(clientIpAddress, out var ctx))
                {
                    TriggerProcessLog($"Error: Client channel [{clientIpAddress}] not found or offline.", true);
                    return -2;
                }

                // Compile Interchange Message
                var interchangeMessage = new InterchangeMessage();
                var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
                interchangeMessage.StartingPoint = 'S';
                interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
                interchangeMessage.CreateDateTime = dateTimeString;

                interchangeMessage.SourceEndPoint = ctx.Channel.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                interchangeMessage.Category = $"G--{categoryCode}";
                interchangeMessage.CommandCode = commandCode;
                interchangeMessage.ReturnOption = true;

                TriggerProcessLog($"Sending command [{commandCode}] category [{categoryCode}] to [{clientIpAddress}]");

                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);

                // Write and flush Netty channel
                ctx.Channel.WriteAndFlush(sendBytes);

                // Write diagnostic log
                WriteSendMessageToLog(interchangeMessage.SourceEndPoint ?? string.Empty, interchangeMessage.DestinationEndPoint ?? string.Empty, sendBytes);

                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send command to client");
                TriggerProcessLog($"Send Command Error: {ex.Message}", true);
                return -3;
            }
        }

        // --- UDP Service Lifecycle ---
        public void StartUdpService()
        {
            if (ServiceMonitorInfo == null) return;
            if (!Convert.ToBoolean(ServiceMonitorInfo.UDPServiceInfo.IsStart.Trim()))
            {
                TriggerProcessLog("UDP service is disabled in configurations.");
                return;
            }

            int port = Convert.ToInt32(ServiceMonitorInfo.UDPServiceInfo.ListenerPort.Trim());
            _isStopUdpServer = false;

            Task.Run(() =>
            {
                try
                {
                    TriggerProcessLog($"UDP Receiver Service started on port: {port}");
                    _udpListener = new UdpClient(port);
                    var groupEP = new IPEndPoint(IPAddress.Any, port);

                    while (!_isStopUdpServer)
                    {
                        byte[] readBytes = _udpListener.Receive(ref groupEP);
                        if (readBytes == null || readBytes.Length == 0) continue;

                        string text = Encoding.Default.GetString(readBytes);
                        OnUdpLogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] {text}");
                    }
                }
                catch (SocketException sex) when (sex.SocketErrorCode == SocketError.Interrupted)
                {
                    // Clean exit on close
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "UDP receiver failure");
                    TriggerProcessLog($"UDP Service Error: {ex.Message}", true);
                }
                finally
                {
                    _udpListener?.Close();
                    _udpListener = null;
                    TriggerProcessLog("UDP Receiver Service stopped.");
                }
            });
        }

        public void StopUdpService()
        {
            _isStopUdpServer = true;
            _udpListener?.Close();
        }

        private void WriteSendMessageToLog(string localEndPoint, string remoteEndPoint, byte[] bytes)
        {
            try
            {
                var length = bytes?.Length ?? 0;
                var logStr = $"WRITE [{localEndPoint} => {remoteEndPoint}]\n" +
                             $"Length: {length}B\n" +
                             $"{ColaCommUtility.PrintBytesMap(bytes ?? Array.Empty<byte>())}\n" +
                             $"[{ColaCommUtility.PrintBytesHex(bytes ?? Array.Empty<byte>())}]\n" +
                             $"[{ColaCommUtility.PrintBytesData(bytes ?? Array.Empty<byte>())}]";
                Log.Info(logStr);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to output diagnostic hex logs");
            }
        }

        private void TriggerProcessLog(string message, bool isError = false)
        {
            OnProcessLog?.Invoke(message, isError);
        }

        public void Dispose()
        {
            StopUdpService();
            StopMonitorService();
        }
    }
}

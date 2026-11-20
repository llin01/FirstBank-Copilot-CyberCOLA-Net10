using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebMonitorService
{
    public class MonitorServiceManager : IDisposable
    {
        private static readonly Com.Cybersoft4u.Utils.Logger Logger;

        static MonitorServiceManager()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WebMonitorService]");
        }

        private TCPClientServiceInfo? _tcpClientServiceInfo;
        private InterchangeMessageCommandInfo? _interchangeMessageCommandInfo;
        private TCPClientService? _tcpClientService;

        private CancellationTokenSource? _udpCts;
        private bool _isUdpRunning = false;
        private bool _isTcpStarted = false;
        private readonly List<string> _routerClientList = new();
        private bool _isInitialized = false;

        public event Action<string, string>? OnSystemLog; // message, color(e.g., "red", "green", "white")
        public event Action<string>? OnUdpLog;

        public TCPClientServiceInfo? ClientServiceInfo => _tcpClientServiceInfo;
        public InterchangeMessageCommandInfo? MessageCommandInfo => _interchangeMessageCommandInfo;
        public bool IsTcpConnected => _tcpClientService?.IsConnected() ?? false;
        public bool IsTcpStarted => _isTcpStarted;
        public bool IsUdpRunning => _isUdpRunning;

        public void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                Logger.Info($"{AppDomain.CurrentDomain.FriendlyName} manager initializing at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LogSystem("System initializing...", "cyan");

                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-client-service-info-xml-file-path");

                XmlSerializer serializer = new XmlSerializer(typeof(TCPClientServiceInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _tcpClientServiceInfo = (TCPClientServiceInfo?)serializer.Deserialize(reader);
                }

                var interchangeXmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "interchange-message-command-info-xml-file-path");
                serializer = new XmlSerializer(typeof(InterchangeMessageCommandInfo));
                using (XmlReader reader = XmlReader.Create(interchangeXmlFilePath))
                {
                    _interchangeMessageCommandInfo = (InterchangeMessageCommandInfo?)serializer.Deserialize(reader);
                }

                if (_tcpClientServiceInfo != null)
                {
                    var remoteHostName = _tcpClientServiceInfo.TCPClientServiceConfig.RemoteHostName.Trim();
                    var remoteIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.RemoteIP.Trim(), AddressFamily.InterNetwork);
                    var remotePortNumberString = _tcpClientServiceInfo.TCPClientServiceConfig.RemotePort.Trim();
                    var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddressString), Convert.ToInt32(remotePortNumberString));

                    var localIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.LocalIP.Trim(), AddressFamily.InterNetwork);
                    var localPortNumberString = _tcpClientServiceInfo.TCPClientServiceConfig.LocalPort.Trim();
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPAddressString), Convert.ToInt32(localPortNumberString));

                    _tcpClientService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
                    _tcpClientService.EnableLogger("TCPClientService", Utils.LogLevel.Debug);

                    if (Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.AutoStartFlag.Trim()))
                    {
                        StartTcpService();
                    }

                    var udpListenerPort = Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.UDPListenerPort.Trim());
                    var udpAutoStartFlag = Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.UDPAutoStartFlag.Trim());
                    if (udpAutoStartFlag)
                    {
                        StartUdpService(udpListenerPort);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Initialization failed: {ex.Message}");
                LogSystem($"Initialization Error: {ex.Message}", "red");
            }
        }

        public void StartTcpService()
        {
            try
            {
                if (_tcpClientService == null) return;
                if (!_isTcpStarted)
                {
                    _tcpClientService.Start();
                    _isTcpStarted = true;
                    LogSystem("TCP Client Service started.", "green");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"StartTcpService failed: {ex.Message}");
                LogSystem($"Start TCP Service Error: {ex.Message}", "red");
            }
        }

        public void StopTcpService()
        {
            try
            {
                if (_tcpClientService != null && _isTcpStarted)
                {
                    _tcpClientService.Stop();
                    _isTcpStarted = false;
                    LogSystem("TCP Client Service stopped.", "yellow");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"StopTcpService failed: {ex.Message}");
                LogSystem($"Stop TCP Service Error: {ex.Message}", "red");
            }
        }

        public void StartUdpService(int port)
        {
            if (_isUdpRunning) return;
            _udpCts = new CancellationTokenSource();
            _isUdpRunning = true;

            Task.Run(async () =>
            {
                UdpClient? udpClient = null;
                try
                {
                    udpClient = new UdpClient(port);
                    LogSystem($"UDP Server listener started on port {port}", "green");

                    while (!_udpCts.Token.IsCancellationRequested)
                    {
                        if (udpClient.Available <= 0)
                        {
                            await Task.Delay(100, _udpCts.Token);
                            continue;
                        }

                        var result = await udpClient.ReceiveAsync(_udpCts.Token);
                        if (result.Buffer != null && result.Buffer.Length > 0)
                        {
                            var logMsg = Encoding.Default.GetString(result.Buffer);
                            OnUdpLog?.Invoke(logMsg);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Clean shutdown
                }
                catch (Exception ex)
                {
                    Logger.Error($"UDP Server Exception: {ex.Message}");
                    LogSystem($"UDP Server Exception: {ex.Message}", "red");
                }
                finally
                {
                    udpClient?.Close();
                    _isUdpRunning = false;
                    LogSystem($"UDP Server listener stopped on port {port}", "yellow");
                }
            });
        }

        public void StopUdpService()
        {
            if (!_isUdpRunning) return;
            _udpCts?.Cancel();
            _isUdpRunning = false;
        }

        public async Task<List<string>> RefreshRouterClientConnectionInfoAsync()
        {
            _routerClientList.Clear();
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Not connected to server.", "red");
                return _routerClientList;
            }

            try
            {
                LogSystem($"Retrieval router client connection status at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}", "cyan");

                var interchangeMessage = CreateMessage('S');
                interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = _tcpClientService.RemoteEndPoint().ToString();
                interchangeMessage.Category = InterChangeMessageCategory.RouterSystemCommand;
                interchangeMessage.CommandCode = InterChangeMessageCommandCode.RouterSYSTEMCommandCode.GetConnectionStatus;

                LogSystem("--- Start display send InterchangeMessage ---", "gray");
                LogSystem(ColaCommUtility.GetObjectProperty(interchangeMessage), "gray");
                LogSystem("--- End display send InterchangeMessage ---", "gray");

                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);

                _tcpClientService.Send(sendBytes);
                await Task.Delay(1000);

                var readBytes = _tcpClientService.GetReturnBytes;
                if (readBytes == null)
                {
                    LogSystem("Receive message no data from Router.", "red");
                    return _routerClientList;
                }

                var returnMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                if (returnMsg == null)
                {
                    LogSystem("Receive message invalid format.", "red");
                }
                else
                {
                    var dataStr = System.Text.Encoding.ASCII.GetString(returnMsg.InterchangeData);
                    LogSystem($"Received: {dataStr}", "green");

                    if (!string.IsNullOrEmpty(dataStr))
                    {
                        var dataElement = dataStr.Trim().Split(new[] { "!" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var data in dataElement)
                        {
                            var fieldElement = data.Trim().Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
                            if (fieldElement.Length >= 2)
                            {
                                // Store raw type and endpoint
                                _routerClientList.Add($"{fieldElement[0]}#{fieldElement[1]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RefreshConnectionInfo Error: {ex.Message}");
                LogSystem($"Exception: {ex.Message}", "red");
            }

            return _routerClientList;
        }

        public async Task<List<GatewayClientRouterInfo>> QueryGatewayInfoAsync(List<string> targetIps, string commandCode)
        {
            var results = new List<GatewayClientRouterInfo>();
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Not connected to server, cannot query.", "red");
                return results;
            }

            try
            {
                LogSystem($"Start send message to gateway client at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}", "cyan");
                int sendCount = 0;

                foreach (var ip in targetIps)
                {
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = ip;
                    interchangeMessage.Category = InterChangeMessageCategory.GatewaySystemCommand;
                    interchangeMessage.CommandCode = commandCode.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                    interchangeMessage.InterchangeData = null;
                    interchangeMessage.ReturnOption = true;

                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
                    sendCount++;

                    _tcpClientService.Send(sendBytes);
                    await Task.Delay(1000);

                    var readBytes = _tcpClientService.GetReturnBytes;
                    if (readBytes == null)
                    {
                        LogSystem($"Receive message no data for target {ip}", "red");
                        continue;
                    }

                    var returnMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (returnMsg == null)
                    {
                        LogSystem($"Receive message invalid for target {ip}", "red");
                        continue;
                    }

                    if (returnMsg.InterchangeData != null)
                    {
                        var returnedData = Encoding.ASCII.GetString(returnMsg.InterchangeData);
                        LogSystem($"Response for {ip}: {returnedData}", "green");
                    }

                    // Query Database log using messageId
                    var messageId = returnMsg.MessageId;
                    var dt = StoreProcUtility.GetServiceRouterLogByMessageId(messageId);
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        LogSystem($"Database log query for MessageId [{messageId}] yielded no records.", "yellow");
                        continue;
                    }

                    LogSystem($"Database found {dt.Rows.Count} logs for message ID: {messageId}", "green");

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var row = dt.Rows[i];
                        var interchangeData = row.Field<string>("Interchange_Data");
                        if (string.IsNullOrEmpty(interchangeData) || interchangeData.Contains("No Data")) continue;

                        if (commandCode.StartsWith("01"))
                        {
                            // Parse plain text format
                            var lines = interchangeData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                results.Add(new GatewayClientRouterInfo
                                {
                                    ClientIPEndPoint = ip,
                                    LocalEndPoint = $"Command : {commandCode}",
                                    RemoteEndPoint = line,
                                    CumulativeToDate = "",
                                    NowProcessDateTime = "",
                                    TotalCount = ""
                                });
                            }
                        }
                        else
                        {
                            // XML parsing
                            try
                            {
                                var xmlStr = ColaCommUtility.PrettyXml(interchangeData);
                                XElement root = XElement.Parse(xmlStr);
                                foreach (var element in root.Elements("anyType"))
                                {
                                    results.Add(new GatewayClientRouterInfo
                                    {
                                        ClientIPEndPoint = ip,
                                        LocalEndPoint = element.Element("LocalEndPoint")?.Value ?? "",
                                        RemoteEndPoint = element.Element("RemoteEndPoint")?.Value ?? "",
                                        InitialCreateDateTime = element.Element("InitialCreateDateTime")?.Value ?? "",
                                        CumulativeToDate = element.Element("CumulativeToDate")?.Value ?? "",
                                        NowProcessDateTime = element.Element("NowProcessDateTime")?.Value ?? "",
                                        NowProcessTransactionType = element.Element("NowProcessTransactionType")?.Value ?? "",
                                        TotalCount = element.Element("TotalCount")?.Value ?? "",
                                        DailyTotalCount = element.Element("DailyTotalCount")?.Value ?? ""
                                    });
                                }
                            }
                            catch (Exception xmlEx)
                            {
                                LogSystem($"XML Parsing error: {xmlEx.Message}", "red");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"QueryGatewayInfoAsync Exception: {ex.Message}");
                LogSystem($"Exception: {ex.Message}", "red");
            }
            return results;
        }

        public async Task<List<ClientAuthClientRouterInfo>> QueryClientAuthInfoAsync(List<string> targetIps, string commandCode)
        {
            var results = new List<ClientAuthClientRouterInfo>();
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Not connected to server, cannot query.", "red");
                return results;
            }

            try
            {
                LogSystem($"Start send message to clientauth client at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}", "cyan");
                int sendCount = 0;

                foreach (var ip in targetIps)
                {
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = ip;
                    interchangeMessage.Category = InterChangeMessageCategory.ClientAuthSystemCommand;
                    interchangeMessage.CommandCode = commandCode.Split(new[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                    interchangeMessage.InterchangeData = null;
                    interchangeMessage.ReturnOption = true;

                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
                    sendCount++;

                    _tcpClientService.Send(sendBytes);
                    await Task.Delay(1000);

                    var readBytes = _tcpClientService.GetReturnBytes;
                    if (readBytes == null)
                    {
                        LogSystem($"Receive message no data for target {ip}", "red");
                        continue;
                    }

                    var returnMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (returnMsg == null)
                    {
                        LogSystem($"Receive message invalid for target {ip}", "red");
                        continue;
                    }

                    if (returnMsg.InterchangeData != null)
                    {
                        var returnedData = Encoding.ASCII.GetString(returnMsg.InterchangeData);
                        LogSystem($"Response for {ip}: {returnedData}", "green");
                    }

                    var messageId = returnMsg.MessageId;
                    var dt = StoreProcUtility.GetServiceRouterLogByMessageId(messageId);
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        LogSystem($"Database log query for MessageId [{messageId}] yielded no records.", "yellow");
                        continue;
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var row = dt.Rows[i];
                        var interchangeData = row.Field<string>("Interchange_Data");
                        if (string.IsNullOrEmpty(interchangeData) || interchangeData.Contains("No Data")) continue;

                        var lines = interchangeData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (commandCode.StartsWith("01"))
                            {
                                var subParts = line.Split(new[] { '=', '>' }, StringSplitOptions.RemoveEmptyEntries);
                                if (subParts.Length == 2)
                                {
                                    results.Add(new ClientAuthClientRouterInfo
                                    {
                                        ClientIPEndPoint = ip,
                                        Column1 = subParts[0].Replace("[", "").Replace("]", "").Replace("-", ""),
                                        Column2 = subParts[1].Replace("[", "").Replace("]", "").Replace("-", "")
                                    });
                                }
                            }
                            else
                            {
                                results.Add(new ClientAuthClientRouterInfo
                                {
                                    ClientIPEndPoint = ip,
                                    Column1 = $"Command : {commandCode}",
                                    Column2 = line
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"QueryClientAuthInfoAsync Exception: {ex.Message}");
                LogSystem($"Exception: {ex.Message}", "red");
            }
            return results;
        }

        private InterchangeMessage CreateMessage(char startingPoint = 'S')
        {
            var interchangeMessage = new InterchangeMessage();
            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
            interchangeMessage.StartingPoint = startingPoint;
            interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
            interchangeMessage.CreateDateTime = dateTimeString;
            interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Null;
            interchangeMessage.InterchangeData = null;
            return interchangeMessage;
        }

        private void WriteSendMessageToLog(string? localEndPoint, string? remoteEndPoint, byte[]? bytes)
        {
            try
            {
                var length = bytes?.Length ?? 0;
                Logger.Info(() => $"WRITE [{localEndPoint ?? ""} => {remoteEndPoint ?? ""}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(bytes ?? Array.Empty<byte>())}");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
            }
        }

        private void LogSystem(string msg, string color)
        {
            OnSystemLog?.Invoke(msg, color);
        }

        public void Dispose()
        {
            StopTcpService();
            StopUdpService();
        }
    }

    public class GatewayClientRouterInfo
    {
        public string ClientIPEndPoint { get; set; } = string.Empty;
        public string LocalEndPoint { get; set; } = string.Empty;
        public string RemoteEndPoint { get; set; } = string.Empty;
        public string InitialCreateDateTime { get; set; } = string.Empty;
        public string CumulativeToDate { get; set; } = string.Empty;
        public string NowProcessDateTime { get; set; } = string.Empty;
        public string NowProcessTransactionType { get; set; } = string.Empty;
        public string TotalCount { get; set; } = string.Empty;
        public string DailyTotalCount { get; set; } = string.Empty;
    }

    public class ClientAuthClientRouterInfo
    {
        public string ClientIPEndPoint { get; set; } = string.Empty;
        public string Column1 { get; set; } = string.Empty;
        public string Column2 { get; set; } = string.Empty;
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Test.Web.WebTCPClientAuthRouterClient.Models;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebTCPClientAuthRouterClient
{
    public class TCPClientAuthRouterManager : IDisposable
    {
        private static readonly Com.Cybersoft4u.Utils.Logger Logger;

        static TCPClientAuthRouterManager()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WebTCPClientAuthRouterService]");
        }

        private TCPClientAuthRouterServiceInfo? _tcpClientAuthRouterServiceInfo;
        private InterchangeMessageCommandInfo? _interchangeMessageCommandInfo;
        private TCPClientService? _tcpClientService;

        private CancellationTokenSource? _udpCts;
        private bool _isUdpRunning = false;
        private bool _isTcpStarted = false;
        private bool _isInitialized = false;
        private int _selectedServerIndex = 0;
        private string _selectGatewayType = string.Empty;

        public event Action<string, string>? OnSystemLog; // message, color
        public event Action<string>? OnUdpLog;

        public TCPClientAuthRouterServiceInfo? ServiceInfo => _tcpClientAuthRouterServiceInfo;
        public InterchangeMessageCommandInfo? MessageCommandInfo => _interchangeMessageCommandInfo;

        public bool IsTcpConnected => _tcpClientService?.IsConnected() ?? false;
        public bool IsTcpStarted => _isTcpStarted;
        public bool IsUdpRunning => _isUdpRunning;
        public string SelectedGatewayType => _selectGatewayType;
        public int SelectedServerIndex => _selectedServerIndex;

        public string LocalEndPointStr => _tcpClientService?.LocalEndPoint()?.ToString() ?? "N/A";
        public string RemoteEndPointStr => _tcpClientService?.RemoteEndPoint()?.ToString() ?? "N/A";

        public void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                Logger.Info("TCPClientAuthRouterManager initializing...");
                LogSystem("System initializing...", "cyan");

                var configurationLoaderRoot = new ConfigurationLoaderRoot();

                var routerXmlPath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-clientauth-router-service-info-xml-file-path");
                var cmdXmlPath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "interchange-message-command-info-xml-file-path");

                if (!string.IsNullOrEmpty(routerXmlPath) && File.Exists(routerXmlPath))
                {
                    var serializer = new XmlSerializer(typeof(TCPClientAuthRouterServiceInfo));
                    using var reader = XmlReader.Create(routerXmlPath);
                    _tcpClientAuthRouterServiceInfo = serializer.Deserialize(reader) as TCPClientAuthRouterServiceInfo;
                }

                if (!string.IsNullOrEmpty(cmdXmlPath) && File.Exists(cmdXmlPath))
                {
                    var serializer = new XmlSerializer(typeof(InterchangeMessageCommandInfo));
                    using var reader = XmlReader.Create(cmdXmlPath);
                    _interchangeMessageCommandInfo = serializer.Deserialize(reader) as InterchangeMessageCommandInfo;
                }

                if (_tcpClientAuthRouterServiceInfo != null)
                {
                    _selectedServerIndex = Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.DefaultServerIndex);

                    if (_selectedServerIndex >= _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Servers.Server.Count)
                    {
                        LogSystem("Default Server Select Index Invalid, reset index to 0", "yellow");
                        _selectedServerIndex = 0;
                    }

                    // Auto connect if flag is true
                    if (Convert.ToBoolean(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.AutoStartFlag.Trim()))
                    {
                        StartTcpService(_selectedServerIndex);
                    }

                    var udpPort = Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPListenerPort.Trim());
                    var udpAutoStart = Convert.ToBoolean(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPAutoStartFlag.Trim());
                    if (udpAutoStart)
                    {
                        StartUdpService(udpPort);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Initialization failed: {ex.Message}");
                LogSystem($"Initialization Error: {ex.Message}", "red");
            }
        }

        public void StartTcpService(int serverIndex)
        {
            try
            {
                if (_tcpClientAuthRouterServiceInfo == null) return;

                _selectedServerIndex = serverIndex;
                var serverConfig = _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Servers.Server[_selectedServerIndex];

                var remoteHostName = serverConfig.RemoteHostName.Trim();
                var remoteIP = CommonUtils.GetIPAddressStringByHostName(serverConfig.RemoteIP.Trim(), AddressFamily.InterNetwork);
                var remotePort = Convert.ToInt32(serverConfig.RemotePort.Trim());
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIP), remotePort);

                var localIP = CommonUtils.GetIPAddressStringByHostName(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.LocalIP.Trim(), AddressFamily.InterNetwork);
                var localPort = Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.LocalPort.Trim());
                var localEndPoint = new IPEndPoint(IPAddress.Parse(localIP), localPort);

                _selectGatewayType = serverConfig.GatewayType.ToUpper().Trim();

                if (_tcpClientService != null)
                {
                    _tcpClientService.Stop();
                }

                _tcpClientService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
                _tcpClientService.EnableLogger("TCPClientAuthRouterService", Utils.LogLevel.Debug);

                _tcpClientService.Start();
                _isTcpStarted = true;
                LogSystem($"TCP Client Service started & connecting to {serverConfig.RemoteIP}:{serverConfig.RemotePort}.", "green");
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
                if (_tcpClientService != null)
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

            Task.Run(async () => await ReadUdpLogsAsync(port, _udpCts.Token));
            LogSystem($"UDP Server listener started on port {port}", "green");
        }

        public void StopUdpService()
        {
            if (!_isUdpRunning) return;
            _udpCts?.Cancel();
            _isUdpRunning = false;
            LogSystem("UDP Server listener stopped.", "yellow");
        }

        private async Task ReadUdpLogsAsync(int port, CancellationToken token)
        {
            using var udpClient = new UdpClient(port);
            var groupEP = new IPEndPoint(IPAddress.Any, port);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (udpClient.Available > 0)
                    {
                        var readBytes = udpClient.Receive(ref groupEP);
                        if (readBytes != null && readBytes.Length > 0)
                        {
                            var logMsg = Encoding.Default.GetString(readBytes);
                            OnUdpLog?.Invoke(logMsg);
                        }
                    }
                    else
                    {
                        await Task.Delay(100, token);
                    }
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested) break;
                    Logger.Error($"UDP Receive Error: {ex.Message}");
                }
            }
        }

        public async Task<string> SendClientAuthCommandAsync(string category, string categoryItem, string commandCode)
        {
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Cannot send command: Service is disconnected.", "red");
                return string.Empty;
            }

            try
            {
                var interchangeMessage = CreateMessage('S');
                var logKey = interchangeMessage.MessageId;

                interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = _tcpClientService.RemoteEndPoint().ToString();
                interchangeMessage.Category = $"{category}--{categoryItem}";
                interchangeMessage.CommandCode = commandCode;

                var tempString = categoryItem;
                if (!tempString.ToUpper().Equals("S"))
                {
                    if (_selectGatewayType.Equals("IAG"))
                    {
                        if (!"VM".Contains(tempString))
                        {
                            LogSystem($"GatewayType[{_selectGatewayType}], Select Category[{tempString}] Error", "red");
                            return string.Empty;
                        }
                    }
                    else if (!_selectGatewayType.Equals(tempString))
                    {
                        LogSystem($"GatewayType[{_selectGatewayType}] mismatch with Category[{tempString}]", "red");
                        return string.Empty;
                    }
                }

                if (commandCode.Equals(InterChangeMessageCommandCode.NCCCClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.NCPSClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0300) ||
                    commandCode.Equals(InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0302) ||
                    commandCode.Equals(InterChangeMessageCommandCode.TWINClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.SVCSClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.ICASHClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.IPASSClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.VISAClientCommandCode.FileMaintUpdate) ||
                    commandCode.Equals(InterChangeMessageCommandCode.MASTERCARDClientCommandCode.FileMaintUpdate))
                {
                    byte[]? fileMaintDataBytes;
                    if (commandCode.Equals(InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0300))
                        fileMaintDataBytes = CreateFileMaintDataBytes(1);
                    else
                        fileMaintDataBytes = CreateFileMaintDataBytes(2);

                    if (fileMaintDataBytes != null)
                    {
                        interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Binary;
                        interchangeMessage.InterchangeData = fileMaintDataBytes;
                    }
                    else
                    {
                        LogSystem("Create file maintenance data error", "red");
                        return string.Empty;
                    }
                }

                interchangeMessage.ReturnOption = true;

                LogSystem("--- Sending Command to ClientAuth Router ---", "gray");
                LogSystem(ColaCommUtility.GetObjectProperty(interchangeMessage), "gray");

                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                _tcpClientService.Send(sendBytes);

                await Task.Delay(1000);

                var readBytes = _tcpClientService.GetReturnBytes;
                if (readBytes == null)
                {
                    LogSystem("Response: No data returned from router", "red");
                }
                else
                {
                    var respMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (respMsg == null)
                    {
                        LogSystem("Response: Invalid XML format", "red");
                    }
                    else if (respMsg.InterchangeData != null)
                    {
                        var respStr = Encoding.ASCII.GetString(respMsg.InterchangeData);
                        LogSystem($"Response: {respStr}", "green");
                    }
                }

                return logKey;
            }
            catch (Exception ex)
            {
                LogSystem($"Command error: {ex.Message}", "red");
                Logger.Error($"Command error: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task QueryDbLogsAsync(string txSeq)
        {
            if (_tcpClientAuthRouterServiceInfo == null) return;

            try
            {
                LogSystem($"Connecting to DB to read transaction logs for Key: {txSeq}...", "cyan");

                await Task.Run(() =>
                {
                    using var dataObject = new NCARDContext(_tcpClientAuthRouterServiceInfo.DatabaseInfo.ConnectionString);
                    var logs = dataObject.ColaTranLogs
                        .Where(c => c.TranLogKey == txSeq)
                        .OrderBy(c => c.MessageType)
                        .ToList();

                    if (!logs.Any())
                    {
                        LogSystem($"No transaction logs found in DB for Key: {txSeq}", "yellow");
                        return;
                    }

                    foreach (var item in logs)
                    {
                        LogSystem($"[DB Log] TxSeq[{item.TranLogKey}], MessageType[{item.MessageType}], RespondCode[{item.P039}], InformationCode[{item.S070}]", "green");
                    }
                });
            }
            catch (Exception ex)
            {
                LogSystem($"Database query failed: {ex.Message}", "red");
                Logger.Error($"Database query failed: {ex.Message}");
            }
        }

        private byte[]? CreateFileMaintDataBytes(int option)
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                FileMaintDataXml? fileMaintDataXml;
                string? xmlFilePath;

                if (option == 1)
                    xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "fisc-file-maintenance-xml-file-path");
                else
                    xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "nccc-file-maintenance-xml-file-path");

                if (string.IsNullOrEmpty(xmlFilePath) || !File.Exists(xmlFilePath))
                {
                    LogSystem($"XML file path for maintenance doesn't exist: {xmlFilePath}", "red");
                    return null;
                }

                var serializer = new XmlSerializer(typeof(FileMaintDataXml));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    fileMaintDataXml = serializer.Deserialize(reader) as FileMaintDataXml;
                }

                if (fileMaintDataXml == null)
                {
                    LogSystem("Deserialized maintenance XML is null", "red");
                    return null;
                }

                var fileMaintData = new FileMaintData();
                if (fileMaintDataXml.DataType != null && fileMaintDataXml.DataType.ToUpper().Equals("FIXED"))
                {
                    fileMaintData.TxKeySeq = fileMaintDataXml.TxKeySeq?.Text;
                    fileMaintData.IsoP2CardNbr = fileMaintDataXml.IsoP2CardNbr?.Text;
                    fileMaintData.IsoP3ProcessCde = fileMaintDataXml.IsoP3ProcessCde?.Text;
                    fileMaintData.IsoP7TxDteTime = fileMaintDataXml.IsoP7TxDteTime?.Text;
                    fileMaintData.IsoP11TraceNo = fileMaintDataXml.IsoP11TraceNo?.Text;
                    fileMaintData.IsoP13TransYear = fileMaintDataXml.IsoP13TransYear?.Text;
                    fileMaintData.IsoP14ExpirDte = fileMaintDataXml.IsoP14ExpirDte?.Text;
                    fileMaintData.IsoP33FwdIICode = fileMaintDataXml.IsoP33FwdIICode?.Text;
                    fileMaintData.IsoP36NTID = fileMaintDataXml.IsoP36NTID?.Text;
                    fileMaintData.IsoP37RefNbr = fileMaintDataXml.IsoP37RefNbr?.Text;
                    fileMaintData.IsoP39Rsp = fileMaintDataXml.IsoP39Rsp?.Text;
                    fileMaintData.IsoP48AdditionalData = fileMaintDataXml.IsoP48AdditionalData?.Text;
                    fileMaintData.IsoP49TranCurCode = fileMaintDataXml.IsoP49TranCurCode?.Text;
                    fileMaintData.IsoP58T05TxDte = fileMaintDataXml.IsoP58T05TxDte?.Text;
                    fileMaintData.IsoP60FromHostMaintData = fileMaintDataXml.IsoP60FromHostMaintData?.Text;
                    fileMaintData.IsoP63SystemID = fileMaintDataXml.IsoP63SystemID?.Text;
                    fileMaintData.IsoS73ActDte = fileMaintDataXml.IsoS73ActDte?.Text ?? "      ";
                    fileMaintData.IsoS91FileUpdCode = fileMaintDataXml.IsoS91FileUpdCode?.Text;
                    fileMaintData.IsoS92FileSecurityCode = fileMaintDataXml.IsoS92FileSecurityCode?.Text ?? "57";
                    fileMaintData.IsoS96MessageSecurity = fileMaintDataXml.IsoS96MessageSecurity?.Text;
                    fileMaintData.IsoS101FileName = fileMaintDataXml.IsoS101FileName?.Text;
                    fileMaintData.IsoS115AdditionalTraceData = fileMaintDataXml.IsoS115AdditionalTraceData?.Text;
                    fileMaintData.IsoS120FileUpd = fileMaintDataXml.IsoS120FileUpd?.Text;
                    fileMaintData.IsoS122HostMaint = fileMaintDataXml.IsoS122HostMaint?.Text;
                    fileMaintData.IsoS124TokenData = fileMaintDataXml.IsoS124TokenData?.Text;
                }
                else
                {
                    var random = new Random();
                    if (fileMaintDataXml.TxKeySeq != null && fileMaintDataXml.TxKeySeq.Changed.ToUpper().Equals("Y"))
                        fileMaintData.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    else
                        fileMaintData.TxKeySeq = fileMaintDataXml.TxKeySeq?.Text;

                    fileMaintData.IsoP2CardNbr = fileMaintDataXml.IsoP2CardNbr?.Text;
                    fileMaintData.IsoP3ProcessCde = fileMaintDataXml.IsoP3ProcessCde?.Text;

                    if (fileMaintDataXml.IsoP7TxDteTime != null && fileMaintDataXml.IsoP7TxDteTime.Changed.ToUpper().Equals("Y"))
                        fileMaintData.IsoP7TxDteTime = $"{DateTime.Now:MMddHHmmss}";
                    else
                        fileMaintData.IsoP7TxDteTime = fileMaintDataXml.IsoP7TxDteTime?.Text;

                    if (fileMaintDataXml.IsoP11TraceNo != null && fileMaintDataXml.IsoP11TraceNo.Changed.ToUpper().Equals("Y"))
                    {
                        int traceNumber = random.Next(1, 999999);
                        fileMaintData.IsoP11TraceNo = $"{traceNumber:000000}";
                    }
                    else
                        fileMaintData.IsoP11TraceNo = fileMaintDataXml.IsoP11TraceNo?.Text;

                    if (fileMaintDataXml.IsoP14ExpirDte != null && fileMaintDataXml.IsoP14ExpirDte.Changed.ToUpper().Equals("Y"))
                    {
                        fileMaintData.IsoP14ExpirDte = $"{DateTime.Now:yyMM}";
                    }
                    else
                        fileMaintData.IsoP14ExpirDte = fileMaintDataXml.IsoP14ExpirDte?.Text;

                    if (fileMaintDataXml.IsoP37RefNbr != null && fileMaintDataXml.IsoP37RefNbr.Changed.ToUpper().Equals("Y"))
                    {
                        int refNumber = random.Next(1, 999999);
                        fileMaintData.IsoP37RefNbr = $"{refNumber:000000000000}";
                    }
                    else
                        fileMaintData.IsoP37RefNbr = fileMaintDataXml.IsoP37RefNbr?.Text;

                    fileMaintData.IsoP33FwdIICode = fileMaintDataXml.IsoP33FwdIICode?.Text;
                    fileMaintData.IsoP39Rsp = fileMaintDataXml.IsoP39Rsp?.Text;
                    fileMaintData.IsoP48AdditionalData = fileMaintDataXml.IsoP48AdditionalData?.Text;
                    fileMaintData.IsoP49TranCurCode = fileMaintDataXml.IsoP49TranCurCode?.Text;

                    if (fileMaintDataXml.IsoP58T05TxDte != null && fileMaintDataXml.IsoP58T05TxDte.Changed.ToUpper().Equals("Y"))
                        fileMaintData.IsoP58T05TxDte = $"{DateTime.Now:yyyyMMddHHmmss}";
                    else
                        fileMaintData.IsoP58T05TxDte = fileMaintDataXml.IsoP58T05TxDte?.Text;

                    fileMaintData.IsoP60FromHostMaintData = fileMaintDataXml.IsoP60FromHostMaintData?.Text;
                    fileMaintData.IsoS73ActDte = fileMaintDataXml.IsoS73ActDte?.Text ?? "      ";
                    fileMaintData.IsoS91FileUpdCode = fileMaintDataXml.IsoS91FileUpdCode?.Text;
                    fileMaintData.IsoS92FileSecurityCode = fileMaintDataXml.IsoS92FileSecurityCode?.Text;
                    fileMaintData.IsoS96MessageSecurity = fileMaintDataXml.IsoS96MessageSecurity?.Text;
                    fileMaintData.IsoS101FileName = fileMaintDataXml.IsoS101FileName?.Text;
                    fileMaintData.IsoS115AdditionalTraceData = fileMaintDataXml.IsoS115AdditionalTraceData?.Text;
                    fileMaintData.IsoS120FileUpd = fileMaintDataXml.IsoS120FileUpd?.Text;
                    fileMaintData.IsoS122HostMaint = fileMaintDataXml.IsoS122HostMaint?.Text;
                    fileMaintData.IsoS124TokenData = fileMaintDataXml.IsoS124TokenData?.Text;
                }

                return ColaCommUtility.ObjectToXMLBytes(fileMaintData);
            }
            catch (Exception ex)
            {
                LogSystem($"CreateFileMaintDataBytes error: {ex.Message}", "red");
                Logger.Fatal(ex.Message);
                return null;
            }
        }

        private InterchangeMessage CreateMessage(char startingPoint = 'S')
        {
            var interchangeMessage = new InterchangeMessage();
            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
            interchangeMessage.StartingPoint = startingPoint;
            interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
            interchangeMessage.CreateDateTime = dateTimeString;
            interchangeMessage.ReturnDateTime = String.Empty;
            return interchangeMessage;
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
}

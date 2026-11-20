using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebTCPClientService
{
    public class TCPClientServiceManager : IDisposable
    {
        private static readonly Com.Cybersoft4u.Utils.Logger Logger;

        static TCPClientServiceManager()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WebTCPClientService]");
        }

        private TCPClientServiceInfo? _tcpClientServiceInfo;
        private InterchangeMessageCommandInfo? _interchangeMessageCommandInfo;
        private TCPClientService? _tcpClientService;

        private CancellationTokenSource? _udpCts;
        private bool _isUdpRunning = false;
        private bool _isTcpStarted = false;
        private bool _isInitialized = false;

        private readonly List<string> _gatewayList = new();
        private readonly List<string> _clientAuthList = new();
        private static readonly Random StaticTraceRnd = new();

        public event Action<string, string>? OnSystemLog; // message, color (e.g., "red", "green", "white")
        public event Action<string>? OnUdpLog;
        public event Action? OnConnectionListUpdated;

        public TCPClientServiceInfo? ClientServiceInfo => _tcpClientServiceInfo;
        public InterchangeMessageCommandInfo? MessageCommandInfo => _interchangeMessageCommandInfo;

        public List<string> GatewayList => _gatewayList;
        public List<string> ClientAuthList => _clientAuthList;

        public bool IsTcpConnected => _tcpClientService?.IsConnected() ?? false;
        public bool IsTcpStarted => _isTcpStarted;
        public bool IsUdpRunning => _isUdpRunning;

        public void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                Logger.Info("TCPClientServiceManager initializing...");
                LogSystem("System initializing...", "cyan");

                var configurationLoaderRoot = new ConfigurationLoaderRoot();

                var tcpXmlPath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-client-service-info-xml-file-path");
                var cmdXmlPath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "interchange-message-command-info-xml-file-path");

                if (!string.IsNullOrEmpty(tcpXmlPath) && File.Exists(tcpXmlPath))
                {
                    var serializer = new XmlSerializer(typeof(TCPClientServiceInfo));
                    using var reader = XmlReader.Create(tcpXmlPath);
                    _tcpClientServiceInfo = serializer.Deserialize(reader) as TCPClientServiceInfo;
                }

                if (!string.IsNullOrEmpty(cmdXmlPath) && File.Exists(cmdXmlPath))
                {
                    var serializer = new XmlSerializer(typeof(InterchangeMessageCommandInfo));
                    using var reader = XmlReader.Create(cmdXmlPath);
                    _interchangeMessageCommandInfo = serializer.Deserialize(reader) as InterchangeMessageCommandInfo;
                }

                if (_tcpClientServiceInfo != null)
                {
                    var remoteHostName = _tcpClientServiceInfo.TCPClientServiceConfig.RemoteHostName.Trim();
                    var remoteIPString = CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.RemoteIP.Trim(), AddressFamily.InterNetwork);
                    var remotePort = Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.RemotePort.Trim());
                    var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPString), remotePort);

                    var localIPString = CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.LocalIP.Trim(), AddressFamily.InterNetwork);
                    var localPort = Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.LocalPort.Trim());
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPString), localPort);

                    _tcpClientService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
                    _tcpClientService.EnableLogger("TCPClientService", Utils.LogLevel.Debug);

                    if (Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.AutoStartFlag.Trim()))
                    {
                        StartTcpService();
                    }

                    var udpPort = Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.UDPListenerPort.Trim());
                    var udpAutoStart = Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.UDPAutoStartFlag.Trim());
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

        public async Task QueryChannelStatusAsync()
        {
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Cannot query status: Service is disconnected.", "red");
                return;
            }

            try
            {
                LogSystem("Sending GetConnectionStatus command to Router...", "cyan");

                var interchangeMessage = CreateMessage('S');
                interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = _tcpClientService.RemoteEndPoint().ToString();
                interchangeMessage.Category = InterChangeMessageCategory.RouterSystemCommand;
                interchangeMessage.CommandCode = InterChangeMessageCommandCode.RouterSYSTEMCommandCode.GetConnectionStatus;

                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                _tcpClientService.Send(sendBytes);

                await Task.Delay(1000);

                var readBytes = _tcpClientService.GetReturnBytes;
                if (readBytes == null)
                {
                    LogSystem("Receive message: No data returned.", "red");
                    return;
                }

                var responseMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                if (responseMessage == null)
                {
                    LogSystem("Receive message: Invalid XML data.", "red");
                }
                else if (responseMessage.InterchangeData != null)
                {
                    var dataStr = Encoding.ASCII.GetString(responseMessage.InterchangeData);
                    LogSystem($"Channel status response: {dataStr}", "green");
                    CreateGatewayClientAuthList(dataStr);
                }
            }
            catch (Exception ex)
            {
                LogSystem($"Query channel status failed: {ex.Message}", "red");
                Logger.Error($"Query channel status failed: {ex.Message}");
            }
        }

        private void CreateGatewayClientAuthList(string interchangeData)
        {
            _gatewayList.Clear();
            _clientAuthList.Clear();

            if (string.IsNullOrEmpty(interchangeData)) return;

            var dataElement = interchangeData.Trim().Split(new[] { "!" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var data in dataElement)
            {
                var fieldElement = data.Trim().Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
                if (fieldElement.Length < 2) continue;

                switch (fieldElement[0].ToUpper())
                {
                    case "GATEWAY":
                        _gatewayList.Add(fieldElement[1]);
                        break;
                    case "CLIENTAUTH":
                        _clientAuthList.Add(fieldElement[1]);
                        break;
                }
            }

            OnConnectionListUpdated?.Invoke();
        }

        public async Task SendGatewayCommandAsync(List<string> selectedIps, string category, string categoryItem, string commandCode, int gwLocalPort)
        {
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Cannot send command: Service is disconnected.", "red");
                return;
            }

            try
            {
                int sendCount = 0;
                foreach (var ip in selectedIps)
                {
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = ip;
                    interchangeMessage.Category = $"{category}--{categoryItem}";
                    interchangeMessage.CommandCode = commandCode;

                    if (gwLocalPort > 0)
                    {
                        interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                        interchangeMessage.InterchangeData = Encoding.ASCII.GetBytes(Convert.ToString(gwLocalPort));
                    }
                    interchangeMessage.ReturnOption = true;

                    LogSystem($"--- Sending Gateway Message to {ip} ---", "gray");
                    LogSystem(ColaCommUtility.GetObjectProperty(interchangeMessage), "gray");

                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    _tcpClientService.Send(sendBytes);
                    sendCount++;

                    await Task.Delay(1000);

                    var readBytes = _tcpClientService.GetReturnBytes;
                    if (readBytes == null)
                    {
                        LogSystem($"[{ip}] Response: No data returned", "red");
                        continue;
                    }

                    var respMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (respMsg == null)
                    {
                        LogSystem($"[{ip}] Response: Invalid data format", "red");
                    }
                    else if (respMsg.InterchangeData != null)
                    {
                        var respStr = Encoding.ASCII.GetString(respMsg.InterchangeData);
                        LogSystem($"[{ip}] Response: {respStr}", "green");
                    }
                }
            }
            catch (Exception ex)
            {
                LogSystem($"Gateway command error: {ex.Message}", "red");
                Logger.Error($"Gateway command error: {ex.Message}");
            }
        }

        public async Task SendClientAuthCommandAsync(List<string> selectedIps, string category, string categoryItem, string commandCode)
        {
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Cannot send command: Service is disconnected.", "red");
                return;
            }

            try
            {
                int sendCount = 0;
                foreach (var ip in selectedIps)
                {
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = ip;
                    interchangeMessage.Category = $"{category}--{categoryItem}";
                    interchangeMessage.CommandCode = commandCode;

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
                            return;
                        }
                    }

                    interchangeMessage.ReturnOption = true;

                    LogSystem($"--- Sending ClientAuth Message to {ip} ---", "gray");
                    LogSystem(ColaCommUtility.GetObjectProperty(interchangeMessage), "gray");

                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    _tcpClientService.Send(sendBytes);
                    sendCount++;

                    await Task.Delay(1000);

                    var readBytes = _tcpClientService.GetReturnBytes;
                    if (readBytes == null)
                    {
                        LogSystem($"[{ip}] Response: No data returned", "red");
                        continue;
                    }

                    var respMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (respMsg == null)
                    {
                        LogSystem($"[{ip}] Response: Invalid data format", "red");
                    }
                    else if (respMsg.InterchangeData != null)
                    {
                        var respStr = Encoding.ASCII.GetString(respMsg.InterchangeData);
                        LogSystem($"[{ip}] Response: {respStr}", "green");
                    }
                }
            }
            catch (Exception ex)
            {
                LogSystem($"ClientAuth command error: {ex.Message}", "red");
                Logger.Error($"ClientAuth command error: {ex.Message}");
            }
        }

        public async Task SendRouterCommandAsync(string category, string categoryItem, string commandCode)
        {
            if (_tcpClientService == null || !_tcpClientService.IsConnected())
            {
                LogSystem("Cannot send command: Service is disconnected.", "red");
                return;
            }

            try
            {
                _gatewayList.Clear();
                _clientAuthList.Clear();
                OnConnectionListUpdated?.Invoke();

                var interchangeMessage = CreateMessage('S');
                interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = _tcpClientService.RemoteEndPoint().ToString();
                interchangeMessage.Category = $"{category}--{categoryItem}";
                interchangeMessage.CommandCode = commandCode;
                interchangeMessage.ReturnOption = true;

                LogSystem("--- Sending Router Message ---", "gray");
                LogSystem(ColaCommUtility.GetObjectProperty(interchangeMessage), "gray");

                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                _tcpClientService.Send(sendBytes);

                await Task.Delay(1000);

                var readBytes = _tcpClientService.GetReturnBytes;
                if (readBytes == null)
                {
                    LogSystem("Response: No data returned", "red");
                    return;
                }

                var respMsg = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                if (respMsg == null)
                {
                    LogSystem("Response: Invalid data format", "red");
                }
                else if (respMsg.InterchangeData != null)
                {
                    var respStr = Encoding.ASCII.GetString(respMsg.InterchangeData);
                    LogSystem($"Response: {respStr}", "green");
                }
            }
            catch (Exception ex)
            {
                LogSystem($"Router command error: {ex.Message}", "red");
                Logger.Error($"Router command error: {ex.Message}");
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
                    fileMaintData.IsoP7TxDteTime = fileMaintDataXml.IsoP7TxDteTime?.Text;
                    fileMaintData.IsoP11TraceNo = fileMaintDataXml.IsoP11TraceNo?.Text;
                    fileMaintData.IsoP13TransYear = fileMaintDataXml.IsoP13TransYear?.Text;
                    fileMaintData.IsoP33FwdIICode = fileMaintDataXml.IsoP33FwdIICode?.Text;
                    fileMaintData.IsoP36NTID = fileMaintDataXml.IsoP36NTID?.Text;
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
                    if (fileMaintDataXml.TxKeySeq.Changed.ToUpper().Equals("Y"))
                        fileMaintData.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    else
                        fileMaintData.TxKeySeq = fileMaintDataXml.TxKeySeq?.Text;

                    fileMaintData.IsoP2CardNbr = fileMaintDataXml.IsoP2CardNbr?.Text;

                    if (fileMaintDataXml.IsoP7TxDteTime.Changed.ToUpper().Equals("Y"))
                        fileMaintData.IsoP7TxDteTime = $"{DateTime.Now:MMddHHmmss}";
                    else
                        fileMaintData.IsoP7TxDteTime = fileMaintDataXml.IsoP7TxDteTime?.Text;

                    if (fileMaintDataXml.IsoP11TraceNo.Changed.ToUpper().Equals("Y"))
                    {
                        int traceNumber = StaticTraceRnd.Next(1, 999999);
                        fileMaintData.IsoP11TraceNo = $"{traceNumber:000000}";
                    }
                    else
                        fileMaintData.IsoP11TraceNo = fileMaintDataXml.IsoP11TraceNo?.Text;

                    fileMaintData.IsoP13TransYear = fileMaintDataXml.IsoP13TransYear?.Text;
                    fileMaintData.IsoP33FwdIICode = fileMaintDataXml.IsoP33FwdIICode?.Text;
                    fileMaintData.IsoP36NTID = fileMaintDataXml.IsoP36NTID?.Text;
                    fileMaintData.IsoP39Rsp = fileMaintDataXml.IsoP39Rsp?.Text;
                    fileMaintData.IsoP48AdditionalData = fileMaintDataXml.IsoP48AdditionalData?.Text;
                    fileMaintData.IsoP49TranCurCode = fileMaintDataXml.IsoP49TranCurCode?.Text;

                    if (fileMaintDataXml.IsoP58T05TxDte.Changed.ToUpper().Equals("Y"))
                        fileMaintData.IsoP58T05TxDte = $"{DateTime.Now:yyyyMMddHHmmss}";
                    else
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
            interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Null;
            interchangeMessage.InterchangeData = null;
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;

namespace Com.Cybersoft4u.Test.Wpf.WpfMonitorService
{
    /// <summary>
    /// ServiceMonitorConsole.xaml Interaction logic
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="System.Windows.Markup.IStyleConnector" />
    public partial class WpfMonitorService : Window
    {
        private readonly Logger Logger;

        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private TCPClientServiceInfo _tcpClientServiceInfo;

        private InterchangeMessageCommandInfo _interchangeMessageCommandInfo;

        private TCPClientService _tcpClientService;

        private bool _isStopUDPServer = false;

        private System.Windows.Threading.DispatcherTimer _getConnectionDispatcherTimer;

        private System.Windows.Threading.DispatcherTimer _routerClientAutoQueryDispatcherTimer;

        private readonly List<string> _routerClientList = new List<string>();

        private readonly List<string> _clientAuthList = new List<string>();

        private static readonly string DatabaseKeyName = "MESSAGE_ID";

        private bool _isAutoRouterClientQuery = false;

        public WpfMonitorService()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WpfMonitorService]");
            InitializeComponent();
            TextBoxInterval.TextChanged += new TextChangedEventHandler(TextChanged);
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            InitSystemConfig();
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void getClientConnectionDispatcherTimer_Tick(object sender, EventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var isConnected = IsConnectedOfTCPSession(_tcpClientService, 2);
                if (isConnected)
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Connect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Query client connection to server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                }
                else
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Disconnect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Query client connection to server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void routerClientAutoQueryDispatcherTimer_Tick(object sender, EventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (RaBtnGateway.IsChecked == true)
                    ProcessGatewayRouterClientCommand();
                else
                    ProcessClientAuthRouterClientCommand();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            if (!(richTextBox is RichTextBox rtb)) return;
            if (rtb.Dispatcher.CheckAccess())
            {
                TextRange rangeOfText1 = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd)
                {
                    Text = message + '\r'
                };
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                rtb.ScrollToEnd();
            } //if
            else
            {
                rtb.Dispatcher.BeginInvoke(new UpdateRichTextBox(SetMessageText), richTextBox, message, brush);
            }
        }

        private class ConnectionInfo
        {
            public string ClientType { get; set; }
            public string IPAddress { get; set; }
            public string Status { get; set; }
            public bool Action { get; set; }
        }
        private class GatewayClientRouterInfo
        {
            public string ClientIPEndPoint { get; set; }
            public string LocalEndPoint { get; set; }
            public string RemoteEndPoint { get; set; }
            public string InitialCreateDateTime { get; set; }
            public string CumulativeToDate { get; set; }
            public string NowProcessDateTime { get; set; }
            public string NowProcessTransactionType { get; set; }
            public string TotalCount { get; set; }
            public string DailyTotalCount { get; set; }
        }

        private class ClientAuthClientRouterInfo
        {
            public string ClientIPEndPoint { get; set; }
            public string Column1 { get; set; }
            public string Column2 { get; set; }
        }

        private void InitSystemConfig()
        {
            try
            {
                InitControlAttribute();
                InitComboBox();
                Logger.Info($"{AppDomain.CurrentDomain.FriendlyName} start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-client-service-info-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(TCPClientServiceInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _tcpClientServiceInfo = (TCPClientServiceInfo)serializer.Deserialize(reader);
                }

                xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "interchange-message-command-info-xml-file-path");
                serializer = new XmlSerializer(typeof(InterchangeMessageCommandInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _interchangeMessageCommandInfo = (InterchangeMessageCommandInfo)serializer.Deserialize(reader);
                }

                var remoteHostName = _tcpClientServiceInfo?.TCPClientServiceConfig.RemoteHostName.Trim();
                var remoteIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.RemoteIP.Trim(), AddressFamily.InterNetwork);
                var remotePortNumberString = _tcpClientServiceInfo.TCPClientServiceConfig.RemotePort.Trim();
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddressString), Convert.ToInt32(remotePortNumberString));

                var localIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.LocalIP.Trim(), AddressFamily.InterNetwork);
                var localPortNumberString = _tcpClientServiceInfo.TCPClientServiceConfig.LocalPort.Trim();
                var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPAddressString), Convert.ToInt32(localPortNumberString));

                var udpListenerPort = Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.UDPListenerPort.Trim());
                var udpAutoStartFlag = Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.UDPAutoStartFlag.Trim());
                _tcpClientService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
                _tcpClientService.EnableLogger("TCPClientService", Utils.LogLevel.Debug);
                if (Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.AutoStartFlag.Trim()))
                {
                    _tcpClientService.Start();
                    BtnStartRouterClient.IsEnabled = false;
                }
                else
                {
                    BtnStartRouterClient.IsEnabled = true;
                }
                StartUDPService(udpListenerPort, udpAutoStartFlag);
                TextBoxConnectPort.Text = remotePortNumberString;
                TextBoxInterval.Text = _tcpClientServiceInfo.TCPClientServiceConfig.ClientRetrievalTimeIntervalSec.Trim();
                _getConnectionDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _getConnectionDispatcherTimer.Tick += getClientConnectionDispatcherTimer_Tick;
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.ClientRetrievalTimeIntervalSec.Trim()));

                _routerClientAutoQueryDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _routerClientAutoQueryDispatcherTimer.Tick += routerClientAutoQueryDispatcherTimer_Tick;
                _routerClientAutoQueryDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(TextBoxRouterClientQueryInterval.Text.Trim()));

                DataGridRouterClientConnectionInfo.Items.Clear();
                DataGridGatewayRouterClientInfo.Items.Clear();
                DataGridClientAuthRouterClientInfo.Items.Clear();
                Title = $"{_tcpClientServiceInfo.BankInfo.BankName} TCPClientService Console For Remote IP {_tcpClientServiceInfo.TCPClientServiceConfig.RemoteIP}";
                TextBoxInterval.Text = _tcpClientServiceInfo.TCPClientServiceConfig.ClientRetrievalTimeIntervalSec.Trim();
                var isConnected = IsConnectedOfTCPSession(_tcpClientService, 2);
                if (isConnected)
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Connect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Query client connection to server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                }
                else
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Disconnect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Query client connection to server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                }
                _getConnectionDispatcherTimer.Start();
                ComboBoxSelectCommand.ItemsSource = null;
                if (RaBtnGateway.IsChecked == true)
                {
                    List<string> commandList = null;
                    commandList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.GatewayServiceCommand.Commands.Command;
                    ComboBoxSelectCommand.ItemsSource = commandList;
                    ComboBoxSelectCommand.SelectedIndex = 0;
                }
                else
                {
                    List<string> commandList = null;
                    commandList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.ClientAuthServiceCommand.Commands.Command;
                    ComboBoxSelectCommand.ItemsSource = commandList;
                    ComboBoxSelectCommand.SelectedIndex = 0;

                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
        }

        private void InitControlAttribute()
        {
            RichTextBoxSystemMessage.Document.Blocks.Clear();
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        /// <summary>
        /// Initializes the ComboBox.
        /// </summary>
        private void InitComboBox()
        {
            BtnStartUDPService.IsEnabled = true;
            BtnStopUDPService.IsEnabled = false;
            BtnRouterClientConnectionInfo.IsEnabled = true;
            BtnGetRouterClientInfo.IsEnabled = true;
        }

        /// <summary>
        /// Creates the message.
        /// </summary>
        /// <param name="startingPoint">The starting point.</param>
        /// <returns>Com.Cybersoft4u.CommonLib.Network.Server.InterchangeMessage.</returns>
        private InterchangeMessage CreateMessage(char startingPoint = 'S')
        {
            var interchangeMessage = new InterchangeMessage();
            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
            interchangeMessage.StartingPoint = startingPoint;
            interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
            interchangeMessage.CreateDateTime = dateTimeString;
            interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Null;
            interchangeMessage.InterchangeData = null;
            return (interchangeMessage);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var result = (sender as CheckBox)?.IsChecked;
            CheckBox checkBox = (CheckBox)sender;
            checkBox.BindingGroup.CommitEdit();
        }

        private void StartUDPService(int udpListenerPort, bool udpAutoStartFlag)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start UDPServer";
            try
            {
                if (!udpAutoStartFlag)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"UDPServer[{udpListenerPort}] service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    Mouse.OverrideCursor = null;
                    return;
                }
                BtnStartUDPService.IsEnabled = false;
                BtnStopUDPService.IsEnabled = !BtnStartUDPService.IsEnabled;
                _isStopUDPServer = false;
                Thread udpListener = new Thread(() => ReadServerLogMessage(udpListenerPort));
                udpListener.Start();
                SetMessageText(RichTextBoxSystemMessage, $"UDPServer[{udpListenerPort}] service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ReadServerLogMessage(int udpListenerPort)
        {
            UdpClient udpClient = null;
            try
            {
                udpClient = new UdpClient(udpListenerPort);
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, udpListenerPort);
                while (!_isStopUDPServer)
                {
                    if (udpClient.Available <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    byte[] readBytes = udpClient.Receive(ref groupEP);
                    if (readBytes == null) continue;
                    SetMessageText(RichTextBoxLogMessage,
                        $"{Encoding.Default.GetString(readBytes, 0, readBytes.Length)}", Brushes.Black);
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
            finally
            {
                udpClient?.Close();
            }
        }

        private void WriteSendMessageToLog(string localEndPoint, string remoteEndPoint, byte[] bytes)
        {
            try
            {
                var length = bytes?.Length ?? 0;
                Logger.Info(
                    () =>
                        $"WRITE [{localEndPoint} => {remoteEndPoint}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(bytes)}");
                Logger.Info(() => $"[{ColaCommUtility.PrintBytesHex(bytes)}]");
                Logger.Info(() => $"[{ColaCommUtility.PrintBytesData(bytes)}]");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpClientService.IsConnected())
            {
                _tcpClientService.Stop();
            }
            if (BtnStopUDPService.IsEnabled)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnStopUDPService);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
            Close();
            Environment.Exit(0);
        }

        private void BtnStartUDPService_Click(object sender, RoutedEventArgs e)
        {
            var udpListenerPort = Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.UDPListenerPort.Trim());
            var udpAutoStartFlag = Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.UDPAutoStartFlag.Trim());
            StartUDPService(udpListenerPort, udpAutoStartFlag);
        }

        private void BtnStopUDPService_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Stop UDPServer";
            _isStopUDPServer = true;
            UdpClient udpClient = new UdpClient();
            IPAddress ipAddress = IPAddress.Parse(CommonUtils.GetIPAddressStringByHostName(_tcpClientServiceInfo.TCPClientServiceConfig.UDPServerName.Trim(), AddressFamily.InterNetwork));
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.UDPListenerPort.Trim()));
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Stop UDP Service");
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length, ipEndPoint);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
            SetMessageText(RichTextBoxSystemMessage, $"UDPServer[{_tcpClientServiceInfo.TCPClientServiceConfig.UDPListenerPort.Trim()}] service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            BtnStopUDPService.IsEnabled = false;
            BtnStartUDPService.IsEnabled = !BtnStopUDPService.IsEnabled;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_tcpClientService.IsConnected())
            {
                _tcpClientService.Stop();
            }
            if (BtnStopUDPService.IsEnabled)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnStopUDPService);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
            Close();
            Environment.Exit(0);
        }

        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxSystemMessage.Document.Blocks.Clear();
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        private void RefreshDataGridRouterClientConnectionInfo()
        {
            try
            {
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                SetMessageText(RichTextBoxSystemMessage, $"Retrieval router client connection status at : {dateTimeNow}", Brushes.Red);
                GroupBoxRouterClient.Header = $"RouterClient connection at : {dateTimeNow}";
                DataGridRouterClientConnectionInfo.Items.Clear();
                foreach (var item in _routerClientList.ToArray())
                {
                    var connectionInfoData = new ConnectionInfo
                    {
                        ClientType = "Gateway",
                        IPAddress = item,
                        Status = "Connection",
                        Action = false
                    };
                    if (RaBtnGateway.IsChecked != true)
                    {
                        connectionInfoData.ClientType = "ClientAuth";
                    }
                    DataGridRouterClientConnectionInfo.Items.Add(connectionInfoData);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception:{ex.Message}", Brushes.Red);
            }
        }

        private void CreateGatewayClineAuthList(string interchangeData)
        {
            _routerClientList.Clear();
            if (string.IsNullOrEmpty(interchangeData)) return;
            var dataElement = interchangeData.Trim().Split(new string[] { "!" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var data in dataElement)
            {
                var fieldElement = data.Trim().Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
                if ((RaBtnGateway.IsChecked == true) && (fieldElement[0].ToUpper().Equals("GATEWAY")))
                {
                    _routerClientList.Add(fieldElement[1]);
                }
                if ((RaBtnClientAuth.IsChecked == true) && (fieldElement[0].ToUpper().Equals("CLIENTAUTH")))
                {
                    _routerClientList.Add(fieldElement[1]);
                }
            }
            RefreshDataGridRouterClientConnectionInfo();
            DataGridGatewayRouterClientInfo.Items.Clear();
            DataGridClientAuthRouterClientInfo.Items.Clear();
        }

        private void AddDataGridGatewayRouterClientInfo(List<XElement> xElementsList, string commandString, string clientIPEndPoint)
        {
            try
            {
                DataGridGatewayRouterClientInfo.Items.Clear();
                DataGridGatewayRouterClientInfo.Columns[1].Header = "LocalEndPoint";
                DataGridGatewayRouterClientInfo.Columns[2].Header = "RemoteEndPoint";
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                foreach (var element in xElementsList)
                {

                    var gatewayClientRouterInfo = new GatewayClientRouterInfo
                    {
                        ClientIPEndPoint = clientIPEndPoint,
                        LocalEndPoint = element.Element("LocalEndPoint")?.Value,
                        RemoteEndPoint = element.Element("RemoteEndPoint")?.Value,
                        InitialCreateDateTime = element.Element("InitialCreateDateTime")?.Value,
                        CumulativeToDate = element.Element("CumulativeToDate")?.Value,
                        NowProcessDateTime = element.Element("NowProcessDateTime")?.Value,
                        NowProcessTransactionType = element.Element("NowProcessTransactionType")?.Value,
                        TotalCount = element.Element("TotalCount")?.Value,
                        DailyTotalCount = element.Element("DailyTotalCount")?.Value
                    };
                    DataGridGatewayRouterClientInfo.Items.Add(gatewayClientRouterInfo);
                }
                DataGridGatewayRouterClientInfo.Items.Refresh();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex.Message}", Brushes.Red);
            }
        }

        private void AddDataGridGatewayRouterClientInfo(string infoString, string commandString, string clientIPEndPoint)
        {
            try
            {
                DataGridGatewayRouterClientInfo.Items.Clear();
                DataGridGatewayRouterClientInfo.Columns[1].Header = "Command String";
                DataGridGatewayRouterClientInfo.Columns[2].Header = "Return Information";
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                var separatingStrings1 = new char[] { '\r', '\n' };
                var separatingStrings2 = new char[] { '=', '>' };
                var stringArrays = infoString.Split(separatingStrings1, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var data in stringArrays)
                {
                    if (data.Trim().Equals(string.Empty)) continue;
                    var stringSubArrays = data.Split(separatingStrings2, System.StringSplitOptions.RemoveEmptyEntries);
                    //if (stringSubArrays.Length != 2) continue;

                    var gatewayClientRouterInfo = new GatewayClientRouterInfo
                    {
                        ClientIPEndPoint = clientIPEndPoint,
                        LocalEndPoint = "Command : " + commandString,
                        RemoteEndPoint = infoString
                        //InitialCreateDateTime = element.Element("InitialCreateDateTime")?.Value,
                        //CumulativeToDate = element.Element("CumulativeToDate")?.Value,
                        //NowProcessDateTime = element.Element("NowProcessDateTime")?.Value,
                        //NowProcessTransactionType = element.Element("NowProcessTransactionType")?.Value,
                        //TotalCount = element.Element("TotalCount")?.Value,
                        //DailyTotalCount = element.Element("DailyTotalCount")?.Value
                    };
                    DataGridGatewayRouterClientInfo.Items.Add(gatewayClientRouterInfo);
                }
                DataGridGatewayRouterClientInfo.Items.Refresh();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex.Message}", Brushes.Red);
            }
        }
        private void AddDataGridClientAuthRouterClientInfo(string infoString, string commandString, string clientIPEndPoint)
        {
            try
            {
                DataGridClientAuthRouterClientInfo.Items.Clear();
                if (commandString.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0].Equals("01"))
                {
                    string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                    DataGridClientAuthRouterClientInfo.Columns[1].Header = "LocalEndPoint";
                    DataGridClientAuthRouterClientInfo.Columns[2].Header = "RemoveEndPoint";
                    var separatingStrings1 = new char[] { '\r', '\n' };
                    var separatingStrings2 = new char[] { '=', '>' };
                    var stringArrays = infoString.Split(separatingStrings1, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var data in stringArrays)
                    {
                        if (data.Trim().Equals(string.Empty)) continue;
                        var stringSubArrays = data.Split(separatingStrings2, System.StringSplitOptions.RemoveEmptyEntries);
                        if (stringSubArrays.Length != 2) continue;
                        var clientAuthClientRouterInfo = new ClientAuthClientRouterInfo
                        {
                            ClientIPEndPoint = clientIPEndPoint,
                            Column1 = stringSubArrays[0].Replace("[", "")
                                      .Replace("]", "")
                                      .Replace("-", ""),
                            Column2 = stringSubArrays[1].Replace("[", "")
                                      .Replace("]", "")
                                      .Replace("-", "")
                        };
                        DataGridClientAuthRouterClientInfo.Items.Add(clientAuthClientRouterInfo);
                    }
                }
                else
                {
                    DataGridClientAuthRouterClientInfo.Columns[1].Header = "Command String";
                    DataGridClientAuthRouterClientInfo.Columns[2].Header = "Return Information";
                    var clientAuthClientRouterInfo = new ClientAuthClientRouterInfo
                    {
                        ClientIPEndPoint = clientIPEndPoint,
                        Column1 = "Command : " + commandString,
                        Column2 = infoString
                    };
                    DataGridClientAuthRouterClientInfo.Items.Add(clientAuthClientRouterInfo);
                }
                DataGridClientAuthRouterClientInfo.Items.Refresh();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex.Message}", Brushes.Red);
            }
        }

        private byte[] GetChannelStatus()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send Message To ServiceRouter Server";
            try
            {
                SetMessageText(RichTextBoxSystemMessage, $"Start send message to service router server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                var interchangeMessage = CreateMessage('S');
                interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = _tcpClientService.RemoteEndPoint().ToString();
                interchangeMessage.Category = InterChangeMessageCategory.RouterSystemCommand;
                interchangeMessage.CommandCode = InterChangeMessageCommandCode.RouterSYSTEMCommandCode.GetConnectionStatus;
                SetMessageText(RichTextBoxSystemMessage, $"--- Start display send InterchangeMessage ---", Brushes.Black);
                SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage), Brushes.Black);
                SetMessageText(RichTextBoxSystemMessage, $"--- End   display send InterchangeMessage ---", Brushes.Black);
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                return (sendBytes);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
                return null;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ProcessGatewayRouterClientCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataGridGatewayRouterClientInfo.Items.Clear();
                DataGridGatewayRouterClientInfo.Columns[1].Header = "LocalEndPoint";
                DataGridGatewayRouterClientInfo.Columns[2].Header = "RemoteEndPoint";
                GroupBoxGatewayRouterClientInfo.Header = $"GatewayRouterClient Information query at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                var ipEndPointMessageIdList = GetGatewayRouterClientLIstMessageID();
                var commandString = ComboBoxSelectCommand.SelectedValue.ToString();
                foreach (var ipEndPointMessageId in ipEndPointMessageIdList)
                {
                    var messageArray = ipEndPointMessageId.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
                    if (messageArray.Length != 2)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"IpEndPointMessageId invalid, Array Count[{messageArray.Length}]", Brushes.Red);
                        return;
                    }
                    //Call Cola_Select_Service_Router_Lo Store Procedure
                    var getDataTableFromDatabase = StoreProcUtility.GetServiceRouterLogByMessageId(messageArray[1]);
                    if (getDataTableFromDatabase == null)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Null Data", Brushes.Red);
                        return;
                    }
                    if (getDataTableFromDatabase.Rows.Count == 0)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] No Data", Brushes.Red);
                        return;
                    }
                    //var serviceRouterLogInfo = new ServiceRouterLogInfoStruct();
                    SetMessageText(RichTextBoxSystemMessage, $"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Have [{getDataTableFromDatabase.Rows.Count}] Row, Last Key[{getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName)}]", Brushes.Red);
                    for (var index = 0; index < getDataTableFromDatabase.Rows.Count; index++)
                    {
                        var serviceRouterLogInfo = new ServiceRouterLogInfoStruct
                        {
                            MessageId = getDataTableFromDatabase.Rows[index].Field<string>("Message_Id"),
                            ProcessDatetime = getDataTableFromDatabase.Rows[index].Field<string>("Process_Date_Time"),
                            SourceEndpoint = getDataTableFromDatabase.Rows[index].Field<string>("Source_End_Point"),
                            DestinationEndpoint = getDataTableFromDatabase.Rows[index].Field<string>("Destination_End_Point"),
                            CreateDatetime = getDataTableFromDatabase.Rows[index].Field<string>("Create_Date_Time"),
                            ReturnDatetime = getDataTableFromDatabase.Rows[index].Field<string>("Return_Date_Time"),
                            Category = getDataTableFromDatabase.Rows[index].Field<string>("Category"),
                            CommandCode = getDataTableFromDatabase.Rows[index].Field<string>("Command_Code"),
                            InterchangeData = getDataTableFromDatabase.Rows[index].Field<string>("Interchange_Data")
                        };
                        if (string.IsNullOrEmpty(serviceRouterLogInfo.InterchangeData) || serviceRouterLogInfo.InterchangeData.Contains("No Data")) continue;
                        if (commandString.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0].Equals("01"))
                        {
                            AddDataGridGatewayRouterClientInfo(serviceRouterLogInfo.InterchangeData, commandString, messageArray[0]);
                        }
                        else
                        {
                            var xmlFormatDataString = ColaCommUtility.PrettyXml(serviceRouterLogInfo.InterchangeData);
                            //SetMessageText(RichTextBoxSystemMessage,xmlFormatDataString,  Brushes.Black);
                            XElement endPoints = XElement.Parse(xmlFormatDataString);
                            List<XElement> endPointsNodes = endPoints.Elements("anyType").ToList();
                            AddDataGridGatewayRouterClientInfo(endPointsNodes, commandString, messageArray[0]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Execute StoreProcUtility.SelectServiceRouterLog Exception : [{ex.Message}]", Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ProcessClientAuthRouterClientCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataGridClientAuthRouterClientInfo.Items.Clear();
                DataGridClientAuthRouterClientInfo.Columns[1].Header = "Column1";
                DataGridClientAuthRouterClientInfo.Columns[2].Header = "Column2";
                GroupBoxClientAuthRouterClientInfo.Header = $"ClientAuthRouterClient Information query at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                var ipEndPointMessageIdList = GetClientAuthRouterClientLIstMessageID();
                var commandString = ComboBoxSelectCommand.SelectedValue.ToString();
                foreach (var ipEndPointMessageId in ipEndPointMessageIdList)
                {
                    var messageArray = ipEndPointMessageId.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
                    if (messageArray.Length != 2)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"IpEndPointMessageId invalid, Array Count[{messageArray.Length}]", Brushes.Red);
                        return;
                    }
                    //Call Cola_Select_Service_Router_Lo Store Procedure
                    var getDataTableFromDatabase = StoreProcUtility.GetServiceRouterLogByMessageId(messageArray[1]);
                    if (getDataTableFromDatabase == null)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Null Data", Brushes.Red);
                        return;
                    }
                    if (getDataTableFromDatabase.Rows.Count == 0)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] No Data", Brushes.Red);
                        return;
                    }
                    //var serviceRouterLogInfo = new ServiceRouterLogInfoStruct();
                    SetMessageText(RichTextBoxSystemMessage, $"Get Database Data At : [{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}] Have [{getDataTableFromDatabase.Rows.Count}] Row, Last Key[{getDataTableFromDatabase.Rows[getDataTableFromDatabase.Rows.Count - 1].Field<string>(DatabaseKeyName)}]", Brushes.Red);
                    for (var index = 0; index < getDataTableFromDatabase.Rows.Count; index++)
                    {
                        var serviceRouterLogInfo = new ServiceRouterLogInfoStruct
                        {
                            MessageId = getDataTableFromDatabase.Rows[index].Field<string>("Message_Id"),
                            ProcessDatetime = getDataTableFromDatabase.Rows[index].Field<string>("Process_Date_Time"),
                            SourceEndpoint = getDataTableFromDatabase.Rows[index].Field<string>("Source_End_Point"),
                            DestinationEndpoint = getDataTableFromDatabase.Rows[index].Field<string>("Destination_End_Point"),
                            CreateDatetime = getDataTableFromDatabase.Rows[index].Field<string>("Create_Date_Time"),
                            ReturnDatetime = getDataTableFromDatabase.Rows[index].Field<string>("Return_Date_Time"),
                            Category = getDataTableFromDatabase.Rows[index].Field<string>("Category"),
                            CommandCode = getDataTableFromDatabase.Rows[index].Field<string>("Command_Code"),
                            InterchangeData = getDataTableFromDatabase.Rows[index].Field<string>("Interchange_Data")
                        };
                        if (string.IsNullOrEmpty(serviceRouterLogInfo.InterchangeData) || serviceRouterLogInfo.InterchangeData.Contains("No Data")) continue;
                        AddDataGridClientAuthRouterClientInfo(serviceRouterLogInfo.InterchangeData, commandString, messageArray[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Execute StoreProcUtility.SelectServiceRouterLog Exception : [{ex.Message}]", Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private List<string> GetGatewayRouterClientLIstMessageID()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to gateway client";
            var messageIdList = new List<string>();
            try
            {
                SetMessageText(RichTextBoxSystemMessage, $"Start send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                if (DataGridRouterClientConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"End   Send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Total Send : 0", Brushes.Red);
                    return messageIdList;
                }
                int sendCount = 0;
                for (int index = 0; index < DataGridRouterClientConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridRouterClientConnectionInfo.Items[index];
                    if (!item.ClientType.ToUpper().Equals("GATEWAY")) continue;
                    if (!item.Action) continue;
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = item.IPAddress;
                    interchangeMessage.Category = InterChangeMessageCategory.GatewaySystemCommand;
                    interchangeMessage.CommandCode = ComboBoxSelectCommand.SelectedValue.ToString().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                    interchangeMessage.InterchangeData = null;
                    interchangeMessage.ReturnOption = true;
                    SetMessageText(RichTextBoxSystemMessage, $"--- Start display send InterchangeMessage ---", Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage), Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, $"--- End   display send InterchangeMessage ---", Brushes.Black);
                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
                    sendCount++;
                    SetMessageText(RichTextBoxSystemMessage, $"End   send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Total Send : {sendCount}", Brushes.Red);
                    _tcpClientService.Send(sendBytes);
                    Thread.Sleep(1000);
                    var readBytes = _tcpClientService.GetReturnBytes;
                    if (readBytes == null)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Receive message no data", Brushes.Red);
                        Logger.Info("Receive message no data");
                        return messageIdList;
                    }
                    interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (interchangeMessage == null)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Receive message invalid", Brushes.Red);
                        Logger.Info("Receive message invalid");
                    }
                    else
                    {
                        if (interchangeMessage.InterchangeData != null)
                            SetMessageText(RichTextBoxSystemMessage, $"{System.Text.Encoding.ASCII.GetString(interchangeMessage.InterchangeData)}", Brushes.Red);
                        WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
                    }
                    messageIdList.Add($"{item?.IPAddress}--{interchangeMessage?.MessageId}");
                }
                return messageIdList;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
                return messageIdList;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private List<string> GetClientAuthRouterClientLIstMessageID()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to clientauth client";
            var messageIdList = new List<string>();
            try
            {
                SetMessageText(RichTextBoxSystemMessage, $"Start send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                if (DataGridRouterClientConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"End   Send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Total Send : 0", Brushes.Red);
                    return messageIdList;
                }
                int sendCount = 0;
                for (int index = 0; index < DataGridRouterClientConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridRouterClientConnectionInfo.Items[index];
                    if (!item.ClientType.ToUpper().Equals("CLIENTAUTH")) continue;
                    if (!item.Action) continue;
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = item.IPAddress;
                    interchangeMessage.Category = InterChangeMessageCategory.ClientAuthSystemCommand;
                    interchangeMessage.CommandCode = ComboBoxSelectCommand.SelectedValue.ToString().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                    interchangeMessage.InterchangeData = null;
                    interchangeMessage.ReturnOption = true;
                    SetMessageText(RichTextBoxSystemMessage, $"--- Start display send InterchangeMessage ---", Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage), Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, $"--- End   display send InterchangeMessage ---", Brushes.Black);
                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
                    sendCount++;
                    SetMessageText(RichTextBoxSystemMessage, $"End   send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Total Send : {sendCount}", Brushes.Red);
                    _tcpClientService.Send(sendBytes);
                    Thread.Sleep(1000);
                    var readBytes = _tcpClientService.GetReturnBytes;
                    if (readBytes == null)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Receive message no data", Brushes.Red);
                        Logger.Info("Receive message no data");
                        return messageIdList;
                    }
                    interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                    if (interchangeMessage == null)
                    {
                        SetMessageText(RichTextBoxSystemMessage, $"Receive message invalid", Brushes.Red);
                        Logger.Info("Receive message invalid");
                    }
                    else
                    {
                        if (interchangeMessage.InterchangeData != null)
                            SetMessageText(RichTextBoxSystemMessage, $"{System.Text.Encoding.ASCII.GetString(interchangeMessage.InterchangeData)}", Brushes.Red);
                        WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
                    }
                    messageIdList.Add($"{item?.IPAddress}--{interchangeMessage?.MessageId}");
                }
                return messageIdList;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
                return messageIdList;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Int32.TryParse(TextBoxInterval.Text, out var timeIntervalSeconds))
            {
                if (_getConnectionDispatcherTimer == null) return;
                _getConnectionDispatcherTimer.Stop();
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, timeIntervalSeconds);
                _getConnectionDispatcherTimer.Start();
                SetMessageText(RichTextBoxSystemMessage, $"Change query interval {timeIntervalSeconds} Seconds", Brushes.Red);
            }
            else
            {
                TextBoxInterval.Focus();
                SetMessageText(RichTextBoxSystemMessage, $"Input invalid data", Brushes.Red);
            }
        }

        private void BtnRouterClientConnectionInfo_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                _routerClientList.Clear();
                _clientAuthList.Clear();
                DataGridRouterClientConnectionInfo.Items.Clear();
                DataGridClientAuthRouterClientInfo.Columns[1].Header = "Column1";
                DataGridClientAuthRouterClientInfo.Columns[2].Header = "Column2";
                DataGridGatewayRouterClientInfo.Items.Clear();
                DataGridGatewayRouterClientInfo.Columns[1].Header = "LocalEndPoint";
                DataGridGatewayRouterClientInfo.Columns[2].Header = "RemoveEndPoint";
                DataGridClientAuthRouterClientInfo.Items.Clear();
                if (!_tcpClientService.IsConnected())
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Not connected to server", Brushes.Red);
                    RefreshDataGridRouterClientConnectionInfo();
                    DataGridGatewayRouterClientInfo.Items.Clear();
                    DataGridClientAuthRouterClientInfo.Items.Clear();
                    return;
                }
                var sendBytes = GetChannelStatus();
                _tcpClientService.Send(sendBytes);
                Thread.Sleep(1000);
                var readBytes = _tcpClientService.GetReturnBytes;
                var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                if (interchangeMessage == null)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Receive message invalid", Brushes.Red);
                    Logger.Info("Receive message invalid");
                }
                else
                {
                    SetMessageText(RichTextBoxSystemMessage, $"{System.Text.Encoding.ASCII.GetString(interchangeMessage.InterchangeData)}", Brushes.Red);
                    CreateGatewayClineAuthList(System.Text.Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnGetRouterClientInfo_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (RaBtnGateway.IsChecked == true)
                    ProcessGatewayRouterClientCommand();
                else
                    ProcessClientAuthRouterClientCommand();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Execute StoreProcUtility.SelectServiceRouterLog Exception : [{ex.Message}]", Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void RaBtnGateway_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeCommandList(InterChangeMessageCategory.GatewaySystemCommand);
                if (_tcpClientService == null) return;
                if (!_tcpClientService.IsConnected())
                {
                    return;
                }
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnRouterClientConnectionInfo);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
        }

        private void RaBtnClientAuth_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeCommandList(InterChangeMessageCategory.ClientAuthSystemCommand);
                if (_tcpClientService == null) return;
                if (!_tcpClientService.IsConnected())
                {
                    return;
                }
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnRouterClientConnectionInfo);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
        }

        private void ChangeCommandList(string category)
        {
            if (_interchangeMessageCommandInfo == null) return;
            ComboBoxSelectCommand.ItemsSource = null;
            List<string> commandList = null;
            switch (category)
            {
                case InterChangeMessageCategory.ClientAuthSystemCommand:
                    commandList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.ClientAuthServiceCommand.Commands.Command;
                    break;
                case InterChangeMessageCategory.GatewaySystemCommand:
                    commandList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.GatewayServiceCommand.Commands.Command;
                    break;
            }
            ComboBoxSelectCommand.ItemsSource = commandList;
            ComboBoxSelectCommand.SelectedIndex = 0;
        }

        private bool IsConnectedOfTCPSession(TCPClientService tcpClientService, int waitingSecs)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (tcpClientService == null) return false;
                var startDateTime = DateTime.Now;
                while (!tcpClientService.IsConnected() && (DateTime.Now.Subtract(startDateTime).TotalSeconds < (double)waitingSecs))
                {
                    Thread.Sleep(100);
                }
                if (tcpClientService.IsConnected())
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
                return false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnStartRouterClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_tcpClientService.IsConnected())
                {
                    _tcpClientService.Stop();
                }
                _tcpClientService.Start();
                BtnStartRouterClient.IsEnabled = false;
                SetMessageText(RichTextBoxSystemMessage, $"Router Client start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
        }

        private void BtnAutoRouterClientQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnAutoRouterClientQuery.IsEnabled = false;
                if (_tcpClientService == null)
                {
                    SetMessageText(RichTextBoxSystemMessage, "Router Client not connect to server, can not auto query",
                        Brushes.Red);
                    return;
                }

                var isConnected = IsConnectedOfTCPSession(_tcpClientService, 2);
                if (!isConnected)
                {
                    SetMessageText(RichTextBoxSystemMessage, "Router Client not connect to server, can not auto query", Brushes.Red);
                    return;
                }

                if (_isAutoRouterClientQuery)
                {
                    BtnRouterClientConnectionInfo.IsEnabled = true;
                    BtnGetRouterClientInfo.IsEnabled = true;
                    _isAutoRouterClientQuery = !_isAutoRouterClientQuery;
                    BtnAutoRouterClientQuery.Content = "Start Auto Query";
                    _routerClientAutoQueryDispatcherTimer.Stop();
                }
                else
                {
                    BtnRouterClientConnectionInfo.IsEnabled = false;
                    BtnGetRouterClientInfo.IsEnabled = false;
                    _isAutoRouterClientQuery = !_isAutoRouterClientQuery;
                    BtnAutoRouterClientQuery.Content = "Stop Auto Query";
                    _routerClientAutoQueryDispatcherTimer.Start();
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                BtnAutoRouterClientQuery.IsEnabled = true;
            }
        }
    }
}

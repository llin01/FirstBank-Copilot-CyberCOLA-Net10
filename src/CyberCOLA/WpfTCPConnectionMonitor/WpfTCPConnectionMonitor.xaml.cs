using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfTCPConnectionMonitor
{
    /// <summary>
    /// ServiceMonitorConsole.xaml Interaction logic
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="System.Windows.Markup.IStyleConnector" />
    public partial class TCPConnectionMonitor : Window
    {
        private readonly Logger Logger;

        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private System.Windows.Threading.DispatcherTimer _getConnectionDispatcherTimer;

        private static readonly Random StaticTraceRnd = new Random();

        private TCPConnectionMonitorInfo _tcpConnectionMonitorInfo;
        private string _udpServerName;
        private int _udpServerListenerPort;
        private bool _isStartUDPService;
        private bool _isStopUDPServer;
        private string _transaction2GatewayServiceDescription;
        private string _colaService2ClientAuthServiceDescription;
        private string _serverRouterService2ClientMonitorServiceDescription;
        private string _clientAuthRouterService2TcpClientDescription;
        private Thread _udpListener;

        public TCPConnectionMonitor()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger($"LoggerHandler[{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}]");
            InitializeComponent();
            TextBoxInterval.TextChanged += new TextChangedEventHandler(TextChanged);
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            InitSystemConfig();
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
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                SetMessageText(RichTextBoxLogMessage, $"Retrieval connection status at : {dateTimeNow}", Brushes.Red);
                DataGridNccc2NcccService.Items.Clear();
                DataGridNccc2NcccService.Items.Refresh();
                DataGridColaService2ClientAuthService.Items.Clear();
                DataGridColaService2ClientAuthService.Items.Refresh();
                DataGridRouterService2ClientMonitorService.Items.Clear();
                DataGridRouterService2ClientMonitorService.Items.Refresh();
                DataGridClientAuthRouterService2TcpClient.Items.Clear();
                DataGridClientAuthRouterService2TcpClient.Items.Refresh();
                var count = 0;
                DataGridNccc2NcccService.Items.Clear();
                //Process Nccc to NcccService
                foreach (var endPoint in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.Nccc2NcccService.EndPoints.EndPoint)
                {
                    string[] stringArray = endPoint.Split(',');
                    var connectionStatus = GetTcpConnections(stringArray[0], stringArray[1]);
                    foreach (var conn in connectionStatus)
                    {
                        var connectionInfoData = new ConnectionInfo
                        {
                            Sequence = ++count,
                            RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                            LocalEndPoint = conn.LocalEndPoint.ToString(),
                            State = conn.State.ToString()
                        };
                        DataGridNccc2NcccService.Items.Add(connectionInfoData);
                    }
                }
                GroupBoxTransaction2GatewayService.Header = $"{_transaction2GatewayServiceDescription} connection at : {dateTimeNow}, Count[{count}]";

                count = 0;
                DataGridColaService2ClientAuthService.Items.Clear();
                //Process ColaService to ClientAuthService
                foreach (var endPoint in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.CloaServiceClientAuthService.EndPoints.EndPoint)
                {
                    string[] stringArray = endPoint.Split(',');
                    var connectionStatus = GetTcpConnections(stringArray[0], stringArray[1]);
                    foreach (var conn in connectionStatus)
                    {
                        var connectionInfoData = new ConnectionInfo
                        {
                            Sequence = ++count,
                            RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                            LocalEndPoint = conn.LocalEndPoint.ToString(),
                            State = conn.State.ToString()
                        };
                        DataGridColaService2ClientAuthService.Items.Add(connectionInfoData);
                    }
                }
                GroupBoxColaService2ClientAuthService.Header = $"{_colaService2ClientAuthServiceDescription} connection at : {dateTimeNow}, Count[{count}]";


                count = 0;
                DataGridRouterService2ClientMonitorService.Items.Clear();
                //Process RouterService to ClientMonitorService
                foreach (var endPoint in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.RouterService2ClientMonitorService.EndPoints.EndPoint)
                {
                    string[] stringArray = endPoint.Split(',');
                    var connectionStatus = GetTcpConnections(stringArray[0], stringArray[1]);
                    foreach (var conn in connectionStatus)
                    {
                        var connectionInfoData = new ConnectionInfo
                        {
                            Sequence = ++count,
                            RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                            LocalEndPoint = conn.LocalEndPoint.ToString(),
                            State = conn.State.ToString()
                        };
                        DataGridRouterService2ClientMonitorService.Items.Add(connectionInfoData);
                    }
                }
                GroupBoxServerRouterService2ClientMonitorService.Header = $"{_serverRouterService2ClientMonitorServiceDescription} connection at : {dateTimeNow}, Count[{count}]";

                count = 0;
                DataGridClientAuthRouterService2TcpClient.Items.Clear();
                //Process ClientAuthRouterService to TcpClient
                foreach (var endPoint in _tcpConnectionMonitorInfo.TCPConnectionMonitorConfig.ClientAuthRouterService2TcpClient.EndPoints.EndPoint)
                {
                    string[] stringArray = endPoint.Split(',');
                    var connectionStatus = GetTcpConnections(stringArray[0], stringArray[1]);
                    foreach (var conn in connectionStatus)
                    {
                        var connectionInfoData = new ConnectionInfo
                        {
                            Sequence = ++count,
                            RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                            LocalEndPoint = conn.LocalEndPoint.ToString(),
                            State = conn.State.ToString()
                        };
                        DataGridClientAuthRouterService2TcpClient.Items.Add(connectionInfoData);
                    }
                }
                GroupBoxClientAuthRouterService2TcpClient.Header = $"{_clientAuthRouterService2TcpClientDescription} connection at : {dateTimeNow}, Count[{count}]";
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxLogMessage, ex.Message, Brushes.Red);
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

        /// <summary>
        /// Get IPAddress By HostName
        /// </summary>
        ///<param name="hostNameOrIPAddress">HostName</param>
        ///<param name="addressFamily">HostName</param>
        ///<returns>Host IP Address String</returns>
        private string GetIPAddressStringByHostName(string hostNameOrIPAddress, AddressFamily addressFamily)
        {
            try
            {
                IPAddress resolveIPAddress;
                if (IPAddress.TryParse(hostNameOrIPAddress, out resolveIPAddress))
                {
                    switch (addressFamily)
                    {
                        case AddressFamily.InterNetwork: // we have IPv4
                            return (hostNameOrIPAddress);
                        case AddressFamily.InterNetworkV6: // we have IPv6
                            return (hostNameOrIPAddress);
                        default:
                            // umm... yeah... I'm going to need to take your red packet and...
                            return null;
                    }
                }
                else
                {
                    IPHostEntry ipHostEntry = Dns.GetHostEntry(hostNameOrIPAddress);
                    IPAddress ipAddress = ipHostEntry.AddressList.First(a => a.AddressFamily == addressFamily);
                    return ipAddress?.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"GetIPAddressStringByHostName[{hostNameOrIPAddress}] Exception:{ex.Message}");
                return null;
            }
        }

        private class ConnectionInfo
        {
            public int Sequence { get; set; }
            public string RemoteEndPoint { get; set; }
            public string LocalEndPoint { get; set; }
            public string State { get; set; }
        }

        private void InitSystemConfig()
        {
            try
            {
                InitControlAttribute();
                Logger.Info($"{AppDomain.CurrentDomain.FriendlyName} Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-connection-monitor-info-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(TCPConnectionMonitorInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _tcpConnectionMonitorInfo = (TCPConnectionMonitorInfo)serializer.Deserialize(reader);
                }

                var autoRetrieval = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "AutoRetrieval").Trim());
                _udpServerName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPServerName");
                _udpServerListenerPort = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPServerListenerPort").Trim());
                _isStartUDPService = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "AutoStartUDPService").Trim());
                _getConnectionDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _getConnectionDispatcherTimer.Tick += getClientConnectionDispatcherTimer_Tick;
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(TextBoxInterval.Text.Trim()));
                TextBoxInterval.Text = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "RetrievalTimeIntervalSec");
                _transaction2GatewayServiceDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "Transaction2GatewayServiceDescription").Trim();
                _colaService2ClientAuthServiceDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ColaService2ClientAuthServiceDescription").Trim();
                _serverRouterService2ClientMonitorServiceDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ServerRouterService2ClientMonitorServiceDescription").Trim();
                _clientAuthRouterService2TcpClientDescription = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ClientAuthRouterService2TcpClientDescription").Trim();
                GroupBoxTransaction2GatewayService.Header = $"{_transaction2GatewayServiceDescription}";
                GroupBoxColaService2ClientAuthService.Header = $"{_colaService2ClientAuthServiceDescription}";
                GroupBoxServerRouterService2ClientMonitorService.Header = $"{_serverRouterService2ClientMonitorServiceDescription}";
                GroupBoxClientAuthRouterService2TcpClient.Header = $"{_clientAuthRouterService2TcpClientDescription}";
                if (autoRetrieval)
                {
                    BtnRetrievalConnectionStatus.Content = "Stop";
                    BtnRetrievalConnectionStatus.Background = new SolidColorBrush(Colors.Green);
                    _getConnectionDispatcherTimer.Start();
                }
                else
                {
                    BtnRetrievalConnectionStatus.Content = "Start";
                    BtnRetrievalConnectionStatus.Background = new SolidColorBrush(Colors.Red);
                    _getConnectionDispatcherTimer.Stop();
                }

                StartUDPService(_udpServerListenerPort, _isStartUDPService);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxLogMessage, ex.Message, Brushes.Red);
            }
        }

        private void InitControlAttribute()
        {
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Close();
            Environment.Exit(0);
        }

        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Int32.TryParse(TextBoxInterval.Text, out var timeIntervalSeconds))
            {
                if (timeIntervalSeconds < 30)
                {
                    TextBoxInterval.Focus();
                    SetMessageText(RichTextBoxLogMessage, $"Input time interval must be greater than 30Sec", Brushes.Red);
                    return;
                }
                if (_getConnectionDispatcherTimer == null) return;
                _getConnectionDispatcherTimer.Stop();
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, timeIntervalSeconds);
                _getConnectionDispatcherTimer.Start();
                SetMessageText(RichTextBoxLogMessage, $"Change query time interval {timeIntervalSeconds} Seconds", Brushes.Red);
            }
            else
            {
                TextBoxInterval.Focus();
                SetMessageText(RichTextBoxLogMessage, $"Input invalid data", Brushes.Red);
            }
        }
        private void BtnRetrievalConnectionStatus_Click(object sender, RoutedEventArgs e)
        {
            if (BtnRetrievalConnectionStatus.Content.Equals("Start"))
            {
                if (Int32.TryParse(TextBoxInterval.Text, out var timeIntervalSeconds))
                {
                    if (timeIntervalSeconds < 30)
                    {
                        TextBoxInterval.Focus();
                        SetMessageText(RichTextBoxLogMessage, $"Input time interval must be greater than 30Sec", Brushes.Red);
                        return;
                    }
                    _getConnectionDispatcherTimer.Stop();
                    _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, timeIntervalSeconds);
                    _getConnectionDispatcherTimer.Start();
                    SetMessageText(RichTextBoxLogMessage, $"Start retrieval connection status at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    BtnRetrievalConnectionStatus.Content = "Stop";
                    BtnRetrievalConnectionStatus.Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    TextBoxInterval.Focus();
                    SetMessageText(RichTextBoxLogMessage, $"Input invalid data", Brushes.Red);
                }
            }
            else
            {
                _getConnectionDispatcherTimer.Stop();
                SetMessageText(RichTextBoxLogMessage, $"Stop retrieval connection status at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                BtnRetrievalConnectionStatus.Content = "Start";
                BtnRetrievalConnectionStatus.Background = new SolidColorBrush(Colors.Red);
            }
        }
        private TcpConnectionInformation[] GetTcpConnections(string remoteEndPoint, string localEndPoint)
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            TcpConnectionInformation[] connections1 = null;
            TcpConnectionInformation[] connections2 = null;
            var ipArray = remoteEndPoint.Split(':');
            if (ipArray.Length == 1)
            {
                connections1 = connections.Where(x =>
                 {
                     var stringArray = x.RemoteEndPoint.ToString().Split(':');
                     if (stringArray[0].Equals(ipArray[0]))
                         return true;
                     else
                         return false;
                 }).ToArray();
            }
            else
            {
                connections1 = connections.Where(x => x.RemoteEndPoint.ToString().Equals(remoteEndPoint)).ToArray();
            }
            ipArray = localEndPoint.Split(':');
            if (ipArray.Length == 1)
            {
                connections2 = connections1.Where(x =>
                {
                    var stringArray = x.LocalEndPoint.ToString().Split(':');
                    if (stringArray[0].Equals(ipArray[0]))
                        return true;
                    else
                        return false;
                }).ToArray();
            }
            else
            {
                connections2 = connections1.Where(x => x.LocalEndPoint.ToString().Equals(localEndPoint)).ToArray();
            }
            return connections2;
        }
        private void StartUDPService(int udpListenerPort, bool udpAutoStartFlag)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start UDPServer";
            try
            {
                if (!udpAutoStartFlag)
                {
                    SetMessageText(RichTextBoxLogMessage, $"UDPServer[{udpListenerPort}] service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    Mouse.OverrideCursor = null;
                    return;
                }
                BtnStartUDPService.IsEnabled = false;
                BtnStopUDPService.IsEnabled = !BtnStartUDPService.IsEnabled;
                _isStopUDPServer = false;
                _udpListener = new Thread(() => ReadServerLogMessage(udpListenerPort));
                _udpListener.Start();
                SetMessageText(RichTextBoxLogMessage, $"UDPServer[{udpListenerPort}] service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxLogMessage, ex.Message, Brushes.Red);
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
            UdpClient udpClient = new UdpClient(udpListenerPort);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, udpListenerPort);
            try
            {
                while (!_isStopUDPServer)
                {
                    if (udpClient.Available <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    byte[] readBytes = udpClient.Receive(ref groupEP);
                    SetMessageText(RichTextBoxLogFatalMessage, $"{Encoding.Default.GetString(readBytes, 0, readBytes.Length)}", Brushes.Black);
                }
                udpClient.Close();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxLogMessage, ex.Message, Brushes.Red);
            }
            finally
            {
                udpClient.Close();
            }
        }

        private void BtnStartUDPService_Click(object sender, RoutedEventArgs e)
        {
            _isStartUDPService = true;
            StartUDPService(_udpServerListenerPort, _isStartUDPService);
        }

        private void BtnStopUDPService_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Stop UDPServer";
            _isStopUDPServer = true;
            UdpClient udpClient = new UdpClient();
            IPAddress ipAddress = IPAddress.Parse(GetIPAddressStringByHostName(_udpServerName, AddressFamily.InterNetwork));
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, _udpServerListenerPort);
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Stop UDP Service");
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length, ipEndPoint);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxLogMessage, ex.Message, Brushes.Red);
            }
            SetMessageText(RichTextBoxLogMessage, $"UDPServer[{_udpServerListenerPort}] service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            udpClient.Close();

            BtnStopUDPService.IsEnabled = false;
            BtnStartUDPService.IsEnabled = !BtnStopUDPService.IsEnabled;
        }
    }
}

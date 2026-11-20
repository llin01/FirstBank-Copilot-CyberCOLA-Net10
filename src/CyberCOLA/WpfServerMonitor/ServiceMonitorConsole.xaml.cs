using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Network.Server;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.Yetty.Context;
using NLog;
using System;
using System.Collections.Generic;
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
using System.Xml;
using System.Xml.Serialization;
using Logger = NLog.Logger;

namespace Com.Cybersoft4u.Test.Wpf.WpfServerMonitor
{
    /// <summary>
    /// ServiceMonitorConsole.xaml Interaction logic
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="System.Windows.Markup.IStyleConnector" />
    public partial class ServiceMonitorConsole : Window
    {
        #region NLog

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        //private static readonly Logger Logger = LoggerAdapter.GetLogger("ServerMonitorService");

        #endregion //NLog

        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private ServiceMonitorInfo _serviceMonitorInfo;

        private ServerMonitorService _monitorServerService;

        private bool _isServiceAutoStart;

        private bool _isStopUPDServer = false;

        private List<string> _clientAuthServiceLocalPorts = new List<string>();

        private System.Windows.Threading.DispatcherTimer _getConnectionDispatcherTimer;

        private static readonly Random StaticTraceRnd = new Random();

        public ServiceMonitorConsole()
        {
            InitializeComponent();
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            InitSystemConfig();
            if (!_isServiceAutoStart) return;
            var peer = new ButtonAutomationPeer(BtnStart);
            var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            invokeProv?.Invoke();
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
            RefreshGatewayDataGrid();
            RefreshClientAuthDataGrid();
        }

        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            RichTextBox rtb = richTextBox as RichTextBox;
            if (rtb == null) return;
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
                            return (null);
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
                Log.Fatal(ex, $"GetIPAddressStringByHostName exception[{hostNameOrIPAddress}]");
                return (null);
            }
        }

        private class ConnectionInfo
        {
            public string ClientType { get; set; }
            public string IPAddress { get; set; }
            public string Status { get; set; }
            public bool Action { get; set; }

        }

        private void InitSystemConfig()
        {
            try
            {
                InitControlAttribute();
                var configurationLoderRoot = new ConfigurationLoaderRoot();
                _isServiceAutoStart = Convert.ToBoolean(configurationLoderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "IsServiceAutoStart").Trim());
                _clientAuthServiceLocalPorts = configurationLoderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ClientAuthServiceLocalPorts").Trim().Split(',').ToList();
                var xmlFilePath = configurationLoderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "service-monitor-info-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(ServiceMonitorInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _serviceMonitorInfo = (ServiceMonitorInfo)serializer.Deserialize(reader);
                }
                TextBoxInterval.Text = _serviceMonitorInfo.ServiceInfo.ClientRetrievalTimeIntervalSec.Trim();
                InitComboBox();
                _getConnectionDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _getConnectionDispatcherTimer.Tick += getClientConnectionDispatcherTimer_Tick;
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(_serviceMonitorInfo.ServiceInfo.ClientRetrievalTimeIntervalSec.Trim()));
                DataGridGatewayConnectionInfo.Items.Clear();
                DataGridClientAuthConnectionInfo.Items.Clear();
                StartUDPService();
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                BtnSend.IsEnabled = !BtnStart.IsEnabled;
                Title = $"{_serviceMonitorInfo.BankInfo.BankName} Service Monitor";
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
        }

        private void InitControlAttribute()
        {
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = false;
            RichTextBoxSystemMessage.Document.Blocks.Clear();
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        /// <summary>
        /// Initializes the ComboBox.
        /// </summary>
        private void InitComboBox()
        {
            ComboBoxSelectCategory.Items.Clear();
            List<string> categoryList = new List<string>();
            categoryList.Add($"V--{_serviceMonitorInfo.CommandItemsInfo.Gateway.VISAService.Description}");
            categoryList.Add($"M--{_serviceMonitorInfo.CommandItemsInfo.Gateway.MASTERCARDService.Description}");
            categoryList.Add($"MANR--{_serviceMonitorInfo.CommandItemsInfo.Gateway.MASTERCARDANRService.Description}");
            categoryList.Add($"C--{_serviceMonitorInfo.CommandItemsInfo.Gateway.COLAService.Description}");
            categoryList.Add($"N--{_serviceMonitorInfo.CommandItemsInfo.Gateway.NCCCService.Description}");

            ComboBoxSelectCategory.ItemsSource = categoryList.Distinct().ToList();
            ComboBoxSelectCategory.SelectedIndex = 0;
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
            return (interchangeMessage);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var result = (sender as CheckBox)?.IsChecked;
            CheckBox checkBox = (CheckBox)sender;
            checkBox.BindingGroup.CommitEdit();
        }

        private void StartUDPService()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start UDPServer";
            try
            {
                if (!Convert.ToBoolean(_serviceMonitorInfo.UDPServiceInfo.IsStart.Trim()))
                {
                    SetMessageText(RichTextBoxSystemMessage,
                        $"UDP Service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Brushes.Red);
                    Mouse.OverrideCursor = null;
                    return;
                }
                BtnStartUDPService.IsEnabled = false;
                BtnStopUDPService.IsEnabled = !BtnStartUDPService.IsEnabled;
                _isStopUPDServer = false;
                Thread udpListener = new Thread(ReadServerLogMessage);
                udpListener.Start();
                SetMessageText(RichTextBoxSystemMessage, $"UDP Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ReadServerLogMessage()
        {
            UdpClient listener = new UdpClient(Convert.ToInt32(_serviceMonitorInfo.UDPServiceInfo.ListenerPort.Trim()));
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any,
                Convert.ToInt32(_serviceMonitorInfo.UDPServiceInfo.ListenerPort.Trim()));
            try
            {
                while (!_isStopUPDServer)
                {
                    byte[] readBytes = listener.Receive(ref groupEP);
                    if (readBytes == null) continue;
                    SetMessageText(RichTextBoxLogMessage,
                        $"{Encoding.Default.GetString(readBytes, 0, readBytes.Length)}", Brushes.Black);
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
            finally
            {
                listener.Close();
            }
        }

        private void WriteSendMessageToLog(string localEndPoint, string remoteEndPoint, byte[] bytes)
        {
            try
            {
                var length = bytes?.Length ?? 0;
                Log.Info(
                    () =>
                        $"WRITE [{localEndPoint} => {remoteEndPoint}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(bytes)}");
                Log.Info(() => $"[{ColaCommUtility.PrintBytesHex(bytes)}]");
                Log.Info(() => $"[{ColaCommUtility.PrintBytesData(bytes)}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
        }

        private void ComboBoxSelectCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Convert.ToBoolean(RtnGateway.IsChecked))
            {
                ComboBoxSelectCommandCode.ItemsSource = null;
                if (ComboBoxSelectCategory.SelectedValue == null) return;
                var categoryCodes = ComboBoxSelectCategory.SelectedValue.ToString().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
                List<string> actionsList = null;
                if (categoryCodes[0].Equals("V"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.Gateway.VISAService.Commands.Command;
                else if (categoryCodes[0].Equals("M"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.Gateway.MASTERCARDService.Commands.Command;
                else if (categoryCodes[0].Equals("MANR"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.Gateway.MASTERCARDANRService.Commands.Command;
                else if (categoryCodes[0].Equals("C"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.Gateway.COLAService.Commands.Command;
                else if (categoryCodes[0].Equals("N"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.Gateway.NCCCService.Commands.Command;

                ComboBoxSelectCommandCode.ItemsSource = actionsList;
                ComboBoxSelectCommandCode.SelectedIndex = 0;
            }
            else
            {
                ComboBoxSelectCommandCode.ItemsSource = null;
                if (ComboBoxSelectCategory.SelectedValue == null) return;
                var categoryCodes = ComboBoxSelectCategory.SelectedValue.ToString().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
                List<string> actionsList = null;
                if (categoryCodes[0].Equals("V"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.ClientAuth.VISAClient.Commands.Command;
                else if (categoryCodes[0].Equals("M"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.ClientAuth.MASTERCARDClient.Commands.Command;
                else if (categoryCodes[0].Equals("N"))
                    actionsList = _serviceMonitorInfo.CommandItemsInfo.ClientAuth.NCCCClient.Commands.Command;

                ComboBoxSelectCommandCode.ItemsSource = actionsList;
                ComboBoxSelectCommandCode.SelectedIndex = 0;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (BtnStop.IsEnabled)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnStop);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
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
            StartUDPService();
        }

        private void BtnStopUDPService_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Stop UDPServer";
            _isStopUPDServer = true;
            UdpClient udpClient = new UdpClient();
            IPAddress ipAddress = IPAddress.Parse(
                GetIPAddressStringByHostName(_serviceMonitorInfo.UDPServiceInfo.ServerName.Trim(),
                    AddressFamily.InterNetwork));
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress,
                Convert.ToInt32(_serviceMonitorInfo.UDPServiceInfo.ListenerPort.Trim()));
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Stop UDP Service");
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length, ipEndPoint);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
            SetMessageText(RichTextBoxSystemMessage, $"UDP Service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Brushes.Red);
            BtnStopUDPService.IsEnabled = false;
            BtnStartUDPService.IsEnabled = !BtnStopUDPService.IsEnabled;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start Service";
            DataGridGatewayConnectionInfo.Items.Clear();
            DataGridClientAuthConnectionInfo.Items.Clear();
            try
            {
                var serviceInfo = _serviceMonitorInfo.ServiceInfo;
                _monitorServerService = null;
                _monitorServerService = new ServerMonitorService(
                    GetIPAddressStringByHostName(serviceInfo.ServerName.Trim(), AddressFamily.InterNetwork),
                                                 Convert.ToInt32(serviceInfo.ListenerPort.Trim()),
                                                 Convert.ToInt32(serviceInfo.BackLog.Trim()),
                                                 "ServerMonitorService");
                _monitorServerService.StartService();
                SetMessageText(RichTextBoxSystemMessage,
                    $"Server service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Listener Port[{Convert.ToInt32(serviceInfo.ListenerPort.Trim())}]",
                    Brushes.Red);
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = !BtnStart.IsEnabled;
                BtnSend.IsEnabled = !BtnStart.IsEnabled;
                BtnAutoRetrieval.Content = "Stop retrieval";
                _getConnectionDispatcherTimer.Start();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (BtnStop.IsEnabled)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnStop);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
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

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Stop service";
            try
            {
                _monitorServerService?.StopService();
                SetMessageText(RichTextBoxSystemMessage, $"Server service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Brushes.Red);
                DataGridGatewayConnectionInfo.Items.Clear();
                DataGridClientAuthConnectionInfo.Items.Clear();
                BtnStop.IsEnabled = false;
                BtnStart.IsEnabled = !BtnStop.IsEnabled;
                BtnSend.IsEnabled = !BtnStart.IsEnabled;
                _getConnectionDispatcherTimer.Stop();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxSystemMessage.Document.Blocks.Clear();
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (Convert.ToBoolean(RtnGateway.IsChecked))
                SendGateayCommand();
            else
                SendClientAuthCommand();
        }

        private void RefreshGatewayDataGrid()
        {
            string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            SetMessageText(RichTextBoxSystemMessage, $"Retrieval gateway connection status at : {dateTimeNow}",
                Brushes.Red);
            GroupBoxGateway.Header = $"Gateway connection at : {dateTimeNow}";
            if (DataGridGatewayConnectionInfo == null) return;
            var clientOfConnections = _monitorServerService?.GetClientOfConnections();
            if (clientOfConnections == null) return;
            if (clientOfConnections.Count == 0)
            {
                DataGridGatewayConnectionInfo.Items.Clear();
                return;
            }
            if (DataGridGatewayConnectionInfo.Items.Count == 0)
            {
                foreach (var item in clientOfConnections)
                {
                    string[] endPoingStrings = item.Key.Trim().Split(':');
                    var isExist = _clientAuthServiceLocalPorts.Exists(x => x == endPoingStrings[1]);
                    var connectionInfoData = isExist
                        ? new ConnectionInfo
                        {
                            ClientType = "ClientAuth",
                            IPAddress = item.Key,
                            Status = item.Value,
                            Action = false
                        }
                        : new ConnectionInfo
                        {
                            ClientType = "Gateway",
                            IPAddress = item.Key,
                            Status = item.Value,
                            Action = false
                        };
                    if (connectionInfoData.ClientType.Equals("Gateway"))
                        DataGridGatewayConnectionInfo.Items.Add(connectionInfoData);
                }
                return;
            }
            try
            {
                DataGridGatewayConnectionInfo.Items.Refresh();
                foreach (var item in DataGridGatewayConnectionInfo.Items)
                {
                    ConnectionInfo connectionInfo = item as ConnectionInfo;
                    SetMessageText(RichTextBoxSystemMessage, $"Refresh{connectionInfo?.IPAddress}", Brushes.Red);
                    if (connectionInfo?.IPAddress != null)
                    {
                        if (!clientOfConnections.ContainsKey(connectionInfo.IPAddress))
                        {
                            DataGridGatewayConnectionInfo.Items.Remove(connectionInfo);
                            SetMessageText(RichTextBoxSystemMessage,
                                $"Remove gateway connection : {connectionInfo.IPAddress}", Brushes.Red);
                        }
                    }
                }
                DataGridGatewayConnectionInfo.Items.Refresh();
                foreach (var item in clientOfConnections)
                {
                    string endPoingStrings = item.Key.Trim();
                    bool isFind = false;
                    for (var i = 0; i < DataGridGatewayConnectionInfo.Items.Count; i++)
                    {
                        if (((ConnectionInfo)DataGridGatewayConnectionInfo.Items[i]).IPAddress == endPoingStrings)
                        {
                            isFind = true;
                            break;
                        }
                    }
                    if (!isFind)
                    {
                        var isExist = _clientAuthServiceLocalPorts.Exists(x => x == endPoingStrings.Split(':')[1]);
                        var connectionInfoData = isExist
                            ? new ConnectionInfo
                            {
                                ClientType = "ClientAuth",
                                IPAddress = item.Key,
                                Status = item.Value,
                                Action = false
                            }
                            : new ConnectionInfo
                            {
                                ClientType = "Gateway",
                                IPAddress = item.Key,
                                Status = item.Value,
                                Action = false
                            };
                        if (connectionInfoData.ClientType.Equals("Gateway"))
                        {
                            DataGridGatewayConnectionInfo.Items.Insert(DataGridGatewayConnectionInfo.Items.Count,
                                connectionInfoData);
                        }
                        DataGridGatewayConnectionInfo.Items.Refresh();
                    }
                }
            }
            catch (InvalidOperationException ex1)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex1.Message}", Brushes.Red);
            }
            catch (Exception ex2)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex2.Message}", Brushes.Red);
            }
            finally
            {
                DataGridGatewayConnectionInfo.Items.Refresh();
            }
        }

        private void RefreshClientAuthDataGrid()
        {
            string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            SetMessageText(RichTextBoxSystemMessage, $"Retrieval clientauth connection status at : {dateTimeNow}",
                Brushes.Red);
            GroupBoxClientAuth.Header = $"ClientAuth connection at : {dateTimeNow}";
            if (DataGridClientAuthConnectionInfo == null) return;
            var clientOfConnections = _monitorServerService?.GetClientOfConnections();
            if (clientOfConnections == null) return;
            if (clientOfConnections.Count == 0)
            {
                DataGridClientAuthConnectionInfo.Items.Clear();
                return;
            }
            if (DataGridClientAuthConnectionInfo.Items.Count == 0)
            {
                foreach (var item in clientOfConnections)
                {
                    string[] endPoingStrings = item.Key.Trim().Split(':');
                    var isExist = _clientAuthServiceLocalPorts.Exists(x => x == endPoingStrings[1]);
                    var connectionInfoData = isExist
                        ? new ConnectionInfo
                        {
                            ClientType = "ClientAuth",
                            IPAddress = item.Key,
                            Status = item.Value,
                            Action = false
                        }
                        : new ConnectionInfo
                        {
                            ClientType = "Gateway",
                            IPAddress = item.Key,
                            Status = item.Value,
                            Action = false
                        };
                    if (connectionInfoData.ClientType.Equals("ClientAuth"))
                        DataGridClientAuthConnectionInfo.Items.Add(connectionInfoData);
                }
                return;
            }
            try
            {
                DataGridClientAuthConnectionInfo.Items.Refresh();
                foreach (var item in DataGridClientAuthConnectionInfo.Items)
                {
                    ConnectionInfo connectionInfo = item as ConnectionInfo;
                    SetMessageText(RichTextBoxSystemMessage, $"Refresh{connectionInfo?.IPAddress}", Brushes.Red);
                    if (connectionInfo?.IPAddress != null)
                    {
                        if (!clientOfConnections.ContainsKey(connectionInfo.IPAddress))
                        {
                            DataGridClientAuthConnectionInfo.Items.Remove(connectionInfo);
                            SetMessageText(RichTextBoxSystemMessage, $"Remove clientauth connection : {connectionInfo.IPAddress}", Brushes.Red);
                        }
                    }
                }
                DataGridClientAuthConnectionInfo.Items.Refresh();
                foreach (var item in clientOfConnections)
                {
                    string endPoingStrings = item.Key.Trim();
                    bool isFind = false;
                    for (var i = 0; i < DataGridClientAuthConnectionInfo.Items.Count; i++)
                    {
                        if (((ConnectionInfo)DataGridClientAuthConnectionInfo.Items[i]).IPAddress == endPoingStrings)
                        {
                            isFind = true;
                            break;
                        }
                    }
                    if (!isFind)
                    {
                        var isExist = _clientAuthServiceLocalPorts.Exists(x => x == endPoingStrings.Split(':')[1]);
                        var connectionInfoData = isExist
                            ? new ConnectionInfo
                            {
                                ClientType = "ClientAuth",
                                IPAddress = item.Key,
                                Status = item.Value,
                                Action = false
                            }
                            : new ConnectionInfo
                            {
                                ClientType = "Gateway",
                                IPAddress = item.Key,
                                Status = item.Value,
                                Action = false
                            };
                        if (connectionInfoData.ClientType.Equals("ClientAuth"))
                        {
                            DataGridClientAuthConnectionInfo.Items.Insert(DataGridClientAuthConnectionInfo.Items.Count,
                                connectionInfoData);
                        }
                        DataGridClientAuthConnectionInfo.Items.Refresh();
                    }
                }
            }
            catch (InvalidOperationException ex1)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex1.Message}", Brushes.Red);
            }
            catch (Exception ex2)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex2.Message}", Brushes.Red);
            }
            finally
            {
                DataGridClientAuthConnectionInfo.Items.Refresh();
            }
        }

        private void SendGateayCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to gateway client";
            try
            {
                SetMessageText(RichTextBoxSystemMessage,
                    $"Start send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Brushes.Red);
                if (DataGridGatewayConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage,
                        $"End   send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : 0",
                        Brushes.Red);
                    return;
                }
                var clientOfChannels = _monitorServerService?.GetClientOfChannels();
                if (clientOfChannels == null)
                {
                    SetMessageText(RichTextBoxSystemMessage,
                        $"End   send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : 0",
                        Brushes.Red);
                    return;
                }
                if (clientOfChannels.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"End   send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : 0", Brushes.Red);
                    return;
                }
                int sendCount = 0;
                for (int index = 0; index < DataGridGatewayConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridGatewayConnectionInfo.Items[index];
                    if (!item.ClientType.ToUpper().Equals("GATEWAY")) continue;
                    if (!item.Action) continue;
                    ChannelHandlerContext ctx;
                    if (clientOfChannels.TryGetValue(item.IPAddress, out ctx))
                    {
                        var interchangeMessage = CreateMessage('S');
                        interchangeMessage.SourceEndPoint = ctx.Channel.LocalEndPoint().ToString();
                        interchangeMessage.DestinationEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                        interchangeMessage.Category = $"G--{ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                        interchangeMessage.CommandCode = ComboBoxSelectCommandCode.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        interchangeMessage.ReturnOption = true;
                        SetMessageText(RichTextBoxSystemMessage, $"--- Start display send interchangemessage ---", Brushes.Black);
                        SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage), Brushes.Black);
                        SetMessageText(RichTextBoxSystemMessage, $"--- End   display send interchangemessage ---", Brushes.Black);
                        //var sendBytes = ColaCommUtility.ObjectSerialize<InterchangeMessage>(interchangeMessage);
                        var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                        //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        //string jsonString = javaScriptSerializer.Serialize(interchangeMessage);
                        //SetMessageText(RichTextBoxSelectCategoryActionCodeInfo, $"JSON Format : {jsonString}", Brushes.Red);
                        //byte[] sendBytes = null;
                        //BinaryFormatter bf = new BinaryFormatter();
                        //using (MemoryStream memoryStream = new MemoryStream())
                        //{
                        //    bf.Serialize(memoryStream, interchangeMessage);
                        //    sendBytes =  memoryStream.ToArray();
                        //}
                        ctx.Channel.WriteAndFlush(sendBytes);
                        WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint,
                            sendBytes);
                        sendCount++;
                    }
                }
                SetMessageText(RichTextBoxSystemMessage,
                    $"End   send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : {sendCount}",
                    Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SendClientAuthCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to clientauth client";
            try
            {
                SetMessageText(RichTextBoxSystemMessage,
                $"Start send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}", Brushes.Red);
                if (DataGridClientAuthConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage,
                        $"End   send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : 0",
                        Brushes.Red);
                    return;
                }
                var clientOfChannels = _monitorServerService?.GetClientOfChannels();
                if (clientOfChannels == null)
                {
                    SetMessageText(RichTextBoxSystemMessage,
                        $"End   send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : 0",
                        Brushes.Red);
                    return;
                }
                if (clientOfChannels.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage,
                        $"End   send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : 0",
                        Brushes.Red);
                    return;
                }
                int sendCount = 0;
                for (int index = 0; index < DataGridClientAuthConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridClientAuthConnectionInfo.Items[index];
                    if (!item.ClientType.ToUpper().Equals("CLIENTAUTH")) continue;
                    if (!item.Action) continue;
                    ChannelHandlerContext ctx;
                    if (clientOfChannels.TryGetValue(item.IPAddress, out ctx))
                    {
                        var interchangeMessage = CreateMessage('S');
                        interchangeMessage.SourceEndPoint = ctx.Channel.LocalEndPoint().ToString();
                        interchangeMessage.DestinationEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                        interchangeMessage.Category = $"C--{ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                        interchangeMessage.CommandCode = ComboBoxSelectCommandCode.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        if (interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.NCCCClientCommandCode.FileMaintUpdate))
                        {
                            var fileMaintDataBytes = CreateFileMaintDataBytes();
                            if (fileMaintDataBytes != null)
                                interchangeMessage.InterchangeData = fileMaintDataBytes;
                            else
                            {
                                SetMessageText(RichTextBoxSystemMessage, $"Create file maintenance data error", Brushes.Red);
                                return;
                            }
                        }
                        interchangeMessage.ReturnOption = true;
                        SetMessageText(RichTextBoxSystemMessage, $"--- Start display send interchangemessage ---",
                            Brushes.Black);
                        SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage),
                            Brushes.Black);
                        SetMessageText(RichTextBoxSystemMessage, $"--- End   display send interchangemessage ---",
                            Brushes.Black);
                        //var sendBytes = ColaCommUtility.ObjectSerialize<InterchangeMessage>(interchangeMessage);
                        var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                        //var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                        //string jsonString = javaScriptSerializer.Serialize(interchangeMessage);
                        //SetMessageText(RichTextBoxSelectCategoryActionCodeInfo, $"JSON Format : {jsonString}", Brushes.Red);
                        //byte[] sendBytes = null;
                        //BinaryFormatter bf = new BinaryFormatter();
                        //using (MemoryStream memoryStream = new MemoryStream())
                        //{
                        //    bf.Serialize(memoryStream, interchangeMessage);
                        //    sendBytes =  memoryStream.ToArray();
                        //}
                        ctx.Channel.WriteAndFlush(sendBytes);
                        WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint,
                            sendBytes);
                        sendCount++;
                    }
                }
                SetMessageText(RichTextBoxSystemMessage,
                    $"End   send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Total Send : {sendCount}",
                    Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void BtnAutoRetrieval_Click(object sender, RoutedEventArgs e)
        {
            if (_getConnectionDispatcherTimer.IsEnabled)
            {
                _getConnectionDispatcherTimer.Stop();
                BtnAutoRetrieval.Content = "Start retrieval";
            }
            else
            {
                int intervalSecond;
                bool result = Int32.TryParse(TextBoxInterval.Text.Trim(), out intervalSecond);
                if (result)
                {
                    _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, intervalSecond);
                    _getConnectionDispatcherTimer.Start();
                    BtnAutoRetrieval.Content = "Stop retrieval";
                }
                else
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Input interval invalid : {TextBoxInterval}", Brushes.Red);
                    TextBoxInterval.Focus();
                }
            }
        }

        private void RtnGateway_Click(object sender, RoutedEventArgs e)
        {

            ComboBoxSelectCategory.ItemsSource = null;
            ComboBoxSelectCommandCode.ItemsSource = null;
            List<string> categoryList = new List<string>();
            categoryList.Add($"V--{_serviceMonitorInfo.CommandItemsInfo.Gateway.VISAService.Description}");
            categoryList.Add($"M--{_serviceMonitorInfo.CommandItemsInfo.Gateway.MASTERCARDService.Description}");
            categoryList.Add($"MANR--{_serviceMonitorInfo.CommandItemsInfo.Gateway.MASTERCARDANRService.Description}");
            categoryList.Add($"C--{_serviceMonitorInfo.CommandItemsInfo.Gateway.COLAService.Description}");
            categoryList.Add($"N--{_serviceMonitorInfo.CommandItemsInfo.Gateway.NCCCService.Description}");

            ComboBoxSelectCategory.ItemsSource = categoryList.Distinct().ToList();
            ComboBoxSelectCategory.SelectedIndex = 0;
        }

        private void ClientAuth_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxSelectCategory.ItemsSource = null;
            ComboBoxSelectCommandCode.ItemsSource = null;
            List<string> categoryList = new List<string>();
            categoryList.Add($"V--{_serviceMonitorInfo.CommandItemsInfo.ClientAuth.VISAClient.Description}");
            categoryList.Add($"M--{_serviceMonitorInfo.CommandItemsInfo.ClientAuth.MASTERCARDClient.Description}");
            categoryList.Add($"N--{_serviceMonitorInfo.CommandItemsInfo.ClientAuth.NCCCClient.Description}");

            ComboBoxSelectCategory.ItemsSource = categoryList.Distinct().ToList();
            ComboBoxSelectCategory.SelectedIndex = 0;
        }

        private byte[] CreateFileMaintDataBytes()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                FileMaintDataXml fileMaintDataXml;
                var xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "nccc-file-maintenance-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(FileMaintDataXml));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    fileMaintDataXml = (FileMaintDataXml)serializer.Deserialize(reader);
                }
                var fileMaintData = new FileMaintData();
                if (fileMaintDataXml.DataType.ToUpper().Equals("FIXID"))
                {
                    fileMaintData.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    fileMaintData.TxKeySeq = fileMaintDataXml.TxKeySeq.Text;
                    fileMaintData.IsoP2CardNbr = fileMaintDataXml.IsoP2CardNbr.Text;
                    fileMaintData.IsoP7TxDteTime = fileMaintDataXml.IsoP7TxDteTime.Text;
                    fileMaintData.IsoP11TraceNo = fileMaintDataXml.IsoP11TraceNo.Text;
                    fileMaintData.IsoP13TransYear = fileMaintDataXml.IsoP13TransYear?.Text;
                    fileMaintData.IsoP36NTID = fileMaintDataXml.IsoP36NTID?.Text;
                    fileMaintData.IsoP39Rsp = fileMaintDataXml.IsoP39Rsp.Text;
                    fileMaintData.IsoP48AdditionalData = fileMaintDataXml.IsoP48AdditionalData.Text;
                    fileMaintData.IsoP49TranCurCode = fileMaintDataXml.IsoP49TranCurCode.Text;
                    fileMaintData.IsoP58T05TxDte = fileMaintDataXml.IsoP58T05TxDte.Text;
                    fileMaintData.IsoP60FromHostMaintData = fileMaintDataXml.IsoP60FromHostMaintData.Text;
                    fileMaintData.IsoP63SystemID = fileMaintDataXml.IsoP63SystemID?.Text;
                    fileMaintData.IsoS73ActDte = fileMaintDataXml.IsoS73ActDte.Text;
                    fileMaintData.IsoS91FileUpdCode = fileMaintDataXml.IsoS91FileUpdCode.Text;
                    fileMaintData.IsoS92FileSecurityCode = fileMaintDataXml.IsoS92FileSecurityCode.Text;
                    fileMaintData.IsoS101FileName = fileMaintDataXml.IsoS101FileName.Text;
                    fileMaintData.IsoS120FileUpd = fileMaintDataXml.IsoS120FileUpd.Text;
                    fileMaintData.IsoS122HostMaint = fileMaintDataXml.IsoS122HostMaint.Text;
                }
                else
                {
                    if (fileMaintDataXml.TxKeySeq.Changed.ToUpper().Equals("Y"))
                        fileMaintData.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    else
                        fileMaintData.TxKeySeq = fileMaintDataXml.TxKeySeq.Text;
                    fileMaintData.IsoP2CardNbr = fileMaintDataXml.IsoP2CardNbr.Text;
                    if (fileMaintDataXml.IsoP7TxDteTime.Changed.ToUpper().Equals("Y"))
                        fileMaintData.IsoP7TxDteTime = $"{DateTime.Now:MMddHHmmss}";
                    else
                        fileMaintData.IsoP7TxDteTime = fileMaintDataXml.IsoP7TxDteTime.Text;

                    if (fileMaintDataXml.IsoP11TraceNo.Changed.ToUpper().Equals("Y"))
                    {
                        int traceNumber = StaticTraceRnd.Next(1, 999999);
                        fileMaintData.IsoP11TraceNo = $"{traceNumber:000000}";
                    }
                    else
                        fileMaintData.IsoP11TraceNo = fileMaintDataXml.IsoP11TraceNo.Text;
                    fileMaintData.IsoP13TransYear = fileMaintDataXml.IsoP13TransYear?.Text;
                    fileMaintData.IsoP36NTID = fileMaintDataXml.IsoP36NTID?.Text;
                    fileMaintData.IsoP39Rsp = fileMaintDataXml.IsoP39Rsp.Text;
                    fileMaintData.IsoP48AdditionalData = fileMaintDataXml.IsoP48AdditionalData.Text;
                    fileMaintData.IsoP49TranCurCode = fileMaintDataXml.IsoP49TranCurCode.Text;
                    if (fileMaintDataXml.IsoP58T05TxDte.Changed.ToUpper().Equals("Y"))
                        fileMaintData.IsoP58T05TxDte = $"{DateTime.Now:yyyyMMddHHmmss}";
                    else
                        fileMaintData.IsoP58T05TxDte = fileMaintDataXml.IsoP58T05TxDte.Text;
                    fileMaintData.IsoP60FromHostMaintData = fileMaintDataXml.IsoP60FromHostMaintData.Text;
                    fileMaintData.IsoP63SystemID = fileMaintDataXml.IsoP63SystemID?.Text;
                    fileMaintData.IsoS73ActDte = fileMaintDataXml.IsoS73ActDte.Text;
                    fileMaintData.IsoS91FileUpdCode = fileMaintDataXml.IsoS91FileUpdCode.Text;
                    fileMaintData.IsoS92FileSecurityCode = fileMaintDataXml.IsoS92FileSecurityCode.Text;
                    fileMaintData.IsoS101FileName = fileMaintDataXml.IsoS101FileName.Text;
                    fileMaintData.IsoS120FileUpd = fileMaintDataXml.IsoS120FileUpd.Text;
                    fileMaintData.IsoS122HostMaint = fileMaintDataXml.IsoS122HostMaint.Text;
                }
                //var fileMaintDataBytes = ColaCommUtility.ObjectSerialize<FileMaintData>(fileMaintData);
                var fileMaintDataBytes = ColaCommUtility.ObjectToXMLBytes(fileMaintData);

                return (fileMaintDataBytes);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                return (null);
            }
        }
    }
}


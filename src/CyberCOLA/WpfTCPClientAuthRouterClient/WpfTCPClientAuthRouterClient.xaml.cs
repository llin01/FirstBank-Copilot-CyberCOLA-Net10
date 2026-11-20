using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using Com.Cybersoft4u.Test.Wpf.WpfTCPClientAuthRouterClient.Models;
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
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfTCPClientAuthRouterClient
{
    /// <summary>
    /// ServiceMonitorConsole.xaml 的互動邏輯
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="System.Windows.Markup.IStyleConnector" />
    public partial class TCPClientAuthRouterService : Window
    {
        private readonly Logger Logger;

        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private TCPClientAuthRouterServiceInfo _tcpClientAuthRouterServiceInfo;

        private InterchangeMessageCommandInfo _interchangeMessageCommandInfo;

        private TCPClientService _tcpClientService;

        private bool _isStopUDPServer = false;

        private System.Windows.Threading.DispatcherTimer _getConnectionDispatcherTimer;

        private static readonly Random StaticTraceRnd = new Random();

        private int _selectServerIndex = 0;

        private string _selectGatewayType;

        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;

        public TCPClientAuthRouterService()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[TCPClientAuthRouterService]");
            InitializeComponent();
            TextBoxInterval.TextChanged += new TextChangedEventHandler(TextChanged);
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            _configurationLoaderRoot = new ConfigurationLoaderRoot();
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
                DataGridClientAuthConnectionInfo.Items.Clear();
                if (_tcpClientService.IsConnected())
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Connect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Query client connection to server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    DisplayClientAuthDataGrid();
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
            public string IPAddress { get; set; }
            public string Status { get; set; }
        }

        private class ServerInfo
        {
            public string GatewayType { get; set; }
            public string RemoteHostName { get; set; }
            public string IPAddress { get; set; }
            public int ListenerPortNumber { get; set; }
            public bool Action { get; set; }
            public bool IsEdit { get; set; }
        }

        private void InitSystemConfig()
        {
            try
            {
                InitControlAttribute();
                Logger.Info($"{AppDomain.CurrentDomain.FriendlyName} Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:ffff}");
                var xmlFilePath = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "tcp-clientauth-router-service-info-xml-file-path");
                //var configurationLoader = new ConfigurationLoader();
                //var xmlFilePath = configurationLoader.Get("tcp-clientauth-router-service-info-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(TCPClientAuthRouterServiceInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _tcpClientAuthRouterServiceInfo = (TCPClientAuthRouterServiceInfo)serializer.Deserialize(reader);
                }

                xmlFilePath = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "interchange-message-command-info-xml-file-path");
                serializer = new XmlSerializer(typeof(InterchangeMessageCommandInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _interchangeMessageCommandInfo = (InterchangeMessageCommandInfo)serializer.Deserialize(reader);
                }
                _selectServerIndex = Convert.ToInt16(_tcpClientAuthRouterServiceInfo?.ClientAuthRouterServiceConfig.Client.DefaultServerIndex);
                if (_selectServerIndex >= _tcpClientAuthRouterServiceInfo?.ClientAuthRouterServiceConfig.Servers.Server.Count)
                {
                    SetMessageText(RichTextBoxSystemMessage, "Default Server Select Index Invalid, Set Select index to 0", Brushes.Red);
                    _selectServerIndex = 0;
                }
                var remoteHostName = _tcpClientAuthRouterServiceInfo?.ClientAuthRouterServiceConfig.Servers.Server[_selectServerIndex].RemoteHostName.Trim();
                var remoteIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Servers.Server[_selectServerIndex].RemoteIP.Trim(), AddressFamily.InterNetwork);
                var remotePortNumberString = _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Servers.Server[_selectServerIndex].RemotePort.Trim();
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddressString), Convert.ToInt32(remotePortNumberString));

                var localIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.LocalIP.Trim(), AddressFamily.InterNetwork);
                var localPortNumberString = _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.LocalPort.Trim();
                var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPAddressString), Convert.ToInt32(localPortNumberString));

                var udpListenerPort = Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPListenerPort.Trim());
                var udpAutoStartFlag = Convert.ToBoolean(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPAutoStartFlag.Trim());
                _tcpClientService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
                _tcpClientService.EnableLogger("TCPClientAuthRouterService", Utils.LogLevel.Debug);
                TextBoxConnectPort.Text = remotePortNumberString;
                //TextBoxInterval.Text = _tcpClientServiceInfo.TCPClientServiceConfig.ClientRetrievalTimeIntervalSec.Trim();
                InitComboBox();
                _getConnectionDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _getConnectionDispatcherTimer.Tick += getClientConnectionDispatcherTimer_Tick;
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.ClientRetrievalTimeIntervalSec.Trim()));
                DisplayServerLIstDataGrid();
                DataGridClientAuthConnectionInfo.Items.Clear();
                StartUDPService(udpListenerPort, udpAutoStartFlag);
                Title = $"{_tcpClientAuthRouterServiceInfo.BankInfo.BankName} TCPClientAuthRouterClient Console For Remote IP {_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Servers.Server[_selectServerIndex].RemoteIP}";
                DataGridClientAuthConnectionInfo.Items.Clear();
                //Icon = new BitmapImage(new Uri("pack://application:,,,/Bank.ico", UriKind.RelativeOrAbsolute));
                if (Convert.ToBoolean(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.AutoStartFlag.Trim()))
                {
                    BtnConnection.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
                else
                {
                    BtnConnection.IsEnabled = true;
                    BtnDisconnection.IsEnabled = false;
                }
                TextBoxInterval.Text = _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.ClientRetrievalTimeIntervalSec.Trim();
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
            ComboBoxSelectCategory.Items.Clear();
            List<string> categoryList = new List<string>();
            //categoryList.Add($"G--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.Description}");
            categoryList.Add($"C--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.Description}");
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
            interchangeMessage.ReturnDateTime = String.Empty;
            return (interchangeMessage);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = (sender as CheckBox)?.IsChecked;
                CheckBox checkBox = (CheckBox)sender;
                checkBox.BindingGroup.CommitEdit();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
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
                    SetMessageText(RichTextBoxLogMessage, $"{Encoding.Default.GetString(readBytes, 0, readBytes.Length)}", Brushes.Black);
                    if (Encoding.Default.GetString(readBytes).Contains("Stop")) break;
                }
                udpClient.Close();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
            }
            finally
            {
                udpClient.Close();
            }
        }

        private void WriteSendMessageToLog(string localEndPoint, string remoteEndPoint, byte[] bytes)
        {
            try
            {
                var length = bytes?.Length ?? 0;
                Logger.Info(() => $"WRITE [{localEndPoint} => {remoteEndPoint}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(bytes)}");
                Logger.Info(() => $"[{ColaCommUtility.PrintBytesHex(bytes)}]");
                Logger.Info(() => $"[{ColaCommUtility.PrintBytesData(bytes)}]");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex.Message);
            }
        }

        private void ComboBoxSelectCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var categorySelect = ComboBoxSelectCategory.SelectedValue.ToString()
                                        ?.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> categoryList = new List<string>();
            switch (categorySelect[0].ToUpper())
            {
                case "C":
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthVISAClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.VISAClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthMASTERCARDClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.MASTERCARDClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthNCCCClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.NCCCClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthNCPSClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.NCPSClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthSTINClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.STINClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthFISCClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.FISCClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthTWINClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.TWINClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthSVCSClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.SVCSClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthICASHClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.ICASHClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthIPASSClient.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.IPASSClient.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.ClientAuthSystemCommand.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.ClientAuthServiceCommand.Description}");

                    ComboBoxSelectCategoryItem.ItemsSource = categoryList.Distinct().ToList();
                    ComboBoxSelectCategoryItem.SelectedIndex = 0;
                    break;
            }
        }

        private void ComboBoxSelectCategoryItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxSelectCategoryItem.Items.Count == 0) return;
            var categorySelect = ComboBoxSelectCategory.SelectedValue.ToString().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (ComboBoxSelectCategoryItem.SelectedValue == null) return;
            var categoryItemSelect = ComboBoxSelectCategoryItem.SelectedValue.ToString().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
            ComboBoxSelectCommandCode.ItemsSource = null;
            List<string> actionsList = null;
            switch (categorySelect.ToUpper())
            {
                case "G":
                    switch ($"{categorySelect.ToUpper()}--{categoryItemSelect.ToUpper()}")
                    {
                        case InterChangeMessageCategory.GatewayVISAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.VISAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayMASTERCARDService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.MASTERCARDService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayMASTERCARDANRService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.MASTERCARDANRService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayNCCCCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCCCCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayNCPSCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCPSCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayTWMPCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWMPCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewaySTINCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.STINCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayFISCCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.FISCCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayTWINCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWINCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewaySVCSCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.SVCSCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayICASHCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.ICASHCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayIPASSCOLAService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.IPASSCOLAService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayNCCCService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCCCService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayNCPSService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCPSService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayTWMPService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWMPService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewaySTINService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.STINService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayFISCService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.FISCService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayTWINService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWINService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewaySVCSService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.SVCSService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayICASHService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.ICASHService.Commands.Command;
                            break;
                        case InterChangeMessageCategory.GatewayIPASSService:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.IPASSService.Commands.Command;
                            break;

                    }
                    break;
                case "C":
                    switch ($"{categorySelect.ToUpper()}--{categoryItemSelect.ToUpper()}")
                    {
                        case InterChangeMessageCategory.ClientAuthVISAClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.VISAClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthMASTERCARDClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.MASTERCARDClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthNCCCClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.NCCCClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthNCPSClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.NCPSClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthSTINClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.STINClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthFISCClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.FISCClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthTWINClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.TWINClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthSVCSClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.SVCSClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthICASHClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.ICASHClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthIPASSClient:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.IPASSClient.Commands.Command;
                            break;
                        case InterChangeMessageCategory.ClientAuthSystemCommand:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.ClientAuthServiceCommand.Commands.Command;
                            break;
                    }
                    break;
            }

            ComboBoxSelectCommandCode.ItemsSource = actionsList;
            ComboBoxSelectCommandCode.SelectedIndex = 0;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpClientService != null && _tcpClientService.IsConnected())
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
            var udpListenerPort = Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPListenerPort.Trim());
            var udpAutoStartFlag = Convert.ToBoolean(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPAutoStartFlag.Trim());
            StartUDPService(udpListenerPort, udpAutoStartFlag);
        }

        private void BtnStopUDPService_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Stop UDPServer";
            _isStopUDPServer = true;
            UdpClient udpClient = new UdpClient();
            IPAddress ipAddress = IPAddress.Parse(CommonUtils.GetIPAddressStringByHostName(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPServerName.Trim(), AddressFamily.InterNetwork));
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, Convert.ToInt32(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPListenerPort.Trim()));
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
            SetMessageText(RichTextBoxSystemMessage, $"UDPServer[{_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.UDPServer.UDPListenerPort.Trim()}] service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            udpClient.Close();
            BtnStopUDPService.IsEnabled = false;
            BtnStartUDPService.IsEnabled = !BtnStopUDPService.IsEnabled;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_tcpClientService != null && _tcpClientService.IsConnected())
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

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (!_tcpClientService.IsConnected())
                {
                    SetMessageText(RichTextBoxSystemMessage, "No Session Connect to Server", Brushes.Red);
                    return;
                }
                var category = ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                switch (category.ToUpper())
                {
                    case "C":
                        ProcessClientAuthCommand();
                        break;
                    default:
                        SetMessageText(RichTextBoxSystemMessage, $"Invalid Category{category}", Brushes.Red);
                        break;
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

        private void DisplayClientAuthDataGrid()
        {
            try
            {
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                SetMessageText(RichTextBoxSystemMessage, $"Retrieval clientauth connection status at : {dateTimeNow}", Brushes.Red);
                GroupBoxClientAuth.Header = $"Client[{_tcpClientService.LocalEndPoint()}] connect at : {dateTimeNow}";
                DataGridClientAuthConnectionInfo.Items.Clear();
                {
                    var connectionInfoData = new ConnectionInfo
                    {
                        IPAddress = _tcpClientService.RemoteEndPoint().ToString()?.Trim(),
                        Status = "Connection",
                    };
                    DataGridClientAuthConnectionInfo.Items.Add(connectionInfoData);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception:{ex.Message}", Brushes.Red);
            }
        }

        private void DisplayServerLIstDataGrid()
        {
            try
            {
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                GroupBoxServer.Header = $"Server List at : {dateTimeNow}";
                DataGridServerInfo.Items.Clear();
                int listIndex = 0;
                foreach (var server in _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Servers.Server)
                {
                    var srerverListsInfo = new ServerInfo
                    {
                        GatewayType = server.GatewayType,
                        RemoteHostName = server.RemoteHostName,
                        IPAddress = CommonUtils.GetIPAddressStringByHostName(server.RemoteIP.Trim(), AddressFamily.InterNetwork),
                        ListenerPortNumber = Convert.ToInt32(server.RemotePort),
                        Action = false,
                        IsEdit = false
                    };
                    if (_selectServerIndex == listIndex) srerverListsInfo.Action = true;
                    DataGridServerInfo.Items.Add(srerverListsInfo);
                    listIndex++;
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception:{ex.Message}", Brushes.Red);
            }
        }
        /// <summary>
        /// Creates the file maint data bytes.
        /// option 1:FISC; 2:NCCC  
        /// </summary>
        /// <param name="option">The option.</param>
        /// <returns></returns>
        private byte[] CreateFileMaintDataBytes(int option)
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                FileMaintDataXml fileMaintDataXml;
                string xmlFilePath;
                if (option == 1)
                    xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "fisc-file-maintenance-xml-file-path");
                else
                    xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "nccc-file-maintenance-xml-file-path");
                XmlSerializer serializer = new XmlSerializer(typeof(FileMaintDataXml));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    fileMaintDataXml = (FileMaintDataXml)serializer.Deserialize(reader);
                }
                var fileMaintData = new FileMaintData();
                if (fileMaintDataXml.DataType.ToUpper().Equals("FIXED"))
                {
                    //fileMaintData.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
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
                    if (fileMaintDataXml.TxKeySeq.Changed.ToUpper().Equals("Y"))
                        fileMaintData.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                    else
                        fileMaintData.TxKeySeq = fileMaintDataXml.TxKeySeq?.Text;
                    fileMaintData.IsoP2CardNbr = fileMaintDataXml.IsoP2CardNbr?.Text;
                    fileMaintData.IsoP3ProcessCde = fileMaintDataXml.IsoP3ProcessCde?.Text;
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

                    if (fileMaintDataXml.IsoP14ExpirDte.Changed.ToUpper().Equals("Y"))
                    {
                        fileMaintData.IsoP14ExpirDte = $"{DateTime.Now:yyMM}";
                    }
                    else
                        fileMaintData.IsoP14ExpirDte = fileMaintDataXml.IsoP14ExpirDte?.Text;

                    if (fileMaintDataXml.IsoP37RefNbr.Changed.ToUpper().Equals("Y"))
                    {
                        int refNumber = StaticTraceRnd.Next(1, 999999);
                        fileMaintData.IsoP37RefNbr = $"{refNumber:000000000000}";
                    }
                    else
                        fileMaintData.IsoP37RefNbr = fileMaintDataXml.IsoP37RefNbr?.Text;

                    fileMaintData.IsoP33FwdIICode = fileMaintDataXml.IsoP33FwdIICode?.Text;
                    fileMaintData.IsoP39Rsp = fileMaintDataXml.IsoP39Rsp?.Text;
                    fileMaintData.IsoP48AdditionalData = fileMaintDataXml.IsoP48AdditionalData?.Text;
                    fileMaintData.IsoP49TranCurCode = fileMaintDataXml.IsoP49TranCurCode?.Text;
                    if (fileMaintDataXml.IsoP58T05TxDte.Changed.ToUpper().Equals("Y"))
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
                var fileMaintDataBytes = ColaCommUtility.ObjectToXMLBytes(fileMaintData);
                return (fileMaintDataBytes);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
                return null;
            }
        }

        private void ProcessClientAuthCommand()
        {
            CreateFileMaintDataBytes(2);
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to clientauth client";
            try
            {
                SetMessageText(RichTextBoxSystemMessage, $"Start send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                if (DataGridClientAuthConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, "No Session", Brushes.Red);
                    SetMessageText(RichTextBoxSystemMessage, $"End   send message to  clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, total send : 0", Brushes.Red);
                    return;
                }
                int sendCount = 0;
                TextBoxLogKey.Text = string.Empty;
                for (int index = 0; index < DataGridClientAuthConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridClientAuthConnectionInfo.Items[index];
                    var interchangeMessage = CreateMessage('S');
                    TextBoxLogKey.Text = interchangeMessage.MessageId;
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = item.IPAddress;
                    interchangeMessage.Category = $"{ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}--{ComboBoxSelectCategoryItem.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                    interchangeMessage.CommandCode = ComboBoxSelectCommandCode.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    var tempString = interchangeMessage.Category.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    if (!tempString.ToUpper().Equals("S"))
                    {
                        if (_selectGatewayType.Equals("IAG"))
                        {
                            if (!"VM".Contains(tempString))
                            {
                                SetMessageText(RichTextBoxSystemMessage,
                                    $"GatewayType[{_selectGatewayType}], Select Category[{tempString}] Error", Brushes.Red);
                                ComboBoxSelectCategory.Focus();
                                return;
                            }
                        }
                        else if (!_selectGatewayType.Equals(tempString))
                        {
                            SetMessageText(RichTextBoxSystemMessage, $"GatewayType[{_selectGatewayType}], Select Category[{tempString}]", Brushes.Red);
                            ComboBoxSelectCategory.Focus();
                            return;
                        }
                    }

                    if (interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.NCCCClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.NCPSClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0300) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0302) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.TWINClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.SVCSClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.ICASHClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.IPASSClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.VISAClientCommandCode.FileMaintUpdate) ||
                        interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.MASTERCARDClientCommandCode.FileMaintUpdate))


                    {
                        byte[] fileMaintDataBytes;
                        if (interchangeMessage.CommandCode.Equals(InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0300))
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
                            SetMessageText(RichTextBoxSystemMessage, $"Create file maintenance data error", Brushes.Red);
                            return;
                        }
                    }
                    interchangeMessage.ReturnOption = true;
                    SetMessageText(RichTextBoxSystemMessage, $"--- Start display send InterchangeMessage ---", Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage), Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, $"--- End   display send InterchangeMessage ---", Brushes.Black);
                    Logger.Info($"--- Start display send InterchangeMessage ---");
                    Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                    Logger.Info($"--- End   display send InterchangeMessage ---");
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
                        return;
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

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TextBoxInterval.Text, out var timeIntervalSeconds))
            {
                if (timeIntervalSeconds < 30)
                {
                    TextBoxInterval.Focus();
                    SetMessageText(RichTextBoxSystemMessage, $"Input time interval must be greater than 30Sec", Brushes.Red);
                    return;
                }
                if (_getConnectionDispatcherTimer == null) return;
                _getConnectionDispatcherTimer.Stop();
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, timeIntervalSeconds);
                _getConnectionDispatcherTimer.Start();
                SetMessageText(RichTextBoxSystemMessage, $"Change query time interval {timeIntervalSeconds} Seconds", Brushes.Red);
            }
            else
            {
                TextBoxInterval.Focus();
                SetMessageText(RichTextBoxSystemMessage, $"Input invalid data", Brushes.Red);
            }
        }

        private void BtnConnection_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                int selectCount = 0;
                ServerInfo serverInfo = null;
                for (int index = 0; index < DataGridServerInfo?.Items.Count; index++)
                {
                    var item = (ServerInfo)DataGridServerInfo.Items[index];
                    item.IsEdit = false;
                    if (item.Action)
                    {
                        serverInfo = item;
                        selectCount++;
                    }
                }
                if (selectCount != 1)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Only Select One Server", Brushes.Red);
                    for (int index = 0; index < DataGridServerInfo?.Items.Count; index++)
                    {
                        var item = (ServerInfo)DataGridServerInfo.Items[index];
                        item.IsEdit = true;
                    }
                    DataGridServerInfo.ScrollIntoView(null, DataGridServerInfo.Columns[4]);
                    return;
                }
                DataGridClientAuthConnectionInfo.Items.Clear();
                BtnConnection.IsEnabled = false;
                BtnSend.IsEnabled = true;
                BtnDisconnection.IsEnabled = true;
                RecreateConnection(serverInfo);
                Thread.Sleep(5000);
                if (_tcpClientService.IsConnected())
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Connect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Session connect, Connected to the server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    DisplayClientAuthDataGrid();
                }
                else
                {
                    ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Disconnect.ico", UriKind.RelativeOrAbsolute));
                    SetMessageText(RichTextBoxSystemMessage, $"Session waiting for connect, Not yet connected to the server[{serverInfo?.IPAddress}]:[{serverInfo?.ListenerPortNumber}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    BtnSend.IsEnabled = false;
                }
                _getConnectionDispatcherTimer.Start();
                DataGridServerInfo?.Items.Refresh();
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

        private void BtnDisconnection_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataGridClientAuthConnectionInfo.Items.Clear();
                SetMessageText(RichTextBoxSystemMessage, $"Disconnect to server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                BtnConnection.IsEnabled = true;
                BtnSend.IsEnabled = false;
                BtnDisconnection.IsEnabled = false;
                try
                {
                    _tcpClientService?.Stop();
                }
                catch (Exception ex)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Exception :{ex.Message}", Brushes.Red);
                }
                _tcpClientService = null;
                _getConnectionDispatcherTimer.Stop();
                Thread.Sleep(2000);
                ImageConnectionStatus.Source = new BitmapImage(new Uri("pack://application:,,,/Disconnect.ico", UriKind.RelativeOrAbsolute));
                DataGridClientAuthConnectionInfo.Items.Clear();
                //Clear Connect
                for (int index = 0; index < DataGridServerInfo?.Items.Count; index++)
                {
                    ((ServerInfo)DataGridServerInfo.Items[index]).Action = false;
                    ((ServerInfo)DataGridServerInfo.Items[index]).IsEdit = true;
                }
                DataGridServerInfo?.Items.Refresh();
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

        private void RecreateConnection(ServerInfo server)
        {
            var remoteHostName = server.RemoteHostName.Trim();
            var remoteIPAddressString = CommonUtils.GetIPAddressStringByHostName(server.IPAddress.Trim(), AddressFamily.InterNetwork);
            var remotePortNumberString = Convert.ToString(server.ListenerPortNumber);
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddressString), Convert.ToInt32(remotePortNumberString));

            var localIPAddressString = CommonUtils.GetIPAddressStringByHostName(_tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.LocalIP.Trim(), AddressFamily.InterNetwork);
            var localPortNumberString = _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.LocalPort.Trim();
            var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPAddressString), Convert.ToInt32(localPortNumberString));
            _selectGatewayType = server.GatewayType.ToUpper().Trim();
            try
            {
                _tcpClientService?.Stop();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception :{ex.Message}", Brushes.Red);
            }
            _tcpClientService = null;
            _tcpClientService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
            _tcpClientService.EnableLogger("TCPClientAuthRouterService", Utils.LogLevel.Debug);
            TextBoxConnectPort.Text = remotePortNumberString;
            _tcpClientService.Start();
            //TextBoxInterval.Text = _tcpClientAuthRouterServiceInfo.ClientAuthRouterServiceConfig.Client.ClientRetrievalTimeIntervalSec.Trim();
        }

        private void BtnDataBase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string txSeq;
                if (string.IsNullOrEmpty(TextBoxLogKey.Text.Trim()))
                {
                    SetMessageText(RichTextBoxSystemMessage, "TextBoxLogKey No Data", Brushes.Red);
                    return;
                }
                txSeq = TextBoxLogKey.Text.Trim();
                if (!(CheckBoxReadLog.IsChecked ?? false))
                {
                    SetMessageText(RichTextBoxSystemMessage, "CheckBoxReadLog No Checked", Brushes.Red);
                    CheckBoxReadLog.Focus();
                    return;
                }
                //var subjectString = ComboBoxSelectCommandCode.Text.Trim();
                //string pattern = @"=([\s\S]+)?";
                //var matches = Regex.Matches(subjectString, pattern);
                //if ((matches.Count == 1))
                //{
                //    var infoString = matches[0].Value.Replace("=", "").Replace(")", "");
                //    SetMessageText(RichTextBoxSystemMessage, infoString, Brushes.Red);
                //}
                //else
                //{
                //    SetMessageText(RichTextBoxSystemMessage, "Get Information Code Error", Brushes.Red);
                //}
                //DataClassesCardDataContext dataObject = new DataClassesCardDataContext(_tcpClientAuthRouterServiceInfo.DatabaseInfo.ConnectionString);
                ////DataClassesCardDataContext dataObject = new DataClassesCardDataContext(connectionString);
                //var tranLog = from c in dataObject.COLA_TRAN_LOG
                //                                       where (c.TRAN_LOG_KEY.Equals(txSeq))
                //                                       orderby c.MESSAGE_TYPE ascending
                //                                       select c;
                //foreach (var item in tranLog)
                //{
                //    SetMessageText(RichTextBoxSystemMessage, $"TxSeq[{item.TRAN_LOG_KEY}], MessageType[{item.MESSAGE_TYPE}], RespondCode[{item.P_039}], InformationCode[{item.S_070}]", Brushes.Red);
                //}

                NCARDContext dataObject = new NCARDContext(_tcpClientAuthRouterServiceInfo.DatabaseInfo.ConnectionString);
                var tranLog = from c in dataObject.ColaTranLogs
                              where (c.TranLogKey.Equals(txSeq))
                              orderby c.MessageType ascending
                              select c;
                foreach (var item in tranLog)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"TxSeq[{item.TranLogKey}], MessageType[{item.MessageType}], RespondCode[{item.P039}], InformationCode[{item.S070}]", Brushes.Red);
                }


            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception :{ex.Message}", Brushes.Red);
            }
        }
    }
}

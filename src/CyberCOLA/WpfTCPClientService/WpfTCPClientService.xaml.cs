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
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;

namespace Com.Cybersoft4u.Test.Wpf.WpfTCPClientService
{
    /// <summary>
    /// ServiceMonitorConsole.xaml 的互動邏輯
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    /// <seealso cref="System.Windows.Markup.IComponentConnector" />
    /// <seealso cref="System.Windows.Markup.IStyleConnector" />
    public partial class WpfTCPClientService : Window
    {
        private readonly Logger Logger;

        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private TCPClientServiceInfo _tcpClientServiceInfo;

        private InterchangeMessageCommandInfo _interchangeMessageCommandInfo;

        private TCPClientService _tcpClientService;

        private bool _isStopUDPServer = false;

        private System.Windows.Threading.DispatcherTimer _getConnectionDispatcherTimer;

        private readonly List<string> _gatewayList = new List<string>();

        private readonly List<string> _clientAuthList = new List<string>();

        private static readonly Random StaticTraceRnd = new Random();

        public WpfTCPClientService()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WpfTCPClientService]");
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

        private void InitSystemConfig()
        {
            try
            {
                InitControlAttribute();
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


                var remoteHostName = _tcpClientServiceInfo.TCPClientServiceConfig.RemoteHostName.Trim();
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
                    _tcpClientService.Start();
                StartUDPService(udpListenerPort, udpAutoStartFlag);
                TextBoxConnectPort.Text = remotePortNumberString;
                InitComboBox();
                _getConnectionDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _getConnectionDispatcherTimer.Tick += getClientConnectionDispatcherTimer_Tick;
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(_tcpClientServiceInfo.TCPClientServiceConfig.ClientRetrievalTimeIntervalSec.Trim()));
                DataGridGatewayConnectionInfo.Items.Clear();
                DataGridClientAuthConnectionInfo.Items.Clear();
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
            categoryList.Add($"G--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.Description}");
            categoryList.Add($"C--{_interchangeMessageCommandInfo.CommandItemsInfo.ClientAuth.Description}");
            categoryList.Add($"R--{_interchangeMessageCommandInfo.CommandItemsInfo.Router.Description}");
            ComboBoxSelectCategory.ItemsSource = categoryList.Distinct().ToList();
            ComboBoxSelectCategory.SelectedIndex = 0;
            //ComboBoxSelectCategoryItem.Items.Clear();
            //List<string> categoryItemList = new List<string>();
            //categoryList.Add($"V--{_tcpClientServiceInfo.CommandItemsInfo.Gateway.VISAService.Description}");
            //categoryList.Add($"M--{_tcpClientServiceInfo.CommandItemsInfo.Gateway.MASTERCARDService.Description}");
            //categoryList.Add($"MANR--{_tcpClientServiceInfo.CommandItemsInfo.Gateway.MASTERCARDANRService.Description}");
            //categoryList.Add($"C--{_tcpClientServiceInfo.CommandItemsInfo.Gateway.COLAService.Description}");
            //categoryList.Add($"N--{_tcpClientServiceInfo.CommandItemsInfo.Gateway.NCCCService.Description}");

            //ComboBoxSelectCategoryItem.ItemsSource = categoryList.Distinct().ToList();
            //ComboBoxSelectCategoryItem.SelectedIndex = 0;
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
                    SetMessageText(RichTextBoxSystemMessage,
                        $"UDPServer[{udpListenerPort}] service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
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

        private void ComboBoxSelectCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var categorySelect = ComboBoxSelectCategory.SelectedValue.ToString()
                .Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> categoryList = new List<string>();
            switch (categorySelect[0].ToUpper())
            {
                case "G":
                    categoryList.Add($"{InterChangeMessageCategory.GatewayVISAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.VISAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayMASTERCARDService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.MASTERCARDService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayMASTERCARDANRService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.MASTERCARDANRService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayNCCCCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCCCCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayNCPSCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCPSCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayTWMPCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWMPCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewaySTINCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.STINCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayFISCCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.FISCCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayTWINCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWINCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewaySVCSCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.SVCSCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayICASHCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.ICASHCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayIPASSCOLAService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.IPASSCOLAService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayNCCCService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCCCService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayNCPSService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.NCPSService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayTWMPService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWMPService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewaySTINService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.STINService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayFISCService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.FISCService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayTWINService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.TWINService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewaySVCSService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.SVCSService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayICASHService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.ICASHService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewayIPASSService.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.IPASSService.Description}");
                    categoryList.Add($"{InterChangeMessageCategory.GatewaySystemCommand.Trim().Substring(3)}--{_interchangeMessageCommandInfo.CommandItemsInfo.Gateway.GatewayServiceCommand.Description}");

                    ComboBoxSelectCategoryItem.ItemsSource = categoryList.Distinct().ToList();
                    ComboBoxSelectCategoryItem.SelectedIndex = 0;
                    break;
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

                case "R":
                    categoryList.Add($"S--{_interchangeMessageCommandInfo.CommandItemsInfo.Router.RouterServiceCommand.Description}");

                    ComboBoxSelectCategoryItem.ItemsSource = categoryList.Distinct().ToList();
                    ComboBoxSelectCategoryItem.SelectedIndex = 0;
                    break;

            }
        }

        private void ComboBoxSelectCategoryItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxSelectCategoryItem.Items.Count == 0) return;
            var categorySelect = ComboBoxSelectCategory.SelectedValue.ToString()
                .Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (ComboBoxSelectCategoryItem.SelectedValue == null) return;
            var categoryItemSelect = ComboBoxSelectCategoryItem.SelectedValue.ToString()
                .Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
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
                        case InterChangeMessageCategory.GatewaySystemCommand:
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Gateway.GatewayServiceCommand.Commands.Command;
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
                case "R":
                    switch (categoryItemSelect.ToUpper())
                    {
                        case "S":
                            actionsList = _interchangeMessageCommandInfo.CommandItemsInfo.Router.RouterServiceCommand.Commands.Command;
                            break;
                    }
                    break;
            }
            ComboBoxSelectCommandCode.ItemsSource = actionsList;
            ComboBoxSelectCommandCode.SelectedIndex = 0;
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
            var udpAutoStartFlag =
                Convert.ToBoolean(_tcpClientServiceInfo.TCPClientServiceConfig.UDPAutoStartFlag.Trim());
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

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var category = ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
            SetMessageText(RichTextBoxSystemMessage, $"Select Category [{ComboBoxSelectCategory.Text.Trim()}]--[{category}]", Brushes.Red);
            switch (category.ToUpper())
            {
                case "G":
                    ProcessGatewayCommand();
                    break;
                case "C":
                    ProcessClientAuthCommand();
                    break;
                case "R":
                    ProcessRouterCommand();
                    break;
                default:
                    SetMessageText(RichTextBoxSystemMessage, $"Invalid Category{category}", Brushes.Red);
                    break;
            }
        }

        private void RefreshGatewayDataGrid()
        {
            try
            {
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                SetMessageText(RichTextBoxSystemMessage, $"Retrieval gateway connection status at : {dateTimeNow}",
                    Brushes.Red);
                GroupBoxGateway.Header = $"Gateway connection at : {dateTimeNow}";
                DataGridGatewayConnectionInfo.Items.Clear();
                foreach (var item in _gatewayList.ToArray())
                {
                    var connectionInfoData = new ConnectionInfo
                    {
                        ClientType = "Gateway",
                        IPAddress = item,
                        Status = "Connection",
                        Action = false
                    };
                    DataGridGatewayConnectionInfo.Items.Add(connectionInfoData);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception:{ex.Message}", Brushes.Red);
            }
        }

        private void CreateGatewayClineAuthList(string interchangeData)
        {
            _gatewayList.Clear();
            _clientAuthList.Clear();
            if (string.IsNullOrEmpty(interchangeData)) return;
            var dataElement = interchangeData.Trim().Split(new string[] { "!" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var data in dataElement)
            {
                var fieldElement = data.Trim().Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);
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
            RefreshGatewayDataGrid();
            RefreshClientAuthDataGrid();
        }

        private void RefreshClientAuthDataGrid()
        {
            try
            {
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                SetMessageText(RichTextBoxSystemMessage, $"Retrieval clientauth connection status at : {dateTimeNow}",
                    Brushes.Red);
                GroupBoxClientAuth.Header = $"ClientAuth connect at : {dateTimeNow}";
                DataGridClientAuthConnectionInfo.Items.Clear();
                {
                    foreach (var item in _clientAuthList.ToArray())
                    {
                        var connectionInfoData = new ConnectionInfo
                        {
                            ClientType = "ClientAuth",
                            IPAddress = item,
                            Status = "Connection",
                            Action = false
                        };


                        DataGridClientAuthConnectionInfo.Items.Add(connectionInfoData);
                    }
                }
            }
            catch (Exception ex2)
            {
                SetMessageText(RichTextBoxSystemMessage, $"Exception : {ex2.Message}", Brushes.Red);
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

        private void ProcessGatewayCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to gateway client";
            try
            {
                SetMessageText(RichTextBoxSystemMessage, $"Start send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                if (DataGridGatewayConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"End   Send message to gateway client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Total Send : 0", Brushes.Red);
                    return;
                }
                int sendCount = 0;
                for (int index = 0; index < DataGridGatewayConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridGatewayConnectionInfo.Items[index];
                    if (!item.ClientType.ToUpper().Equals("GATEWAY")) continue;
                    if (!item.Action) continue;
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = item.IPAddress;
                    interchangeMessage.Category = $"{ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}--{ComboBoxSelectCategoryItem.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                    interchangeMessage.CommandCode = ComboBoxSelectCommandCode.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    //This port number is only used by GW and assigns GW local port number to handle this function, if value is zero GW all local ports handle this function
                    if (int.TryParse(TextBoxGWPortNumber.Text.Trim(), out var gwLocalPort))
                    {
                        interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                        interchangeMessage.InterchangeData = Encoding.ASCII.GetBytes(Convert.ToString(gwLocalPort));
                    }
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

        private void ProcessClientAuthCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to clientauth client";
            try
            {
                SetMessageText(RichTextBoxSystemMessage, $"Start send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                if (DataGridClientAuthConnectionInfo?.Items.Count == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"End   send message to  clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, total send : 0", Brushes.Red);
                    return;
                }
                int sendCount = 0;
                for (int index = 0; index < DataGridClientAuthConnectionInfo?.Items.Count; index++)
                {
                    var item = (ConnectionInfo)DataGridClientAuthConnectionInfo.Items[index];
                    if (!item.ClientType.ToUpper().Equals("CLIENTAUTH")) continue;
                    if (!item.Action) continue;
                    var interchangeMessage = CreateMessage('S');
                    interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                    interchangeMessage.DestinationEndPoint = item.IPAddress;
                    interchangeMessage.Category = $"{ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}--{ComboBoxSelectCategoryItem.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                    interchangeMessage.CommandCode = ComboBoxSelectCommandCode.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
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
                    SetMessageText(RichTextBoxSystemMessage, $"--- Start display send InterchangeMessage ---",
                        Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage),
                        Brushes.Black);
                    SetMessageText(RichTextBoxSystemMessage, $"--- End   display send InterchangeMessage ---",
                        Brushes.Black);
                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                    WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint,
                        sendBytes);
                    sendCount++;
                    SetMessageText(RichTextBoxSystemMessage,
                                   $"End   send message to clientauth client at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Total Send : {sendCount}",
                                   Brushes.Red);
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

        private void ProcessRouterCommand()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Send message to routing service";
            try
            {
                _gatewayList.Clear();
                _clientAuthList.Clear();
                DataGridGatewayConnectionInfo.Items.Clear();
                DataGridClientAuthConnectionInfo.Items.Clear();
                if (!_tcpClientService.IsConnected())
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Not connected to server", Brushes.Red);
                    RefreshGatewayDataGrid();
                    RefreshClientAuthDataGrid();
                    return;
                }
                var interchangeMessage = CreateMessage('S');
                interchangeMessage.SourceEndPoint = _tcpClientService.LocalEndPoint().ToString();
                interchangeMessage.DestinationEndPoint = _tcpClientService.RemoteEndPoint().ToString();
                interchangeMessage.Category = $"{ComboBoxSelectCategory.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}--{ComboBoxSelectCategoryItem.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                interchangeMessage.CommandCode = ComboBoxSelectCommandCode.Text.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                interchangeMessage.ReturnOption = true;
                SetMessageText(RichTextBoxSystemMessage, $"--- Start display send InterchangeMessage ---", Brushes.Black);
                SetMessageText(RichTextBoxSystemMessage, ColaCommUtility.GetObjectProperty(interchangeMessage), Brushes.Black);
                SetMessageText(RichTextBoxSystemMessage, $"--- End   display send InterchangeMessage ---", Brushes.Black);
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                WriteSendMessageToLog(interchangeMessage.SourceEndPoint, interchangeMessage.DestinationEndPoint, sendBytes);
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

        private void BtnInformation_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                _gatewayList.Clear();
                _clientAuthList.Clear();
                DataGridGatewayConnectionInfo.Items.Clear();
                DataGridClientAuthConnectionInfo.Items.Clear();
                SetMessageText(RichTextBoxSystemMessage, $"Send R--S Command for get channel status", Brushes.Red);
                if (!_tcpClientService.IsConnected())
                {
                    SetMessageText(RichTextBoxSystemMessage, $"Not connected to server", Brushes.Red);
                    RefreshGatewayDataGrid();
                    RefreshClientAuthDataGrid();
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

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Int32.TryParse(TextBoxInterval.Text, out var timeIntervalSeconds))
            {
                if (_getConnectionDispatcherTimer == null) return;
                _getConnectionDispatcherTimer.Stop();
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, timeIntervalSeconds);
                _getConnectionDispatcherTimer.Start();
                SetMessageText(RichTextBoxSystemMessage, $"Change query interval {timeIntervalSeconds} Seconds",
                    Brushes.Red);
            }
            else
            {
                TextBoxInterval.Focus();
                SetMessageText(RichTextBoxSystemMessage, $"Input invalid data", Brushes.Red);
            }
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

        private void GetServiceRouterLog(string messageIdString)
        {
            try
            {
                ServiceRouterLogInfoStruct serviceRouterLogInfo = new ServiceRouterLogInfoStruct();
                var retInt = CommonLib.DBUtils.StoreProcUtility.SelectServiceRouterLog(messageIdString, ref serviceRouterLogInfo);
                if (retInt == 0)
                {
                    SetMessageText(RichTextBoxSystemMessage, $"GetServiceRouterLog get data [{serviceRouterLogInfo.InterchangeData}]", Brushes.Red);
                }
                else
                {
                    SetMessageText(RichTextBoxSystemMessage, $"GetServiceRouterLog get data invalid[{retInt}]", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxSystemMessage, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }

        }
    }
}

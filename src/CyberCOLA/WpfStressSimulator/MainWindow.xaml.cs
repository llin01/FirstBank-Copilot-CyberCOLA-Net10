using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Experimental.System.Messaging;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using Logger = NLog.Logger;
using RichTextBox = System.Windows.Controls.RichTextBox;


namespace Com.Cybersoft4u.Simulator.Wpf.WpfStressSimulator
{
    /// <summary>
    /// MainWindow.xaml的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);
        private System.Windows.Threading.DispatcherTimer? _getConnectionDispatcherTimer;
        private bool _isReplaceCardNumber;
        private bool _isReplaceOriginalData;
        private bool _isAutoEcho;
        private string? _initialDirectory;
        private string? _serverName;
        private bool _isWaitingSignOn = false;

        private int _listenPortNumber;
        private int _sessionCount;
        private int _sendInterval = 1000;
        private string? _cardXmlFilePath;
        private string? _prefixQueueName;
        private string[]? _queueNameArray;
        private List<string>? _maintRemotePortList;
        private readonly List<byte[]> _templateRawDataList = new List<byte[]>();
        private MessageQueue[]? _messageQueueArrays;
        private bool _selectType;
        private int _udpPortNumber;
        private ServerService? _serverService;
        private bool _isStopUDPServer;
        private readonly string? _startListenerPortNumber;
        private readonly string? _autoStartListenService;
        private string? _initialTitle;
        private int _sendMessageRepeatCount = 1;
        private long _lastSuccessCount;
        public MainWindow()
        {
            try
            {
                _startListenerPortNumber = (Application.Current as App)?.StartListenerPortNumber;
                _autoStartListenService = (Application.Current as App)?.AutoStartListenService;
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += DispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
        }
        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                string tranType = string.Empty;
                int listenPort = 0;
                bool isServiceRunning = false;
                long successCount = 0;
                long errorCount = 0;
                int activeConnections = 0;
                int tps = 0;

                if (ComboBoxTransactionType != null && ComboBoxListenerPort != null && ButtonStop != null)
                {
                    tranType = ComboBoxTransactionType.Text.Trim().ToUpper();
                    int.TryParse(ComboBoxListenerPort.Text.Trim(), out listenPort);
                    isServiceRunning = ButtonStop.IsEnabled; // ButtonStop.IsEnabled is true only when the TCP simulator service is running
                }

                if (isServiceRunning)
                {
                    successCount = ServerService.Metrics.SuccessCount;
                    errorCount = ServerService.Metrics.ErrorCount;
                    activeConnections = ServerService.Metrics.ActiveConnections;

                    long diff = successCount - _lastSuccessCount;
                    _lastSuccessCount = successCount;
                    tps = (int)diff; // Since timer fires every 1 second, diff is directly TPS
                }
                else
                {
                    _lastSuccessCount = 0;
                }

                string summaryPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OperationSummary.json");
                string json = $"{{\n" +
                              $"  \"TransactionType\": \"{tranType}\",\n" +
                              $"  \"ListenPort\": \"{listenPort}\",\n" +
                              $"  \"ActiveConnections\": {activeConnections},\n" +
                              $"  \"SuccessCount\": {successCount},\n" +
                              $"  \"ErrorCount\": {errorCount},\n" +
                              $"  \"Tps\": {tps},\n" +
                              $"  \"IsServiceRunning\": {(isServiceRunning ? "true" : "false")},\n" +
                              $"  \"LastUpdated\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\"\n" +
                              $"}}";
                System.IO.File.WriteAllText(summaryPath, json);
            }
            catch
            {
                // Suppress exception to avoid UI crashing
            }

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void ButtonCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Clear Message Content";
            RichTextBox1.Document.Blocks.Clear();
            RichTextBox2.Document.Blocks.Clear();
        }

        private void InitialSystemParameter()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _isReplaceCardNumber = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "IsReplaceCardNumber").Trim());
                _isReplaceOriginalData = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "IsReplaceOriginalData") ?? "true");
                _isAutoEcho = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "IsAutoEcho") ?? "true");
                _initialDirectory = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory");

                var listenPortList = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ListenPortList")
                                          .Trim()
                                          .Split(',');
                TextBoxRepeatCount.Text = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialRepeatCount").Trim();
                _maintRemotePortList = new List<string>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "MaintRemotePortList")
                                                        .Trim()
                                                        .Split(','));

                ComboBoxListenerPort.Items.Clear();
                ComboBoxListenerPort.ItemsSource = listenPortList;
                ComboBoxListenerPort.SelectedIndex = 0;
                if (_startListenerPortNumber != null)
                    ComboBoxListenerPort.SelectedValue = _startListenerPortNumber;
                _serverName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ServerName");
                _isWaitingSignOn = Convert.ToBoolean(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "IsWaitingSignOn").Trim());
                _prefixQueueName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "PrefixQueueName").Trim();
                TextBoxSendIntervalMilliseconds.Text = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SendIntervalInMilliseconds").Trim();
                _sessionCount = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SessionCount").Trim());
                _sendMessageRepeatCount = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SendMessageRepeatCount").Trim());
                _queueNameArray = new string[_sessionCount];
                _messageQueueArrays = new MessageQueue[_sessionCount];
                for (int index = 0; index < _sessionCount; index++)
                {
                    _queueNameArray[index] = $"{_prefixQueueName}_{index:000}";
                    if (MessageQueue.Exists(_queueNameArray[index]))
                    {
                        _messageQueueArrays[index] = new MessageQueue(_queueNameArray[index])
                        {
                            Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) })
                        };
                        _messageQueueArrays[index].Purge();
                    }
                    else
                    {
                        _messageQueueArrays[index] = MessageQueue.Create(_queueNameArray[index]);
                        _messageQueueArrays[index].Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
                    }
                    SetMessageText(RichTextBox1, $"{index + 1:000}. Queue Name[{_queueNameArray[index]}]", Brushes.Black);
                }
                var transactionType = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TransactionType").Trim();
                var item = (ComboBoxItem)ComboBoxTransactionType.FindName(transactionType);
                if (item != null)
                {
                    int index = ComboBoxTransactionType.Items.IndexOf(item);
                    ComboBoxTransactionType.SelectedIndex = index;
                    _selectType = true;
                }
                Title = $"{transactionType.Substring(4, transactionType.Length - 4)} {Title}";
                // If it is NCCC QRCode, update the Title to distinguish it from the normal NCCC Simulator
                if (transactionType.Substring(4, transactionType.Length - 4).ToUpper() == "NCCC" && _prefixQueueName != null && _prefixQueueName.Contains("QRCode", StringComparison.OrdinalIgnoreCase))
                {
                    Title = $"NcccQRCode {Title.Substring(5)}"; // Replace "Nccc " prefix with "NcccQRCode "
                }
                // If it is FISC ID Verify, update the Title to distinguish it from the normal FISC Simulator
                if (transactionType.Substring(4, transactionType.Length - 4).ToUpper() == "FISC" && _prefixQueueName != null && _prefixQueueName.Contains("IDVerify", StringComparison.OrdinalIgnoreCase))
                {
                    Title = $"FiscIDVerify {Title.Substring(5)}"; // Replace "Fisc " prefix with "FiscIDVerify "
                }
                Title = $"{Title}(IsWaitingSignOn[{_isWaitingSignOn}], MaintRemotePortList[{configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "MaintRemotePortList").Trim()}])";
                _initialTitle = Title;
                switch (transactionType.Substring(4, transactionType.Length - 4).ToUpper())
                {
                    case "FISC":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Fisc.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "IAG":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/CreditCard.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "MASTERCARD":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/MasterCard.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "NCCC":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Nccc.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "NCPS":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Ncps.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "STIN":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/CreditCard.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "TWIN":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Twin.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "TWMP":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Twmp.ico", UriKind.RelativeOrAbsolute));
                        break;
                    case "VISA":
                        Icon = new BitmapImage(new Uri("pack://application:,,,/Visa.ico", UriKind.RelativeOrAbsolute));
                        break;
                    default:
                        Icon = new BitmapImage(new Uri("pack://application:,,,/TCPIP.ico", UriKind.RelativeOrAbsolute));
                        break;
                }
                _getConnectionDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _getConnectionDispatcherTimer.Tick += GetQueueDispatcherTimer_Tick;
                _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, Convert.ToInt32(TextBoxInterval.Text.Trim()));
                _getConnectionDispatcherTimer.Stop();
                TextBoxInterval.Text = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "RetrievalTimeIntervalSec");
                _udpPortNumber = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPPortNumber").Trim());
                SetBtnAttribute();
                ButtonInitialQueueData.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                if (!(string.IsNullOrEmpty(_autoStartListenService)) &&
                    _autoStartListenService.Trim().Equals("AUTO"))
                    ButtonStart.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                SetBtnAttribute(3);
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
                TextRange rangeOfText1 = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                rangeOfText1.Text = message + '\r';
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                rtb.ScrollToEnd();
            } //if
            else
            {
                rtb.Dispatcher.BeginInvoke(new UpdateRichTextBox(SetMessageText), richTextBox, message, brush);
            }
        }

        private void SetBtnAttribute(int option = 0)
        {
            if (option == 0)
            {
                ButtonStart.IsEnabled = false;
                ButtonStop.IsEnabled = false;
                ButtonReset.IsEnabled = true;
                ButtonInitialQueueData.IsEnabled = true;
                //ButtonReCreateQueue.IsEnabled = false;
                ButtonCleanMessage.IsEnabled = true;
                ButtonClosed.IsEnabled = true;
                ButtonRetrievalQueue.IsEnabled = false;
                ButtonRetrievalQueue.Content = "Start Retrieval Queue";
                ButtonRetrievalQueue.Background = new SolidColorBrush(Colors.Red);
                ComboBoxTransactionType.IsEnabled = !_selectType;
                TextBoxSendIntervalMilliseconds.IsEnabled = ButtonStart.IsEnabled;
            }
            else if (option == 1)
            {
                ButtonStart.IsEnabled = true;
                ButtonStop.IsEnabled = false;
                ButtonReset.IsEnabled = true;
                ButtonInitialQueueData.IsEnabled = false;
                //ButtonReCreateQueue.IsEnabled = true;
                ButtonCleanMessage.IsEnabled = true;
                ButtonClosed.IsEnabled = true;
                ComboBoxTransactionType.IsEnabled = false;
                TextBoxSendIntervalMilliseconds.IsEnabled = ButtonStart.IsEnabled;
            }
            else if (option == 2)
            {
                ButtonStart.IsEnabled = false;
                ButtonStop.IsEnabled = true;
                ButtonReset.IsEnabled = true;
                ButtonInitialQueueData.IsEnabled = false;
                //ButtonReCreateQueue.IsEnabled = true;
                ButtonCleanMessage.IsEnabled = true;
                ButtonClosed.IsEnabled = true;
                ComboBoxTransactionType.IsEnabled = false;
                TextBoxSendIntervalMilliseconds.IsEnabled = ButtonStart.IsEnabled;
            }
            else if (option == 3)
            {
                ButtonStart.IsEnabled = false;
                ButtonStop.IsEnabled = false;
                ButtonReset.IsEnabled = false;
                ButtonInitialQueueData.IsEnabled = false;
                //ButtonReCreateQueue.IsEnabled = false;
                ButtonCleanMessage.IsEnabled = false;
                ButtonClosed.IsEnabled = true;
                ComboBoxTransactionType.IsEnabled = false;
                TextBoxSendIntervalMilliseconds.IsEnabled = ButtonStart.IsEnabled;
            }
        }

        private int GetMessageCount(MessageQueue messageQueue)
        {
            int messageCount = 0;
            using (MessageEnumerator messageEnumerator = messageQueue.GetMessageEnumerator2())
            {
                while (messageEnumerator.MoveNext())
                {
                    messageCount++;
                }
            }
            return messageCount;
        }

        private void ButtonClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            _getConnectionDispatcherTimer?.Stop();
            Close();
            Environment.Exit(0);
        }

        private void ButtonInitialQueueData_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Initial Queue Data";
            try
            {
                string tempWorkDirectory;
                if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("FISC"))
                    tempWorkDirectory = $"{_initialDirectory}\\FISC";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("IAG"))
                    tempWorkDirectory = $"{_initialDirectory}\\IAG";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("MASTERCARD"))
                    tempWorkDirectory = $"{_initialDirectory}\\MasterCard";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("NCCC"))
                    tempWorkDirectory = $"{_initialDirectory}\\Nccc";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("NCPS"))
                    tempWorkDirectory = $"{_initialDirectory}\\Ncps";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("STIN"))
                    tempWorkDirectory = $"{_initialDirectory}\\STIN";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("TWIN"))
                    tempWorkDirectory = $"{_initialDirectory}\\Twin";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("TWMP"))
                    tempWorkDirectory = $"{_initialDirectory}\\Twmp";
                else
                    tempWorkDirectory = $"{_initialDirectory}\\Visa";
                SetMessageText(RichTextBox2, $"Template Work Directory[{tempWorkDirectory}]", Brushes.Black);
                SetMessageText(RichTextBox2, $"Create Queue Data in Work Directory[{tempWorkDirectory}]", Brushes.Black);
                _templateRawDataList.Clear();
                //bool result = int.TryParse(TextBoxRepeatCount.Text.Trim(), out int repeatCount);
                //if (!result)
                //{
                //    SetMessageText(RichTextBox2, $"Repeat Count Invalid {TextBoxRepeatCount.Text.Trim()}", Brushes.Red);
                //    TextBoxRepeatCount.Focus();
                //    return;
                //}
                //string[] fileEntries = Directory.GetFiles(tempWorkDirectory);
                //List<string> returnData = null;
                //Mouse.OverrideCursor = null;
                //SelectFileWindow selectFileWindow = new SelectFileWindow(fileEntries);
                //if (!selectFileWindow.ShowDialog() == true)
                //    returnData = selectFileWindow.GetFilePaths();
                //Mouse.OverrideCursor = Cursors.Wait;
                //if ((returnData == null) || (returnData.Count <= 0))
                //{
                //    SetMessageText(RichTextBox1, "No Select File", Brushes.Red);
                //    return;
                //}
                //for (int indexCount = 1; indexCount <= repeatCount; indexCount++)
                //{
                //    foreach (string fileName in returnData)
                //    {
                //        byte[] bytesData = File.ReadAllBytes(fileName);
                //        _templateRawDataList.Add(bytesData);
                //    }
                //}
                //if (WriteBytesDataToQueue() != 0)
                //{
                //    SetMessageText(RichTextBox2, "Write Data Queue Error", Brushes.Red);
                //    return;
                //}
                SetBtnAttribute(1);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                SetBtnAttribute(3);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // <summary>
        /// Writes the bytes data to queue.
        ///
        /// <returns>System.Int32.</returns>
        private int WriteBytesDataToQueue()
        {
            try
            {
                SetMessageText(RichTextBox1, $"Queue write data[{_templateRawDataList.Count}]", Brushes.Red);
                if (_messageQueueArrays == null)
                {
                    SetMessageText(RichTextBox2, "Message queue array is not initialized.", Brushes.Red);
                    return 1;
                }
                foreach (MessageQueue messageQueue in _messageQueueArrays)
                {
                    int count = 0;
                    foreach (byte[] bytesArray in _templateRawDataList)
                    {
                        var sendData = new Experimental.System.Messaging.Message(bytesArray, new XmlMessageFormatter(new Type[] { typeof(byte[]) }));
                        sendData.Label = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}_{++count:000000}";
                        messageQueue.Send(sendData);
                    }
                    SetMessageText(RichTextBox1, $"Message Queue Path[{messageQueue.Path}], Write Count[{count}]", Brushes.Black);
                }
                SetMessageText(RichTextBox1, "Write Data To Queue Completed", Brushes.Black);
                return (0);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                return (1);
            }
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start Service";
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var tranType = ComboBoxTransactionType.Text.Trim().ToUpper();
                var cardXmlPrefixFileDirectory = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "CardXmlPrefixFileDirectory").Trim();

                if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("FISC"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\FISC\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("IAG"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\IAG\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("MASTERCARD"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\MasterCard\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("NCCC"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\Nccc\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("NCPS"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\Ncps\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("TWMP"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\Twmp\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("TWIN"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\Twin\\CardXmlFilePath.xml";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("STIN"))
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\STIN\\CardXmlFilePath.xml";
                else
                    _cardXmlFilePath = $"{cardXmlPrefixFileDirectory}\\Visa\\CardXmlFilePath.xml";

                bool result = int.TryParse(ComboBoxListenerPort.Text.Trim(), out _listenPortNumber);
                if (!result)
                {
                    SetMessageText(RichTextBox2, $"Port Number Invalid {ComboBoxListenerPort.Text.Trim()}", Brushes.Red);
                    ComboBoxListenerPort.Focus();
                    return;
                }
                result = int.TryParse(TextBoxSendIntervalMilliseconds.Text.Trim(), out _sendInterval);
                if (!result)
                {
                    SetMessageText(RichTextBox2, $"Send Interval Invalid {TextBoxSendIntervalMilliseconds.Text.Trim()}", Brushes.Red);
                    TextBoxSendIntervalMilliseconds.Focus();
                    return;
                }
                _serverService = null;
                _serverService = new ServerService(tranType,
                                                   CommonUtils.GetIPAddressStringByHostName(_serverName, AddressFamily.InterNetwork),
                                                   _listenPortNumber,
                                                   _messageQueueArrays,
                                                   _sendInterval,
                                                   _cardXmlFilePath,
                                                   _isWaitingSignOn,
                                                   _maintRemotePortList,
                                                   _isReplaceCardNumber,
                                                   _isReplaceOriginalData,
                                                   _sendMessageRepeatCount,
                                                   _isAutoEcho);
                _serverService.StartService();
                SetMessageText(RichTextBox2, $"Server Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Listener Port[{ComboBoxListenerPort.Text.Trim()}]", Brushes.Red);
                SetBtnAttribute(2);
                Thread tcpListener = new Thread(ReadLogMessage);
                tcpListener.Start();
                SetMessageText(RichTextBox2, $"UDP Service[{_udpPortNumber}] start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Listener Port[{_udpPortNumber}]", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                SetBtnAttribute(3);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Reset Function";
            try
            {
                _serverService?.StopService();
                if (_messageQueueArrays == null || _queueNameArray == null)
                {
                    SetMessageText(RichTextBox2, "Message queue array is not initialized.", Brushes.Red);
                    SetBtnAttribute(3);
                    return;
                }
                for (int index = 0; index < _messageQueueArrays.Length; index++)
                {
                    _queueNameArray[index] = $"{_prefixQueueName}_{index:000}";
                    if (MessageQueue.Exists(_queueNameArray[index]))
                    {
                        _messageQueueArrays[index] = new MessageQueue(_queueNameArray[index])
                        {
                            Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) })
                        };
                        _messageQueueArrays[index].Purge();
                    }
                    else
                    {
                        _messageQueueArrays[index] = MessageQueue.Create(_queueNameArray[index]);
                        _messageQueueArrays[index].Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
                    }
                    SetMessageText(RichTextBox2, $"Queue Name[{_queueNameArray[index]}] Reset", Brushes.Black);
                }
                SetMessageText(RichTextBox2, $"Server Service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Listener Port[{ComboBoxListenerPort.Text.Trim()}]", Brushes.Red);
                SetBtnAttribute();
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                SetBtnAttribute(3);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Stop Service";
            StopUDPService();
            _isStopUDPServer = true;
            _serverService?.StopService();
            SetMessageText(RichTextBox2, $"UDP Service[{_udpPortNumber}] stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            SetBtnAttribute(1);
        }

        private void ButtonReCreateQueue_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Re-Create Queue Data";
            _templateRawDataList.Clear();
            try
            {
                string tempWorkDirectory;

                if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("FISC"))
                    tempWorkDirectory = $"{_initialDirectory}\\FISC";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("IAG"))
                    tempWorkDirectory = $"{_initialDirectory}\\IAG";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("MASTERCARD"))
                    tempWorkDirectory = $"{_initialDirectory}\\MasterCard";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("NCCC"))
                    tempWorkDirectory = $"{_initialDirectory}\\Nccc";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("NCPS"))
                    tempWorkDirectory = $"{_initialDirectory}\\Ncps";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("TWIN"))
                    tempWorkDirectory = $"{_initialDirectory}\\Twin";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("TWMP"))
                    tempWorkDirectory = $"{_initialDirectory}\\Twmp";
                else if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("STIN"))
                    tempWorkDirectory = $"{_initialDirectory}\\STIN";
                else
                    tempWorkDirectory = $"{_initialDirectory}\\Visa";
                SetMessageText(RichTextBox2, $"Template Work Directory[{tempWorkDirectory}]", Brushes.Black);
                bool result = int.TryParse(TextBoxRepeatCount.Text.Trim(), out int repeatCount);
                if (!result)
                {
                    SetMessageText(RichTextBox2, $"Repeat Count Invalid {TextBoxRepeatCount.Text.Trim()}", Brushes.Red);
                    TextBoxRepeatCount.Focus();
                    return;
                }

                if (repeatCount == 0)
                {
                    SetMessageText(RichTextBox2, $"Repeat Count Data Is Zero", Brushes.Red);
                    TextBoxRepeatCount.Focus();
                    return;
                }
                string[] fileEntries = Directory.GetFiles(tempWorkDirectory, "*.*", SearchOption.AllDirectories);
                List<string>? returnData = null;
                Mouse.OverrideCursor = null;
                SelectFileWindow selectFileWindow = new SelectFileWindow(fileEntries);
                if (!selectFileWindow.ShowDialog() == true)
                    returnData = selectFileWindow.GetFilePaths();
                Mouse.OverrideCursor = Cursors.Wait;
                if ((returnData == null) || (returnData.Count <= 0))
                {
                    SetMessageText(RichTextBox1, "No Select File", Brushes.Red);
                    return;
                }
                SetMessageText(RichTextBox2, $"Select File[{returnData.Count}], Repeat Count[{repeatCount}]", Brushes.Red);
                var rnd = new Random();
                for (int indexCount = 1; indexCount <= repeatCount; indexCount++)
                {
                    var randomNumbers = Enumerable.Range(1, returnData.Count).OrderBy(x => rnd.Next()).Take(returnData.Count).ToList();
                    foreach (var index in randomNumbers)
                    {
                        var fileName = returnData[index - 1];
                        byte[] bytesData;
                        if (ComboBoxTransactionType.Text.Trim().ToUpper().Equals("FISC"))
                        {
                            var tempReadBytes = File.ReadAllBytes(fileName);
                            bytesData = new byte[tempReadBytes.Length - 2];
                            Buffer.BlockCopy(tempReadBytes, 2, bytesData, 0, bytesData.Length);
                        }
                        else
                            bytesData = File.ReadAllBytes(fileName);
                        //skip fisc layout 2-byte length
                        _templateRawDataList.Add(bytesData);
                    }
                }
                if (WriteBytesDataToQueue() != 0)
                    SetMessageText(RichTextBox2, "Write Data Queue Error", Brushes.Red);
                //SetBtnAttribute(1);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ButtonStart_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ButtonReCreateQueue.IsEnabled = !ButtonStart.IsEnabled;
            TextBoxSendIntervalMilliseconds.IsEnabled = ButtonStart.IsEnabled;
            ComboBoxListenerPort.IsEnabled = ButtonStart.IsEnabled;
        }
        private void ButtonGetQueueCount_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Get Queue Count";
            try
            {
                if (_messageQueueArrays != null)
                {
                    foreach (MessageQueue messageQueue in _messageQueueArrays)
                    {
                        int getQueueCount = GetMessageCount(messageQueue);
                        SetMessageText(RichTextBox2, $"Message Queue Path[{messageQueue.Path}], Queue Count[{getQueueCount}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Black);
                    }
                }
                else
                {
                    SetMessageText(RichTextBox2, "Message queue array is not initialized.", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void StopUDPService()
        {
            UdpClient udpClient = new UdpClient();
            string ipStr = CommonUtils.GetIPAddressStringByHostName(_serverName, AddressFamily.InterNetwork);
            if (ipStr == "0.0.0.0") ipStr = "127.0.0.1";
            IPAddress ipAddress = IPAddress.Parse(ipStr);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, _udpPortNumber);
            Byte[] sendBytes = Encoding.ASCII.GetBytes($"Stop UDP Service[{_udpPortNumber}]");
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length, ipEndPoint);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBox2, $"UDP Service[{_udpPortNumber}] stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Listener Port[{_udpPortNumber}]", Brushes.Red);
            }
        }
        private void ButtonInitialQueueData_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ButtonGetQueueCount.IsEnabled = !ButtonInitialQueueData.IsEnabled;
            ButtonRetrievalQueue.IsEnabled = !ButtonInitialQueueData.IsEnabled;
            if (!ButtonRetrievalQueue.IsEnabled)
            {
                ButtonRetrievalQueue.Content = "Start Retrieval Queue";
                ButtonRetrievalQueue.Background = new SolidColorBrush(Colors.Red);
                _getConnectionDispatcherTimer?.Stop();
            }
        }
        private void ReadLogMessage()
        {
            UdpClient udpClient = new UdpClient(_udpPortNumber);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, _udpPortNumber);
            try
            {
                while (!_isStopUDPServer)
                {
                    if (udpClient.Available <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    byte[] bytes = udpClient.Receive(ref groupEP);
                    SetMessageText(RichTextBox1, $"{Encoding.Default.GetString(bytes, 0, bytes.Length)}", Brushes.Black);
                }

            }
            catch (Exception e)
            {
                SetMessageText(RichTextBox1, e.Message, Brushes.Red);
            }
            finally
            {
                udpClient.Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        private class QueueInfo
        {
            public string? QueueName { get; set; }
            public int QueueCount { get; set; }
        }
        private void GetQueueDispatcherTimer_Tick(object sender, EventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                string dateTimeNow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                SetMessageText(RichTextBox2, $"Retrieval Queue Information Status at : {dateTimeNow}", Brushes.Red);
                DataGridRetrievalQueue.Items.Clear();
                DataGridRetrievalQueue.Items.Refresh();
                if (_messageQueueArrays != null)
                {
                    foreach (MessageQueue messageQueue in _messageQueueArrays)
                    {
                        int getQueueCount = GetMessageCount(messageQueue);
                        var queueInfoData = new QueueInfo
                        {
                            QueueName = messageQueue.Path.Replace(@".\Private$\", ""),
                            QueueCount = getQueueCount
                        };
                        DataGridRetrievalQueue.Items.Add(queueInfoData);
                    }
                }
                else
                {
                    SetMessageText(RichTextBox2, "Message queue array is not initialized.", Brushes.Red);
                }
                Message3.Header = $"Retrieval Queue Information at : {dateTimeNow}";
                Title = $"{_initialTitle} Retrieval at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ButtonRetrievalQueue_Click(object sender, RoutedEventArgs e)
        {
            if (!_getConnectionDispatcherTimer.IsEnabled)
            {
                if (Int32.TryParse(TextBoxInterval.Text, out int timeIntervalSeconds))
                {
                    if (timeIntervalSeconds < 30)
                    {
                        TextBoxInterval.Focus();
                        SetMessageText(RichTextBox2, $"Input time interval must be greater than 30Sec", Brushes.Red);
                        return;
                    }
                    _getConnectionDispatcherTimer.Stop();
                    _getConnectionDispatcherTimer.Interval = new TimeSpan(0, 0, 0, timeIntervalSeconds);
                    _getConnectionDispatcherTimer.Start();
                    SetMessageText(RichTextBox2, $"Start Retrieval Connection Status at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    ButtonRetrievalQueue.Content = "Stop Retrieval Queue";
                    ButtonRetrievalQueue.Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    TextBoxInterval.Focus();
                    SetMessageText(RichTextBox2, $"Input invalid data", Brushes.Red);
                }
            }
            else
            {
                _getConnectionDispatcherTimer.Stop();
                SetMessageText(RichTextBox2, $"Stop Retrieval Connection Status at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                ButtonRetrievalQueue.Content = "Start Retrieval Queue";
                ButtonRetrievalQueue.Background = new SolidColorBrush(Colors.Red);
            }
        }

        private void ButtonSetFolderLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = Environment.CurrentDirectory };
            var result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
            {
                _initialDirectory = folderBrowserDialog.SelectedPath;
            }
        }
    }
}

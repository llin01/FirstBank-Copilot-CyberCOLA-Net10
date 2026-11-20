using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Cursors = System.Windows.Input.Cursors;
using Logger = NLog.Logger;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfSVCSClientSimulator
{
    /// <summary>
    /// MainWindow.xaml Interaction logic
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private delegate void UpdateRichTextBox(System.Windows.Controls.Control richTextBox, string message, Brush brush);

        private dynamic clientETicket;
        private string _initialDirectory;
        private byte[] _getISOBytes;
        private bool _isStopUDPServer = false;
        private int _timeInterval = 10;
        private readonly List<byte[]> _templateRawDataList = new List<byte[]>();
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;

        private static readonly Random StaticTraceRnd = new Random();
        private static readonly Random StaticAmtRnd = new Random();
        private static readonly Random StaticRefRnd = new Random();

        private readonly string _sessionCount;
        private readonly string _autoStartClientService;

        public MainWindow()
        {
            try
            {
                _sessionCount = (Application.Current as App)?.SessionCount;
                _autoStartClientService = (Application.Current as App)?.AutoStartListenService;
                LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
                _configurationLoaderRoot = new ConfigurationLoaderRoot();
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
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
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
                _initialDirectory = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory").Trim();
                TextBoxListenerPort.Text = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ETicketListenerPort").Trim();
                TextBoxStartLocalPort.Text = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ETicketLocalPort").Trim();
                TextBoxSelectFileName.Text = _initialDirectory;
                _timeInterval = Convert.ToInt32(TextBoxTimeIntervalMillisecond.Text.Trim());
                TextBoxSessionCount.IsEnabled = true;
                BtnClientStart.IsEnabled = true;
                BtnClientStop.IsEnabled = !BtnClientStart.IsEnabled;
                BtnUDPStart.IsEnabled = true;
                BtnUDPStop.IsEnabled = false;
                BtnSelectFileName.IsEnabled = false;
                BtnSendFile.IsEnabled = false;
                ComboBoxETicketFunction.Items.Clear();
                ComboBoxETicketFunction.Items.Add("ICash");
                ComboBoxETicketFunction.Items.Add("IPass");
                ComboBoxETicketFunction.Items.Add("SVCS");
                //ComboBoxETicketFunction.SelectedIndex = 0;
                //Trigger BtnUDPStart
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnUDPStart);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
                if (!(string.IsNullOrEmpty(_autoStartClientService)) && _autoStartClientService.Trim().Equals("AUTO"))
                {
                    TextBoxSessionCount.Text = _sessionCount.Trim();
                    BtnClientStart.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                BtnClientStart.IsEnabled = false;
                BtnClientStop.IsEnabled = false;
                BtnSendFile.IsEnabled = false;
                BtnUDPStart.IsEnabled = false;
                BtnUDPStop.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void SetMessageText(System.Windows.Controls.Control richTextBox, string message, Brush brush)
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

        private class ETicketDataInfo
        {
            public string MessageType { get; set; }
            public string ProcessCode { get; set; }
            public string SystemTraceNumber { get; set; }
        }
        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
            {
                case "ICASH":
                    ((ClientICASH)clientETicket)?.Stop();
                    break;
                case "IPASS":
                    ((ClientIPASS)clientETicket)?.Stop();
                    break;
                case "SVCS":
                    ((ClientSVCS)clientETicket)?.Stop();
                    break;
            }
            SetMessageText(RichTextBox2, $"Authorization process stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            Close();
            Environment.Exit(0);
        }


        private async void BtnClientStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start Service";
            try
            {
                UpdateParameterUseAppSettingJson();
                clientETicket = null;
                switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
                {
                    case "ICASH":
                        clientETicket = new ClientICASH();
                        await Task.Run(() => ((ClientICASH)clientETicket).Start());
                        break;
                    case "IPASS":
                        clientETicket = new ClientIPASS();
                        await Task.Run(() => ((ClientIPASS)clientETicket).Start());
                        break;
                    case "SVCS":
                        clientETicket = new ClientSVCS();
                        await Task.Run(() => ((ClientSVCS)clientETicket).Start());
                        break;
                    default:
                        SetMessageText(RichTextBox2, $"Select E-Ticket Function Error [{ComboBoxETicketFunction.Text.Trim().ToUpper()}]", Brushes.Red);
                        return;
                }
                SetMessageText(RichTextBox2, $"{ComboBoxETicketFunction.Text.Trim().ToUpper()} CyberAuth start process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                TextBoxSessionCount.IsEnabled = false;
                BtnClientStart.IsEnabled = false;
                BtnClientStop.IsEnabled = !BtnClientStart.IsEnabled;
                BtnSelectFileName.IsEnabled = true;
                BtnSendFile.IsEnabled = false;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                BtnClientStart.IsEnabled = false;
                BtnClientStop.IsEnabled = false;
                BtnSelectFileName.IsEnabled = false;
                BtnSendFile.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnClientStop_Click(object sender, RoutedEventArgs e)
        {
            switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
            {
                case "ICASH":
                    await Task.Run(() => ((ClientICASH)clientETicket)?.Stop());
                    break;
                case "IPASS":
                    await Task.Run(() => ((ClientIPASS)clientETicket)?.Stop());
                    break;
                case "SVCS":
                    await Task.Run(() => ((ClientSVCS)clientETicket)?.Stop());
                    break;
            }
            SetMessageText(RichTextBox2, $"Authorization process stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            clientETicket = null;
            TextBoxSessionCount.IsEnabled = true;
            BtnClientStart.IsEnabled = true;
            BtnClientStop.IsEnabled = false;
            BtnSelectFileName.IsEnabled = false;
            BtnSendFile.IsEnabled = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
            {
                case "ICASH":
                    ((ClientICASH)clientETicket)?.Stop();
                    break;
                case "IPASS":
                    ((ClientIPASS)clientETicket)?.Stop();
                    break;
                case "SVCS":
                    ((ClientSVCS)clientETicket)?.Stop();
                    break;
            }

            clientETicket = null;
            SetMessageText(RichTextBox2, $"Authorization process stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            Environment.Exit(0);
        }

        private void BtnSelectFileName_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Create Send Message";
            _templateRawDataList.Clear();
            DataGridRetrievalQueue.Items.Clear();
            DataGridRetrievalQueue.Items.Refresh();
            BtnSendFile.IsEnabled = false;
            try
            {
                SetMessageText(RichTextBox2, $"Template Work Directory[{_initialDirectory}]", Brushes.Black);
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
                string[] fileEntries = Directory.GetFiles(_initialDirectory, "*.*", SearchOption.AllDirectories);
                List<string> returnData = null;
                Mouse.OverrideCursor = null;
                SelectFileWindow selectFileWindow = new SelectFileWindow(fileEntries);
                if (!selectFileWindow.ShowDialog() == true)
                    returnData = selectFileWindow.GetFilePaths();
                Mouse.OverrideCursor = Cursors.Wait;
                if ((returnData == null) || (returnData.Count <= 0))
                {
                    SetMessageText(RichTextBox1, "No Select File", Brushes.Red);
                    BtnSendFile.IsEnabled = false;
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
                        byte[] dataBytes = File.ReadAllBytes(fileName);
                        _templateRawDataList.Add(dataBytes);
                        dynamic requestMessage;
                        switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
                        {
                            case "ICASH":
                                requestMessage = CreateICASHRequestMessage(dataBytes);
                                if (requestMessage != null)
                                {
                                    var svcaDataInfo = new ETicketDataInfo { MessageType = requestMessage.Mti() };
                                    var fieldName = FIELD.F_3;
                                    if (requestMessage.Core().Field(fieldName) != null)
                                    {
                                        svcaDataInfo.ProcessCode = requestMessage.Core().Field(fieldName).ToString();
                                    }
                                    else
                                    {
                                        svcaDataInfo.ProcessCode = string.Empty;
                                    }
                                    fieldName = FIELD.F_11;
                                    if (requestMessage.Core().Field(fieldName) != null)
                                    {
                                        svcaDataInfo.SystemTraceNumber = requestMessage.Core().Field(fieldName).ToString();
                                    }
                                    else
                                    {
                                        svcaDataInfo.ProcessCode = string.Empty;
                                    }
                                    DataGridRetrievalQueue.Items.Add(svcaDataInfo);
                                }
                                break;
                            case "IPASS":
                                requestMessage = CreateIPASSRequestMessage(dataBytes);
                                if (requestMessage != null)
                                {
                                    var svcaDataInfo = new ETicketDataInfo { MessageType = requestMessage.Mti() };
                                    var fieldName = FIELD.F_3;
                                    if (requestMessage.Core().Field(fieldName) != null)
                                    {
                                        svcaDataInfo.ProcessCode = requestMessage.Core().Field(fieldName).ToString();
                                    }
                                    else
                                    {
                                        svcaDataInfo.ProcessCode = string.Empty;
                                    }
                                    fieldName = FIELD.F_11;
                                    if (requestMessage.Core().Field(fieldName) != null)
                                    {
                                        svcaDataInfo.SystemTraceNumber = requestMessage.Core().Field(fieldName).ToString();
                                    }
                                    else
                                    {
                                        svcaDataInfo.ProcessCode = string.Empty;
                                    }
                                    DataGridRetrievalQueue.Items.Add(svcaDataInfo);
                                }
                                break;
                            case "SVCS":
                                requestMessage = CreateSVCSRequestMessage(dataBytes);
                                if (requestMessage != null)
                                {
                                    var svcaDataInfo = new ETicketDataInfo { MessageType = requestMessage.Mti() };
                                    var fieldName = FIELD.F_3;
                                    if (requestMessage.Core().Field(fieldName) != null)
                                    {
                                        svcaDataInfo.ProcessCode = requestMessage.Core().Field(fieldName).ToString();
                                    }
                                    else
                                    {
                                        svcaDataInfo.ProcessCode = string.Empty;
                                    }
                                    fieldName = FIELD.F_11;
                                    if (requestMessage.Core().Field(fieldName) != null)
                                    {
                                        svcaDataInfo.SystemTraceNumber = requestMessage.Core().Field(fieldName).ToString();
                                    }
                                    else
                                    {
                                        svcaDataInfo.ProcessCode = string.Empty;
                                    }
                                    DataGridRetrievalQueue.Items.Add(svcaDataInfo);
                                }
                                break;
                        }
                    }
                }
                BtnSendFile.IsEnabled = true;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                BtnSendFile.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void GetISOBytes()
        {
            try
            {
                _getISOBytes = null;
                var fileName = TextBoxSelectFileName.Text.Trim();
                if (!File.Exists(fileName))
                {
                    SetMessageText(RichTextBox1, $"File[{fileName}] Not Found", Brushes.Red);
                    _getISOBytes = null;
                }
                var readBytes = File.ReadAllBytes(fileName);
                _getISOBytes = new byte[readBytes.Length];
                Buffer.BlockCopy(readBytes, 0, _getISOBytes, 0, readBytes.Length);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                _getISOBytes = null;
            }
        }
        private async void BtnSendFile_Click(object sender, RoutedEventArgs e)
        {
            if (!clientETicket.IsHaveChannels())
            {
                SetMessageText(RichTextBox1, $"{ComboBoxETicketFunction.Text.Trim().ToUpper()} No Channels Connection, Please Recheck Connection", Brushes.Red);
                return;
            }
            try
            {
                foreach (var dataBytes in _templateRawDataList)
                {

                    dynamic requestMessage;
                    switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
                    {
                        case "ICASH":
                            requestMessage = CreateICASHRequestMessage(dataBytes);
                            SetMessageText(RichTextBox2, "Request Message", Brushes.Red);
                            SetMessageText(RichTextBox2, $"Header[{requestMessage.Header().Encode()}]", Brushes.Black);
                            SetMessageText(RichTextBox2, $"MTI [{requestMessage.Mti()}]", Brushes.Black);
                            foreach (var pair in (requestMessage.Core().OrderedFields()))
                            {
                                var key = pair.Key;
                                var value = pair.Value;
                                if (value is byte[] bytes)
                                {
                                    var tempString = HexStringConverter.FromByteArray(bytes);
                                    SetMessageText(RichTextBox2, $"[FIELD {key}] VALUE [{tempString}]", Brushes.Black);
                                }
                                else
                                {
                                    SetMessageText(RichTextBox2, $"[FIELD {key}] VALUE [{value}]", Brushes.Black);
                                }
                            }
                            await Task.Run(() => ((ClientICASH)clientETicket).AllChannelSendMessageToGateway(requestMessage));
                            SetMessageText(RichTextBox2, $"{ComboBoxETicketFunction.Text.Trim().ToUpper()} CyberAuth start process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                            await Task.Delay(_timeInterval);
                            SetMessageText(RichTextBox2, $"Waiting {_timeInterval} Millisecond", Brushes.Red);
                            break;
                        case "IPASS":
                            requestMessage = CreateIPASSRequestMessage(dataBytes);
                            SetMessageText(RichTextBox2, "Request Message", Brushes.Red);
                            SetMessageText(RichTextBox2, $"Header[{requestMessage.Header().Encode()}]", Brushes.Black);
                            SetMessageText(RichTextBox2, $"MTI [{requestMessage.Mti()}]", Brushes.Black);
                            foreach (var pair in (requestMessage.Core().OrderedFields()))
                            {
                                var key = pair.Key;
                                var value = pair.Value;
                                if (value is byte[] bytes)
                                {
                                    var tempString = HexStringConverter.FromByteArray(bytes);
                                    SetMessageText(RichTextBox2, $"[FIELD {key}] VALUE [{tempString}]", Brushes.Black);
                                }
                                else
                                {
                                    SetMessageText(RichTextBox2, $"[FIELD {key}] VALUE [{value}]", Brushes.Black);
                                }
                            }
                            await Task.Run(() => ((ClientIPASS)clientETicket).AllChannelSendMessageToGateway(requestMessage));
                            SetMessageText(RichTextBox2, $"{ComboBoxETicketFunction.Text.Trim().ToUpper()} CyberAuth start process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                            await Task.Delay(_timeInterval);
                            SetMessageText(RichTextBox2, $"Waiting {_timeInterval} Millisecond", Brushes.Red);
                            break;
                        case "SVCS":
                            requestMessage = CreateSVCSRequestMessage(dataBytes);
                            SetMessageText(RichTextBox2, "Request Message", Brushes.Red);
                            SetMessageText(RichTextBox2, $"Header[{requestMessage.Header().Encode()}]", Brushes.Black);
                            SetMessageText(RichTextBox2, $"MTI [{requestMessage.Mti()}]", Brushes.Black);
                            foreach (var pair in (requestMessage.Core().OrderedFields()))
                            {
                                var key = pair.Key;
                                var value = pair.Value;
                                if (value is byte[] bytes)
                                {
                                    var tempString = HexStringConverter.FromByteArray(bytes);
                                    SetMessageText(RichTextBox2, $"[FIELD {key}] VALUE [{tempString}]", Brushes.Black);
                                }
                                else
                                {
                                    SetMessageText(RichTextBox2, $"[FIELD {key}] VALUE [{value}]", Brushes.Black);
                                }
                            }
                            await Task.Run(() => ((ClientSVCS)clientETicket).AllChannelSendMessageToGateway(requestMessage));
                            SetMessageText(RichTextBox2, $"{ComboBoxETicketFunction.Text.Trim().ToUpper()} CyberAuth start process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                            await Task.Delay(_timeInterval);
                            SetMessageText(RichTextBox2, $"Waiting {_timeInterval} Millisecond", Brushes.Red);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
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
                    SetMessageText(RichTextBox2, $"UDPServer[{udpListenerPort}] service not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    Mouse.OverrideCursor = null;
                    return;
                }
                BtnUDPStart.IsEnabled = false;
                BtnUDPStop.IsEnabled = !BtnUDPStart.IsEnabled;
                _isStopUDPServer = false;
                Thread udpListener = new Thread(() => ReadServerLogMessage(udpListenerPort));
                udpListener.Start();
                SetMessageText(RichTextBox2, $"UDPServer[{udpListenerPort}] service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
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
                    if (readBytes == null) continue;
                    SetMessageText(RichTextBox1, $"{Encoding.Default.GetString(readBytes, 0, readBytes.Length)}", Brushes.Black);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex.Message);
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
            finally
            {
                udpClient.Close();
            }
        }

        private void BtnUDPStart_Click(object sender, RoutedEventArgs e)
        {
            var udpListenerPort = Convert.ToInt32(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPListenerPort").Trim());
            var udpAutoStartFlag = Convert.ToBoolean(_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPAutoStartFlag").Trim());
            StartUDPService(udpListenerPort, udpAutoStartFlag);
        }

        private void BtnUDPStop_Click(object sender, RoutedEventArgs e)
        {
            _isStopUDPServer = true;
            SetMessageText(RichTextBox2, $"UDPServer[{_configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPListenerPort").Trim()}] service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            BtnUDPStop.IsEnabled = false;
            BtnUDPStart.IsEnabled = !BtnUDPStop.IsEnabled;
        }

        //private void UpdateParameter()
        //{
        //    Mouse.OverrideCursor = Cursors.Wait;
        //    var listenerPort = TextBoxListenerPort.Text.Trim();
        //    var startLocalPort = TextBoxStartLocalPort.Text.Trim();
        //    var sessionCount = TextBoxSessionCount.Text.Trim();
        //    try
        //    {
        //
        //        var listenPortKey = "cola-service-target-port";
        //        var startLocalPortKey  = "cola-service-local-port-start";
        //        var sessionsKey   = "cola-service-sessions";
        //        var fileName = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        //        var configFile = System.Configuration.ConfigurationManager.OpenExeConfiguration(fileName);
        //        configFile.AppSettings.Settings[listenPortKey].Value = listenerPort;
        //        configFile.AppSettings.Settings[startLocalPortKey].Value = startLocalPort;
        //        configFile.AppSettings.Settings[sessionsKey].Value = sessionCount;
        //        configFile.Save();
        //        SetMessageText(RichTextBox1, "UpdateParameter process completely", Brushes.Red);
        //    }
        //    catch (Exception ex)
        //    {
        //        SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
        //    }
        //    finally
        //    {
        //        Mouse.OverrideCursor = null;
        //    }
        //}

        private void UpdateParameterUseAppSettingJson()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var listenerPort = TextBoxListenerPort.Text.Trim();
            var startLocalPort = TextBoxStartLocalPort.Text.Trim();
            var sessionCount = TextBoxSessionCount.Text.Trim();
            try
            {

                var sectionName = "ColaServiceConfig";
                var listenPortKey = "cola-service-target-port";
                var startLocalPortKey = "cola-service-local-port-start";
                var sessionsKey = "cola-service-sessions";
                switch (ComboBoxETicketFunction.Text.Trim().ToUpper())
                {
                    case "ICASH":
                        listenPortKey = "icash-cola-service-target-port";
                        startLocalPortKey = "icash-cola-service-local-port-start";
                        sessionsKey = "icash-cola-service-sessions";
                        break;
                    case "IPASS":
                        listenPortKey = "ipass-cola-service-target-port";
                        startLocalPortKey = "ipass-cola-service-local-port-start";
                        sessionsKey = "ipass-cola-service-sessions";
                        break;
                    case "SVCS":
                        listenPortKey = "svcs-cola-service-target-port";
                        startLocalPortKey = "svcs-cola-service-local-port-start";
                        sessionsKey = "svcs-cola-service-sessions";
                        break;
                }
                var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "appSettings.json");
                string json = File.ReadAllText(jsonFilePath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                jsonObj[sectionName][listenPortKey] = listenerPort;
                jsonObj[sectionName][startLocalPortKey] = startLocalPort;
                jsonObj[sectionName][sessionsKey] = sessionCount;
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(jsonFilePath, output);
                SetMessageText(RichTextBox1, "UpdateParameterUseAppSettingJson process completely", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private ICASH CreateICASHRequestMessage(byte[] originalMessage)
        {
            try
            {
                var requestMessage = new ICASH(originalMessage);
                //Change Field Data
                var fieldName = FIELD.F_4;
                if ((requestMessage).Core().Field(fieldName) != null)
                {
                    int amtMount = StaticAmtRnd.Next(1, 100);
                    requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
                }
                fieldName = FIELD.F_7;
                (requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //if ((requestMessage).Core().Field(fieldName) != null)
                //{
                //    (requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //}
                fieldName = FIELD.F_11;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    int traceNumber = StaticTraceRnd.Next(1, 999999);
                    (requestMessage).Core().Field(fieldName, $"{traceNumber:000000}");
                }
                fieldName = FIELD.F_12;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
                }
                fieldName = FIELD.F_13;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_15;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    (requestMessage).Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_17;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_37;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    int refNumber = StaticRefRnd.Next(1, 999999);
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                }
                return (requestMessage);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                return null;
            }
        }

        private IPASS CreateIPASSRequestMessage(byte[] originalMessage)
        {
            try
            {
                var requestMessage = new IPASS(originalMessage);
                //Change Field Data
                var fieldName = FIELD.F_4;
                if ((requestMessage).Core().Field(fieldName) != null)
                {
                    int amtMount = StaticAmtRnd.Next(1, 100);
                    requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
                }
                fieldName = FIELD.F_7;
                (requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //if ((requestMessage).Core().Field(fieldName) != null)
                //{
                //    (requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //}
                fieldName = FIELD.F_11;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    int traceNumber = StaticTraceRnd.Next(1, 999999);
                    (requestMessage).Core().Field(fieldName, $"{traceNumber:000000}");
                }
                fieldName = FIELD.F_12;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
                }
                fieldName = FIELD.F_13;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_15;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    (requestMessage).Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_17;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_37;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    int refNumber = StaticRefRnd.Next(1, 999999);
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                }
                return (requestMessage);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                return null;
            }
        }

        private SVCS CreateSVCSRequestMessage(byte[] originalMessage)
        {
            try
            {
                var requestMessage = new SVCS(originalMessage);
                //Change Field Data
                var fieldName = FIELD.F_4;
                if ((requestMessage).Core().Field(fieldName) != null)
                {
                    int amtMount = StaticAmtRnd.Next(1, 100);
                    requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
                }
                fieldName = FIELD.F_7;
                (requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //if ((requestMessage).Core().Field(fieldName) != null)
                //{
                //    (requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //}
                fieldName = FIELD.F_11;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    int traceNumber = StaticTraceRnd.Next(1, 999999);
                    (requestMessage).Core().Field(fieldName, $"{traceNumber:000000}");
                }
                fieldName = FIELD.F_12;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
                }
                fieldName = FIELD.F_13;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_15;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    (requestMessage).Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_17;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_37;
                if (requestMessage.Core().Field(fieldName) != null)
                {
                    int refNumber = StaticRefRnd.Next(1, 999999);
                    requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
                }
                return (requestMessage);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                return null;
            }
        }
    }
}

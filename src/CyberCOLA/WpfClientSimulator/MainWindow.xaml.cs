using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
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
using Cursors = System.Windows.Input.Cursors;
using Logger = NLog.Logger;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfClientSimulator
{
    /// <summary>
    /// Main window for client simulator application.
    /// Simulates client-side transaction processing and testing for authorization systems.
    /// Provides UI for sending test transactions and monitoring responses.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private delegate void UpdateRichTextBox(System.Windows.Controls.Control richTextBox, string message, Brush brush);

        private CyberAuthNCCC cyberAuthNCCC;
        private string _initialDirectory;
        private byte[] _getISOBytes;
        private bool _isStopUDPServer = false;
        private int _timeInterval = 10;
        private readonly List<byte[]> _templateRawDataList = new List<byte[]>();
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;

        private static readonly Random StaticTraceRnd = new Random();
        private static readonly Random StaticAmtRnd = new Random();
        private static readonly Random StaticRefRnd = new Random();

        public MainWindow()
        {
            try
            {
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
                _initialDirectory = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory").Trim();
                TextBoxListenerPort.Text = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NCCCListenerPort").Trim();
                TextBoxStartLocalPort.Text = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NCCCLocalPort").Trim();
                TextBoxSelectFileName.Text = _initialDirectory;
                _timeInterval = Convert.ToInt32(TextBoxTimeIntervalMillisecond.Text.Trim());
                TextBoxSessionCount.IsEnabled = true;
                BtnClientStart.IsEnabled = true;
                BtnClientStop.IsEnabled = !BtnClientStart.IsEnabled;
                BtnUDPStart.IsEnabled = true;
                BtnUDPStop.IsEnabled = false;
                BtnSelectFileName.IsEnabled = false;
                BtnSendFile.IsEnabled = false;
                //Trigger BtnUDPStart
                ButtonAutomationPeer peer = new ButtonAutomationPeer(BtnUDPStart);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
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

        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            cyberAuthNCCC?.Stop();
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
                cyberAuthNCCC = null;
                cyberAuthNCCC = new CyberAuthNCCC();
                await Task.Run(() => cyberAuthNCCC.Start());
                SetMessageText(RichTextBox2, $"NCCC CyberAuth start process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
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
            await Task.Run(() => cyberAuthNCCC?.Stop());
            SetMessageText(RichTextBox2, $"Authorization process stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            cyberAuthNCCC = null;
            TextBoxSessionCount.IsEnabled = true;
            BtnClientStart.IsEnabled = true;
            BtnClientStop.IsEnabled = false;
            BtnSelectFileName.IsEnabled = false;
            BtnSendFile.IsEnabled = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            cyberAuthNCCC?.Stop();
            cyberAuthNCCC = null;
            SetMessageText(RichTextBox2, $"Authorization process stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            Environment.Exit(0);
        }

        private void BtnSelectFileName_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Create Send Message";
            _templateRawDataList.Clear();
            BtnSendFile.IsEnabled = false;
            try
            {
                SetMessageText(RichTextBox2, $"Template Work Directory[{_initialDirectory}]", Brushes.Black);
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
                foreach (var fileName in returnData)
                {
                    byte[] bytesData = File.ReadAllBytes(fileName);
                    _templateRawDataList.Add(bytesData);
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
            if (!cyberAuthNCCC.IsHaveChannels())
            {
                SetMessageText(RichTextBox1, "NCCC No Channels Connection, Please Recheck Connection", Brushes.Red);
                return;
            }
            try
            {
                foreach (var dataBytes in _templateRawDataList)
                {

                    var requestMessage = CreateRequestMessage(dataBytes);
                    SetMessageText(RichTextBox2, "Request Message", Brushes.Red);
                    SetMessageText(RichTextBox2, $"Header[{((NcccIso8583)requestMessage).Header().Encode()}]", Brushes.Black);
                    SetMessageText(RichTextBox2, $"MTI [{((NcccIso8583)requestMessage).Mti()}]", Brushes.Black);
                    foreach (var pair in (((NcccIso8583)requestMessage).Core().OrderedFields()))
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
                    await Task.Run(() => cyberAuthNCCC.AllChannelSendMessageToGateway(requestMessage));
                    SetMessageText(RichTextBox2, $"NCCC CyberAuth start process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    await Task.Delay(_timeInterval);
                    SetMessageText(RichTextBox2, $"Waiting {_timeInterval} Millisecond", Brushes.Red);
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
        //
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

        private NcccIso8583 CreateRequestMessage(byte[] originalMessage)
        {
            try
            {
                var requestMessage = new NcccIso8583(originalMessage);
                //Change Field Data
                var fieldName = FIELD.F_4;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    int amtMount = StaticAmtRnd.Next(1, 100);
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{amtMount:000000000000}");
                }
                fieldName = FIELD.F_7;
                ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                //{
                //    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                //}
                fieldName = FIELD.F_11;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    int traceNumber = StaticTraceRnd.Next(1, 999999);
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{traceNumber:000000}");
                }
                fieldName = FIELD.F_12;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
                }
                fieldName = FIELD.F_13;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_15;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_17;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now:MMdd}");
                }
                fieldName = FIELD.F_37;
                if (((NcccIso8583)requestMessage).Core().Field(fieldName) != null)
                {
                    int refNumber = StaticRefRnd.Next(1, 999999);
                    ((NcccIso8583)requestMessage).Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
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

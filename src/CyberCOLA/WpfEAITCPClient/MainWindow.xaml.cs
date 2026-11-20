using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using Newtonsoft.Json;
using Cursors = System.Windows.Input.Cursors;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Com.Cybersoft4u.Test.WpfEAITCPClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private delegate void UpdateRichTextBox(RichTextBox richTextBox, string message, Brush brush);

        private readonly Logger? Logger;
        private string? _initialFolderPath;
        private string? _serverName;
        private int _serverListenerPort = 9999;
        private int _connectionTimeout = 2000;
        private int _sendReceiveTimeout = 5000;

        private byte[]? _sendBytes;
        private static readonly Random Random = new Random();
        public MainWindow()
        {
            try
            {
                LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
                Logger = LoggerAdapter.GetLogger("LoggerHandler[WpfEAITCPClient]");
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex);
            }
        }
        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the InvalidateRequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitialSystemParameter()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _initialFolderPath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "initial-folder-path").Trim();
                _serverName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "remote-server-hostname").Trim();
                _serverListenerPort = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "remote-service-target-port").Trim());
                _connectionTimeout = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "connection-timeout-millisecond").Trim());
                _sendReceiveTimeout = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "send-receive-timeout-millisecond").Trim());
                TextBoxServerName.Text = _serverName;
                TextBoxListenerPort.Text = _serverListenerPort.ToString();
                BtnParsingFile.IsEnabled = false;
                BtnProcess.IsEnabled = false;
            }
            catch (Exception ex)
            {
                BtnParsingFile.IsEnabled = false;
                BtnProcess.IsEnabled = false;
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Logger?.Fatal(ex);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void SetMessageText(RichTextBox richTextBox, string message, Brush brush)
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

        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Clear Message Content";
            RichTextBox1.Document.Blocks.Clear();
            RichTextBox2.Document.Blocks.Clear();
        }

        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            Close();
        }

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Mouse.OverrideCursor = Cursors.Wait;
            BtnProcess.IsEnabled = false;
            try
            {
                var receiveBytes = await Task.Run(ProcessTask);
                //var receiveBytes = ProcessTCPTask();
                if (receiveBytes == null) return;
                var rspJsonString = Encoding.Default.GetString(receiveBytes);
                var rspObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_RSP>(rspJsonString);

                if (rspObject == null)
                {
                    SetMessageText(RichTextBox1, $"Receive Bytes Data String[{rspJsonString}], Length[{receiveBytes.Length}] , Can't Convert To FCB9GQ.FCB9GQ_RSP", Brushes.Red);
                    BtnProcess.IsEnabled = false;
                    return;
                }
                SetMessageText(RichTextBox1, $"Receive Message[{rspJsonString}], Length[{receiveBytes.Length}]", Brushes.Red);
                BtnParsingFile.IsEnabled = false;
                BtnProcess.IsEnabled = false;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                BtnParsingFile.IsEnabled = false;
                BtnProcess.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
                stopWatch.Stop();
                TimeSpan timeSpan = stopWatch.Elapsed;
                var elapsedTime = $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds / 10:00}";
                SetMessageText(RichTextBox1, $"Execution Time : {elapsedTime}", Brushes.Red);
                BtnParsingFile.IsEnabled = false;
                BtnProcess.IsEnabled = false;
            }
        }

        // Change the return type of ProcessTask from byte[]? to byte[], ensuring it never returns null
        private byte[] ProcessTask()
        {
            try
            {
                if (_sendBytes == null)
                {
                    Logger?.Info($"Send Data Is Error, No Process at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    // Return an empty array instead of null
                    return Array.Empty<byte>();
                }
                SocketClient sa = new SocketClient(_serverName, _serverListenerPort, _connectionTimeout, _sendReceiveTimeout);
                sa.Connect();
                var receiveBytes = sa.SendReceive(_sendBytes);
                if (receiveBytes == null)
                {
                    SetMessageText(RichTextBox1, $" ProcessTask No message received", Brushes.Red);
                    return Array.Empty<byte>();
                }
                Logger?.Info($"Receive Message {Encoding.Default.GetString(receiveBytes)} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                sa.Disconnect();
                Logger?.Info($"ProcessTask process completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return receiveBytes;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $" ProcessTask Exception:{ex.Message}", Brushes.Red);
                return Array.Empty<byte>();
            }

        }

        private byte[]? ProcessTCPTask()
        {
            try
            {
                IPHostEntry ipHost = Dns.GetHostEntry(_serverName);
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, _serverListenerPort);

                // Creation TCP/IP Socket using
                // Socket Class Constructor
                Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    sender.Connect(localEndPoint);
                    SetMessageText(RichTextBox1, $"Socket connected to -> {sender.RemoteEndPoint?.ToString() ?? "Unknown"}", Brushes.Red);

                    // Fix: Check if _sendBytes is null
                    if (_sendBytes == null)
                    {
                        SetMessageText(RichTextBox1, $"Send buffer is null.", Brushes.Red);
                        return null;
                    }

                    int byteSent = sender.Send(_sendBytes);

                    // Data buffer
                    byte[] receiveBytes = new byte[1024];
                    int byteRecv = sender.Receive(receiveBytes);
                    SetMessageText(RichTextBox1, $"SMessage from Server -> {Encoding.ASCII.GetString(receiveBytes, 0, byteRecv)}", Brushes.Red);
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    byte[] returnBytes = new byte[byteRecv];
                    Buffer.BlockCopy(receiveBytes, 0, returnBytes, 0, byteRecv);
                    return receiveBytes;
                }
                // Manage of Socket's Exceptions
                catch (ArgumentNullException ane)
                {
                    SetMessageText(RichTextBox1, $" ArgumentNullException:{ane.Message}", Brushes.Red);
                    return null;
                }
                catch (SocketException se)
                {
                    SetMessageText(RichTextBox1, $" SocketException Exception:{se.Message}", Brushes.Red);
                    return null;
                }
                catch (Exception e)
                {
                    SetMessageText(RichTextBox1, $" Unexpected Exception:{e.Message}", Brushes.Red);
                    return null;
                }
            }
            catch (Exception e)
            {
                SetMessageText(RichTextBox1, $" ProcessTCPTask Exception:{e.Message}", Brushes.Red);
                return null;
            }

        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = _initialFolderPath,
                Title = @"Browse Process Files",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "dat",
                Filter = @"Text files (*.dat)|*.dat|All files (*.*)|*.*",
                FilterIndex = 2,
                RestoreDirectory = true,
                ReadOnlyChecked = true,
                ShowReadOnly = true
            };
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxFileName.Text = openFileDialog.FileName;
            }
            var fileName = TextBoxFileName.Text.Trim();
            _sendBytes = System.IO.File.ReadAllBytes(fileName);
            if (_sendBytes == null)
            {
                SetMessageText(RichTextBox1, $"Read File[{fileName} Error", Brushes.Red);
                return;
            }

            string reqJsonString = Encoding.Default.GetString(_sendBytes);
            SetMessageText(RichTextBox1, $"JSON Request[{reqJsonString}]", Brushes.Red);
            var reqObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_REQ>(reqJsonString);
            if (reqObject == null)
            {
                SetMessageText(RichTextBox1, $"Read File[{fileName}], Data[{reqJsonString}] Length[{_sendBytes.Length}] Convert To FCB9GQ.FCB9GQ_REQ Error", Brushes.Red);
                return;
            }
            BtnParsingFile.IsEnabled = true;
            BtnProcess.IsEnabled = true;
            SetMessageText(RichTextBox1, $"Create Send Message : {reqJsonString} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
        }

        private void BtnParsingFile_Click(object sender, RoutedEventArgs e)
        {

        }

        public string RandomAlphanumeric(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }
        public string RandomNumeric(int length)
        {
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private void BtnCommand4_Click(object sender, RoutedEventArgs e)
        {
            var inputData = "428430010685772";
            var cardNumber = EAICommUtility.CreateCardNumberCheckDigit(inputData);
            SetMessageText(RichTextBox1, $"Input Data[{inputData}], Card Number[{cardNumber}]", Brushes.Red);
        }

        private void BtnCommand6_Click(object sender, RoutedEventArgs e)
        {
            string data = @"Data Source=cciasdb-t;TrustServerCertificate=True;Initial Catalog=CARD;User ID=cciasuser;Password=cciasuser123";
            string subData = data.Substring(0, data.ToUpper().IndexOf("USER", StringComparison.Ordinal));
            var sb = new StringBuilder();
            sb.Append("test")
              .Append(Encoding.ASCII.GetString(new byte[] { 0x0d, 0x1a }));

            var stringData = sb.ToString();
            var bytesData = Encoding.ASCII.GetBytes(stringData);
            SetMessageText(RichTextBox1, $"Data[{stringData}],Length[{stringData.Length}]", Brushes.Red);
        }

        private void BtnInitialFCB9GQIncomingMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = TextBoxFileName.Text.Trim();
                var receiveBytes = System.IO.File.ReadAllBytes(fileName);
                //Format Check Message
                var checkMessage = EAICommUtility.CheckFCB9GQMessageFormat(receiveBytes);
                if (!checkMessage.Equals(SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess))
                {
                    SetMessageText(RichTextBox1, $"Check Message Error[{checkMessage}]", Brushes.Red);
                    return;
                }
                //Convert Bytes array To FCB9GQ.FCB9GQ_REQ
                string reqJsonString = Encoding.Default.GetString(receiveBytes);
                SetMessageText(RichTextBox1, $"JSON Request[{reqJsonString}]", Brushes.Red);
                var reqJSONObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_REQ>(reqJsonString);
                if (reqJSONObject == null)
                {
                    SetMessageText(RichTextBox1, $"JSON Data[{reqJsonString}] Can't Convert To FCB9GQ.FCB9GQ_REQ", Brushes.Red);
                    return;
                }
                SetMessageText(RichTextBox1, $"JSON Data[{reqJsonString}]", Brushes.Red);
                //Convert FCB9GQ.FCB9GQ_REQ To NcccIso8583
                var nccc = EAICommUtility.ConvertFCB9GQMessage2NcccIso8583(reqJSONObject, FIELD.F_57);
                if (nccc == null)
                {
                    SetMessageText(RichTextBox1, $"Convert NcccISo8583 error", Brushes.Red);
                    return;
                }
                SetMessageText(RichTextBox1, $"--- Begin Parsing NCCC Message ---", Brushes.Red);
                SetMessageText(RichTextBox1, $"Header[{nccc.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(nccc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{nccc.Mti()}]", Brushes.Black);
                var tokenField = nccc.GetTokenFieldId();
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                    if (key == tokenField)
                    {
                        var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey(tokenField.ToString());
                        if (tokenGroup == null) continue;
                        foreach (var token in tokenGroup.Tokens)
                        {
                            SetMessageText(RichTextBox1, $"{tokenField.ToString()}[Token{token.Key}] = [{token.Value.Encode()}]", Brushes.Red);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $"Exception:{ex.Message}", Brushes.Red);
            }
        }
    }
}

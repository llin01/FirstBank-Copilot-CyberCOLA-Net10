using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Logger = NLog.Logger;


namespace Com.Cybersoft4u.Simulator.Wpf.WpfServerSimulator
{
    /// <summary>
    /// Main window for server simulator application.
    /// Simulates server-side authorization processing for testing client applications.
    /// Provides UI for monitoring incoming transactions and sending test responses.
    /// </summary>
    // Legacy HSM server simulator: answers on the ports of Config\HSMServerInfo.xml (12119-12123) with the
    // ASCII function id and the old reply layouts of its own Config\HSMRespConfig.xml. No current client uses
    // those ports: the Auth's HSM client (DLL\SocketClient.dll) connects to port 8500, answered by
    // WpfHSMSimulator / WebHSMSimulator (CommonLib AsynTcpServer). Do not use this one for the auth tests.
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        #region RichTextBox Target
        private const string ConsoleTargetName = "WpfRichTextBox";
        private AsyncTargetWrapper _wrapper;
        #endregion //RichTextBox Target

        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);
        private static string[] _listenPortNumber;
        private static int _listenPortNumberCount;
        private static int _sessionNumber;
        private static readonly SortedList<string, string> HSMRespList = new SortedList<string, string>();
        static Thread[] _threadHSMServer;
        static AsynTcpServer[] _asyncTcpServer;
        private string _autoStartListenService;

        public MainWindow()
        {
            try
            {
                _autoStartListenService = (Application.Current as Simulator.Wpf.WpfServerSimulator.App)?.AutoStartListenService;
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            this.TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitialSystemParameter()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var stringPortNumber = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ListenPortNumber").Trim();
                var hsmRespConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "HSMRespConfig").Trim();
                _sessionNumber = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SessionNumber").Trim());
                _listenPortNumber = stringPortNumber.Split(',');
                _listenPortNumberCount = _listenPortNumber.Count();
                var message = $"Listen PortNumber[{stringPortNumber}], HSMRespConfig File Path[{hsmRespConfigFilePath}]";
                Log.Info(message);
                SetMessageText(RichTextBox1, message, Brushes.Black);
                if (_listenPortNumberCount <= 0)
                {
                    Log.Info("Listener PortNumber Data Error : [{0}]", stringPortNumber);
                    return;
                }
                foreach (var portNumber in _listenPortNumber)
                {
                    ComboBoxListenerPort.Items.Add(portNumber);
                }
                ComboBoxListenerPort.SelectedIndex = 0;

                var doc = XDocument.Load(hsmRespConfigFilePath);
                var suipFuncQuery = from results in doc.Descendants("SuipFunc")
                                    select new
                                    {
                                        key = results.Attribute("Function-ID")?.Value,
                                        value = results.Attribute("RespCode")?.Value
                                    };
                foreach (var suipFunc in suipFuncQuery)
                {
                    HSMRespList.Add(suipFunc.key, suipFunc.value);
                }
                if (HSMRespList.Count <= 0)
                {
                    Log.Info("HSMRespConfig File Error : [{0}]", hsmRespConfigFilePath);
                    return;
                }
                _threadHSMServer = new Thread[_listenPortNumberCount];
                _asyncTcpServer = new AsynTcpServer[_listenPortNumberCount];
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                WpfRichTextBoxIInitialized();
                if (!(string.IsNullOrEmpty(_autoStartListenService)) &&
                   _autoStartListenService.Trim().Equals("AUTO"))
                    BtnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            RichTextBox rtb = richTextBox as RichTextBox;
            if (rtb == null) return;
            if (rtb.Dispatcher.CheckAccess())
            {
                TextRange rangeOfText1 = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd) { Text = message + '\r' };
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
                IPAddress resolveIPAddress = null;
                if (IPAddress.TryParse(hostNameOrIPAddress, out resolveIPAddress))
                {
                    switch (addressFamily)
                    {
                        case System.Net.Sockets.AddressFamily.InterNetwork:      // we have IPv4
                            return (hostNameOrIPAddress);
                        case System.Net.Sockets.AddressFamily.InterNetworkV6:   // we have IPv6
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
                Log.Fatal(ex, $"GetIPAddressStringByHostName ExceptionL[{hostNameOrIPAddress}]");
                return null;
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
            this.Close();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                for (int index = 0; index < _listenPortNumberCount; index++)
                {
                    _asyncTcpServer[index] = new AsynTcpServer(HSMRespList)
                    {
                        PortNumber = Convert.ToInt32(_listenPortNumber[index]),
                        SessionNumber = _sessionNumber
                    };
                    _threadHSMServer[index] = new Thread(_asyncTcpServer[index].StartServer)
                    {
                        Name = $"HSMServer_{_listenPortNumber[index]}"
                    };
                    _threadHSMServer[index].Start();
                    var message = $"HSMServer[{_threadHSMServer[index].Name}] start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                    Log.Info(message);
                    SetMessageText(RichTextBox1, message, Brushes.Black);
                }
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = false;
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                for (int index = 0; index < _listenPortNumberCount; index++)
                {
                    // StartServer() only binds and starts the async accept, so its thread has already ended; closing
                    // the listening socket is what stops the server. Thread.Abort() threw PlatformNotSupportedException
                    // on .NET 10 before StopServer() ran, so Stop never closed the socket.
                    _asyncTcpServer[index].StopServer();
                    var message = $"HSMServer[{_threadHSMServer[index].Name}] stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
                    Log.Info(message);
                    SetMessageText(RichTextBox1, message, Brushes.Black);
                }
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;

            }
            catch (Exception ex)
            {
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = false;
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WpfRichTextBoxIInitialized()
        {
            //Set Log Target
            // https://nlog.codeplex.com/workitem/6272
            var target = new WpfRichTextBoxTarget
            {
                Name = ConsoleTargetName,
                Layout = "[${longdate}][${level:uppercase=true:padding=-5}][${threadid:padding=3}][${message}]",
                ControlName = RichTextBox2.Name,
                FormName = Name,
                AutoScroll = true,
                MaxLines = 100000,
                UseDefaultRowColoringRules = true
            };
            _wrapper = new AsyncTargetWrapper
            {
                Name = ConsoleTargetName,
                WrappedTarget = target
            };
            // SimpleConfigurator is obsolete since NLog 5.2; this builds the same single-target, Info-and-above configuration.
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteTo(_wrapper);
            });
        }

        private void BtnSelectFileName_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            openFileDialog1.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? @"E:\";
            openFileDialog1.Title = @"Browse Process Files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "dat";
            openFileDialog1.Filter = @"Text files (*.dat)|*.dat|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            openFileDialog1.ReadOnlyChecked = true;
            openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxSelectFileName.Text = openFileDialog1.FileName;
            }
        }
    }
    class AsynTcpServer
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private readonly byte[] _dataBuffer = new byte[1024];
        private readonly SortedList<string, string> _respSortedList;
        private Socket _asyncServer;

        public bool IsRunning { get; set; }
        public int PortNumber { get; set; }
        public int SessionNumber { get; set; }
        public AsynTcpServer(SortedList<string, string> sortedList)
        {
            _respSortedList = new SortedList<string, string>(sortedList);
            IsRunning = false;
        }
        /*
         * 
         * 
         */
        public void StartServer()
        {
            _asyncServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var iep = new IPEndPoint(IPAddress.Any, PortNumber);
            _asyncServer.Bind(iep);
            _asyncServer.Listen(SessionNumber);
            _asyncServer.BeginAccept(AcceptConn, _asyncServer);
            IsRunning = true;
            Log.Info($"HSMServer Listener PortNumber[{PortNumber}] Waiting Connect ...");
        }

        void AcceptConn(IAsyncResult pAsyncRes)
        {
            try
            {
                Socket Client = ((Socket)pAsyncRes.AsyncState).EndAccept(pAsyncRes);
                Log.Info("Connected From : {0} At {1:yyyy-MM-dd HH:mm:ss:fff}", Client.RemoteEndPoint, DateTime.Now);
                Client.BeginReceive(_dataBuffer, 0, _dataBuffer.Length, SocketFlags.None, CallBackReceiveData, Client);
                _asyncServer.BeginAccept(AcceptConn, _asyncServer);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Exception occurred ({ex.Message}). Closing connection.");
            }
        }

        void CallBackSendData(IAsyncResult pAsyncRes)
        {
            var client = (Socket)pAsyncRes.AsyncState;
            int lBytesSend = client.EndSend(pAsyncRes);
            Log.Info("Send Data Length[{0}]", lBytesSend);
            client.Close();
        }

        void CallBackReceiveData(IAsyncResult pAsyncRes)
        {
            var client = (Socket)pAsyncRes.AsyncState;

            try
            {
                int bytesRead;
                var hsmMessage = new StringBuilder("");
                byte[] message;
                if ((bytesRead = client.EndReceive(pAsyncRes)) <= 2)
                {
                    Log.Info("Listen PortNumber[{0}] Receive Data Error,Data[{Encoding.ASCII.GetString(_dataBuffer, 0, bytesRead).TrimEnd()}", PortNumber);
                    hsmMessage.Append("9901");
                    message = Encoding.ASCII.GetBytes(hsmMessage.ToString());
                    client.BeginSend(message, 0, message.Length, SocketFlags.None, CallBackSendData, client);
                    Log.Info("Send Data[{0}],Length[{1}]", Encoding.ASCII.GetString(message), message.Length);
                    return;
                }
                string dataBufferString = Encoding.ASCII.GetString(_dataBuffer, 0, bytesRead).TrimEnd();
                Log.Info("Listen PortNumber[{0}] Receive Data[{1}]", PortNumber, dataBufferString);
                string funcID = dataBufferString.Substring(0, 2);
                var respCode = FindRespCodeByFuncID(funcID);
                hsmMessage.Append(respCode);
                message = Encoding.ASCII.GetBytes(hsmMessage.ToString());
                client.BeginSend(message, 0, message.Length, SocketFlags.None, CallBackSendData, client);
                Log.Info("Send Data[{0}],Length[{1}]", Encoding.ASCII.GetString(message), message.Length);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Exception occurred ({ex.Message}). Closing connection.");
                client.Close();
            }
        }

        string FindRespCodeByFuncID(string funcID)
        {
            try
            {
                var respCode = _respSortedList[funcID];
                switch (respCode)
                {
                    default:
                        string returnString = respCode.Replace('x', '0');
                        returnString = returnString.Replace("-", string.Empty);
                        return (returnString);
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $" : {ex.Message}");
                return ("9901");
            }
        }

        public void StopServer()
        {
            try
            {
                if (IsRunning)
                {
                    _asyncServer.Shutdown(SocketShutdown.Both);
                    _asyncServer.Close();

                }

            }
            catch (SocketException soketex)
            {
                Log.Fatal(soketex, soketex.Message);
                _asyncServer.Close();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }

    }
}

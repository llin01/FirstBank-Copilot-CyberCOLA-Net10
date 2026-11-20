using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using Com.Cybersoft4u.CommonLib.Network.Server;
using NLog.Targets.Wrappers;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Logger = NLog.Logger;


namespace Com.Cybersoft4u.Simulator.Wpf.WpfHSMSimulator
{
    /// <summary>
    /// Main window for HSM (Hardware Security Module) simulator.
    /// Simulates HSM cryptographic operations for testing secure transaction processing.
    /// </summary>
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
        static Thread[] _threadHSMServer;
        static AsynTcpServer[] _asyncTcpServer;
        static CancellationTokenSource[] _serverCts;
        private string _autoStartListenService;

        public MainWindow()
        {
            try
            {
                _autoStartListenService = (Application.Current as Simulator.Wpf.WpfHSMSimulator.App)?.AutoStartListenService;
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
                _sessionNumber = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SessionNumber").Trim());
                _listenPortNumber = stringPortNumber.Split(',');
                _listenPortNumberCount = _listenPortNumber.Count();
                // Replies are built in AsynTcpServer in the layout SocketClient.dll reads; no response file is used.
                var message = $"Listen PortNumber[{stringPortNumber}]";
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

                _threadHSMServer = new Thread[_listenPortNumberCount];
                _asyncTcpServer = new AsynTcpServer[_listenPortNumberCount];
                _serverCts = new CancellationTokenSource[_listenPortNumberCount];
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
                    _serverCts[index] = new CancellationTokenSource();
                    _asyncTcpServer[index] = new AsynTcpServer(_serverCts[index].Token)
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
                    // Signal cancellation instead of Thread.Abort (obsolete in .NET 6+).
                    _serverCts[index]?.Cancel();
                    _asyncTcpServer[index]?.StopServer();
                    // Give the listener thread a moment to exit gracefully.
                    if (_threadHSMServer[index] != null && _threadHSMServer[index].IsAlive)
                    {
                        _threadHSMServer[index].Join(TimeSpan.FromSeconds(2));
                    }
                    var message = $"HSMServer[{_threadHSMServer[index]?.Name}] stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
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
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteTo(_wrapper);
            });
        }

        private void BtnCommand4_Click(object sender, RoutedEventArgs e)
        {
            LogManager.Setup().LoadConfiguration(builder =>
            {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteTo(_wrapper);
            });
            Log.Info("Current level set to ");
        }
    }
}

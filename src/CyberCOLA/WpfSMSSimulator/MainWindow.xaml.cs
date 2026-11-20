using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace Com.Cybersoft4u.Simulator.Wpf.WpfSMSSimulator
{
    /// <summary>
    /// Main window for SMS simulator application.
    /// Provides UI for testing SMS message sending and receiving functionality.
    /// Simulates HSMS server socket listener for development and testing purposes.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private SMSSocketListener _smsSocketListener;
        private string _serverIPAddr;
        private int _serverListenerPort;
        private int _maxConnection;
        private int _maxTimeoutMilliseconds;
        private int _bufferSize;


        private readonly string _startListenerPortNumber;
        private readonly string _autoStartListenService;

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
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _serverIPAddr = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-server-name"), AddressFamily.InterNetwork);
                if (!(string.IsNullOrEmpty(_startListenerPortNumber)))
                {
                    _serverListenerPort = Convert.ToInt32(_startListenerPortNumber);
                    TextBoxListenerPort.Text = Convert.ToString(_serverListenerPort);
                }
                else
                {
                    _serverListenerPort = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-listen-port").Trim());
                    TextBoxListenerPort.Text = Convert.ToString(_serverListenerPort);
                }
                _maxConnection = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-max-connection").Trim());
                _bufferSize = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-buffer-size").Trim());
                _maxTimeoutMilliseconds = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "server-service-max-timeout-milliseconds").Trim());
                ButtonStart.IsEnabled = true;
                ButtonStop.IsEnabled = false;
                if (!(string.IsNullOrEmpty(_autoStartListenService)) &&
                     _autoStartListenService.Trim().Equals("AUTO"))
                    ButtonStart.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                ButtonStart.IsEnabled = false;
                ButtonStop.IsEnabled = false;
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

        private void ButtonClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            Close();
            Environment.Exit(0);
        }


        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TextBlockMessage.Text = "Start Service";
            try
            {
                var serverServiceEndPoint = new IPEndPoint(IPAddress.Parse(_serverIPAddr), _serverListenerPort);
                _smsSocketListener = new SMSSocketListener(serverServiceEndPoint, _maxConnection, _bufferSize);
                _smsSocketListener.Start();
                SetMessageText(RichTextBox2, $"Server Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, IP Address[{_serverIPAddr}], Listener Port[{_serverListenerPort}]", Brushes.Red);
                ButtonStart.IsEnabled = false;
                ButtonStop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                ButtonStart.IsEnabled = false;
                ButtonStop.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            _smsSocketListener.Stop();
            SetMessageText(RichTextBox2, $"Server Service stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Listener Port[{_serverListenerPort}]", Brushes.Red);
            ButtonStart.IsEnabled = true;
            ButtonStop.IsEnabled = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}

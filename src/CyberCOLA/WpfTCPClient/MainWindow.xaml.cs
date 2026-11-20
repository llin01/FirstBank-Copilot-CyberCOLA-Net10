using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.ServiceRouter;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Logger = NLog.Logger;

namespace Com.Cybersoft4u.Test.Wpf.WpfTCPClient
{
    /// <summary>
    /// Main window for TCP client test application.
    /// Provides UI for monitoring and testing TCP client connections to authorization services.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up the dispatcher timer for time display and initializes system parameters.
        /// </summary>
        public MainWindow()
        {
            try
            {
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
        /// <summary>
        /// Handles the dispatcher timer tick event to update the time display.
        /// Updates the time label every second and forces command manager to refresh.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            this.TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Initializes system parameters and starts the TCP client monitor service if configured.
        /// Loads configuration, sets up logging, and establishes TCP client connection.
        /// </summary>
        private void InitialSystemParameter()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                if (configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-auto-start-flag").ToUpper().Equals("TRUE"))
                {
                    SetMessageText(RichTextBox2, $"Client Monitor Service start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    var remoteHostName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-remote-hostname").Trim();
                    var remoteIPAddressString = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-target-ip").Trim(), AddressFamily.InterNetwork);
                    var remotePortNumberString = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-target-port").Trim();
                    var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddressString), Convert.ToInt32(remotePortNumberString));
                    var localIPAddressString = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-local-ip").Trim(), AddressFamily.InterNetwork);
                    var localPortNumberString = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "client-monitor-service-local-port").Trim();
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(localIPAddressString), Convert.ToInt32(localPortNumberString));

                    var clientMonitorService = new TCPClientService(remoteHostName, remoteEndPoint, localEndPoint);
                    clientMonitorService.EnableLogger("ClientAuthClientMonitor", Com.Cybersoft4u.Utils.LogLevel.Debug);
                    clientMonitorService.Start();
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
        /// <summary>
        /// Sets message text in a RichTextBox control with specified brush color.
        /// Handles thread-safe UI updates using dispatcher if necessary.
        /// </summary>
        /// <param name="richTextBox">The RichTextBox control to update.</param>
        /// <param name="message">The message text to display.</param>
        /// <param name="brush">The brush color for the text.</param>
        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            RichTextBox rtb = richTextBox as RichTextBox;
            if (rtb == null) return;
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

        /// <summary>
        /// Handles the clean message button click event.
        /// Clears all content from the message RichTextBox controls.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Clear Message Content";
            RichTextBox1.Document.Blocks.Clear();
            RichTextBox2.Document.Blocks.Clear();
        }

        /// <summary>
        /// Handles the close button click event.
        /// Closes the main window and terminates the application.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            this.Close();
        }

        /// <summary>
        /// Handles the command 1 button click event.
        /// Placeholder for future command functionality.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnCommand1_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

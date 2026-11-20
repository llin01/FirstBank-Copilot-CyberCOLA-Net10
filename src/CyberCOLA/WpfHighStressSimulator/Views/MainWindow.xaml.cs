using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Simulator.Wpf.WpfHighStressSimulator.ViewModels;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfHighStressSimulator.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private readonly MainViewModel _viewModel;
        private readonly string? _startListenerPortNumber;
        private string? _autoStartListenService;
        private Thread? _udpListenerThread;
        private UdpClient? _udpClient;
        private bool _isStopUdpServer;
        private int _udpPortNumber = 8011;

        public MainWindow()
        {
            _startListenerPortNumber = (Application.Current as App)?.StartListenerPortNumber;
            _autoStartListenService = (Application.Current as App)?.AutoStartListenService;

            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Wire UI callbacks
            _viewModel.LogAction = (msg) => LogToTerminal(RichTextBox1, msg, Brushes.Green);
            _viewModel.ShowFileSelector = (files) =>
            {
                // If auto-start mode is configured, automatically load all files without blocking on UI
                if (!string.IsNullOrEmpty(_autoStartListenService) &&
                    _autoStartListenService.Trim().Equals("AUTO", StringComparison.OrdinalIgnoreCase))
                {
                    _autoStartListenService = null; // Clear the flag immediately upon first usage
                    return files;
                }

                var selector = new SelectFileWindow(files, _viewModel.TransactionType) { Owner = this };
                if (selector.ShowDialog() == true)
                {
                    return selector.GetFilePaths();
                }
                return null;
            };

            // Start standard UI clock timer
            var clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clockTimer.Tick += (s, e) => TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            clockTimer.Start();

            // Load UDP port configuration safely
            var config = new ConfigurationLoaderRoot();
            int.TryParse(config.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPPortNumber"), out int port);
            if (port > 0) _udpPortNumber = port;

            // Start UDP background log listener
            StartUdpListener();

            // Handle dynamic window icon updates based on selected transaction type
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateWindowIcon(_viewModel.TransactionType);

            // Register Loaded event to execute dynamic command line startup sequences safely
            Loaded += MainWindow_Loaded;

            // Initialize default log directory for Log Analyzer
            InitializeLogAnalyzerDir();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.TransactionType))
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateWindowIcon(_viewModel.TransactionType)));
            }
        }

        private void UpdateWindowIcon(string transactionType)
        {
            try
            {
                string iconName = transactionType.ToUpper() switch
                {
                    "FISC" => "Fisc.ico",
                    "IAG" => "CreditCard.ico",
                    "MASTERCARD" => "MasterCard.ico",
                    "NCCC" => "Nccc.ico",
                    "NCPS" => "Ncps.ico",
                    "STIN" => "CreditCard.ico",
                    "TWIN" => "Twin.ico",
                    "TWMP" => "Twmp.ico",
                    "VISA" => "Visa.ico",
                    _ => "TCPIP.ico"
                };

                var uri = new Uri($"pack://application:,,,/{iconName}", UriKind.RelativeOrAbsolute);
                Icon = System.Windows.Media.Imaging.BitmapFrame.Create(uri);

                // Dynamic update window title to show active transaction protocol type
                Title = $"CyberCOLA TCP Simulator - [{transactionType.ToUpper()}]";
            }
            catch
            {
                // Graceful fallback to default TCPIP icon
                try
                {
                    Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/TCPIP.ico", UriKind.RelativeOrAbsolute));
                }
                catch { /* Fallback failed, ignore icon load errors */ }
            }
        }

        private void StartUdpListener()
        {
            _isStopUdpServer = false;
            _udpListenerThread = new Thread(ReadLogMessage)
            {
                IsBackground = true,
                Name = "UDP-Log-Listener"
            };
            _udpListenerThread.Start();
        }

        private void ReadLogMessage()
        {
            try
            {
                _udpClient = new UdpClient(_udpPortNumber);
                var groupEp = new IPEndPoint(IPAddress.Any, _udpPortNumber);

                while (!_isStopUdpServer)
                {
                    if (_udpClient.Available <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    byte[] bytes = _udpClient.Receive(ref groupEp);
                    string rawMessage = Encoding.Default.GetString(bytes);

                    // Sort logs: Trace/Hex messages go to RichTextBox2 (blue), system logs go to RichTextBox1 (green)
                    if (rawMessage.Contains("Server Receive Data") || rawMessage.Contains("Write Data") || rawMessage.Contains("Server Receive") || rawMessage.Contains("Write Data["))
                    {
                        SetMessageText(RichTextBox2, rawMessage, Brushes.Cyan);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, rawMessage, Brushes.GreenYellow);
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $"[UDP Listener Error] {ex.Message}", Brushes.Red);
            }
            finally
            {
                _udpClient?.Close();
            }
        }

        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            if (richTextBox is not RichTextBox rtb) return;

            if (rtb.Dispatcher.CheckAccess())
            {
                var range = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd)
                {
                    Text = message + "\r"
                };
                range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Medium);
                rtb.ScrollToEnd();
            }
            else
            {
                rtb.Dispatcher.BeginInvoke(new UpdateRichTextBox(SetMessageText), richTextBox, message, brush);
            }
        }

        private void LogToTerminal(Control richTextBox, string message, Brush brush)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}] {message}";
            SetMessageText(richTextBox, formatted, brush);
        }

        private void ButtonCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            RichTextBox1.Document.Blocks.Clear();
            RichTextBox2.Document.Blocks.Clear();
            _viewModel.StatusMessage = "Message Content Cleared";
        }

        private void ButtonClosed_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonShutdown_Click(object sender, RoutedEventArgs e)
        {
            PerformShutdown();
        }

        private void PerformShutdown()
        {
            try
            {
                _viewModel.ShutdownCleanup();
                StopUdpService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Shutdown error: {ex.Message}", "Shutdown", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Close();
                Environment.Exit(0);
            }
        }

        private void StopUdpService()
        {
            _isStopUdpServer = true;
            try
            {
                using var client = new UdpClient();
                var ep = new IPEndPoint(IPAddress.Loopback, _udpPortNumber);
                byte[] quitBytes = Encoding.ASCII.GetBytes("Quit");
                client.Send(quitBytes, quitBytes.Length, ep);
            }
            catch
            {
                // Graceful fail on quit push
            }
            _udpClient?.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _viewModel.ShutdownCleanup();
            StopUdpService();
            Environment.Exit(0);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Check and set custom command-line port
            if (!string.IsNullOrEmpty(_startListenerPortNumber))
            {
                if (!_viewModel.AvailablePorts.Contains(_startListenerPortNumber))
                {
                    _viewModel.AvailablePorts.Add(_startListenerPortNumber);
                }
                _viewModel.ListenPort = _startListenerPortNumber;
            }

            // 2. If AUTO-start argument is supplied, ingest payloads and start listener dynamically
            if (!string.IsNullOrEmpty(_autoStartListenService) &&
                _autoStartListenService.Trim().Equals("AUTO", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Load all template files and ingest payloads without UI prompts in auto-start mode
                    _viewModel.LoadFilesCommand.Execute(null);
                    _viewModel.IngestPayloadsCommand.Execute(null);

                    // Start the TCP server listening service immediately
                    if (_viewModel.StartCommand.CanExecute(null))
                    {
                        _viewModel.StartCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    LogToTerminal(RichTextBox1, $"[AutoStart Error] {ex.Message}", Brushes.Red);
                }
                finally
                {
                    // Always clear the AUTO flag to ensure subsequent manual clicks will prompt the UI dialog
                    _autoStartListenService = null;
                }
            }
        }

        private void InitializeLogAnalyzerDir()
        {
            // The repository's LogFiles when the program runs inside the repo, otherwise its own Log folder.
            TextBoxLogDir.Text = CyberCOLAPaths.ResolveDirectory("Log", "LogFiles");
        }

        private void ButtonBrowseLogDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Log Directory",
                InitialDirectory = TextBoxLogDir.Text
            };
            if (dialog.ShowDialog() == true)
            {
                TextBoxLogDir.Text = dialog.FolderName;
            }
        }

        private async void ButtonScanLogs_Click(object sender, RoutedEventArgs e)
        {
            string logDir = TextBoxLogDir.Text;
            if (string.IsNullOrWhiteSpace(logDir) || !Directory.Exists(logDir))
            {
                MessageBox.Show("Please specify a valid log directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ButtonScanLogs.IsEnabled = false;
            RichTextBoxScanResults.Document.Blocks.Clear();

            AppendScanText($"[Log Analyzer] Starting log scan in: {logDir}...\r\n", Brushes.Cyan, FontWeights.Bold);

            await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(logDir, "*.log", SearchOption.AllDirectories);
                    int totalMatches = 0;

                    foreach (var file in files)
                    {
                        var matchedLines = new List<string>();
                        try
                        {
                            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var sr = new StreamReader(fs))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    if (Regex.IsMatch(line, @"\b(WARN|ERROR|FATAL)\b", RegexOptions.IgnoreCase))
                                    {
                                        matchedLines.Add(line);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignore locked files gracefully
                            continue;
                        }

                        if (matchedLines.Count > 0)
                        {
                            totalMatches += matchedLines.Count;

                            Dispatcher.Invoke(() =>
                            {
                                AppendScanText($"\n>>> File: {file} <<<\n", Brushes.Yellow, FontWeights.Bold);
                                foreach (var m in matchedLines)
                                {
                                    Brush color = Brushes.Yellow;
                                    if (Regex.IsMatch(m, @"\b(FATAL)\b", RegexOptions.IgnoreCase))
                                    {
                                        color = Brushes.Red;
                                    }
                                    else if (Regex.IsMatch(m, @"\b(ERROR)\b", RegexOptions.IgnoreCase))
                                    {
                                        color = new SolidColorBrush(Color.FromRgb(255, 100, 100)); // Visible light red
                                    }
                                    AppendScanText($"  {m}\n", color, FontWeights.Normal);
                                }
                            });
                        }
                    }

                    Dispatcher.Invoke(() =>
                    {
                        if (totalMatches == 0)
                        {
                            AppendScanText("\n[Success] No WARN, ERROR, or FATAL messages found in any log files.\n", Brushes.Green, FontWeights.Bold);
                        }
                        else
                        {
                            AppendScanText($"\n[Done] Found {totalMatches} matching messages.\n", Brushes.Cyan, FontWeights.Bold);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendScanText($"\n[Scan Error] {ex.Message}\n", Brushes.Red, FontWeights.Bold);
                    });
                }
            });

            ButtonScanLogs.IsEnabled = true;
        }

        private void ButtonClearScanResults_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxScanResults.Document.Blocks.Clear();
            AppendScanText("[Log Analyzer Cleared]\n", Brushes.Gray, FontWeights.Normal);
        }

        private void AppendScanText(string message, Brush brush, FontWeight fontWeight)
        {
            var range = new TextRange(RichTextBoxScanResults.Document.ContentEnd, RichTextBoxScanResults.Document.ContentEnd)
            {
                Text = message
            };
            range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            range.ApplyPropertyValue(TextElement.FontWeightProperty, fontWeight);
            RichTextBoxScanResults.ScrollToEnd();
        }
    }
}

using Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.Views;
using NLog;
using System.Windows;
using System.Windows.Media;

namespace Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester
{
    public partial class MainWindow : Window
    {
        private readonly SimulationService _simService = new SimulationService();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            Log.Info("WpfMaintGatewayTester MainWindow initialized.");
            MainFrame.Navigate(new MaintGatewayTesterPage());
            UpdateButtonStates(SimulationMode.Basic);
        }

        private void BtnMaint_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MaintGatewayTesterPage());
            UpdateButtonStates(SimulationMode.Basic);
        }

        private void BtnMaintHttps_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MaintGatewayTesterHttpsPage());
            UpdateButtonStates(SimulationMode.Https);
        }

        private void BtnMaintJwt_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MaintGatewayTesterJwtPage());
            UpdateButtonStates(SimulationMode.Jwt);
        }

        private void UpdateButtonStates(SimulationMode mode)
        {
            BtnSimBasic.IsEnabled = (mode == SimulationMode.Basic);
            BtnSimHttps.IsEnabled = (mode == SimulationMode.Https);
            BtnSimJwt.IsEnabled = (mode == SimulationMode.Jwt);

            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(200);
                Dispatcher.Invoke(() =>
                {
                    var currentPage = MainFrame.Content as System.Windows.Controls.Page;
                    if (currentPage == null) return;

                    var btnTrigger = FindVisualChild<System.Windows.Controls.Button>(currentPage, "BtnTrigger");
                    var btnSend = FindVisualChild<System.Windows.Controls.Button>(currentPage, "BtnSend");

                    if (btnTrigger != null) btnTrigger.IsEnabled = true;
                    if (btnSend != null) btnSend.IsEnabled = true;
                });
            });
        }

        private async void BtnSimBasic_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSimulation(SimulationMode.Basic);
        }

        private async void BtnSimHttps_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSimulation(SimulationMode.Https);
        }

        private async void BtnSimJwt_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSimulation(SimulationMode.Jwt);
        }

        private async System.Threading.Tasks.Task ExecuteSimulation(SimulationMode mode)
        {
            switch (mode)
            {
                case SimulationMode.Basic: MainFrame.Navigate(new MaintGatewayTesterPage()); break;
                case SimulationMode.Https: MainFrame.Navigate(new MaintGatewayTesterHttpsPage()); break;
                case SimulationMode.Jwt: MainFrame.Navigate(new MaintGatewayTesterJwtPage()); break;
            }

            await System.Threading.Tasks.Task.Delay(200);

            var currentPage = MainFrame.Content as System.Windows.Controls.Page;
            if (currentPage == null) return;

            try
            {
                string targetUrl;
                string configKey = mode switch
                {
                    SimulationMode.Jwt => "GatewaySettings:JwtUrl",
                    SimulationMode.Basic => "GatewaySettings:BasicUrl",
                    SimulationMode.Https => "GatewaySettings:HttpsUrl",
                    _ => ""
                };
                targetUrl = ConfigHelper.Configuration?[configKey] ?? string.Empty;

                if (string.IsNullOrEmpty(targetUrl))
                {
                    MessageBox.Show($"Configuration URL not found.");
                    return;
                }

                var urlTextBox = FindVisualChild<System.Windows.Controls.TextBox>(currentPage, "TxtUrl");
                if (urlTextBox != null)
                {
                    urlTextBox.Text = targetUrl;
                    urlTextBox.Focus();
                }
                else
                {
                    MessageBox.Show($"Could not find TxtUrl on the page.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting URL: {ex.Message}");
            }
        }

        private TElement? FindVisualChild<TElement>(System.Windows.Controls.Page parent, string childName) where TElement : System.Windows.UIElement
        {
            if (parent.Content == null) return null;
            var results = FindVisualChildren<TElement>(parent.Content as DependencyObject);
            if (results == null) return null;

            foreach (var element in results)
            {
                if ((element as FrameworkElement)?.Name == childName)
                    return element;
            }
            return null;
        }

        private System.Collections.Generic.IEnumerable<TElement> FindVisualChildren<TElement>(DependencyObject? obj) where TElement : System.Windows.UIElement
        {
            if (obj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is TElement t) yield return t;
                foreach (var childOfChild in FindVisualChildren<TElement>(child))
                    yield return childOfChild;
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

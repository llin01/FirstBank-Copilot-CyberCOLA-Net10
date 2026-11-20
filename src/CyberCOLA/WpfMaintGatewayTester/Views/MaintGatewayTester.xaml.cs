using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.Views
{
    public partial class MaintGatewayTesterPage : Page
    {
        private static readonly HttpClient client = new HttpClient();

        public MaintGatewayTesterPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            TxtRequest.Text = Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.ConfigHelper.GetXmlTestDataAsJson();
            var httpUrl = Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester.ConfigHelper.Configuration?["TesterSettings:HttpProxyUrl"];
            if (!string.IsNullOrEmpty(httpUrl))
            {
                TxtUrl.Text = httpUrl;
            }
        }

        private async void BtnTrigger_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text;
            try
            {
                var response = await client.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();
                TxtResponse.Text = $"Trigger Status: {response.StatusCode}\n\n{responseString}";
            }
            catch (Exception ex)
            {
                TxtResponse.Text = $"Trigger Error: {ex.Message}";
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text;
            string body = TxtRequest.Text;

            try
            {
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                TxtResponse.Text = $"Status: {response.StatusCode}\n\n{responseString}";
            }
            catch (Exception ex)
            {
                TxtResponse.Text = $"Error: {ex.Message}";
            }
        }
    }
}

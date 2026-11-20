using System.Windows;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfVDMQSimulator
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public string AutoStartService { get; set; }
        protected override void OnStartup(StartupEventArgs e)
        {

            if (e.Args.Length == 1)
            {
                AutoStartService = e.Args[0].ToString();
            }
            base.OnStartup(e);
        }
    }
}

using System.Windows;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfEAISimulator
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public string StartListenerPortNumber { get; set; }
        public string AutoStartListenService { get; set; }
        protected override void OnStartup(StartupEventArgs e)
        {

            if (e.Args.Length == 2)
            {
                StartListenerPortNumber = e.Args[0].ToString();
                AutoStartListenService = e.Args[1].ToString();
            }
            base.OnStartup(e);
        }
    }
}

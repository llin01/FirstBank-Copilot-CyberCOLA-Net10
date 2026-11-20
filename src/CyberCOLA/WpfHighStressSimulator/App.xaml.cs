using System.Windows;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfHighStressSimulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string? StartListenerPortNumber { get; set; }
        public string? AutoStartListenService { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length == 2)
            {
                StartListenerPortNumber = e.Args[0];
                AutoStartListenService = e.Args[1];
            }
            base.OnStartup(e);
        }
    }
}

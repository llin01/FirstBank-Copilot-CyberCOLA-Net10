using System.Windows;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfETicketClientSimulator
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public string SessionCount { get; set; }
        public string AutoStartListenService { get; set; }
        public string ETicketType { get; set; }
        // Set by "<SessionCount> AUTO <ETicketType> SEND <RepeatCount> <IntervalMs>": once connected, send every
        // template without anyone at the window (tools\Test-AuthorizationSystem.ps1 -AutoSend).
        public int? AutoSendRepeatCount { get; set; }
        public int AutoSendIntervalMs { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {

            if (e.Args.Length >= 3)
            {
                SessionCount = e.Args[0].ToString();
                AutoStartListenService = e.Args[1].ToString();
                ETicketType = e.Args[2].ToString();
            }
            if (e.Args.Length == 6 && e.Args[3].Equals("SEND", System.StringComparison.OrdinalIgnoreCase)
                && int.TryParse(e.Args[4], out int repeatCount) && repeatCount > 0
                && int.TryParse(e.Args[5], out int intervalMs) && intervalMs >= 0)
            {
                AutoSendRepeatCount = repeatCount;
                AutoSendIntervalMs = intervalMs;
            }
            base.OnStartup(e);
        }
    }
}

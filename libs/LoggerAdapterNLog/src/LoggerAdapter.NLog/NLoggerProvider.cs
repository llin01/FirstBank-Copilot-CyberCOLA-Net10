// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NLoggerProvider.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NLoggerProvider type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.Utils.NLog
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.Utils;

    #endregion

    public class NLoggerProvider : LoggerProvider
    {
        private static int exitHooked;

        public NLoggerProvider()
        {
            // Every service creates this provider at start-up. NLog.config is loaded automatically, so mask the
            // output of all its targets here (and again whenever the configuration is reloaded) - PCI DSS.
            CardDataMaskingLayout.Apply(global::NLog.LogManager.Configuration);
            global::NLog.LogManager.ConfigurationChanged += (sender, e) => CardDataMaskingLayout.Apply(e.ActivatedConfiguration);

            // NLog shuts its targets down on process exit by itself, possibly before the adapter's queue is written,
            // and everything still queued (a service's stop sequence under load) was then dropped. Write the queue
            // first, then shut NLog down.
            if (System.Threading.Interlocked.Exchange(ref exitHooked, 1) == 0)
            {
                global::NLog.LogManager.AutoShutdown = false;
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Logger.Flush(TimeSpan.FromSeconds(5));
                    global::NLog.LogManager.Shutdown();
                };
            }
        }

        public override LoggerProvider ConfigureFilePath(string path)
        {
            return this;
        }

        public override Logger CreateLogger(string name)
        {
            return new NLogger(name);
        }

        public override Logger CreateLogger(Type type)
        {
            return new NLogger(type);
        }
    }
}

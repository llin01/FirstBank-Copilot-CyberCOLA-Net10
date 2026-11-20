// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoggerAdapter.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the LoggerAdapter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.Utils
{
    #region DEPENDENCY

    using System;

    #endregion

    public class LoggerAdapter
    {
        private const string DefaultConfigureFilePath = "log4net.config";

        private static readonly object Locker = new object();

        private static LoggerProvider loggerProvider;

        public static Logger GetLogger(string name)
        {
            CheckAndCreateDefaultInstance();
            return loggerProvider.CreateLogger(name);
        }

        public static Logger GetLogger(Type type)
        {
            CheckAndCreateDefaultInstance();
            return loggerProvider.CreateLogger(type);
        }

        public static LoggerProvider LoggerProvider()
        {
            return loggerProvider;
        }

        public static void ReBuild(LoggerProvider loggerProvider, string logConfigureFilePath)
        {
            lock (Locker)
            {
                if (LoggerAdapter.loggerProvider != null)
                {
                    return;
                }

                LoggerAdapter.loggerProvider = loggerProvider;
                LoggerAdapter.loggerProvider.ConfigureFilePath(logConfigureFilePath);
            }
        }

        private static void CheckAndCreateDefaultInstance()
        {
            if (loggerProvider != null)
            {
                return;
            }

            ReBuild(new NullLoggerProvider(), DefaultConfigureFilePath);
        }
    }

    internal class NullLoggerProvider : LoggerProvider
    {
        public override LoggerProvider ConfigureFilePath(string path) => this;
        public override Logger CreateLogger(string name) => new NullLogger(name);
        public override Logger CreateLogger(Type type) => new NullLogger(type);
    }

    internal class NullLogger : Logger
    {
        public NullLogger(string name) : base(name) { }
        public NullLogger(Type type) : base(type) { }
        protected override bool LoggerDebugEnabled() => false;
        protected override bool LoggerInfoEnabled() => false;
        protected override bool LoggerWarnEnabled() => false;
        protected override bool LoggerErrorEnabled() => false;
        protected override bool LoggerFatalEnabled() => false;
        protected override void WriteMessage(LogMessage logMessage) { }
        protected override void WriteMessageException(LogMessage logMessage) { }
        protected override void WriteMessageTask(LogMessage logMessage) { }
    }
}

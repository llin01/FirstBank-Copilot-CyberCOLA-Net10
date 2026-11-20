// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NLogger.cs">
//   Author: Yeti
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.Utils.NLog
{
    #region DEPENDENCY

    using System;

    using global::NLog;

    using Logger = Com.Cybersoft4u.Utils.Logger;

    #endregion

    public class NLogger : Logger
    {
        private readonly global::NLog.Logger logger;

        public NLogger(Type type)
            : base(type)
        {
            this.logger = LogManager.LogFactory.GetLogger(type.Name);
        }

        public NLogger(string name)
            : base(name)
        {
            this.logger = LogManager.GetLogger(name);
        }

        protected override bool LoggerDebugEnabled()
        {
            return this.logger.IsDebugEnabled;
        }

        protected override bool LoggerInfoEnabled()
        {
            return this.logger.IsInfoEnabled;
        }

        protected override bool LoggerWarnEnabled()
        {
            return this.logger.IsWarnEnabled;
        }

        protected override bool LoggerErrorEnabled()
        {
            return this.logger.IsErrorEnabled;
        }

        protected override bool LoggerFatalEnabled()
        {
            return this.logger.IsFatalEnabled;
        }

        protected override void WriteMessage(LogMessage logMessage)
        {
            switch (logMessage.LogLevel)
            {
                case Utils.LogLevel.Debug:
                    this.logger.Debug(logMessage.Message);
                    break;
                case Utils.LogLevel.Info:
                    this.logger.Info(logMessage.Message);
                    break;
                case Utils.LogLevel.Warn:
                    this.logger.Warn(logMessage.Message);
                    break;
                case Utils.LogLevel.Error:
                    this.logger.Error(logMessage.Message);
                    break;
                case Utils.LogLevel.Fatal:
                    this.logger.Fatal(logMessage.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void WriteMessageException(LogMessage logMessage)
        {
            switch (logMessage.LogLevel)
            {
                case Utils.LogLevel.Debug:
                    this.logger.Debug(logMessage.Exception, logMessage.Message.ToString());
                    break;
                case Utils.LogLevel.Info:
                    this.logger.Info(logMessage.Exception, logMessage.Message.ToString());
                    break;
                case Utils.LogLevel.Warn:
                    this.logger.Warn(logMessage.Exception, logMessage.Message.ToString());
                    break;
                case Utils.LogLevel.Error:
                    this.logger.Error(logMessage.Exception, logMessage.Message.ToString());
                    break;
                case Utils.LogLevel.Fatal:
                    this.logger.Fatal(logMessage.Exception, logMessage.Message.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void WriteMessageTask(LogMessage logMessage)
        {
            try
            {
                switch (logMessage.LogLevel)
                {
                    case Utils.LogLevel.Debug:
                        this.logger.Debug(logMessage.LogTask());
                        break;
                    case Utils.LogLevel.Info:
                        this.logger.Info(logMessage.LogTask());
                        break;
                    case Utils.LogLevel.Warn:
                        this.logger.Warn(logMessage.LogTask());
                        break;
                    case Utils.LogLevel.Error:
                        this.logger.Error(logMessage.LogTask());
                        break;
                    case Utils.LogLevel.Fatal:
                        this.logger.Fatal(logMessage.LogTask());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}

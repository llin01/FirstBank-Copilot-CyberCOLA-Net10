// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Logger.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the LogLevel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.Utils
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    #endregion

    public enum LogLevel
    {
        Debug,

        Info,

        Warn,

        Error,

        Fatal
    }

    public abstract class Logger
    {
        public delegate string LogTask();

        private static readonly BlockingCollection<LogMessage> LogMessages = new BlockingCollection<LogMessage>();

        // Messages queued but not yet written: Flush waits for this to reach zero.
        private static int pendingMessages;

        static Logger()
        {
            ThreadPool.QueueUserWorkItem(state => QueueProcesser());
        }

        /// <summary>
        /// Waits until every message queued so far has been written. Messages are written by a background worker,
        /// so a process that exits right after logging (a service's stop sequence under load) loses the tail
        /// unless the provider calls this before shutting its logging framework down.
        /// </summary>
        /// <returns>false when <paramref name="timeout"/> elapsed first.</returns>
        public static bool Flush(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (Volatile.Read(ref pendingMessages) > 0)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    return false;
                }

                Thread.Sleep(10);
            }

            return true;
        }

        protected internal Logger(Type type)
        {
        }

        protected internal Logger(string name)
        {
        }

        public bool IsDebugEnabled
        {
            get
            {
                return this.LoggerDebugEnabled();
            }
        }

        public bool IsInfoEnabled
        {
            get
            {
                return this.LoggerInfoEnabled();
            }
        }

        public bool IsWarnEnabled
        {
            get
            {
                return this.LoggerWarnEnabled();
            }
        }

        public bool IsErrorEnabled
        {
            get
            {
                return this.LoggerErrorEnabled();
            }
        }

        public bool IsFatalEnabled
        {
            get
            {
                return this.LoggerFatalEnabled();
            }
        }

        public void Debug(object message)
        {
            if (this.IsDebugEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Debug, message, this));
            }
        }

        public void Debug(object message, Exception exception)
        {
            if (this.IsDebugEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Debug, message, this, exception));
            }
        }

        public void Debug(params LogTask[] logTasks)
        {
            if (this.IsDebugEnabled)
            {
                Array.ForEach(logTasks, task => Enqueue(Recycler.GetMessage(LogLevel.Debug, task, this)));
            }
        }

        public void Info(object message)
        {
            if (this.IsInfoEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Info, message, this));
            }
        }

        public void Info(object message, Exception exception)
        {
            if (this.IsInfoEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Info, message, this, exception));
            }
        }

        public void Info(params LogTask[] logTasks)
        {
            if (this.IsInfoEnabled)
            {
                Array.ForEach(logTasks, task => Enqueue(Recycler.GetMessage(LogLevel.Info, task, this)));
            }
        }

        public void Warn(object message)
        {
            if (this.IsWarnEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Warn, message, this));
            }
        }

        public void Warn(object message, Exception exception)
        {
            if (this.IsWarnEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Warn, message, this, exception));
            }
        }

        public void Warn(params LogTask[] logTasks)
        {
            if (this.IsWarnEnabled)
            {
                Array.ForEach(logTasks, task => Enqueue(Recycler.GetMessage(LogLevel.Warn, task, this)));
            }
        }

        public void Error(object message)
        {
            if (this.IsErrorEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Error, message, this));
            }
        }

        public void Error(object message, Exception exception)
        {
            if (this.IsErrorEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Error, message, this, exception));
            }
        }

        public void Error(params LogTask[] logTasks)
        {
            if (this.IsErrorEnabled)
            {
                Array.ForEach(logTasks, task => Enqueue(Recycler.GetMessage(LogLevel.Error, task, this)));
            }
        }

        public void Fatal(object message)
        {
            if (this.IsFatalEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Fatal, message, this));
            }
        }

        public void Fatal(object message, Exception exception)
        {
            if (this.IsFatalEnabled)
            {
                Enqueue(Recycler.GetMessage(LogLevel.Fatal, message, this, exception));
            }
        }

        public void Fatal(params LogTask[] logTasks)
        {
            if (this.IsFatalEnabled)
            {
                Array.ForEach(logTasks, task => Enqueue(Recycler.GetMessage(LogLevel.Fatal, task, this)));
            }
        }

        protected abstract bool LoggerDebugEnabled();

        protected abstract bool LoggerInfoEnabled();

        protected abstract bool LoggerWarnEnabled();

        protected abstract bool LoggerErrorEnabled();

        protected abstract bool LoggerFatalEnabled();

        protected abstract void WriteMessage(LogMessage logMessage);

        protected abstract void WriteMessageException(LogMessage logMessage);

        protected abstract void WriteMessageTask(LogMessage logMessage);

        private static void QueueProcesser()
        {
            while (true)
            {
                LogMessage tmp;
                if (!LogMessages.TryTake(out tmp, 1000))
                {
                    break;
                }

                try
                {
                    switch (tmp.MessageType)
                    {
                        case LogMessage.Type.Message:
                            tmp.Logger.WriteMessage(tmp);
                            break;
                        case LogMessage.Type.MessageException:
                            tmp.Logger.WriteMessageException(tmp);
                            break;
                        case LogMessage.Type.Task:
                            tmp.Logger.WriteMessageTask(tmp);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Recycler.Recycle(tmp);
                }
                finally
                {
                    Interlocked.Decrement(ref pendingMessages);
                }
            }

            ThreadPool.QueueUserWorkItem(state => QueueProcesser());
        }

        private static void Enqueue(LogMessage item)
        {
            // Counted before it is added, so the worker can never decrement it first.
            Interlocked.Increment(ref pendingMessages);
            switch (item.LogLevel)
            {
                case LogLevel.Debug:
                    LogMessages.Add(item);
                    break;
                case LogLevel.Info:
                    LogMessages.Add(item);
                    break;
                case LogLevel.Warn:
                    LogMessages.Add(item);
                    break;
                case LogLevel.Error:
                    LogMessages.Add(item);
                    break;
                case LogLevel.Fatal:
                    LogMessages.Add(item);
                    break;
                default:
                    Interlocked.Decrement(ref pendingMessages);
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected class LogMessage
        {
            public enum Type
            {
                Message,

                MessageException,

                Task
            }

            public LogMessage()
            {
            }

            public LogMessage(LogLevel logLevel, object message, Logger logger, Exception exception = null)
            {
                this.LogLevel = logLevel;
                this.MessageType = exception == null ? Type.Message : Type.MessageException;
                this.Exception = exception;
                this.Message = message;
                this.Logger = logger;
            }

            public LogMessage(LogLevel logLevel, LogTask logTask, Logger logger)
            {
                this.LogLevel = logLevel;
                this.MessageType = Type.Task;
                this.LogTask = logTask;
                this.Logger = logger;
            }

            public Logger Logger { get; internal set; }

            public object Message { get; internal set; }

            public LogTask LogTask { get; internal set; }

            public Type MessageType { get; internal set; }

            public LogLevel LogLevel { get; internal set; }

            public Exception Exception { get; internal set; }

            internal LogMessage Recycle()
            {
                this.LogLevel = LogLevel.Info;
                this.MessageType = Type.Message;
                this.Exception = null;
                this.Message = null;
                this.Logger = null;
                this.LogTask = null;
                return this;
            }
        }

        private class Recycler
        {
            private static readonly ConcurrentQueue<LogMessage> Messages = new ConcurrentQueue<LogMessage>();

            public static LogMessage GetMessage(
                LogLevel logLevel,
                object message,
                Logger logger,
                Exception exception = null)
            {
                LogMessage logMessage;
                if (!Messages.TryDequeue(out logMessage))
                {
                    logMessage = new LogMessage(logLevel, message, logger, exception);
                }
                else
                {
                    logMessage.LogLevel = logLevel;
                    logMessage.MessageType = exception == null
                                                 ? LogMessage.Type.Message
                                                 : LogMessage.Type.MessageException;
                    logMessage.Exception = exception;
                    logMessage.Message = message;
                    logMessage.Logger = logger;
                }

                return logMessage;
            }

            public static LogMessage GetMessage(LogLevel logLevel, LogTask logTask, Logger logger)
            {
                LogMessage logMessage;
                if (!Messages.TryDequeue(out logMessage))
                {
                    logMessage = new LogMessage(logLevel, logTask, logger);
                }
                else
                {
                    logMessage.LogLevel = logLevel;
                    logMessage.MessageType = LogMessage.Type.Task;
                    logMessage.LogTask = logTask;
                    logMessage.Logger = logger;
                }

                return logMessage;
            }

            public static void Recycle(LogMessage logMessage)
            {
                Messages.Enqueue(logMessage.Recycle());
            }
        }
    }
}

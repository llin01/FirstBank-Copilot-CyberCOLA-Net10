namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;
    using System.Diagnostics;

    using Com.Cybersoft4u.Utils;

    #endregion

    public class TprsException : SystemException
    {
        private readonly bool printTrace;

        public TprsException(bool printTrace = true, bool silent = false)
            : base(string.Empty)
        {
            this.printTrace = printTrace;
            if (!silent)
            {
                //LoggerAdapter.GetLogger(this.GetType()).Error(this.Message);
                LoggerAdapter.GetLogger(this.GetType().FullName).Error(this.Message);
            }

        }

        public TprsException(string message, bool printTrace = true)
            : base(message)
        {
            this.printTrace = printTrace;
            //LoggerAdapter.GetLogger(this.GetType()).Error(this.Message);
            LoggerAdapter.GetLogger(this.GetType().FullName).Error(this.Message);
        }

        public TprsException(Exception exception, bool printTrace = true)
            : base(exception.Message, exception)
        {
            this.printTrace = printTrace;
            //LoggerAdapter.GetLogger(this.GetType()).Error(this.Message);
            LoggerAdapter.GetLogger(this.GetType().FullName).Error(this.Message);
        }

        public override sealed string Message
        {
            get
            {
                return this.printTrace
                           ? string.Format(
                               "ErrorCode: {0}, Message: {1}\n---StackTrace---\n{2}",
                               this.Code(),
                               base.Message,
                               new StackTrace())
                           : string.Format("ErrorCode: {0}, Message: {1}", this.Code(), base.Message);
            }
        }

        public virtual int Code()
        {
            return 10000;
        }
    }
}

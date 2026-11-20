namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;
    using System.Diagnostics;

    #endregion

    public class CsException : SystemException
    {
        private readonly bool printTrace;

        public CsException(bool printTrace = true)
            : base(string.Empty)
        {
            this.printTrace = printTrace;
        }

        public CsException(string message, bool printTrace = true)
            : base(message)
        {
            this.printTrace = printTrace;
        }

        public CsException(Exception exception, bool printTrace = true)
            : base(exception.Message, exception)
        {
            this.printTrace = printTrace;
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
            return 1;
        }
    }
}

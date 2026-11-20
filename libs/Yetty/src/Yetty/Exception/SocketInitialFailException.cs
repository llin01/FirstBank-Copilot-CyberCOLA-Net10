namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    internal class SocketInitialFailException : TprsException
    {
        public SocketInitialFailException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public SocketInitialFailException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public SocketInitialFailException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10002;
        }
    }
}

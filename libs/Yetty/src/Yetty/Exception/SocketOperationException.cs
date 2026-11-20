namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    internal class SocketOperationException : TprsException
    {
        public SocketOperationException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public SocketOperationException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public SocketOperationException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10003;
        }
    }
}

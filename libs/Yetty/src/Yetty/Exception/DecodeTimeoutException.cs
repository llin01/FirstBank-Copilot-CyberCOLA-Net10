namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DecodeTimeoutException : TprsException
    {
        public DecodeTimeoutException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public DecodeTimeoutException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public DecodeTimeoutException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10005;
        }
    }
}

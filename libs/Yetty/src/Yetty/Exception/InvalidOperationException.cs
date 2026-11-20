namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class InvalidOperationException : TprsException
    {
        public InvalidOperationException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public InvalidOperationException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public InvalidOperationException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10010;
        }
    }
}

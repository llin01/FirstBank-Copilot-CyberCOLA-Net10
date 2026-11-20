namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class InvalidOperationException : CsException
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
            return 7;
        }
    }
}

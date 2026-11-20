namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class NoSuchElementException : CsException
    {
        public NoSuchElementException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public NoSuchElementException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public NoSuchElementException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 5;
        }
    }
}

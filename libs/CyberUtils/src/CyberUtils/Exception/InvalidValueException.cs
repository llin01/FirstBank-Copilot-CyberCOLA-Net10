namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class InvalidValueException : CsException
    {
        public InvalidValueException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public InvalidValueException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public InvalidValueException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 4;
        }
    }
}

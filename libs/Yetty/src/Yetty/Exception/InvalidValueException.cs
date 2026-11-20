namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class InvalidValueException : TprsException
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
            return 10007;
        }
    }
}

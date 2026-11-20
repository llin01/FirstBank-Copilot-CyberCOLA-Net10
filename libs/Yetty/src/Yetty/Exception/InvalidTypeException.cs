namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class InvalidTypeException : TprsException
    {
        public InvalidTypeException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public InvalidTypeException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public InvalidTypeException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10006;
        }
    }
}

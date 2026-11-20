namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DuplicateHandlerException : TprsException
    {
        public DuplicateHandlerException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public DuplicateHandlerException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public DuplicateHandlerException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10001;
        }
    }
}

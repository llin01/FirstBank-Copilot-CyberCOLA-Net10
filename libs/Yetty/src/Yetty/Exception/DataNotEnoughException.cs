namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DataNotEnoughException : TprsException
    {
        public DataNotEnoughException(bool printTrace = true, bool silent = false)
            : base(printTrace, silent)
        {
        }

        public DataNotEnoughException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public DataNotEnoughException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10004;
        }
    }
}

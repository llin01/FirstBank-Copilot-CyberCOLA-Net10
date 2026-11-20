namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class ValueAlreadySetException : TprsException
    {
        public ValueAlreadySetException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public ValueAlreadySetException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public ValueAlreadySetException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10009;
        }
    }
}

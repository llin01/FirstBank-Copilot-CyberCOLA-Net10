namespace Com.Cybersoft4u.TM.ISO8583.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class TransactionPrepareException : Iso8583Exception
    {
        public TransactionPrepareException(string message)
            : base(message)
        {
        }

        public TransactionPrepareException(Exception exception)
            : base(exception)
        {
        }

        public override int Code()
        {
            return 20003;
        }
    }
}

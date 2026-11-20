namespace Com.Cybersoft4u.TM.ISO8583.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class TransactionConversionException : Iso8583Exception
    {
        public TransactionConversionException(string message)
            : base(message)
        {
        }

        public TransactionConversionException(Exception exception)
            : base(exception)
        {
        }

        public override int Code()
        {
            return 20004;
        }
    }
}

namespace Com.Cybersoft4u.TM.ISO8583.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DecodeFailException : Iso8583Exception
    {
        public DecodeFailException(string message)
            : base(message)
        {
        }

        public DecodeFailException(Exception exception)
            : base(exception)
        {
        }

        public override int Code()
        {
            return 20004;
        }
    }
}

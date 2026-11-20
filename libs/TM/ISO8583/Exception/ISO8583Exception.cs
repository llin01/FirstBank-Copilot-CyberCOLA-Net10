namespace Com.Cybersoft4u.TM.ISO8583.Exception
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Exception;

    #endregion

    public class Iso8583Exception : CsException
    {
        public Iso8583Exception()
        {
        }

        public Iso8583Exception(string message)
            : base(message)
        {
        }

        public Iso8583Exception(Exception exception)
            : base(exception)
        {
        }

        public override int Code()
        {
            return 20000;
        }
    }
}

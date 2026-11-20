namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class ConvertException : CsException
    {
        public ConvertException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public ConvertException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public ConvertException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 2;
        }
    }
}

namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DbCheckException : CsException
    {
        public DbCheckException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public DbCheckException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public DbCheckException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10;
        }
    }
}

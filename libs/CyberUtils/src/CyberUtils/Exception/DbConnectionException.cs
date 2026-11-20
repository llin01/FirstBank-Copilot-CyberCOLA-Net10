namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DbConnectionException : CsException
    {
        public DbConnectionException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public DbConnectionException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public DbConnectionException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10;
        }
    }
}

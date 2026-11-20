namespace Com.Cybersoft4u.CyberUtils.Exception
{
    #region DEPENDENCY

    using System;

    #endregion

    public class DbExecutionException : CsException
    {
        public DbExecutionException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public DbExecutionException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public DbExecutionException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10;
        }
    }
}

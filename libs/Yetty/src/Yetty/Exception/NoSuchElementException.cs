namespace Com.Cybersoft4u.Yetty.Exception
{
    #region DEPENDENCY

    #endregion

    public class NoSuchElementException : TprsException
    {
        public NoSuchElementException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public NoSuchElementException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public NoSuchElementException(System.Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 10008;
        }
    }
}

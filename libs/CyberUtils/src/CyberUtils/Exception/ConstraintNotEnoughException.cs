namespace Com.Cybersoft4u.CyberUtils.Exception
{
    using System;

    public class ConstraintNotEnoughException : CsException
    {
        public ConstraintNotEnoughException(bool printTrace = true)
            : base(printTrace)
        {
        }

        public ConstraintNotEnoughException(string message, bool printTrace = true)
            : base(message, printTrace)
        {
        }

        public ConstraintNotEnoughException(Exception exception, bool printTrace = true)
            : base(exception, printTrace)
        {
        }

        public override int Code()
        {
            return 8;
        }
    }
}

namespace Com.Cybersoft4u.TM.ISO8583.Exception
{
    public class FieldDecodeException : Iso8583Exception
    {
        public FieldDecodeException(FIELD field)
        {
            this.Field = field;
        }

        public FieldDecodeException(FIELD field, string message)
            : base(message)
        {
            this.Field = field;
        }

        public FIELD Field { get; private set; }

        public override int Code()
        {
            return 20002;
        }

        public override string ToString()
        {
            return string.Format("Field[{0}] Decode Error.", this.Field) + base.ToString();
        }
    }
}

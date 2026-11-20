namespace Com.Cybersoft4u.TM.ISO8583.Exception
{
    public class FieldEncodeException : Iso8583Exception
    {
        public FieldEncodeException(FIELD field)
        {
            this.Field = field;
        }

        public FieldEncodeException(FIELD field, string message)
            : base(message)
        {
            this.Field = field;
        }

        public FIELD Field { get; private set; }

        public override int Code()
        {
            return 20001;
        }

        public override string ToString()
        {
            return string.Format("Field[{0}] Encode Error.", this.Field) + base.ToString();
        }
    }
}

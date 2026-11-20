namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    public abstract class DataElementSpec : ISO8583.DataElementSpec
    {
        protected DEType type;

        protected Packer Packer;

        protected DataElementSpec(FIELD field, int maxLength, DEType type, Packer packer)
            : base(field, maxLength)
        {
            this.type = type;
            this.Packer = packer;
        }

        public DEType Type()
        {
            return this.type;
        }

        public abstract string Unpack(Iso8583 iso8583, string record);

        public abstract string Pack(Iso8583 iso8583);
    }
}

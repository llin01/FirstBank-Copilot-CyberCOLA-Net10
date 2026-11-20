namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    public class DefaultField : DataElementSpec
    {
        public DefaultField(FIELD field, int maxLength, DEType type, Packer packers)
            : base(field, maxLength, type, packers)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            return this.Packer.Unpack(iso8583, this, record);
        }

        public override string Pack(Iso8583 iso8583)
        {
            return this.Packer.Pack(iso8583.Val(this.field), this);
        }
    }
}

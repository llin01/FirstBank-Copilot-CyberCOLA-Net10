namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.V1987.DataElementSpec;

    #endregion

    public class MasterCardField : DataElementSpec
    {
        public MasterCardField(FIELD field, int maxLength, DEType type, Packer packers)
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

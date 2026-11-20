namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public class FieldDef : DataElementSpec
    {
        public FieldDef(FIELD field, int maxLength, Format format, Representation representation, Packer packer)
            : base(field, maxLength, format, representation, packer)
        {
        }

        public override object Unpack(ByteBuffer record)
        {
            return this.Packer.Unpack(this, record);
        }

        public override ByteBuffer Pack(object data)
        {
            return this.Packer.Pack(data, this);
        }
    }
}

namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public class FIELD_35 : DefaultField
    {
        public FIELD_35(int maxLength)
            : base(FIELD.F_35, maxLength, DEType.LL, null)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            var lenLength = 2;
            var dataLength = int.Parse(record.IndexSubstring(0, lenLength));
            record = record.Substring(lenLength);
            iso8583.Val(this.Field(), record.IndexSubstring(0, dataLength));
            return record.Substring(dataLength);
        }

        public override string Pack(Iso8583 iso8583)
        {
            var dataLength = Bytes.From(iso8583.Val(this.field)).Length();
            return dataLength.ToString().PadLeft(2, '0') + iso8583.Val(this.field);
        }
    }
}

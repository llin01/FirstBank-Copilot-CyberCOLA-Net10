namespace Com.Cybersoft4u.TM.ISO8583.Edc.Field
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class EdcFIELD_59 : DefaultField
    {
        public EdcFIELD_59(int maxLength)
            : base(FIELD.F_59, maxLength, DEType.N, null)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            var lenLength = 4;
            var dataLength = int.Parse(record.IndexSubstring(0, lenLength)) * 2;
            record = record.Substring(lenLength);
            iso8583.Val(this.Field(), record.IndexSubstring(0, dataLength));
            return record.Substring(dataLength);
        }

        public override string Pack(Iso8583 iso8583)
        {
            var dataLength = Bytes.From(iso8583.Val(this.field)).Length() / 2;
            return dataLength.ToString().PadLeft(4, '0') + iso8583.Val(this.field);
        }
    }
}

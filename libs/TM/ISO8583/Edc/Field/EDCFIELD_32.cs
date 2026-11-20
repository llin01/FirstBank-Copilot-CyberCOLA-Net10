namespace Com.Cybersoft4u.TM.ISO8583.Edc.Field
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class EdcFIELD_32 : DefaultField
    {
        public EdcFIELD_32(int maxLength)
            : base(FIELD.F_32, maxLength, DEType.N, null)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            var lenLength = 2;
            var dataLength = int.Parse(record.IndexSubstring(0, lenLength)) * 2;
            record = record.Substring(lenLength);
            iso8583.Val(this.Field(), record.IndexSubstring(1, 4));
            return record.Substring(dataLength);
        }

        public override string Pack(Iso8583 iso8583)
        {
            return "020" + iso8583.Val(this.field);
        }
    }
}

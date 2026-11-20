namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class FIELD_55 : MasterCardField
    {
        public FIELD_55(int maxLength)
            : base(FIELD.F_55, maxLength, DEType.LL, null)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            var lenLength = 3;
            lenLength = lenLength * 2;

            var dataLengthString = EbcdicConverter.GetString(record.IndexSubstring(0, lenLength));
            var dataLength = int.Parse(dataLengthString) * 2;
            record = record.Substring(lenLength);

            iso8583.Val(this.Field(), record.IndexSubstring(0, dataLength));

            return record.Substring(dataLength);
        }

        public override string Pack(Iso8583 iso8583)
        {
            var data = iso8583.Val(this.Field());

            var lenLength = 3;
            var dataLength = Bytes.FromHexString(data).Length();

            return EbcdicConverter.FromString(dataLength.ToString().PadLeft(lenLength, '0')) + data;
        }
    }
}

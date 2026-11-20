namespace Com.Cybersoft4u.TM.ISO8583.Edc.Field
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class EdcFIELD_35 : DefaultField
    {
        public EdcFIELD_35(int maxLength)
            : base(FIELD.F_35, maxLength, DEType.N, null)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            var lenLength = 2;
            var dataLength = int.Parse(record.IndexSubstring(0, lenLength));
            record = record.Substring(lenLength);
            iso8583.Val(this.Field(), record.IndexSubstring(0, dataLength));
            var decodeDataLength = dataLength + (dataLength % 2);
            return record.Substring(decodeDataLength);
        }

        public override string Pack(Iso8583 iso8583)
        {
            var dataLength = Bytes.From(iso8583.Val(this.field)).Length();
            var data = iso8583.Val(this.field);
            var paddingCharactor = ((EdcIso8583)iso8583).Core().CardType.Equals(CardType.Default) ? "F" : "0";
            data += data.Length % 2 == 0 ? string.Empty : paddingCharactor;
            return dataLength.ToString().PadLeft(2, '0') + data;
        }
    }
}

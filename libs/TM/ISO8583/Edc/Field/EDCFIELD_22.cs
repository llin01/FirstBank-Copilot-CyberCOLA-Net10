namespace Com.Cybersoft4u.TM.ISO8583.Edc.Field
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class EdcFIELD_22 : DefaultField
    {
        public EdcFIELD_22(int maxLength)
            : base(FIELD.F_22, maxLength, DEType.N, null)
        {
        }

        public override string Unpack(Iso8583 iso8583, string record)
        {
            iso8583.Val(this.Field(), record.IndexSubstring(0, 4));
            return record.Substring(4);
        }

        public override string Pack(Iso8583 iso8583)
        {
            var data = iso8583.Val(this.field);
            return data.Length == 4 ? data : ("5" + data);
        }
    }
}

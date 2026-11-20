namespace Com.Cybersoft4u.TM.FISC
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    /// <summary>
    /// Represents FISC Field 55 (Integrated Circuit Card (ICC) System-Related Data).
    /// </summary>
    public class FIELD_55 : FISCField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FIELD_55"/> class.
        /// </summary>
        /// <param name="maxLength">The maximum length of the field.</param>
        public FIELD_55(int maxLength)
            : base(FIELD.F_55, maxLength, DEType.LL, null)
        {
        }

        /// <summary>
        /// Unpacks Field 55 data from the incoming message record.
        /// </summary>
        /// <param name="iso8583">The ISO8583 message object.</param>
        /// <param name="record">The raw message record string.</param>
        /// <returns>The remaining part of the message record after unpacking.</returns>
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

        /// <summary>
        /// Packs Field 55 data for the outgoing message.
        /// </summary>
        /// <param name="iso8583">The ISO8583 message object.</param>
        /// <returns>The packed field data string.</returns>
        public override string Pack(Iso8583 iso8583)
        {
            var data = iso8583.Val(this.Field());

            var lenLength = 3;
            var dataLength = Bytes.FromHexString(data).Length();

            return EbcdicConverter.FromString(dataLength.ToString().PadLeft(lenLength, '0')) + data;
        }
    }
}

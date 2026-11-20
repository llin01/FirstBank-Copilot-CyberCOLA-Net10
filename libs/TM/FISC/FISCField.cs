using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.V1987;

namespace Com.Cybersoft4u.TM.FISC
{
    #region DEPENDENCY

    using DataElementSpec = ISO8583.V1987.DataElementSpec;

    #endregion

    /// <summary>
    /// Represents a data element specification for the FISC protocol.
    /// </summary>
    public class FISCField : DataElementSpec
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FISCField"/> class.
        /// </summary>
        /// <param name="field">The field identifier.</param>
        /// <param name="maxLength">The maximum length of the field.</param>
        /// <param name="type">The data element type.</param>
        /// <param name="packers">The packer used for encoding/decoding.</param>
        public FISCField(FIELD field, int maxLength, DEType type, Packer packers)
            : base(field, maxLength, type, packers)
        {
        }

        /// <summary>
        /// Unpacks the field data from the raw message record.
        /// </summary>
        /// <param name="iso8583">The ISO8583 message object.</param>
        /// <param name="record">The raw message record string.</param>
        /// <returns>The remaining message record after unpacking this field.</returns>
        public override string Unpack(Iso8583 iso8583, string record)
        {
            return this.Packer.Unpack(iso8583, this, record);
        }

        /// <summary>
        /// Packs the field data for the outgoing message.
        /// </summary>
        /// <param name="iso8583">The ISO8583 message object.</param>
        /// <returns>The packed field data string.</returns>
        public override string Pack(Iso8583 iso8583)
        {
            return this.Packer.Pack(iso8583.Val(this.field), this);
        }
    }
}

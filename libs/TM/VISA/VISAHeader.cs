// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VISAHeader.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VISAHeader type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class VISAHeader : ICodecable
    {
        public byte[] BaseIFlags = { 0x00, 0x00 };

        public byte BatchNumber;

        public byte[] Bitmap;

        public string DestinationId = "000000";

        public byte HeaderFlagAndFormat = 0x01;

        public short HeaderLength = 22;

        public byte[] MessageStatusFlags = { 0x00, 0x00, 0x00 };

        public string RejectDataGroup;

        public byte[] Reserved = { 0x00, 0x00, 0x00 };

        public byte RoundTripControlInformation;

        public string SourceId = "655425";

        public byte TextFormat = 0x02;

        public ushort TotalLength;

        public byte UserInformation;

        public VISAHeader()
        {
        }

        public VISAHeader(byte[] message)
        {
            this.Decode(message);
        }

        public Bytes Encode()
        {
            var data = new ByteBuffer();
            data.WriteSingleByteChar(this.HeaderLength);
            data.WriteByte(this.HeaderFlagAndFormat);
            data.WriteByte(this.TextFormat);
            data.WriteUnsignedShort(this.TotalLength);
            data.WriteBytes(HexStringConverter.ToByteArray(this.DestinationId));
            data.WriteBytes(HexStringConverter.ToByteArray(this.SourceId));
            data.WriteByte(this.RoundTripControlInformation);
            data.WriteBytes(this.BaseIFlags);
            data.WriteBytes(this.MessageStatusFlags);
            data.WriteByte(this.BatchNumber);
            data.WriteBytes(this.Reserved);
            data.WriteByte(this.UserInformation);
            if (this.RejectDataGroup != null)
            {
                this.Bitmap = new byte[] { 0x80, 0x00 };
            }

            if (this.Bitmap == null)
            {
                return new Bytes(data.Val());
            }

            data.WriteBytes(this.Bitmap);
            data.WriteBytes(HexStringConverter.ToByteArray(this.RejectDataGroup));
            return new Bytes(data.Val());
        }

        public ICodecable Decode(byte[] message)
        {
            var data = new ByteBuffer(message);
            this.HeaderLength = data.ReadByte();
            this.HeaderFlagAndFormat = data.ReadByte();
            this.TextFormat = data.ReadByte();
            this.TotalLength = data.ReadUnsignedShort();
            this.DestinationId = HexStringConverter.FromByteArray(data.ReadBytes(3));
            this.SourceId = HexStringConverter.FromByteArray(data.ReadBytes(3));
            this.RoundTripControlInformation = data.ReadByte();
            this.BaseIFlags = data.ReadBytes(2);
            this.MessageStatusFlags = data.ReadBytes(3);
            this.BatchNumber = data.ReadByte();
            this.Reserved = data.ReadBytes(3);
            this.UserInformation = data.ReadByte();
            if (this.HeaderLength <= 22)
            {
                return this;
            }

            this.Bitmap = data.ReadBytes(2);
            if (((this.Bitmap[0] >> 7) & 1) == 1)
            {
                this.RejectDataGroup = HexStringConverter.FromByteArray(data.ReadBytes(2));
            }

            return this;
        }
    }
}

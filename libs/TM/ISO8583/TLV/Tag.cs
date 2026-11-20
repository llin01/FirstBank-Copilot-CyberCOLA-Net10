// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Tag.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Tag type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TLV
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class Tag
    {
        public string Id;

        public byte[] Value;

        public Tag()
        {
        }

        public Tag(byte[] data)
        {
            this.Decode(new ByteBuffer(data));
        }

        public ByteBuffer Encode()
        {
            var rtn = new ByteBuffer();
            rtn.WriteBytes(HexStringConverter.ToByteArray(this.Id));
            if (this.Value.Length < 128)
            {
                rtn.WriteSingleByteChar((short)this.Value.Length);
            }
            else if (this.Value.Length / 65536 == 0)
            {
                rtn.WriteByte(0x82);
                rtn.WriteUnsignedShort((ushort)this.Value.Length);
            }
            else if (this.Value.Length / 1677216 == 0)
            {
                rtn.WriteByte(0x83);
                rtn.WriteSingleByteChar((short)(this.Value.Length / 65536));
                rtn.WriteUnsignedShort((ushort)(this.Value.Length % 65536));
            }
            else
            {
                rtn.WriteByte(0x84);
                rtn.WriteUnsignedInteger((uint)this.Value.Length);
            }

            rtn.WriteBytes(this.Value);
            return rtn;
        }

        //9FXX,5FXX,DFED,DFEF tag 2Byte
        public Tag Decode(ByteBuffer data)
        {
            var id = new ByteBuffer();
            bool firstByte = true;

            while (true)
            {
                var tmpByte = data.ReadByte();
                id.WriteByte(tmpByte);

                if (firstByte)
                {
                    firstByte = false;
                    //C.F Update for tagid dfed
                    if (((tmpByte & 0x1f) == 0x1f) || (tmpByte == 0xdf))
                    {
                        continue;
                    }
                }
                else
                {
                    if ((tmpByte >> 7 == 1) && (tmpByte != 0xed) && (tmpByte != 0xef))
                    {
                        continue;
                    }
                }

                break;
            }

            this.Id = id.Val().GetHexString();

            uint length = 0;
            var tmpLength = data.ReadByte();
            if (tmpLength >> 7 == 0)
            {
                length = tmpLength;
            }
            else
            {
                switch (tmpLength)
                {
                    case 0x81:
                        length = data.ReadByte();
                        break;
                    case 0x82:
                        length = data.ReadUnsignedShort();
                        break;
                    case 0x84:
                        length = (uint)(data.ReadByte() * 65536);
                        length += data.ReadUnsignedShort();
                        break;
                    case 0x88:
                        length = data.ReadUnsignedInteger();
                        break;
                }
            }

            this.Value = data.ReadBytes((int)length);
            return this;
        }
    }
}

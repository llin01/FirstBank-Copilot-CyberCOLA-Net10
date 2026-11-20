// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_142FieldCodec.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_142FieldCodec type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec
{
    #region DEPENDENCY

    using System.Collections.Generic;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.TLV;

    #endregion

    public class F_142FieldCodec
    {
        public string Id;

        public Dictionary<string, Tag> Tags = new Dictionary<string, Tag>();

        public F_142FieldCodec()
        {
        }

        public F_142FieldCodec(byte[] data)
        {
            this.Decode(new ByteBuffer(data));
        }

        public ByteBuffer Encode()
        {
            var data = new ByteBuffer();
            foreach (var tag in this.Tags)
            {
                data.WriteByteBuffer(tag.Value.Encode());
            }

            var rtn = new ByteBuffer();
            var valueLength = (ushort)data.Val().Length;

            rtn.WriteBytes(HexStringConverter.ToByteArray(this.Id));
            if (valueLength < 128)
            {
                rtn.WriteSingleByteChar((short)valueLength);
            }
            else if (valueLength / 65536 == 0)
            {
                rtn.WriteByte(0x82);
                rtn.WriteUnsignedShort(valueLength);
            }
            else if (valueLength / 1677216 == 0)
            {
                rtn.WriteByte(0x83);
                rtn.WriteSingleByteChar((short)(valueLength / 65536));
                rtn.WriteUnsignedShort((ushort)(valueLength % 65536));
            }
            else
            {
                rtn.WriteByte(0x84);
                rtn.WriteUnsignedInteger(valueLength);
            }

            rtn.WriteByteBuffer(data);
            return rtn;
        }

        public F_142FieldCodec Decode(ByteBuffer data)
        {
            var id = new ByteBuffer();
            var firstByte = true;

            while (true)
            {
                var tmpByte = data.ReadByte();
                id.WriteByte(tmpByte);

                if (firstByte)
                {
                    firstByte = false;
                    if ((tmpByte & 0x1f) == 0x1f)
                    {
                        continue;
                    }
                }
                else
                {
                    if (tmpByte >> 7 == 1)
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

            var tmpTags = new ByteBuffer(data.ReadBytes((int)length));
            while (tmpTags.ReadRemainLength() > 0)
            {
                var tag = new Tag().Decode(tmpTags);
                this.Tags[tag.Id] = tag;
            }

            return this;
        }
    }
}

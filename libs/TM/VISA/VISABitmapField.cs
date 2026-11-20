// IAG-TM-VISABitmapField.cs
namespace Com.Cybersoft4u.TM.VISA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    public abstract class VISABitmapField : AbstractBitmapField
    {
        protected VISABitmapField(byte bitmapLength)
            : base(bitmapLength)
        {
        }

        protected VISABitmapField(byte bitmapLength, byte[] fieldBytes)
            : base(bitmapLength, fieldBytes)
        {
        }

        public byte[] Bitmap()
        {
            var data = new byte[this.BitmapLength];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = 0x00;
            }

            foreach (var pair in
                this.OrderedFields().Where(pair => FIELDMethods.Val(pair.Key) < ((this.BitmapLength * 8) + 1) && pair.Value != null))
            {
                Mark(data, FIELDMethods.Val(pair.Key));
            }

            return data;
        }

        public override Bytes Encode()
        {
            this.Logger.Debug(() => "Bitmap Field Encoding...");
            var data = new ByteBuffer();
            data.WriteBytes(this.Bitmap());
            this.Logger.Debug(() => string.Format("Bitmap      :{0}", HexStringConverter.FromByteArray(this.Bitmap())));
            foreach (var pair in this.OrderedFields().Where(pair => pair.Value != null))
            {
                var key = pair.Key;
                var value = this.Field(key);
                var fieldData = ((VISAField)this.Spec(key)).Pack(value);
                data.WriteByteBuffer(fieldData);
                this.Logger.Debug(
                    () => string.Format(
                        "{0}:{1}",
                        string.Format("FIELD[{0}]", key).PadRight(12, ' '),
                        value is byte[]? HexStringConverter.FromByteArray((byte[])value) : value));
            }

            this.Logger.Debug(() => "Bitmap Field Encoded.");
            return new Bytes(data.Val());
        }

        public override ICodecable Decode(byte[] message)
        {
            this.Logger.Debug(() => string.Format("Bitmap Field Decoding... Hex String: {0}", HexStringConverter.FromByteArray(message)));
            var data = new ByteBuffer(message);
            var bitmap = data.ReadBytes(this.BitmapLength);
            this.UnpackDataElement(bitmap, data, 0);
            this.Logger.Debug(() => "Bitmap Field Decoded.");
            return this;
        }

        protected static void Mark(IList<byte> bytes, int index)
        {
            var indexShift = index - 1;
            bytes[indexShift / 8] |= (byte)(1 << (7 - (indexShift % 8)));
        }

        protected void UnpackDataElement(byte[] bitmap, ByteBuffer record, int elementOffset)
        {
            var defaultBitmapLength = bitmap.Length * 8;
            var bitSetIndex = defaultBitmapLength;
            var arrangedBitMap = new Bytes();
            for (var i = bitmap.Length - 1; i > -1; i--)
            {
                arrangedBitMap.Append(bitmap[i]);
            }

            var bitmapSet = new BitSet(arrangedBitMap);
            while (true)
            {
                bitSetIndex = bitmapSet.PreviousSetBit(bitSetIndex - 1);
                if (bitSetIndex < 0)
                {
                    break;
                }

                var currentField = FIELD.F_UKN;
                try
                {
                    currentField = FIELDMethods.Val(defaultBitmapLength - bitSetIndex + elementOffset);
                    var spec = (VISAField)this.Spec(currentField);
                    if (ObjectUtils.IsNull(spec))
                    {
                        continue;
                    }

                    this.Field(spec.Field(), spec.Unpack(record));
                    this.Logger.Debug(
                        () =>
                        {
                            var value = this.Field(currentField);
                            return string.Format(
                                "{0}:{1}",
                                string.Format("FIELD[{0}]", FIELDMethods.Val(currentField)).PadRight(12, ' '),
                                value is byte[]? HexStringConverter.FromByteArray((byte[])value) : value);
                        });
                }
                catch (FieldDecodeException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new FieldDecodeException(
                        currentField,
                        string.Format("Field[{0}] decode failed.", currentField));
                }
            }
        }

    }
}

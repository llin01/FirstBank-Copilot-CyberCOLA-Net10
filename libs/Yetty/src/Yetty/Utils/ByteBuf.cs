namespace Com.Cybersoft4u.Yetty.Utils
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.Yetty.Exception;

    using InvalidOperationException = Com.Cybersoft4u.Yetty.Exception.InvalidOperationException;

    #endregion

    public class ByteBuf
    {
        private static readonly bool LittleEndian = BitConverter.IsLittleEndian;

        private static readonly byte[] Initval = { };

        private byte[] data;

        public ByteBuf(byte[] data = null)
        {
            this.ReadOffset = 0;
            this.MarkRead = -1;
            this.MarkWrite = -1;
            this.data = data ?? Initval;
            this.WriteOffset = data == null ? -1 : data.Length;
        }

        public int ReadOffset { get; private set; }

        public int WriteOffset { get; private set; }

        private int MarkRead { get; set; }

        private int MarkWrite { get; set; }

        public static ByteBuf From(byte[] data, int offset, int length)
        {
            if (length == 0)
            {
                return new ByteBuf(new byte[] { });
            }

            var dataOut = new byte[length];
            System.Array.Copy(data, offset, dataOut, 0, length);
            return new ByteBuf(dataOut);
        }

        public int Length()
        {
            return this.data == null ? 0 : this.data.Length;
        }

        public int ReadRemainLength()
        {
            return this.data.Length - this.ReadOffset;
        }

        public byte[] Array()
        {
            return this.data;
        }

        public byte[] Val()
        {
            var length = this.data.Length - this.ReadOffset;
            var dataOut = new byte[length];
            System.Array.Copy(this.data, this.ReadOffset, dataOut, 0, length);
            return dataOut;
        }

        public ByteBuf ReadIndex(int index)
        {
            if (index > this.Length() || index < 0)
            {
                throw new InvalidValueException("index out of bound");
            }

            this.ReadOffset = index;
            return this;
        }

        public int ReadIndex()
        {
            return this.ReadOffset;
        }

        public ByteBuf WriteIndex(int index)
        {
            if (index > this.Length() || index < 0)
            {
                throw new InvalidValueException("index out of bound");
            }

            this.WriteOffset = index;
            return this;
        }

        public int WriteIndex()
        {
            return this.WriteOffset;
        }

        public ushort ReadUnsignedShort()
        {
            this.CheckRequireLength(2);
            ushort rtn;
            if (LittleEndian)
            {
                var tmpData = new byte[2];
                System.Array.Copy(this.data, this.ReadOffset, tmpData, 0, 2);
                System.Array.Reverse(tmpData);
                rtn = BitConverter.ToUInt16(tmpData, 0);
            }
            else
            {
                rtn = BitConverter.ToUInt16(this.data, this.ReadOffset);
            }

            this.ReadOffset += 2;
            return rtn;
        }

        public int ReadInteger()
        {
            this.CheckRequireLength(4);
            int rtn;
            if (LittleEndian)
            {
                var tmpData = new byte[4];
                System.Array.Copy(this.data, this.ReadOffset, tmpData, 0, 4);
                System.Array.Reverse(tmpData);
                rtn = BitConverter.ToInt32(tmpData, 0);
            }
            else
            {
                rtn = BitConverter.ToInt32(this.data, this.ReadOffset);
            }

            this.ReadOffset += 4;
            return rtn;
        }

        public uint ReadUnsignedInteger()
        {
            this.CheckRequireLength(4);
            uint rtn;
            if (LittleEndian)
            {
                var tmpData = new byte[4];
                System.Array.Copy(this.data, this.ReadOffset, tmpData, 0, 4);
                System.Array.Reverse(tmpData);
                rtn = BitConverter.ToUInt32(tmpData, 0);
            }
            else
            {
                rtn = BitConverter.ToUInt32(this.data, this.ReadOffset);
            }

            this.ReadOffset += 4;
            return rtn;
        }

        public byte ReadByte()
        {
            this.CheckRequireLength(1);
            var rtn = this.data[this.ReadOffset];
            this.ReadOffset += 1;
            return rtn;
        }

        public byte[] ReadBytes(int length)
        {
            this.CheckRequireLength(length);
            var rtn = new byte[length];
            System.Array.Copy(this.data, this.ReadOffset, rtn, 0, length);
            this.ReadOffset += length;
            return rtn;
        }

        public ByteBuf WriteSingleByteChar(short value)
        {
            this.WriteValueAndExpendData(CheckEndianAndReverse(BitConverter.GetBytes(value)), 1, 1);
            return this;
        }

        public ByteBuf WriteUnsignedShort(ushort value)
        {
            this.WriteValueAndExpendData(CheckEndianAndReverse(BitConverter.GetBytes(value)), 0, 2);
            return this;
        }

        public ByteBuf WriteInteger(int value)
        {
            this.WriteValueAndExpendData(CheckEndianAndReverse(BitConverter.GetBytes(value)), 0, 4);
            return this;
        }

        public ByteBuf WriteUnsignedInteger(uint value)
        {
            this.WriteValueAndExpendData(CheckEndianAndReverse(BitConverter.GetBytes(value)), 0, 4);
            return this;
        }

        public ByteBuf WriteBytes(byte[] value)
        {
            this.WriteValueAndExpendData(value, 0, value.Length);
            return this;
        }

        public ByteBuf WriteByte(byte value)
        {
            this.WriteValueAndExpendData(new[] { value }, 0, 1);
            return this;
        }

        public ByteBuf WriteByteBuf(ByteBuf buf)
        {
            this.WriteValueAndExpendData(buf.Val(), 0, buf.ReadRemainLength());
            return this;
        }

        public void SkipAll()
        {
            this.Skip();
        }

        public void Skip(int length = -1)
        {
            if (length == -1)
            {
                this.ReadOffset = this.data.Length;
            }
            else
            {
                this.ReadOffset += length;
            }
        }

        public ByteBuf MarkReadIndex()
        {
            this.MarkRead = this.ReadOffset;
            return this;
        }

        public ByteBuf MarkWriteIndex()
        {
            this.MarkWrite = this.WriteOffset;
            return this;
        }

        public ByteBuf ResetReadIndex()
        {
            if (this.MarkRead == -1)
            {
                throw new InvalidOperationException();
            }

            if (this.WriteOffset < this.MarkRead)
            {
                throw new IndexOutOfRangeException();
            }

            this.ReadOffset = this.MarkRead;
            this.MarkRead = -1;
            return this;
        }

        public ByteBuf ResetWriteIndex()
        {
            if (this.MarkWrite == -1)
            {
                throw new InvalidOperationException();
            }

            if (this.ReadOffset > this.MarkWrite)
            {
                throw new IndexOutOfRangeException();
            }

            this.WriteOffset = this.MarkWrite;
            this.MarkWrite = -1;
            return this;
        }

        private static byte[] CheckEndianAndReverse(byte[] value)
        {
            if (!LittleEndian)
            {
                return value;
            }

            var length = value.Length;
            var rtn = new byte[length];
            System.Array.Copy(value, 0, rtn, 0, length);
            System.Array.Reverse(rtn);
            return rtn;
        }

        /// <summary>
        ///     copy origin to target byte[] from index 0.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        private static void CopyOriginData(byte[] origin, byte[] target)
        {
            if (origin.Length <= 0)
            {
                return;
            }

            System.Array.Copy(origin, 0, target, 0, origin.Length);
        }

        private void WriteValueAndExpendData(byte[] value, int start, int length)
        {
            var originLength = this.data.Length;
            var target = new byte[originLength + length];
            CopyOriginData(this.data, target);
            System.Array.Copy(value, start, target, originLength, length);
            this.data = target;
        }

        private void CheckRequireLength(int length)
        {
            if (this.data.Length - this.ReadOffset < length)
            {
                throw new DataNotEnoughException(false, true);
            }
        }
    }
}

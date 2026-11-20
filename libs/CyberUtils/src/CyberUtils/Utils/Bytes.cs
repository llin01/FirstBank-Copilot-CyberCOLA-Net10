namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Exception;

    #endregion

    public class Bytes
    {
        private byte[] data;

        public Bytes()
        {
            this.data = new byte[] { };
        }

        public Bytes(int capacity)
        {
            this.data = new byte[capacity];
        }

        public Bytes(byte[] data)
        {
            this.data = data ?? new byte[] { };
        }

        public static Bytes FromHexString(string hexString)
        {
            try
            {
                return new Bytes(HexStringConverter.ToByteArray(hexString));
            }
            catch (Exception e)
            {
                throw new ConvertException(e);
            }
        }

        public static Bytes From(byte[] data)
        {
            try
            {
                return new Bytes(data);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Bytes From(string str)
        {
            try
            {
                //update for FISC DE48(44,72)
                //return new Bytes(Encoding.UTF8.GetBytes(str));
                return new Bytes(Encoding.ASCII.GetBytes(str));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Bytes From(int number)
        {
            try
            {
                return new Bytes().Append(number);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Bytes From(ushort number)
        {
            try
            {
                return new Bytes().Append(number);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Bytes From(byte[] data, int offset, int length)
        {
            if (length == 0)
            {
                return new Bytes();
            }

            var rtn = new Bytes(length);
            Array.Copy(data, offset, rtn.data, 0, length);
            return rtn;
        }

        public byte[] Val()
        {
            return this.data;
        }

        public int Length()
        {
            return this.data.Length;
        }

        public Bytes Append(int number)
        {
            var numberData = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numberData);
            }

            this.data = AppendByteArray(numberData, this.data);
            return this;
        }

        public Bytes Append(ushort number)
        {
            var numberData = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numberData);
            }

            this.data = AppendByteArray(numberData, this.data);
            return this;
        }

        public Bytes Append(Bytes bytes)
        {
            if (bytes == null || bytes.Length() == 0)
            {
                return this;
            }

            this.data = AppendByteArray(bytes.Val(), this.data);
            return this;
        }

        public Bytes Append(byte b)
        {
            this.data = AppendByteArray(new[] { b }, this.data);
            return this;
        }

        public Bytes Append(byte[] byteArray)
        {
            this.data = AppendByteArray(byteArray, this.data);
            return this;
        }

        public Bytes Append(string str)
        {
            if (str == null)
            {
                return this;
            }

            var strBytes = Encoding.UTF8.GetBytes(str);
            this.data = AppendByteArray(strBytes, this.data);
            return this;
        }

        public Bytes AppendHex(string hexString)
        {
            if (hexString == null)
            {
                return this;
            }

            this.Append(FromHexString(hexString));
            return this;
        }

        public string ToHexString()
        {
            return HexStringConverter.FromByteArray(this.data).ToUpper();
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(this.data);
        }

        private static byte[] AppendByteArray(byte[] from, byte[] after)
        {
            var srcLength = from == null ? 0 : from.Length;
            var destLength = after == null ? 0 : after.Length;
            var rtn = new byte[destLength + srcLength];
            if (destLength > 0)
            {
                Array.Copy(after, 0, rtn, 0, destLength);
            }

            if (srcLength > 0)
            {
                Array.Copy(from, 0, rtn, destLength, srcLength);
            }

            return rtn;
        }
    }
}

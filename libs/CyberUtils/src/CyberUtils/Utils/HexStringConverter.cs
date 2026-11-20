namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;

    #endregion

    public class HexStringConverter
    {
        private static readonly uint[] Lookup32 = CreateLookup32();

        public static string FromByteArray(byte[] bytes)
        {
            if (bytes.Length == 0) return string.Empty;
            var lookup32 = Lookup32;
            var result = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[(2 * i) + 1] = (char)(val >> 16);
            }

            return new string(result);
        }

        public static byte[] ToByteArray(string hexString)
        {
            var len = hexString.Length;
            var bytes = new byte[len / 2];
            for (var i = 0; i < len; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("X2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }

            return result;
        }
    }
}

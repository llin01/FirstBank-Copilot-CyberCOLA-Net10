namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System.Text;

    #endregion

    /// <summary>
    /// Provides string encoding/decoding extension methods for ASCII, UTF-8, and hexadecimal conversions.
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Converts a string to ASCII byte array.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>ASCII-encoded byte array.</returns>
        public static byte[] GetAsciiBytes(this string value)
        {
            return Encoding.ASCII.GetBytes(value);
        }

        /// <summary>
        /// Converts an ASCII byte array to string.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>ASCII-decoded string.</returns>
        public static string GetAsciiString(this byte[] value)
        {
            return Encoding.ASCII.GetString(value);
        }

        /// <summary>
        /// Converts a string to UTF-8 byte array.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>UTF-8-encoded byte array.</returns>
        public static byte[] GetUTF8Bytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Converts a UTF-8 byte array to string.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>UTF-8-decoded string.</returns>
        public static string GetUTF8String(this byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }

        /// <summary>
        /// Converts a byte array to hexadecimal string representation.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>Hexadecimal string.</returns>
        public static string GetHexString(this byte[] value)
        {
            return HexStringConverter.FromByteArray(value);
        }

        /// <summary>
        /// Converts a hexadecimal string to byte array.
        /// </summary>
        /// <param name="value">The hexadecimal string to convert.</param>
        /// <returns>Byte array.</returns>
        public static byte[] GetHexBytes(this string value)
        {
            return HexStringConverter.ToByteArray(value);
        }

        /// <summary>
        ///     If <code>self</code> is null, return <code>defaultValue</code>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string Default(this string self, string defaultValue)
        {
            return self ?? defaultValue;
        }
    }
}

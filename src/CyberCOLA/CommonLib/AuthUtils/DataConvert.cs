using NLog;
using System;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Provides encoding conversion utilities for Big5, UTF-8, and Unicode character sets.
    /// </summary>
    public class DataConvert
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        public DataConvert()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        /// <summary>
        /// Convert Big5 Data To Utf8
        /// </summary>
        /// <param name="big5Bytes">Input Big5 Data</param>
        /// <returns>Utf8 Bytes</returns>
        public static byte[] Big5ToUtf8(byte[] big5Bytes)
        {
            try
            {
                string s = Encoding.GetEncoding("BIG5").GetString(big5Bytes);
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(s);
                return utf8Bytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Big5ToUtf8 Exception:{ex.Message}");

                return null;
            }
        }
        /// <summary>
        /// Convert Big5 Data To Utf8
        /// </summary>
        /// <param name="big5String">Input Big5 Data</param>
        /// <returns>Utf8 Bytes</returns>
        public static byte[] Big5ToUtf8(string big5String)
        {
            try
            {
                byte[] big5Bytes = Encoding.GetEncoding("BIG5").GetBytes(big5String);
                string s = Encoding.GetEncoding("BIG5").GetString(big5Bytes);
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(s);
                return utf8Bytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Big5ToUtf8 Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// Convert Utf8 Data To Big5
        /// </summary>
        /// <param name="utf8Bytes">Input Utf8 Data</param>
        /// <returns>Big5 Bytes</returns>
        public static byte[] Utf8ToBig5(byte[] utf8Bytes)
        {
            try
            {
                string s = Encoding.GetEncoding("utf-8").GetString(utf8Bytes);
                byte[] big5Bytes = Encoding.GetEncoding("BIG5").GetBytes(s);
                return big5Bytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Utf8ToBig5 Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// Convert Utf8 Data To Big5
        /// </summary>
        /// <param name="utf8String">Input Utf8 Data</param>
        /// <returns>Big5 Bytes</returns>
        public static byte[] Utf8ToBig5(string utf8String)
        {
            try
            {
                byte[] utf8Bytes = Encoding.GetEncoding("utf-8").GetBytes(utf8String);
                string s = Encoding.GetEncoding("utf-8").GetString(utf8Bytes);
                byte[] big5Bytes = Encoding.GetEncoding("BIG5").GetBytes(s);
                return big5Bytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Utf8ToBig5 Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// Convert Big5 Data To Unicode
        /// </summary>
        /// <param name="big5Bytes">Input Big5 Data</param>
        /// <returns>Unicode Bytes</returns>
        public static byte[] Big5ToUnicode(byte[] big5Bytes)  //Unicode little-Edi
        {
            try
            {
                string s = Encoding.GetEncoding("BIG5").GetString(big5Bytes);
                byte[] unicodeBytes = Encoding.Unicode.GetBytes(s);
                return unicodeBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Big5ToUnicode Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// Convert Unicode Data To Big5
        /// </summary>
        /// <param name="unicodeBytes">Input Unicode Data</param>
        /// <returns>Big5 Bytes</returns>
        public static byte[] UnicodeToBig5(byte[] unicodeBytes)
        {
            try
            {
                string s = Encoding.GetEncoding("Unicode").GetString(unicodeBytes);
                byte[] big5Bytes = Encoding.GetEncoding("BIG5").GetBytes(s);
                return big5Bytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"UnicodeToBig5 Exception:{ex.Message}");
                return null;
            }
        }
    }
}

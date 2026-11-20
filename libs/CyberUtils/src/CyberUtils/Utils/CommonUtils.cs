using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;

    #endregion

    /// <summary>
    /// Provides common utility methods for network operations, token parsing, and data conversions.
    /// </summary>
    public class CommonUtils
    {

        /// <summary>
        /// Gets the name of the ip address string by host.
        /// </summary>
        /// <param name="hostNameOrIPAddress">The host name or ip address.</param>
        /// <returns>System.String.</returns>
        public static string GetIPAddressStringByHostName(string hostNameOrIPAddress)
        {
            try
            {
                IPAddress resolveIPAddress;
                if (IPAddress.TryParse(hostNameOrIPAddress, out resolveIPAddress))
                {
                    return (hostNameOrIPAddress);
                }
                var addressList = Dns.GetHostAddresses(hostNameOrIPAddress);
                if (addressList.Length == 0)
                    return null;
                return (addressList[0].ToString());
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get IPAddress By HostName
        /// </summary>
        ///<param name="hostNameOrIPAddress">HostName</param>
        ///<param name="addressFamily">HostName</param>
        ///<returns>Host IP Address String</returns> 
        public static string GetIPAddressStringByHostName(string hostNameOrIPAddress, AddressFamily addressFamily)
        {
            try
            {
                IPAddress resolveIPAddress;
                if (IPAddress.TryParse(hostNameOrIPAddress, out resolveIPAddress))
                {
                    switch (addressFamily)
                    {
                        case AddressFamily.InterNetwork:      // we have IPv4
                            return (hostNameOrIPAddress);
                        case AddressFamily.InterNetworkV6:   // we have IPv6
                            return (hostNameOrIPAddress);
                        default:
                            // umm... yeah... I'm going to need to take your red packet and...
                            return null;
                    }
                }
                else
                {
                    IPHostEntry ipHostEntry = Dns.GetHostEntry(hostNameOrIPAddress);
                    IPAddress ipAddress = ipHostEntry.AddressList.First(a => a.AddressFamily == addressFamily);
                    return ipAddress?.ToString();
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        /// <summary>
        /// Gets the host name by ip address.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <returns>System.String.</returns>
        public static string GetHostNameByIPAddress(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                return entry.HostName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Dictionary<string, string> ParsingNcccTokenData(string tokenDataString)
        {
            try
            {
                if (tokenDataString == null) return null;
                var returnDictionary = new Dictionary<string, string>();
                const int tagIdConstLength = 2;
                const int tagIdDataLengthConstLength = 3;
                int startIndex = 0;
                var originalTokenDataLength = tokenDataString.Length;
                if (originalTokenDataLength < (tagIdConstLength + tagIdDataLengthConstLength)) return null;
                var tagId = tokenDataString.Substring(startIndex, tagIdConstLength);
                startIndex += tagIdConstLength;
                var tagIdDataStringLength = tokenDataString.Substring(startIndex, tagIdDataLengthConstLength);
                startIndex += tagIdDataLengthConstLength;
                int.TryParse(tagIdDataStringLength, out var tagIdDataLengthValue);
                var tagIdData = tokenDataString.Substring(startIndex, tagIdDataLengthValue);
                startIndex += tagIdData.Length;
                while (startIndex <= originalTokenDataLength)
                {
                    returnDictionary[tagId] = tagIdData;
                    //Get Next Data
                    if ((startIndex + tagIdConstLength + tagIdDataLengthConstLength) >= originalTokenDataLength)
                    {
                        return null;
                    }
                    tagId = tokenDataString.Substring(startIndex, tagIdConstLength);
                    startIndex += tagIdConstLength;
                    tagIdDataStringLength = tokenDataString.Substring(startIndex, tagIdDataLengthConstLength);
                    startIndex += tagIdDataLengthConstLength;
                    int.TryParse(tagIdDataStringLength, out tagIdDataLengthValue);
                    tagIdData = tokenDataString.Substring(startIndex, tagIdDataLengthValue);
                    startIndex += tagIdData.Length;
                }
                return returnDictionary;
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>
        /// Get The SubArray From  Array
        /// </summary>
        ///<param name="array">Input Bytes Array Data</param>
        ///<param name="offset">Offset Of Bytes Array</param>
        ///<param name="length">Get Array length</param>
        ///<returns>Sub Bytes Array</returns>
        public static byte[] GetSubBytes(byte[] array, int offset, int length)
        {
            int copyLength;
            if (array.Length <= 0) return null;
            if (offset > (array.Length - 1)) return null;
            if (((offset + length) > array.Length))
                copyLength = array.Length - offset;
            else
                copyLength = length;
            var result = new byte[copyLength];
            Array.Copy(array, offset, result, 0, copyLength);
            return (result);
        }

        /// <summary>
        /// Convert Byte Array To String(Binary Code Decimal 0xAA --> AA)
        /// </summary>
        ///<param name="hexBinArray">Binary Array</param>
        ///<returns>HexDecimal String</returns>

        public static string HexBinary2HexString(byte[] hexBinArray)
        {
            if (hexBinArray == null)
                return null;
            StringBuilder tempStringBuilder = new StringBuilder("");
            foreach (byte byteData in hexBinArray)
            {
                byte leftData = (byte)(byteData >> 4);
                byte rightData = (byte)((byte)byteData & 0x0f);
                tempStringBuilder.Append(Convert.ToString(leftData, 16).ToUpper() + Convert.ToString(rightData, 16).ToUpper());
            }
            return (tempStringBuilder.ToString());
        }


        /// <summary>
        /// Convert HexDecimal String To Byte Array AA01 --> 0xaa,0x01)
        /// </summary>
        ///<param name="hexBytes">HexDecimal String</param>
        ///<returns>Byte Array</returns>
        public static byte[] HexString2HexBinary(byte[] hexBytes)
        {
            string convertString;
            if (hexBytes == null)
                return null;
            if (hexBytes.Length % 2 != 0)
            {
                convertString = "0" + Encoding.ASCII.GetString(hexBytes);
            }
            else
            {
                convertString = Encoding.ASCII.GetString(hexBytes);
            }
            foreach (char ch in convertString)
            {
                char upch = Char.ToUpper(ch);
                if (((upch >= '0') && (upch <= '9')) ||
                    ((upch >= 'A') && (upch <= 'F')))
                    continue;
                return null;
            }
            int loopCount = (convertString.Length / 2);
            var hexBinary = new byte[loopCount];
            for (int index = 0; index < loopCount; index++)
            {
                int startPos = (2 * index);
                byte leftByte = Convert.ToByte(convertString.Substring(startPos, 1), 16);
                byte rightByte = Convert.ToByte(convertString.Substring((startPos + 1), 1), 16);
                var comByte = (byte)((byte)(leftByte << 4) | rightByte);
                hexBinary[index] = comByte;
            }
            return (hexBinary);
        }

        /// <summary>
        /// Convert HexDecimal String To Byte Array AA01 --> 0xaa,0x01)
        /// </summary>
        /// <param name="hexString">HexDecimal String</param>
        /// <returns>Byte Array</returns>
        public static byte[] HexString2HexBinary(string hexString)
        {
            string convertString;
            if (hexString == null)
                return null;
            if (hexString.Length % 2 != 0)
            {
                convertString = "0" + hexString;
            }
            else
            {
                convertString = hexString;
            }
            foreach (char ch in convertString)
            {
                char upch = Char.ToUpper(ch);
                if (((upch >= '0') && (upch <= '9')) ||
                    ((upch >= 'A') && (upch <= 'F')))
                    continue;
                return null;
            }
            int loopCount = (convertString.Length / 2);
            var hexBinary = new byte[loopCount];
            for (int index = 0; index < loopCount; index++)
            {
                int startPos = (2 * index);
                byte leftByte = Convert.ToByte(convertString.Substring(startPos, 1), 16);
                byte rightByte = Convert.ToByte(convertString.Substring((startPos + 1), 1), 16);
                var comByte = (byte)((byte)(leftByte << 4) | rightByte);
                hexBinary[index] = comByte;
            }
            return (hexBinary);
        }

        /// <summary>
        /// Add 2 bytes Array Length to Original Arrays
        /// </summary>
        /// <param name="bytesArray"></param>
        /// <returns>Byte Array</returns>
        public static byte[] AddBinaryLength2BytesArray(byte[] bytesArray)
        {
            if (bytesArray == null) return null;
            var retnBytes = new byte[bytesArray.Length + 2];
            retnBytes[0] = (byte)(bytesArray.Length >> 8);
            retnBytes[1] = (byte)bytesArray.Length;
            Buffer.BlockCopy(bytesArray, 0, retnBytes, 2, bytesArray.Length);
            return retnBytes;
        }

        /// <summary>
        /// Converts the message.
        /// </summary>
        /// <param name="originalBytes">The original bytes.</param>
        /// <returns>Byte Array</returns>
        public static byte[] ConvertMessage(byte[] originalBytes)
        {
            //this code for convert fisc incoming 0310 asc fixed format message
            byte[] mtiAscBytes = { 0x00, 0x87, 0x30, 0x33, 0x31, 0x30 };
            byte[] bytes = CommonUtils.GetSubBytes(originalBytes, 0, 6);
            if (!bytes.SequenceEqual(mtiAscBytes))
                return originalBytes;
            int offset = 2;
            //convert fisc 0300 ASC bytes to EBCDIC
            byte[] originalDataBytes = CommonUtils.GetSubBytes(originalBytes, offset, originalBytes.Length - offset);
            Encoding ascii = Encoding.ASCII;
            Encoding ebcdic = Encoding.GetEncoding("IBM037");
            byte[] originalDataEBCDICBytes = Encoding.Convert(ascii, ebcdic, originalDataBytes);
            byte[] originalEBCDICBytes = CommonUtils.AddBinaryLength2BytesArray(originalDataEBCDICBytes);

            //example F0-F3-F1-F0-00-20-00-00-02-00-00-00-F1-F2-F3-F4-F5-F6-F0-F0
            byte[] byteArray = Enumerable.Repeat((byte)0x00, (20 + 2)).ToArray();
            byteArray[00] = 0x00;
            byteArray[01] = 0x14;
            int startIndex = 2;
            int endIndex = 5;
            int byArrayIndex = 2;
            for (int index = startIndex; index <= endIndex; index++)
                byteArray[byArrayIndex++] = originalEBCDICBytes[index];

            byteArray[06] = 0x00;
            byteArray[07] = 0x20;
            byteArray[08] = 0x00;
            byteArray[09] = 0x00;
            byteArray[10] = 0x02;
            byteArray[11] = 0x00;
            byteArray[12] = 0x00;
            byteArray[13] = 0x00;
            startIndex = 77;
            endIndex = 82;
            byArrayIndex = 14;
            for (int index = startIndex; index <= endIndex; index++)
                byteArray[byArrayIndex++] = originalEBCDICBytes[index];
            startIndex = 75;
            endIndex = 76;
            byArrayIndex = 20;
            for (int index = startIndex; index <= endIndex; index++)
                byteArray[byArrayIndex++] = originalEBCDICBytes[index];
            return byteArray;
        }
    }
}

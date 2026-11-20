using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Core utility class providing common functions for authorization processing, database operations, 
    /// encryption/decryption, SMS messaging, and ISO 8583 message handling.
    /// </summary>
    public static partial class ColaCommUtility
    {

        ///<summary>
        ///Convert byte Array To Structure
        ///</summary>
        ///<param name="byteArray">byte Array</param>
        ///<param name="structObject">Structure Object</param>
        public static void ByteArrayToStructure(byte[] byteArray, ref object structObject)
        {
            int structLength = Marshal.SizeOf(structObject);
            IntPtr ptr = Marshal.AllocHGlobal(structLength);
            Marshal.Copy(byteArray, 0, ptr, structLength);
            structObject = Marshal.PtrToStructure(ptr, structObject.GetType());
            Marshal.FreeHGlobal(ptr);
        }

        ///<summary>
        ///Convert Structure To byte Array
        ///</summary>
        ///<param name="structObject">Structure Object</param>
        ///<returns>byte array</returns>
        public static byte[] StructureToByteArray(object structObject)
        {
            try
            {
                int structLength = Marshal.SizeOf(structObject);
                var byteArray = new byte[structLength];
                IntPtr ptr = Marshal.AllocHGlobal(structLength);
                Marshal.StructureToPtr(structObject, ptr, true);
                Marshal.Copy(ptr, byteArray, 0, structLength);
                Marshal.FreeHGlobal(ptr);
                return byteArray;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"StructureToByteArray Exception:{ex.Message}");
                return null;
            }
        }

        ///<summary>
        ///Convert Integer To Bytes Array(2 byte)
        ///</summary>
        ///<param name="inputData">Input Integer Data</param>
        ///<returns>byte Array</returns>

        public static byte[] IntegerToBinary(int inputData)
        {
            try
            {
                if ((inputData < 0) || (inputData > Convert.ToUInt16("FFFF", 16)))
                    return null;
                var convertData = new byte[2];
                convertData[0] = Convert.ToByte(inputData / 256);
                convertData[1] = Convert.ToByte(inputData % 256);
                return (convertData);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IntegerToBinary Exception:{ex.Message}");
                return null;
            }
        }

        ///<summary>
        ///Convert Bytes Array To Integer
        ///</summary>
        ///<param name="inputData">Input Bytes Array Data</param>
        ///<returns>integer</returns>

        public static int BinaryToInteger(byte[] inputData)
        {
            if (inputData.Length != 2)
                return (-1);
            int convertData = (Convert.ToInt16(inputData[0]) * 256) +
                              (Convert.ToInt16(inputData[1]));
            return (convertData);
        }

        ///<summary>
        ///Concatenate 2 Bytes Array
        ///</summary>
        ///<param name="firstArray">First Bytes Array</param>
        ///<param name="secondArray">Second Bytes Array</param>
        ///<returns>Combined Byte Array</returns>

        public static byte[] ContactBytesArray(byte[] firstArray, byte[] secondArray)
        {
            var byteConcat = new byte[firstArray.Length + secondArray.Length];
            Buffer.BlockCopy(firstArray, 0, byteConcat, 0, firstArray.Length);
            Buffer.BlockCopy(secondArray, 0, byteConcat, firstArray.Length, secondArray.Length);
            return (byteConcat);
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
        /// Convert HexDecimal String To Binary String(AA --> 10101010)
        /// </summary>
        ///<param name="hexString">HexDecimal String</param>
        ///<returns>Binary String</returns>

        public static string HexString2BinString(string hexString)
        {
            StringBuilder tempStringBuilder = new StringBuilder("");
            if (hexString == null)
                return null;
            foreach (char ch in hexString)
            {
                char upch = Char.ToUpper(ch);
                if (((upch >= '0') && (upch <= '9')) ||
                    ((upch >= 'A') && (upch <= 'F')))
                {
                    string tempString = Convert.ToString(Convert.ToInt32(upch.ToString(), 16), 2);
                    tempStringBuilder.Append(tempString.PadLeft(4, '0'));
                }
            }
            return (tempStringBuilder.ToString());
        }

        /// <summary>
        /// Convert Binary String To HexDecimal String(10101010 --> AA)
        /// </summary>
        ///<param name="binString">Binary String</param>
        ///<returns>HexDecimal String</returns>

        public static string BinString2HexString(string binString)
        {
            if (binString == null)
                return null;
            foreach (char ch in binString)
            {
                char upperChar = Char.ToUpper(ch);
                if ((upperChar == '0') || (upperChar == '1'))
                    continue;
                return null;
            }
            StringBuilder tempStringBuilder = new StringBuilder("");
            int binStringLength = binString.Length;
            int padTotalLength = 0;
            if ((binStringLength % 4) != 0)
                padTotalLength = ((binStringLength / 4) + 1) * 4;
            string processString = binString.PadLeft(padTotalLength, '0');
            int loopCount = processString.Length / 4;
            for (int index = 0; index < loopCount; index++)
            {
                string processSubString = processString.Substring((index * 4), 4);
                tempStringBuilder.Append(Convert.ToString(Convert.ToInt32(processSubString, 2), 16).ToUpper());
            }
            return (tempStringBuilder.ToString());
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
                byte rightData = (byte)(byteData & 0x0f);
                tempStringBuilder.Append(Convert.ToString(leftData, 16).ToUpper() + Convert.ToString(rightData, 16).ToUpper());
            }
            return (tempStringBuilder.ToString());
        }

        /// <summary>
        /// Convert HexDecimal String To Byte Array AA01 --> 0xaa,0x01)
        /// </summary>
        ///<param name="hexString">HexDecimal String</param>
        ///<returns>Byte Array</returns>
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
        /// Add 2 bytes Array Length to Original Arrays
        /// </summary>
        /// <param name="bytesArray"></param>
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
        /// Mapping Byte Array To Structure
        /// </summary>
        ///<param name="rawData">Raw Bytes Data Array</param>
        ///<param name="anyType">Mapping Structure Type</param>
        ///<returns>Structure Data</returns>
        public static object RawDeserialize(byte[] rawData, System.Type anyType)
        {
            if (rawData == null) return (new object());
            int rawSize = Marshal.SizeOf(anyType);
            if (rawSize > rawData.Length)
                return (new object());
            IntPtr buffer = Marshal.AllocHGlobal(rawSize);
            try
            {
                Marshal.Copy(rawData, 0, buffer, rawSize);
                object retobj = Marshal.PtrToStructure(buffer, anyType);
                Marshal.FreeHGlobal(buffer);
                return (retobj);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"RawDeserialize Exception:{ex.Message}");
                return (new object());
            }
        }

        ///  <summary>
        ///  Mapping Byte Array To Structure
        ///  </summary>
        /// <param name="msg"></param>
        /// <returns>Structure Data</returns>
        public static byte[] RawSerialize<T>(T msg) where T : struct
        {
            int objectSize = Marshal.SizeOf(typeof(T));
            byte[] retnBytes = new byte[objectSize];
            IntPtr buffer = Marshal.AllocHGlobal(objectSize);
            try
            {
                Marshal.StructureToPtr(msg, buffer, true);
                Marshal.Copy(buffer, retnBytes, 0, objectSize);
                Marshal.FreeHGlobal(buffer);
                return retnBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"RawSerialize Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serializes the structure.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg">The MSG.</param>
        /// <returns>System.Byte[].</returns>
        public static byte[] SerializeStruct<T>(T msg) where T : struct
        {
            try
            {
                var objectSize = Marshal.SizeOf(typeof(T));
                var retBytes = new byte[objectSize];
                IntPtr buff = Marshal.AllocHGlobal(objectSize);
                Marshal.StructureToPtr(msg, buff, true);
                Marshal.Copy(buff, retBytes, 0, objectSize);
                Marshal.FreeHGlobal(buff);
                return retBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SerializeStruct Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes the MSG.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public static T DeserializeMsg<T>(Byte[] data) where T : struct
        {
            try
            {
                var objectSize = Marshal.SizeOf(typeof(T));
                IntPtr buff = Marshal.AllocHGlobal(objectSize);
                Marshal.Copy(data, 0, buff, objectSize);
                T returnStruct = (T)Marshal.PtrToStructure(buff, typeof(T));
                Marshal.FreeHGlobal(buff);
                return returnStruct;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"DeserializeMsg Exception:{ex.Message}");
                return default(T);
            }
        }
    }
}

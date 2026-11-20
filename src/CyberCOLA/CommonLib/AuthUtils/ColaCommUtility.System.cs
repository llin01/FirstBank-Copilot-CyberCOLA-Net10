using System;
using System.Diagnostics;
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
        ///Check Process Is Exist
        ///</summary>
        ///<param name="processName">Windows Process Name</param>
        ///<returns>bool</returns>

        public static bool IsProcessExist(string processName)
        {
            Process[] processList = Process.GetProcesses();
            foreach (Process process in processList)
            {
                if (processName.Equals(process.ProcessName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Display Byte Array Data
        /// </summary>
        ///<param name="rawData">Display Bytes Raw Data</param>
        ///<returns>Display Raw Data String</returns>
        public static string GetDisplayForRawData(byte[] rawData)
        {
            const string stringFormat = "Length[{0:X2}{1:X2}],Data[{2}]";
            if (rawData == null)
                return ("Length[0],Data[null]");
            if (rawData.Length <= 2)
            {
                switch (rawData.Length)
                {
                    case 0:
                        return ("Length[0],Data[]");
                    case 1:
                        return ($"Length[{rawData[0]:X2}],Data[]");
                    case 2:
                        return ($"Length[{rawData[0]:X2}{rawData[1]:X2}],Data[]");
                }
            }
            return (string.Format(stringFormat,
                                 rawData[0],
                                 rawData[1],
                                 Encoding.ASCII.GetString(GetSubBytes(rawData, 2, rawData.Length - 2))
                                ));
        }

        /// <summary>
        /// Display Call Store Procedure Return Strings Array Data
        /// </summary>
        ///<param name="storeProcedureName">Store Procedure Name</param>
        ///<param name="returnStrings">Call Store Procedure Return String Array</param>
        ///<returns>Return String Array Data</returns>
        public static string DisplaySpReturnStrings(string storeProcedureName, string?[] returnStrings)
        {
            return DisplaySpReturnStrings(storeProcedureName, returnStrings, Array.Empty<int>());
        }

        /// <summary>
        /// Display Call Store Procedure Return Strings Array Data, hiding the values at maskIndexes
        /// </summary>
        ///<param name="storeProcedureName">Store Procedure Name</param>
        ///<param name="returnStrings">Call Store Procedure Return String Array</param>
        ///<param name="maskIndexes">Indexes whose values (activation code, SMS text, name, phone, e-mail, customer id) must not reach the log</param>
        ///<returns>Return String Array Data</returns>
        // Elements may be null (output parameters the stored procedure left unset); those are skipped.
        public static string DisplaySpReturnStrings(string storeProcedureName, string?[] returnStrings, params int[] maskIndexes)
        {
            bool addFlag = false;
            var stringBuilder = new StringBuilder(storeProcedureName + "--->");
            try
            {
                for (int index = 0; index < returnStrings.Length; index++)
                {
                    if (returnStrings[index] != null)
                    {
                        // Keep the length so an empty or truncated value is still visible when debugging.
                        var value = Array.IndexOf(maskIndexes, index) >= 0 ? $"***(len {returnStrings[index].Length})" : returnStrings[index];
                        if (!addFlag)
                            stringBuilder.Append($"index[{index}]--Data[{value}]");
                        else
                            stringBuilder.Append($",index[{index}]--Data[{value}]");
                        if (!addFlag) addFlag = true;
                    }
                }
                return (stringBuilder.ToString());

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"DisplaySpReturnStrings Exception:{ex.Message}");
                return (string.Empty);
            }
        }
    }
}

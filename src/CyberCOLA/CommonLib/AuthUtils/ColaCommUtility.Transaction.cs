using Com.Cybersoft4u.CommonLib.Resource;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Core utility class providing common functions for authorization processing, database operations, 
    /// encryption/decryption, SMS messaging, and ISO 8583 message handling.
    /// </summary>
    public static partial class ColaCommUtility
    {
        /// <summary>
        /// Get Next Settlement Date For Cut-Over 0800 Message Type(Information Code (S70:201)
        /// </summary>
        ///<param name="colaSystemDate">Coal System Date(yyyy-mm-dd 00:00:00)</param>
        ///<param name="isoSettlementDate">0800 Settlement Date(mmdd)</param>
        ///<returns>isoSettlementDate + 1 Days(yyyy-mm-dd 00:00:00)</returns>
        public static string GetNextSettlementDateForCutover(string colaSystemDate, string isoSettlementDate)
        {
            try
            {
                if (string.IsNullOrEmpty(isoSettlementDate))
                {
                    Log.Fatal($"GetNextSettlementDateForCutover Create Date Error, Input isoSettlementDate invalid");
                    return null;
                }
                if (string.IsNullOrEmpty(colaSystemDate) || colaSystemDate.Length < 10)
                {
                    colaSystemDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                var isoSettlementDateTime = new DateTime(Convert.ToInt32(colaSystemDate.Substring(0, 4)),
                                                         Convert.ToInt32(isoSettlementDate.Substring(0, 2)),
                                                         Convert.ToInt32(isoSettlementDate.Substring(2, 2)),
                                                         0,
                                                         0,
                                                         0);
                DateTime isoNextSettlementDateTime = isoSettlementDateTime.AddDays(1);
                string isoNextSettlementDateTimeString = isoNextSettlementDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                if (String.CompareOrdinal(colaSystemDate, isoNextSettlementDateTimeString) >= 0)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              (new StackTrace(true)).GetFrame(0)?.GetMethod()?.Name,
                              "GetNextSettlementDateForCutover Create Date Error",
                              colaSystemDate + "--" + isoNextSettlementDateTimeString);
                    Log.Fatal($"OldColaSystemDate[{colaSystemDate}], ISONextSettlementDateTime[{isoNextSettlementDateTimeString}]");
                    return null;
                }
                return (isoNextSettlementDateTimeString);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"GetNextSettlementDateForCutOver Exception:{ex.Message},isoSettlementDate[{isoSettlementDate}]");
                return null;
            }
        }

        ///  <summary>
        ///  Get Next Settlement Date For Cut-Over 0800 Message Type(Information Code (S70:201) depend on CardInfo
        ///  </summary>
        ///  <param name="cardInfo"></param>
        ///  <param name="isoSettlementDate">0800 Settlement Date(mmdd)</param>
        ///  <returns>isoSettlementDate + 1 Days(yyyy-mm-dd 00:00:00)</returns>
        public static string GetNextSettlementDateForCutover(int cardInfo, string isoSettlementDate)
        {
            try
            {
                string colaSystemDate;
                if (string.IsNullOrEmpty(isoSettlementDate))
                {
                    Log.Fatal($"GetNcccNextSettlementDateForCutover Create Date Error, Input isoSettlementDate invalid");
                    return null;
                }
                switch (cardInfo)
                {
                    case SystemConstantData.CommonData.PasystfNCCCInfo:
                        colaSystemDate = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCSystemDt;
                        break;
                    case SystemConstantData.CommonData.PasystfFISCInfo:
                        colaSystemDate = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().FISCSystemDt;
                        break;
                    case SystemConstantData.CommonData.PasystfTWINInfo:
                        colaSystemDate = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().TWINSystemDt;
                        break;
                    default:
                        Log.Fatal($"GetNextSettlementDateForCutover Error,Input Error[{cardInfo}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}, Value[{isoSettlementDate}]");
                        return null;

                }
                if (string.IsNullOrEmpty(colaSystemDate) || colaSystemDate.Length < 10)
                {
                    colaSystemDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                //增加colaSystemDate的日期判斷,避免交易重覆導致日期跨年出現問題
                DateTime isoNextSettlementDateTime;
                string ncccSystemDt = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCSystemDt;
                if (string.IsNullOrEmpty(ncccSystemDt) || ncccSystemDt.Length < 10)
                {
                    ncccSystemDt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                var OrigSystemDte = DateTime.ParseExact(ncccSystemDt,
                                                        "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                if (isoSettlementDate.Equals("1231") && OrigSystemDte.Month == 1 && OrigSystemDte.Day == 1)
                {
                    isoNextSettlementDateTime = OrigSystemDte;
                }
                else
                {
                    var isoSettlementDateTime = new DateTime(Convert.ToInt32(colaSystemDate.Substring(0, 4)),
                                                             Convert.ToInt32(isoSettlementDate.Substring(0, 2)),
                                                             Convert.ToInt32(isoSettlementDate.Substring(2, 2)),
                                                             0,
                                                             0,
                                                             0);
                    isoNextSettlementDateTime = isoSettlementDateTime.AddDays(1);
                }
                string isoNextSettlementDateTimeString = isoNextSettlementDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                if (String.CompareOrdinal(colaSystemDate, isoNextSettlementDateTimeString) > 0)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              (new StackTrace(true)).GetFrame(0)?.GetMethod()?.Name,
                              "GetNcccNextSettlementDateForCutover Create Date Error",
                              colaSystemDate + "--" + isoNextSettlementDateTimeString);
                    Log.Fatal($"OldColaSystemDate[{colaSystemDate}], ISONextSettlementDateTime[{isoNextSettlementDateTimeString}]");
                    return null;
                }
                return (isoNextSettlementDateTimeString);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"GetNextSettlementDateForCutOver Exception:{ex.Message},isoSettlementDate[{isoSettlementDate}]");
                return null;
            }
        }

        /// <summary>
        /// Use RNGCryptoServiceProvider Create Unique Key
        /// </summary>
        ///<param name="dateTimeString">DateTime String(yyyyMMddHHmmss</param>
        ///<returns>Unique Key</returns>
        public static string GetUniqueKey(string dateTimeString)
        {
            const int maxSize = 8;
            var chars = new char[62];
            string a;
            a = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            chars = a.ToCharArray();
            int size = maxSize;
            var data = new byte[1];
            var crypto = RandomNumberGenerator.Create();
            crypto.GetNonZeroBytes(data);
            size = maxSize;
            data = new byte[size];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length - 1)]);
            }
            return (dateTimeString + result.ToString());
        }

        /// <summary>
        /// Get Approve Code 1...999999
        /// </summary>
        /// <returns>
        /// String
        /// </returns>
        public static string GetApproveCode()
        {
            //const int startApproveCode = 1;
            //const int endApproveCode = 999999;
            //Random randomSeed = new Random(Guid.NewGuid().GetHashCode());
            //return ($"{randomSeed.Next(startApproveCode, endApproveCode):000000}");
            int approveCode = GetSecurityNumber(999999) + 1;
            return (approveCode.ToString("000000"));
        }
        /// <summary>
        /// Get System Trace Audit Number 1...899999
        /// </summary>
        /// <returns>
        /// String
        /// </returns>
        public static string GetSystemTraceNumber()
        {
            //return (DateTime.Now.ToString("HHmmss"));
            //return ($"{TraceNumberSeed.Next(1, 899999):000000}");
            int traceNumber = GetSecurityNumber(899999) + 1;
            return (traceNumber.ToString("000000"));
        }

        /// <summary>
        /// Get Settlement Date
        /// </summary>
        /// <returns>
        /// String
        /// </returns>
        public static string GetSettlementDate()
        {
            var structInfo = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct();
            string systemDte = structInfo.NCCCSystemDt;
            if (string.IsNullOrEmpty(systemDte) || systemDte.Length < 10)
            {
                return DateTime.Now.ToString("MMdd");
            }
            return (systemDte.Substring(5, 2) + systemDte.Substring(8, 2));
        }
        public static string GetSettlementDate(int cardInfo)
        {
            var structInfo = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct();
            string systemDte;
            switch (cardInfo)
            {
                case SystemConstantData.CommonData.PasystfNCCCInfo:
                    systemDte = structInfo.NCCCSystemDt;
                    break;
                case SystemConstantData.CommonData.PasystfFISCInfo:
                    systemDte = structInfo.FISCSystemDt;
                    break;
                case SystemConstantData.CommonData.PasystfTWINInfo:
                    systemDte = structInfo.TWINSystemDt;
                    break;
                default:
                    Log.Error($"GetSettlementDat, Input Error[{cardInfo}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    return null;
            }
            if (string.IsNullOrEmpty(systemDte) || systemDte.Length < 10)
            {
                return DateTime.Now.ToString("MMdd");
            }
            return (systemDte.Substring(5, 2) + systemDte.Substring(8, 2));
        }

        /// <summary>
        /// Get Capture Date
        /// </summary>
        /// <returns>
        /// String
        /// </returns>
        public static string GetCaptureDate()
        {
            //string systemDte = InitialSystemParm.GetColaPasystfInfoStruct().SystemDte;
            string systemDte = DateTime.Now.ToString("yyyyMMddHHmmss");
            return (systemDte.Substring(4, 4));
        }
    }
}

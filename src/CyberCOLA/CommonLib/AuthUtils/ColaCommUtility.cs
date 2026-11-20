using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Network.Client;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.FISC.DE48;
using Com.Cybersoft4u.TM.FISC.DE58;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using Com.Cybersoft4u.TM.ISO8583.TLV;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Core utility class providing common functions for authorization processing, database operations, 
    /// encryption/decryption, SMS messaging, and ISO 8583 message handling.
    /// </summary>
    public static partial class ColaCommUtility
    {
        /// <summary>
        /// Gets the NLog logger instance for this utility class.
        /// </summary>
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        /// <summary>
        /// Gets the singleton instance of SystemParameterInfo from the resource configuration.
        /// </summary>
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        /// <summary>
        /// Gets the singleton instance of SMSWebAPIInfo.
        /// </summary>
        private static readonly SingletonOfSMSWebAPIInfo SingletonOfSMSWebAPIInfo = SingletonOfSMSWebAPIInfo.Instance;
        /// <summary>
        /// Controls whether the full connection string is displayed in logs.
        /// </summary>
        private static bool IsDisplayConnectionString = true;
        /// <summary>
        /// The initialization vector (IV) string used for AES-GCM encryption/decryption.
        /// </summary>
        private const string ivString = "87654321";
        /// <summary>
        /// Stores the decrypted connection string to avoid repeated decryption.
#nullable enable
        private static string? _ConnectionString = null;
#nullable restore

        /// <summary>
        /// Create FISC Field 48 Tag 45 & 87
        /// authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        /// authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        /// authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        /// authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        /// authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        /// authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        /// authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        /// authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        /// authReturnString[18]:@vdBlockAmt
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// FISC P48 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateFisc0100P48Data(FISC requestFiscMessage,
                                                   FiscAuthInfoStruct authInfoData,
                                                   string[] authReturnString)
        {
            try
            {
                var fieldPos = FIELD.F_48;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    return (null);
                string originalDE48Data = requestFiscMessage.RawVal(fieldPos).ToString();
                if (string.IsNullOrEmpty(originalDE48Data)) return null;
                var de48TagField = new DE48TagField(originalDE48Data);
                var de48TagIdSortedList = de48TagField.GetSortedListDE48TagField();

                //ARQC Check
                string tagIdString = "74";
                if (!de48TagField.FindDE48TagIdExist(tagIdString))
                {
                    if (authInfoData.arqcCheck.Equals('Y'))
                    {
                        //string tempString;
                        if (authInfoData.arqcChkResult.Equals('Y'))
                            de48TagIdSortedList.Add(tagIdString, "50S");
                        else
                            de48TagIdSortedList.Add(tagIdString, "50I");
                    }
                }
                //Add Check CVV2
                tagIdString = "87";
                if (!de48TagField.FindDE48TagIdExist(tagIdString))
                {
                    string prefix = authInfoData.isoP2CardNbr.Substring(0, 1);
                    if (authInfoData.cvv2Check.Equals('Y'))
                    {
                        if (authInfoData.cvv2ChkResult.Equals('Y'))
                        {
                            de48TagIdSortedList.Add(tagIdString, "M");
                        }
                        else
                        {
                            de48TagIdSortedList.Add(tagIdString, "N");
                        }
                    }
                    else
                    {
                        if (prefix.Equals("4") || prefix.Equals("3"))
                        {
                            const string tagId92String = "92";
                            if (!de48TagField.FindDE48TagIdExist(tagId92String))
                            {
                                string cvv2Value = de48TagField.GetDE48TagDataByTokenId(tagId92String);
                                if (string.IsNullOrEmpty(cvv2Value))
                                {
                                    de48TagIdSortedList.Add(tagIdString, "P");
                                }
                            }
                        }
                    }
                }
                tagIdString = "45";
                if (!de48TagField.FindDE48TagIdExist(tagIdString))
                {
                    if (authInfoData.isoP48T45 != null && authInfoData.isoP48T45.Length == 1)
                    {
                        de48TagIdSortedList.Add(tagIdString, authInfoData.isoP48T45);
                    }
                }

                var cardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                //#48 Tag 92 CVV2的移除一定要放在最後一個位置
                tagIdString = "92";
                if (de48TagField.FindDE48TagIdExist(tagIdString) && cardType.Equals(CreditCardType.JCB))
                {
                    de48TagIdSortedList.Remove(tagIdString);
                }
                //#48 Tag 42 Electronic Commerce Security Level Indicator移除
                tagIdString = "42";
                if (de48TagField.FindDE48TagIdExist(tagIdString) && cardType.Equals(CreditCardType.JCB))
                {
                    de48TagIdSortedList.Remove(tagIdString);
                }
                //#48 Tag 43 CAVV的移除
                tagIdString = "43";
                if (de48TagField.FindDE48TagIdExist(tagIdString) && cardType.Equals(CreditCardType.JCB))
                {
                    de48TagIdSortedList.Remove(tagIdString);
                }
                //#48 Tag 44 3-D Secure Electronic Commerce Tx Identifier
                tagIdString = "44";
                if (de48TagField.FindDE48TagIdExist(tagIdString) && cardType.Equals(CreditCardType.JCB))
                {
                    de48TagIdSortedList.Remove(tagIdString);
                }
                var stringBuilder = new StringBuilder(de48TagField.GetDE48TCC());
                foreach (var data in de48TagIdSortedList)
                {
                    stringBuilder.Append($"{data.Key}{Convert.ToInt16(data.Value.Length):00}{data.Value}");
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[CreateFisc0100P48Data Error]-- Exception:[{ex.Message}]");

                return null;
            }
        }

        /// <summary>
        /// Create FISC Field 58
        /// Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        /// authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        /// authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        /// authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        /// authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        /// authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        /// authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        /// authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        /// authReturnString[18]:@vdBlockAmt
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// FISC P58 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateFisc0100P58Data(FISC requestFiscMessage,
                                                   FiscAuthInfoStruct authInfoData,
                                                   string[] authReturnString)
        {
            try
            {
                var fieldPos = FIELD.F_58;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    return (null);
                string originalDE58Data = requestFiscMessage.RawVal(fieldPos).ToString();
                if (string.IsNullOrEmpty(originalDE58Data)) return null;
                var de58TagField = new DE58TagField(originalDE58Data);
                var de58TagIdSortedList = de58TagField.GetSortedListDE58TagField();

                // cash rebate information 
                // authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
                // authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
                // authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
                string tagIdString = "21";
                if (authReturnString[04].Length == 1 && authReturnString[05].Length == 2
                    && authReturnString.Length > 10)
                {
                    if (de58TagField.FindDE58TagIdExist(tagIdString))
                    {
                        de58TagIdSortedList.Remove(tagIdString);
                    }

                    de58TagIdSortedList.Add(tagIdString,
                    string.Format(SystemConstantData.CommonData.FISCField48Tag21RebateStringFormat,
                                                                    authInfoData.isoP58T21LoyaltyNbr,
                                                                    authReturnString[04],
                                                                    authReturnString[05],
                                                                    authReturnString[06],
                                                                    authReturnString[07],
                                                                    authReturnString[08],
                                                                    authReturnString[09]));
                }
                var stringBuilder = new StringBuilder();
                foreach (var data in de58TagIdSortedList)
                {
                    stringBuilder.Append($"{data.Key}{Convert.ToInt16(data.Value.Length):00}{data.Value}");
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[CreateFisc0100P58Data Error]-- Exception:[{ex.Message}]");

                return (null);
            }
        }

        /// authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        /// authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        /// authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        /// authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        /// authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        /// authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        /// authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        /// authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        /// authReturnString[18]:@vdBlockAmt
        /// </summary>

        /// <summary>
        /// Create FISC Field 55 EMV Tag Data For Response Data
        /// Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP48T45             , authReturnString[03]:@isoS112DownPayment
        /// authReturnString[04]:@isoS112InstPayment    , authReturnString[05]:@isoS112InstFee
        /// authReturnString[06]:@isoS112InstTxResp     , authReturnString[07]:@isoP55ARPC
        /// authReturnString[08]:@isoP55RSP
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// FISC P55 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateFisc0100P55Data(FISC requestFiscMessage,
                                                   FiscAuthInfoStruct authInfoData,
                                                   string[] authReturnString,
                                                   string[] data9F10Array)
        {
            try
            {
                const int authReturnStringCount = 19;
                string arcString = string.Empty;
                string arpcString = string.Empty;

                //CreateB5ARPC(requestFiscMessage, authInfoData, authReturnString, data9F10Array, out arcString, out arpcString);

                //Add Input Have Token Data
                var fieldPos = FIELD.F_55;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    return null;
                string originalDE55Data = requestFiscMessage.RawVal(fieldPos).ToString();
                if (string.IsNullOrEmpty(originalDE55Data)) return null;
                var fiscTLV = new FISCTLV().Decode(HexStringConverter.ToByteArray(originalDE55Data));
                var emvTags = fiscTLV.Tags;

                //Check EMV Tag 91 For EMV Data
                var emvTagId = "91";
                if (!emvTags.ContainsKey(emvTagId))
                {
                    if ((authReturnString.Length == authReturnStringCount) &&
                        (arpcString.Trim().Length == 16) &&
                        (arcString != string.Empty))
                    {
                        Tag tag = new Tag();
                        tag.Id = emvTagId;
                        tag.Value = HexStringConverter.ToByteArray(arpcString + arcString);
                        emvTags.Add(emvTagId, tag);
                    }
                }

                //Check EMV Tag 8A For EMV Data
                emvTagId = "8A";
                if (!emvTags.ContainsKey(emvTagId))
                {
                    if (authReturnString[01] != null)
                    {
                        if (authReturnString[01].Length == 2)
                        {
                            Tag tag = new Tag();
                            tag.Id = emvTagId;
                            tag.Value = HexStringConverter.ToByteArray(authReturnString[01]);
                            emvTags.Add(emvTagId, tag);
                        }
                    }
                }

                var valByteBuf = fiscTLV.Encode();
                var valByteArray = valByteBuf.Array();

                if (valByteArray != null && valByteArray.Length > 0)
                    return HexStringConverter.FromByteArray(valByteArray);
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[CreateFisc0100P55Data Error]-- Exception:[{ex.Message}]");

                return (null);
            }
        }

        /// <summary>
        /// Create FISC Field 112
        /// Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        /// authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        /// authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        /// authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        /// authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        /// authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        /// authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        /// authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        /// authReturnString[18]:@vdBlockAmt
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// FISC S112 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateFisc0100S112Data(FISC requestFiscMessage,
                                                   FiscAuthInfoStruct authInfoData,
                                                   string[] authReturnString)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                if (authReturnString[10].Length == 8 && authReturnString[11].Length == 8 &&
                     authReturnString[12].Length == 6 && authReturnString[13].Length == 2
                    && authReturnString.Length > 12)
                {
                    stringBuilder.Append(
                    string.Format(SystemConstantData.CommonData.FISCField112InstallmentStringFormat,
                                                                    authInfoData.isoS112InstTxInd,
                                                                    authInfoData.isoS112InstPeriod,
                                                                    authReturnString[10],
                                                                    authReturnString[11],
                                                                    authReturnString[12],
                                                                    authReturnString[13])
                    );
                }
                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[CreateFisc0100P58Data Error]-- Exception:[{ex.Message}]");

                return (null);
            }
        }

        /// <summary>
        /// Create FISC Field 58
        /// FiscMessage0420 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode           , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction          , authReturnString[03]:@isoP58T21TxResp
        /// authReturnString[04]:@isoP58T21SignPoint       , authReturnString[05]:@isoP58T21PointBalance
        /// authReturnString[06]:@isoP58T21PointRedemption , authReturnString[07]:@isoP58T21PaidCAmt
        /// authReturnString[08]:@vdLinkNbr                , authReturnString[09]:@vdLinkNbrSeq
        /// authReturnString[10]:@vdActionCode             , authReturnString[11]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	 varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	 varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	         varchar(01)    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @isoP58T21TxResp             varchar(02)    Output,  --紅利回覆碼
        /// @isoP58T21SignPoint          varchar(01)    Output,  --< 紅利, 餘點正負項 >
        /// @isoP58T21PointBalance       varchar(10)    Output,  --< 紅利, 餘點 >
        /// @isoP58T21PointRedemption    varchar(10)    Output,  --< 紅利,折扺點數 >
        /// @isoP58T21PaidCAmt           varchar(12)    Output,  --< 紅利,實際支付金額 >
        /// @vdLinkNbr                   varchar(20)    Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq                varchar(02)    Output   VD 圈存帳戶序號
        /// @vdActionCode                varchar(01)    Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額
        /// </summary>

        /// <param name="requestFiscMessage">Request FISC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// FISC P58 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateFisc0420P58Data(FISC requestFiscMessage,
                                                   FiscAuthInfoStruct authInfoData,
                                                   string[] authReturnString)
        {
            try
            {
                var fieldPos = FIELD.F_58;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    return (null);
                string originalDE58Data = requestFiscMessage.RawVal(fieldPos).ToString();
                if (string.IsNullOrEmpty(originalDE58Data)) return null;
                var de58TagField = new DE58TagField(originalDE58Data);
                var de58TagIdSortedList = de58TagField.GetSortedListDE58TagField();

                //cash rebate information 
                // authReturnString[03]:@isoP58T21TxResp
                // authReturnString[04]:@isoP58T21SignPoint    , authReturnString[05]:@isoP58T21PointBalance
                // authReturnString[06]: @isoP58T21PointRedemption , authReturnString[07]:@isoP58T21PaidCAm

                string tagIdString = "21";
                if (authReturnString[03].Length == 2 && authReturnString[06].Length == 10
                    && authReturnString.Length > 10)
                {
                    if (de58TagField.FindDE58TagIdExist(tagIdString))
                    {
                        de58TagIdSortedList.Remove(tagIdString);
                    }

                    de58TagIdSortedList.Add(tagIdString,
                    string.Format(SystemConstantData.CommonData.FISCField48Tag21RebateStringFormat,
                                                                    authInfoData.isoP58T21LoyaltyNbr,
                                                                    authInfoData.isoP58T21LoyaltyInd,
                                                                    authReturnString[03],
                                                                    authReturnString[04],
                                                                    authReturnString[05],
                                                                    authReturnString[06],
                                                                    authReturnString[07]));
                }
                var stringBuilder = new StringBuilder();
                foreach (var data in de58TagIdSortedList)
                {
                    stringBuilder.Append($"{data.Key}{Convert.ToInt16(data.Value.Length):00}{data.Value}");
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[CreateFisc0100P58Data Error]-- Exception:[{ex.Message}]");

                return (null);
            }
        }
        public static string PrintBytesMap(byte[] bytes)
        {
            var dataOut = new StringBuilder();
            dataOut.Append(
                "         +-------------------------------------------------+\n         |  0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f |\n+--------+-------------------------------------------------+----------------+");
            var length = bytes.Length;
            var index = 0;
            var byteContent = string.Empty;
            var dataContent = string.Empty;
            foreach (var b in bytes)
            {
                if (index % 16 == 0)
                {
                    dataOut.Append($"\n|{(index / 16).ToString().PadLeft(8, '0')}| ");
                    byteContent = string.Empty;
                    dataContent = string.Empty;
                }

                byteContent += $"{b:X2} ";
                dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);

                if (index % 16 == 15 | index + 1 == length)
                {
                    dataOut.AppendFormat("{0}|{1}|", byteContent.PadRight(48, ' '), dataContent.PadRight(16, ' '));
                }

                index++;
            }

            dataOut.Append("\n+--------+-------------------------------------------------+----------------+");
            return dataOut.ToString();
        }

        public static string PrintBytesHex(byte[] bytes)
        {
            var dataOut = new StringBuilder();

            foreach (var b in bytes)
            {
                dataOut.Append($"{b:X2}");
            }

            return dataOut.ToString();
        }

        public static string PrintBytesData(byte[] bytes)
        {
            var dataOut = new StringBuilder();

            foreach (var b in bytes)
            {
                dataOut.Append(b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b));
            }

            return dataOut.ToString();
        }

        //Serializing Object To byte the List
        //public static byte[] ObjectSerialize<T>(T objectType)
        //{
        //    byte[] sendBytes = null;
        //    BinaryFormatter bf = new BinaryFormatter();
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        bf.Serialize(ms, objectType);
        //        sendBytes = ms.ToArray();
        //    }
        //    byte[] returnBytes = new byte[sendBytes.Length];
        //    Buffer.BlockCopy(sendBytes, 0, returnBytes, 0, sendBytes.Length);
        //    return (sendBytes);
        //}
        //
        //public static T ObjectDeserialize<T>(byte[] objectBytes)
        //{
        //    if (objectBytes == null) return (default(T));
        //    T returnObject = default(T);
        //    MemoryStream memoryStream = new MemoryStream(objectBytes);
        //    try
        //    {
        //        BinaryFormatter binaryFormatter = new BinaryFormatter();
        //        returnObject = (T) binaryFormatter.Deserialize(memoryStream);
        //    }
        //    catch (SerializationException ex)
        //    {
        //        Log.Fatal(ex, $"ObjectDeserialize Exception:{ex.Message}");
        //    }
        //    finally
        //    {
        //        memoryStream.Close();
        //    }
        //    return returnObject;
        //}

        /// <summary>
        /// Gets the object property.
        /// </summary>
        /// <param name="classObject">The class object.</param>
        /// <returns>System.String.</returns>
        public static string GetObjectProperty(object classObject)
        {
            var props = classObject.GetType().GetProperties();
            var sb = new StringBuilder().Append($"{Environment.NewLine}");
            foreach (var prop in props)
            {
                if (prop.PropertyType == typeof(byte[]))
                {
                    byte[] value = prop.GetValue(classObject, null) as byte[];
                    if (value != null)
                    {
                        sb.Append(prop.Name + "(Hex String  ): " + BitConverter.ToString(value).Replace("-", string.Empty) + Environment.NewLine);
                        sb.Append(prop.Name + "(ASCII String): " + Encoding.Default.GetString(value) + Environment.NewLine);
                    }
                }
                else
                    sb.Append(prop.Name + ": " + prop.GetValue(classObject, null) + Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the structure property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msgStruct">The MSG structure.</param>
        /// <returns>System.String.</returns>
        public static string GetStructProperty<T>(T msgStruct) where T : struct
        {
            var sb = new StringBuilder().Append(Environment.NewLine);
            foreach (var field in typeof(T).GetFields(BindingFlags.Instance |
                                                      BindingFlags.NonPublic |
                                                      BindingFlags.Public))
            {
                if (field.FieldType == typeof(byte[]))
                {
                    var content = field.GetValue(msgStruct) as byte[];
                    sb.Append(content == null ? $"{field.Name} = null" : $"{field.Name} = {Encoding.Default.GetString(content)}{Environment.NewLine}");
                }
                else
                    sb.Append($"{field.Name} = {field.GetValue(msgStruct)}{Environment.NewLine}");
            }
            return sb.ToString();
        }
        /// <summary>
        /// Determines whether [is synchronize pasys information data] [the specified NCCC].
        /// </summary>
        /// <param name="nccc">The NCCC.</param>
        /// <returns><c>true</c> if [is synchronize pasys information data] [the specified NCCC]; otherwise, <c>false</c>.</returns>
        public static bool IsSyncPasysInfoData(NcccIso8583 nccc)
        {
            try
            {
                if (!(nccc.Mti().Equals("0800") ||
                      nccc.Mti().Equals("0820") ||
                      nccc.Mti().Equals("0821")))
                {
                    return false;
                }

                var fieldPos = FIELD.F_70;
                var informationCode = !ObjectUtils.IsNull(nccc.RawVal(fieldPos)) ? nccc.RawVal(fieldPos).ToString() : string.Empty;
                if (string.IsNullOrEmpty(informationCode)) return false;
                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.Cutover) ||
                    informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.NewKey))
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IsSyncPasysInfoData Exception");
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[IsSyncPasysInfoData Exception]-- Exception:[{ex.Message}]");
                return (false);
            }
        }

        public static bool IsSyncPasysInfoData(TWINIso8583 twin)
        {
            try
            {
                if (!(twin.Mti().Equals("0800") ||
                      twin.Mti().Equals("0820") ||
                      twin.Mti().Equals("0821")))
                {
                    return false;
                }

                var fieldPos = FIELD.F_70;
                var informationCode = !ObjectUtils.IsNull(twin.RawVal(fieldPos)) ? twin.RawVal(fieldPos).ToString() : string.Empty;
                if (string.IsNullOrEmpty(informationCode)) return false;
                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.Cutover) ||
                    informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.NewKey))
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IsSyncPasysInfoData Exception");
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[IsSyncPasysInfoData Exception]-- Exception:[{ex.Message}]");
                return (false);
            }
        }

        // XmlSerializer instances from the plain XmlSerializer(Type) ctor are thread-safe for
        // Serialize/Deserialize, so one instance per Type can be reused across calls. This avoids
        // repeated type inspection / dynamic-assembly work on every serialization call.
        private static readonly ConcurrentDictionary<Type, XmlSerializer> s_xmlSerializers = new();

        private static XmlSerializer GetXmlSerializer(Type type) =>
            s_xmlSerializers.GetOrAdd(type, t => new XmlSerializer(t));

        /// <summary>
        /// Converts the object to XML string.
        /// </summary>
        /// <param name="classObject">The class object.</param>
        /// <returns>System.String.</returns>
        public static string ObjectToXMLString(object classObject)
        {
            try
            {
                string xmlString;
                XmlSerializer xmlSerializer = GetXmlSerializer(classObject.GetType());
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    xmlSerializer.Serialize(memoryStream, classObject);
                    memoryStream.Position = 0;
                    xmlString = new StreamReader(memoryStream).ReadToEnd();
                }
                return xmlString;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ObjectToXMLString Exception:{ex.Message}");
                return null;
            }
        }
        public static string ObjectToXMLString<T>(T classObject, bool indent = false) where T : class
        {
            try
            {
                // removes version
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;
                settings.Indent = indent;

                XmlSerializer xmlSerializer = GetXmlSerializer(typeof(T));
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sw, settings))
                    {
                        // removes namespace
                        var xmlns = new XmlSerializerNamespaces();
                        xmlns.Add(string.Empty, string.Empty);

                        xmlSerializer.Serialize(writer, classObject, xmlns);
                        return sw.ToString(); // XML String
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ObjectToXMLString<T> Exception:{ex.Message}");
                return null;
            }
        }
        public static T XStringToObject<T>(string xmlString)
        {
            try
            {
                XmlSerializer xmlSerializer = GetXmlSerializer(typeof(T));
                T classObject;
                using (StringReader stringReader = new StringReader(xmlString))
                {
                    classObject = (T)xmlSerializer.Deserialize(stringReader);
                }
                return classObject;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"XStringToObject<T> Exception:{ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Determines whether [is synchronize pasys information data] [the specified NCCC].
        /// </summary>
        /// <param name="nccc">The NCCC.</param>
        /// <returns><c>true</c> if [is synchronize pasys information data] [the specified NCCC]; otherwise, <c>false</c>.</returns>
        public static bool NCCCIsSyncPasysInfoData(NcccIso8583 nccc)
        {
            try
            {
                if (!(nccc.Mti().Equals("0800") ||
                      nccc.Mti().Equals("0820") ||
                      nccc.Mti().Equals("0821")))
                {
                    return false;
                }
                var fieldPos = FIELD.F_70;
                var informationCode = !ObjectUtils.IsNull(nccc.RawVal(fieldPos)) ? nccc.RawVal(fieldPos).ToString() : string.Empty;
                if (string.IsNullOrEmpty(informationCode)) return false;
                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.Cutover) ||
                    informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.NewKey))
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NCCCIsSyncPasysInfoData Exception:{ex.Message}");
                return (false);
            }
        }

        /// <summary>
        /// Converts the object to XML bytes.
        /// </summary>
        /// <param name="classObject">The class object.</param>
        /// <returns>System.String.</returns>
        public static byte[] ObjectToXMLBytes(object classObject)
        {
            const int maxMemorySize = 20 * 1024;
            try
            {
                byte[] xmlBytes;
                XmlSerializer xmlSerializer = GetXmlSerializer(classObject.GetType());
                using (MemoryStream memoryStream = new MemoryStream(maxMemorySize))
                {
                    xmlSerializer.Serialize(memoryStream, classObject);
                    memoryStream.Position = 0;
                    xmlBytes = memoryStream.ToArray();
                }
                return xmlBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ObjectToXMLBytes Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts XML bytes To Object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        public static T XMLBytesToObject<T>(byte[] bytes)
        {
            try
            {
                T classObject;
                if (bytes == null)
                {
                    Log.Error($"XMLBytesToObject data is null");
                    return default(T);
                }
                XmlSerializer xmlSerializer = GetXmlSerializer(typeof(T));
                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    classObject = (T)xmlSerializer.Deserialize(memoryStream);
                }
                return classObject;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"XMLBytesToObject Exception:{ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Converts XML string To Object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString"></param>
        public static T XMLBytesToObject<T>(string xmlString)
        {
            try
            {
                T classObject;
                XmlReaderSettings settings = new XmlReaderSettings { IgnoreWhitespace = false };
                using (XmlReader reader = XmlReader.Create(new StringReader(xmlString), settings))
                {
                    XmlSerializer serializer = GetXmlSerializer(typeof(T));
                    classObject = (T)serializer.Deserialize(reader);
                }
                return classObject;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"XMLBytesToObject Exception:{ex.Message}");
                return default(T);
            }
        }
        /// <summary></summary>
        /// <param name="xml"></param>
        public static string PrettyXml(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.XmlResolver = null;
                doc.LoadXml(xml);

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    NewLineChars = Environment.NewLine,
                    NewLineHandling = NewLineHandling.Replace,
                    Encoding = new UTF8Encoding(false)
                };

                using (var ms = new MemoryStream())
                using (var writer = XmlWriter.Create(ms, settings))
                {
                    doc.Save(writer);
                    var xmlString = Encoding.UTF8.GetString(ms.ToArray());
                    return xmlString;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"PrettyXml Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary></summary>
        /// <param name="rtnString"></param>
        public static string GetHSMReasonCodeMessage(string rtnString)
        {
            try
            {
                const int minRtnStringLength = 2 + 4 + 4;
                if (string.IsNullOrEmpty(rtnString) || rtnString.Length < minRtnStringLength)
                {
                    var tempString = $"GetHSMReasonCodeMessage Input RtnString Invalid[{rtnString}]";
                    Log.Error(tempString);
                    return tempString;
                }

                var startPos = 0;
                var dataLength = 2;
                var responseCodeString = SystemStaticData.HSMResponseCodeMapping[rtnString.Substring(startPos, dataLength)];

                startPos += dataLength;
                dataLength = 4;
                var returnCodeString = SystemStaticData.HSMReturnCodeMapping[rtnString.Substring(startPos, dataLength)];

                startPos += dataLength;
                dataLength = 4;
                var reasonCodeString = SystemStaticData.HSMReasonCodeMapping[rtnString.Substring(startPos, dataLength)];
                return $"Response Code[{responseCodeString}], Return Code[{returnCodeString}], Reason Code[{reasonCodeString}], Input String[{rtnString}]";
            }
            catch (Exception ex)
            {
                var tempString = $"GetHSMReasonCodeMessage Exception:{ex.Message}, Input RtnString[{rtnString}]";
                Log.Error(tempString);
                return tempString;
            }
        }

        /// <summary></summary>
        /// <param name="respCode"></param>
        /// <param name="rtnRsnCode"></param>
        /// <param name="rtnString"></param>
        public static string GetHSMReasonCodeMessage(string respCode, string rtnRsnCode, string rtnString)
        {
            try
            {
                Log.Debug($"respCode[{respCode}], rtnRsnCode[{rtnRsnCode}]");
                var respCodeString = string.Empty;
                if (!string.IsNullOrEmpty(respCode) && respCode.Length == 2)
                    SystemStaticData.HSMResponseCodeMapping.TryGetValue(respCode, out respCodeString);
                var rtnCodeString = string.Empty;
                var rsnCodeString = string.Empty;
                if (!string.IsNullOrEmpty(rtnRsnCode) && rtnRsnCode.Length == 8)
                {
                    SystemStaticData.HSMReturnCodeMapping.TryGetValue(rtnRsnCode.Substring(0, 4), out rtnCodeString);
                    SystemStaticData.HSMReasonCodeMapping.TryGetValue(rtnRsnCode.Substring(4, 4), out rsnCodeString);
                }
                return $"Response Code[{respCodeString}], Return Code[{rtnCodeString}], Reason Code[{rsnCodeString}], Return String[{rtnString}]";
            }
            catch (Exception ex)
            {
                Log.Error($"GetHSMReasonCodeMessage Exception:{ex.Message}, RespCode[{respCode}], RetRsnCode[{rtnRsnCode}], RtnString[{rtnString}]");
                return ex.Message;
            }
        }
        /// <summary></summary>
        /// <param name="currencyCode"></param>
        public static int GetDecimalPointByCurrencyCode(string currencyCode)
        {
            var defaultDecimalPoint = 2;
            if (string.IsNullOrEmpty(currencyCode))
                return defaultDecimalPoint;
            if (SystemStaticData.CurrencyDecimalNumber.ContainsKey(currencyCode))
            {
                return SystemStaticData.CurrencyDecimalNumber[currencyCode];
            }
            return defaultDecimalPoint;
        }

        /// <summary></summary>
        /// <param name="ncccMessage"></param>
        /// <param name="bicC6TokenStructData"></param>
        /// <param name="bicTrack2Data"></param>
        public static string CreateVisaCAVVPan(NcccIso8583 ncccMessage, NcccMessage.BicC6TokenStruct bicC6TokenStructData, NcccMessage.BicTrack2 bicTrack2Data)
        {
            try
            {
                if (bicTrack2Data == null)
                {
                    Log.Error("CreateVisaCAVVPan Data Invalid");
                    return null;
                }
                //C.F Check this ?????????
                //var field = FIELD.F_63;
                var field = ncccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(ncccMessage.GetTokenFieldId())))
                {
                    field = ncccMessage.GetTokenFieldId();
                }
                else if (!ObjectUtils.IsNull(ncccMessage.RawVal(FIELD.F_126)))
                {
                    field = FIELD.F_126;
                }
                var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                if (tokenGroup == null) return null;
                var tokenC0 = (TokenC0)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenC0.Id);
                StringBuilder sb = new StringBuilder();
                sb.Append($"{bicTrack2Data.IsoCardNbr.Substring(0, 15)}{tokenC0.MotoOrECFlag}");
                string c6AtnString = Encoding.ASCII.GetString(bicC6TokenStructData.c6_atn);
                string hexString = c6AtnString.Substring(0, 10);
                long decimalData = Int64.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                sb.Append($"{decimalData:D12}");
                sb.Append($"{c6AtnString.Substring(10, 3)}");
                sb.Append($"{DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss").Substring(2, 2)}");
                sb.Append($"{c6AtnString.Substring(13, 3)}");
                string inputData = sb.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(inputData);
                //byte[] hashBytes = System.Security.Cryptography.SHA256.Create().ComputeHash(bytes);
                byte[] hashBytes = null;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(bytes);
                }
                if (hashBytes == null)
                {
                    Log.Error("CreateVisaCAVVPan HashBytes Invalid");
                    return null;
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("X2"));
                }
                var digitCount = 0;
                StringBuilder finalString = new StringBuilder();
                StringBuilder nonNumericalString = new StringBuilder();
                foreach (var dataChar in builder.ToString())
                {
                    if ((dataChar >= '0') && (dataChar <= '9'))
                    {
                        finalString.Append(dataChar);
                        digitCount++;
                        if (digitCount == 16) break;
                    }
                    else
                    {
                        nonNumericalString.Append(Char.ToUpper(dataChar));
                    }
                }
                if (digitCount != 16)
                {
                    var appendCount = 16 - digitCount;
                    var loopCount = 0;
                    foreach (var dataChar in nonNumericalString.ToString())
                    {
                        finalString.Append($"{dataChar - 'A': 0}".Trim());
                        loopCount++;
                        if (loopCount == appendCount) break;
                    }
                }
                if (finalString.ToString().Length == 16)
                    return finalString.ToString();
                else
                    return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"CreateVisaCAVVPan Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary></summary>
        /// <param name="IsoCardNbr"></param>
        /// <param name="isoP48T42EcFlag"></param>
        /// <param name="tag48HexString"></param>

        public static string CreateVisaCAVVPan(string IsoCardNbr, string isoP48T42EcFlag, string tag48HexString)
        {
            try
            {
                if (IsoCardNbr == null || tag48HexString == null || isoP48T42EcFlag == null)
                {
                    Log.Error("CreateVisaCAVVPan Data Invalid");
                    return null;
                }
                StringBuilder sb = new StringBuilder();
                sb.Append($"{IsoCardNbr.Substring(0, 15)}{isoP48T42EcFlag}");
                string c6AtnString = tag48HexString.Substring(14, 16);
                string hexString = c6AtnString.Substring(0, 10);
                long decimalData = Int64.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                sb.Append($"{decimalData:D12}");
                sb.Append($"{c6AtnString.Substring(10, 3)}");
                sb.Append($"{DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss").Substring(2, 2)}");
                sb.Append($"{c6AtnString.Substring(13, 3)}");
                string inputData = sb.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(inputData);
                //byte[] hashBytes = System.Security.Cryptography.SHA256.Create().ComputeHash(bytes);
                byte[] hashBytes = null;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(bytes);
                }
                if (hashBytes == null)
                {
                    Log.Error("CreateVisaCAVVPan HashBytes Invalid");
                    return null;
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("X2"));
                }
                var digitCount = 0;
                StringBuilder finalString = new StringBuilder();
                StringBuilder nonNumericalString = new StringBuilder();
                foreach (var dataChar in builder.ToString())
                {
                    if ((dataChar >= '0') && (dataChar <= '9'))
                    {
                        finalString.Append(dataChar);
                        digitCount++;
                        if (digitCount == 16) break;
                    }
                    else
                    {
                        nonNumericalString.Append(Char.ToUpper(dataChar));
                    }
                }
                if (digitCount != 16)
                {
                    var appendCount = 16 - digitCount;
                    var loopCount = 0;
                    foreach (var dataChar in nonNumericalString.ToString())
                    {
                        finalString.Append($"{dataChar - 'A': 0}".Trim());
                        loopCount++;
                        if (loopCount == appendCount) break;
                    }
                }
                if (finalString.ToString().Length == 16)
                    return finalString.ToString();
                else
                    return null;
            }
            catch (Exception ex)
            {
                Log.Error($"CreateVisaCAVVPan Exception:{ex.Message}", ex);
                return null;
            }
        }

        /// <summary></summary>
        /// <param name="tokenQ8Data"></param>
        public static string PackTokenQ8Data(Dictionary<string, string> tokenQ8Data)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (var subToken in tokenQ8Data)
                {
                    var tokenId = subToken.Key;
                    var tokenData = subToken.Value;
                    if (tokenId.Length != 2 || string.IsNullOrEmpty(tokenData))
                    {
                        Log.Error($"PackTokenQ8Data Error, SubToken Data Error[{tokenId}]-[{tokenData}]");
                        return null;
                    }

                    sb.Append($"{tokenId}{tokenData.Length:D3}{tokenData}");
                }
                if (sb.Length % 2 != 0) sb.Append(' ');
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"PackTokenQ8Data Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Send Neg file maintenance to NCCC.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <param name="sourceEndPoint">The source.</param>
        /// <param name="destinationEndPoint">The destination.</param>
        /// <returns></returns>
        public static async Task SendNegFileMaintToNccc(string cardNumber, string sourceEndPoint, string destinationEndPoint)
        {
            var fileMaintData = new FileMaintData
            {
                TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss")),
                IsoP2CardNbr = cardNumber
            };
            DateTime dateNow = DateTime.Now;
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateNow);
            fileMaintData.IsoP7TxDteTime = utcDateTime.ToString("MMddHHmmss");
            fileMaintData.IsoP11TraceNo = ColaCommUtility.GetSystemTraceNumber();
            fileMaintData.IsoP48AdditionalData = $"000BK{SystemParameterInfo.BankInfo.BankID}31{new string('0', 67)}";
            fileMaintData.IsoP49TranCurCode = "901";
            fileMaintData.IsoP60FromHostMaintData = $"90480000PRO2{new string('0', 14)}YY{new string('0', 30)}";
            //fileMaintData.IsoS73ActDte = DateTime.Now.ToString("yyMMdd");
            fileMaintData.IsoS91FileUpdCode = "3";
            fileMaintData.IsoS101FileName = "NF";
            //fileMaintData.IsoS115AdditionalTraceData = "Y";

            var interchangeMessage = new InterchangeMessage();
            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
            interchangeMessage.StartingPoint = 'S';
            interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
            interchangeMessage.SourceEndPoint = sourceEndPoint;
            interchangeMessage.DestinationEndPoint = destinationEndPoint;
            interchangeMessage.CreateDateTime = dateTimeString;
            interchangeMessage.Category = InterChangeMessageCategory.ClientAuthNCCCClient;
            interchangeMessage.CommandCode = InterChangeMessageCommandCode.NCCCClientCommandCode.FileMaintUpdate;
            interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Binary;
            interchangeMessage.InterchangeData = ColaCommUtility.ObjectToXMLBytes(fileMaintData);
            interchangeMessage.ReturnOption = true;
            var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
            //byte[] retnBytes = await SendDataToNcccClientRouterService(sendBytes);
            byte[] retnBytes;
            try
            {
                retnBytes = await Task.Run(() =>
                {
                    var clientService = new ClientService(1);
                    var clientRetnBytes = clientService.GetClientService();
                    if (clientRetnBytes != null && Encoding.ASCII.GetString(clientRetnBytes).Equals("00"))
                    {
                        clientRetnBytes = clientService.ProcessByConnection(sendBytes);
                        clientService.DisconnectToServer();
                    }

                    return clientRetnBytes;
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Process Send FileMaint To NcccClientRouterService Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return;
            }
            Log.Info($"Process Send FileMaint To NcccClientRouterService process completely at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            if (retnBytes == null)
            {
                Log.Error($"Receive Data From NcccClientRouterService invalid at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return;
            }

            if (retnBytes.Length == 2)
            {
                Log.Error($"Receive Data From NcccClientRouterService invalid at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Response Code[{Encoding.ASCII.GetString(retnBytes)}]");
                return;
            }
            var returnInterchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(retnBytes);
            if (returnInterchangeMessage == null)
            {
                Log.Info("Receive Message Invalid");
            }
            else
            {
                Log.Info($"SendNegFileMaintToNccc --- Start Display Receive Message ---");
                Log.Info(ColaCommUtility.GetObjectProperty(returnInterchangeMessage));
                Log.Info($"SendNegFileMaintToNccc --- End   Display Receive Message ---");
            }
        }


        /// <summary>
        /// Send Delete Stip file maintenance to Icash.
        /// </summary>
        /// <param name="cardNumber">The Icash card number.</param>
        /// <param name="sourceEndPoint">The source.</param>
        /// <param name="destinationEndPoint">The destination.</param>
        /// <returns></returns>
        public static async Task SendDelStipFileMaintToIcash(string cardNumber, string sourceEndPoint, string destinationEndPoint)
        {
            var fileMaintData = new FileMaintData
            {
                TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss")),
                IsoP2CardNbr = cardNumber
            };
            DateTime dateNow = DateTime.Now;
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateNow);
            fileMaintData.IsoP3ProcessCde = "990178";
            fileMaintData.IsoP7TxDteTime = utcDateTime.ToString("MMddHHmmss");
            fileMaintData.IsoP11TraceNo = ColaCommUtility.GetSystemTraceNumber();
            fileMaintData.IsoP37RefNbr = DateTime.Now.ToString("yyMMdd") + ColaCommUtility.GetSystemTraceNumber();
            fileMaintData.IsoS91FileUpdCode = "3";

            var interchangeMessage = new InterchangeMessage();
            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
            interchangeMessage.StartingPoint = 'S';
            interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
            interchangeMessage.SourceEndPoint = sourceEndPoint;
            interchangeMessage.DestinationEndPoint = destinationEndPoint;
            interchangeMessage.CreateDateTime = dateTimeString;
            interchangeMessage.Category = InterChangeMessageCategory.ClientAuthICASHClient;
            interchangeMessage.CommandCode = InterChangeMessageCommandCode.ICASHClientCommandCode.FileMaintUpdate;
            interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Binary;
            interchangeMessage.InterchangeData = ColaCommUtility.ObjectToXMLBytes(fileMaintData);
            interchangeMessage.ReturnOption = true;
            var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
            byte[] retnBytes;
            try
            {
                retnBytes = await Task.Run(() =>
                {
                    var clientService = new ClientService(2);
                    var clientRetnBytes = clientService.GetClientService();
                    if (clientRetnBytes != null && Encoding.ASCII.GetString(clientRetnBytes).Equals("00"))
                    {
                        clientRetnBytes = clientService.ProcessByConnection(sendBytes);
                        clientService.DisconnectToServer();
                    }

                    return clientRetnBytes;
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Process Send FileMaint To IcashClientRouterService Exception:{ex.Message} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return;
            }
            Log.Info($"Process Send FileMaint To IcashClientRouterService process completely at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

            if (retnBytes == null)
            {
                Log.Error($"Receive Data From IcashClientRouterService invalid at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                return;
            }

            if (retnBytes.Length == 2)
            {
                Log.Error($"Receive Data From IcashClientRouterService invalid at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}, Response Code[{Encoding.ASCII.GetString(retnBytes)}]");
                return;
            }
            var returnInterchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(retnBytes);
            if (returnInterchangeMessage == null)
            {
                Log.Info("Receive Message Invalid");
            }
            else
            {
                Log.Info($"SendDelStipFileMaintToIcash --- Start Display Receive Message ---");
                Log.Info(ColaCommUtility.GetObjectProperty(returnInterchangeMessage));
                Log.Info($"SendDelStipFileMaintToIcash --- End   Display Receive Message ---");
            }
        }

        public static Dictionary<string, string> CreateTLV(int idTagLength, int idDataLength, string stringData)
        {
            var tlv = new Dictionary<string, string>();
            try
            {
                var startPointer = 0;
                var totalDataLength = stringData.Length;
                while (startPointer < totalDataLength)
                {
                    var offsetLength = idTagLength;
                    string id = stringData.Substring(startPointer, offsetLength);
                    startPointer += offsetLength;

                    offsetLength = idDataLength;
                    //int dataLength = Convert.ToInt16(stringData.Substring(startPointer, offsetLength), 16);
                    int dataLength = Convert.ToInt16(stringData.Substring(startPointer, offsetLength));
                    startPointer += offsetLength;

                    offsetLength = dataLength;
                    string idData = stringData.Substring(startPointer, offsetLength);
                    startPointer += offsetLength;
                    tlv[id] = idData;
                }
                return tlv;
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, $"CreateTLV Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary></summary>
        /// <param name="cardNum"></param>
        public static bool IsVDCard(string cardNum)
        {
            try
            {
                if (string.IsNullOrEmpty(cardNum)) return false;
                var isNumber = cardNum.All(char.IsDigit);
                if (!isNumber) return false;
                bool checkFlag = false;
                foreach (var vdCardRange in SystemParameterInfo.BankInfo.DebitInfo.VDCardRangeInfo.VDCardRange)
                {
                    if (string.Compare(cardNum, vdCardRange.CardLowRange.Trim(), StringComparison.Ordinal) >= 0 &&
                        string.Compare(cardNum, vdCardRange.CardHighRange.Trim(), StringComparison.Ordinal) <= 0)
                    {
                        checkFlag = true;
                        break;
                    }
                }
                return checkFlag;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IsVDCard Exception:{ex.Message}");
                return false;
            }
        }

        /// <summary></summary>
        /// <param name="cardNum"></param>
        public static bool IsMDCard(string cardNum)
        {
            try
            {
                if (string.IsNullOrEmpty(cardNum)) return false;
                var isNumber = cardNum.All(char.IsDigit);
                if (!isNumber) return false;
                bool checkFlag = false;
                foreach (var mdCardRange in SystemParameterInfo.BankInfo.DebitInfo.MDCardRangeInfo.MDCardRange)
                {
                    if (string.Compare(cardNum, mdCardRange.CardLowRange.Trim(), StringComparison.Ordinal) >= 0 &&
                        string.Compare(cardNum, mdCardRange.CardHighRange.Trim(), StringComparison.Ordinal) <= 0)
                    {
                        checkFlag = true;
                        break;
                    }
                }
                return checkFlag;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IsMDCard Exception:{ex.Message}");
                return false;
            }
        }

        /// <summary></summary>
        /// <param name="cardNum"></param>
        public static bool IsJcbTokenPanCard(string cardNum)
        {
            try
            {
                if (string.IsNullOrEmpty(cardNum)) return false;
                var isNumber = cardNum.All(char.IsDigit);
                if (!isNumber) return false;
                bool checkFlag = false;
                foreach (var jcbCardRange in SystemParameterInfo.BankInfo.TPInfo.JCBCardRangeInfo.JCBCardRange)
                {
                    if (string.Compare(cardNum, jcbCardRange.CardLowRange.Trim(), StringComparison.Ordinal) >= 0 &&
                        string.Compare(cardNum, jcbCardRange.CardHighRange.Trim(), StringComparison.Ordinal) <= 0)
                    {
                        checkFlag = true;
                        break;
                    }
                }
                return checkFlag;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IsJcbTokenPanCard Exception:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// input NcccIso8583 get card number
        /// </summary>
        /// <param name="ncccIso8583"></param>
        /// <returns></returns>
        public static string GetCardNumber(NcccIso8583 ncccIso8583)
        {
            string cardNumber;
            try
            {
                if (ncccIso8583 == null) return null;
                if (!ObjectUtils.IsNull(ncccIso8583.RawVal(FIELD.F_35)))
                {
                    var p35Data = ncccIso8583.RawVal(FIELD.F_35).ToString();
                    int startIndex = p35Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                    if (startIndex < 0)
                        if (p35Data.Length > 0)
                            cardNumber = p35Data.Substring(0, p35Data.Length);
                        else
                            cardNumber = String.Empty;
                    else
                        cardNumber = p35Data.Substring(0, startIndex);

                }
                else if (!ObjectUtils.IsNull(ncccIso8583.RawVal(FIELD.F_2)))
                {
                    var p2Data = ncccIso8583.RawVal(FIELD.F_2).ToString();
                    if (p2Data.Length > 0)
                        cardNumber = p2Data.Substring(0, p2Data.Length);
                    else
                        cardNumber = String.Empty;
                }
                else
                {
                    cardNumber = String.Empty;
                }
                return cardNumber;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"GetCardNumber Exception:{ex.Message}");
                return null;
            }
        }
        /// <summary></summary>
        /// <param name="authInfo"></param>
        /// <param name="authReturnString"></param>
        /// Nccc0200AuthProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
        /// authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
        /// authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
        /// authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
        /// authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
        /// authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
        /// authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
        /// authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
        /// authReturnString[18]:@icashCardNbr        , authReturnString[19]:@vdLinkNbr
		/// authReturnString[20]:@vdLinkNbrSeq        , authReturnString[21]:@vdActionCode
		/// authReturnString[22]:@vdBlockAmt	

        /// Nccc0220AuthProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@vdLinkNbr
        /// authReturnString[04]:@vdLinkNbrSeq        , authReturnString[05]:@vdActionCode
        /// authReturnString[06]:@vdBlockAmt

        /// Nccc0420AuthProcess Return String Arrays Information
        /// authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@isoP39RspAction
        /// authReturnString[02]:@vdLinkNbr           , authReturnString[03]:@vdLinkNbrSeq
        /// authReturnString[04]:@vdActionCode        , authReturnString[05]:@vdBlockAmt
        public static VDMessage.RequestXML CreteVDMQRequestMessage(NcccAuthInfoStruct authInfo, string[] authReturnString)
        {
            try
            {
                //int checkMessage = authInfo.isoMessageType.Substring(2, 1) == "0" ? -1 : 0;
                var authReturrCount = authReturnString.Length;// + checkMessage;
                if (authReturrCount < 5)
                {
                    Log.Fatal($"CreteVDMQRequestMessage authReturnString Count[{authReturrCount}] Error");
                    return null;
                }
                var vdRequestMessage = new VDMessage.RequestXML();
                var header = new VDMessage.RequestHeader();
                header.TxID = "FCB91125S";
                header.SystemKey = "FCB4D";
                header.TxSeqNo = DateTime.Now.ToString("MMddHHmmssfff");
                header.MsgSeqNo = authInfo.tranLogKey.Substring(authInfo.tranLogKey.Length - 6, 6).ToUpper();
                header.CltTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                header.CustID = "";
                header.UserID = "";
                header.AcctNo = "";
                header.MsgDirection = "RQ";
                vdRequestMessage.RequestHeader = header;
                var txRq = new VDMessage.TxRq();
                txRq.FuncCode = authReturnString[authReturrCount - 2];
                txRq.AcctNo = authReturnString[authReturrCount - 4];
                txRq.CardNo = authReturnString[authReturrCount - 3];
                txRq.CreditNo = authInfo.isoP2CardNbr;
                txRq.AuthNo = authReturnString[00];
                var authAmt = Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100;
                //var amtString = String.Format("{0:0.00}", Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100);
                var amtString = String.Format("{0:0.00}", authAmt < 0 ? authAmt * -1 : authAmt);
                txRq.TxAmt = amtString;
                vdRequestMessage.TxRq = txRq;
                return vdRequestMessage;
            }
            catch (System.Xml.XmlException ex1)
            {
                Log.Fatal(ex1, $"CreteVDMQRequestMessage Exception:{ex1.Message}");
                return null;
            }
            catch (Exception ex2)
            {
                Log.Fatal(ex2, $"CreteVDMQRequestMessage Exception:{ex2.Message}");
                return null;
            }
        }

        /// <summary></summary>
        /// <param name="authInfo"></param>
        /// <param name="authReturnString"></param>
        /// Fisc0100AuthProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        /// authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        /// authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        /// authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        /// authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        /// authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        /// authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        /// authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        /// authReturnString[18]:@vdBlockAmt

        /// Fisc0120AuthProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@vdLinkNbr             , authReturnString[03]:@vdLinkNbrSeq
        /// authReturnString[04]:@vdActionCode          , authReturnString[05]:@vdBlockAmt

        /// Fisc0420AuthProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@vdLinkNbr             , authReturnString[03]:@vdLinkNbrSeq
        /// authReturnString[04]:@vdActionCode          , authReturnString[05]:@vdBlockAmt
        public static VDMessage.RequestXML CreteVDMQRequestMessage(FiscAuthInfoStruct authInfo, string[] authReturnString)
        {
            try
            {
                var authReturrCount = authReturnString.Length;
                if (authReturrCount < 5)
                {
                    Log.Fatal($"CreteVDMQRequestMessage authReturnString Count[{authReturrCount}] Error");
                    return null;
                }
                var vdRequestMessage = new VDMessage.RequestXML();
                var header = new VDMessage.RequestHeader();
                header.TxID = "FCB91125S";
                header.SystemKey = "FCB4D";
                header.TxSeqNo = DateTime.Now.ToString("yyyyMMddHHmmss");
                header.MsgSeqNo = authInfo.tranLogKey.Substring(authInfo.tranLogKey.Length - 5, 5);
                header.CltTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); ;
                header.CustID = "";
                header.UserID = "";
                header.AcctNo = "";
                header.MsgDirection = "RQ";
                vdRequestMessage.RequestHeader = header;
                var txRq = new VDMessage.TxRq();
                txRq.FuncCode = authReturnString[authReturrCount - 2];
                txRq.AcctNo = authReturnString[authReturrCount - 4];
                txRq.CardNo = authReturnString[authReturrCount - 3];
                txRq.CreditNo = authInfo.isoP2CardNbr;
                txRq.AuthNo = authReturnString[00];
                var authAmt = Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100;
                //var amtString = String.Format("{0:0.00}", Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100);
                var amtString = String.Format("{0:0.00}", authAmt < 0 ? authAmt * -1 : authAmt);
                txRq.TxAmt = amtString;
                vdRequestMessage.TxRq = txRq;
                return vdRequestMessage;
            }
            catch (System.Xml.XmlException ex1)
            {
                Log.Fatal(ex1, $"CreteVDMQRequestMessage Exception:{ex1.Message}");
                return null;
            }
            catch (Exception ex2)
            {
                Log.Fatal(ex2, $"CreteVDMQRequestMessage Exception:{ex2.Message}");
                return null;
            }
        }

        public static VDMessage.RequestXML CreteVDMQRequestMessage(EsvcAuthInfoStruct authInfo, string[] authReturnString)
        {
            try
            {
                var authReturrCount = authReturnString.Length;
                if (authReturrCount < 7)
                {
                    Log.Fatal($"CreteVDMQRequestMessage authReturnString Count[{authReturrCount}] Error");
                    return null;
                }
                var vdRequestMessage = new VDMessage.RequestXML();
                var header = new VDMessage.RequestHeader();
                header.TxID = "FCB91125S";
                header.SystemKey = "FCB4D";
                header.TxSeqNo = DateTime.Now.ToString("yyyyMMddHHmmss");
                header.MsgSeqNo = authInfo.tranLogKey.Substring(authInfo.tranLogKey.Length - 5, 5);
                header.CltTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); ;
                header.CustID = "";
                header.UserID = "";
                header.AcctNo = "";
                header.MsgDirection = "RQ";
                vdRequestMessage.RequestHeader = header;
                var txRq = new VDMessage.TxRq();
                txRq.FuncCode = authReturnString[authReturrCount - 2];
                txRq.AcctNo = authReturnString[authReturrCount - 4];
                txRq.CardNo = authReturnString[authReturrCount - 3];
                txRq.CreditNo = authReturnString[authReturrCount - 5];
                txRq.AuthNo = ColaCommUtility.GetApproveCode();
                var authAmt = Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100;
                //var amtString = String.Format("{0:0.00}", Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100);
                var amtString = String.Format("{0:0.00}", authAmt < 0 ? authAmt * -1 : authAmt);
                txRq.TxAmt = amtString;
                vdRequestMessage.TxRq = txRq;
                return vdRequestMessage;
            }
            catch (System.Xml.XmlException ex1)
            {
                Log.Fatal(ex1, $"CreteVDMQRequestMessage Exception:{ex1.Message}");
                return null;
            }
            catch (Exception ex2)
            {
                Log.Fatal(ex2, $"CreteVDMQRequestMessage Exception:{ex2.Message}");
                return null;
            }
        }

        /// <summary></summary>
        /// <param name="vdRequestMessage"></param>
        /// <param name="authReturnString"></param>
        /// <param name="returnCode"></param>

        public static VDMessage.ResponseXML CreteVDMQResponseXML(VDMessage.RequestXML vdRequestMessage, string[] authReturnString, string returnCode)
        {
            try
            {
                var authReturrCount = authReturnString.Length;
                if (authReturrCount < 5)
                {

                    Log.Fatal($"CreteVDMQResponseMessage authReturnString Count[{authReturrCount}] Error");
                    return null;
                }
                var vdResponseMessage = new VDMessage.ResponseXML();
                var header = new VDMessage.ResponseHeader();
                DateTime processDateTime = DateTime.Now;
                header.TxID = "TX_FCB91125S";
                header.SystemKey = vdRequestMessage.RequestHeader.SystemKey;
                header.TxSeqNo = vdRequestMessage.RequestHeader.TxSeqNo;
                header.MsgSeqNo = vdRequestMessage.RequestHeader.MsgSeqNo;
                header.SvrTimeStamp = processDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                header.MsgDirection = "RS";
                header.System = "FCB91";
                header.StatusCode = returnCode;
                header.StatusDesc = returnCode == "T100" ? "Success" : "Fail";
                vdResponseMessage.ResponseHeader = header;
                var txRs = new VDMessage.TxRs();
                txRs.BusType = "X";
                txRs.WsID = "2S";
                txRs.HostSeqNo = "XXXXX";
                txRs.TxDate = processDateTime.ToString("yyyMMdd");
                txRs.TxTime = processDateTime.ToString("HH:mm:ss");
                txRs.TxBrn = "XXX";
                txRs.FuncCode = authReturnString[authReturrCount - 2];
                txRs.AcctNo = authReturnString[authReturrCount - 4];
                txRs.CardNo = authReturnString[authReturrCount - 3];
                txRs.CreditNo = vdRequestMessage.TxRq.CreditNo;
                txRs.AuthNo = authReturnString[00];
                var authAmt = Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100;
                //var amtString = String.Format("{0:0.00}", Convert.ToDecimal(authReturnString[authReturrCount - 1].Trim()) / 100);
                var amtString = String.Format("{0:0.00}", authAmt < 0 ? authAmt * -1 : authAmt);
                txRs.TxnAmt = amtString;
                txRs.AvaiBalSign = "+";
                txRs.AvaiBal = "0";
                vdResponseMessage.TxRs = txRs;
                return vdResponseMessage;
            }
            catch (System.Xml.XmlException ex1)
            {
                Log.Fatal(ex1, $"CreteVDMQResponseMessage Exception:{ex1.Message}");
                return null;
            }
            catch (Exception ex2)
            {
                Log.Fatal(ex2, $"CreteVDMQRequestMessage Exception:{ex2.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts the fisc03002 ISO.
        /// </summary>
        /// <param name="fiscMessage">The fisc message.</param>
        /// <returns></returns>
        public static byte[] ConvertFisc0300ToIsoFixed(FISC fiscMessage)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                string isoMessageType = fiscMessage.Mti();
                string isoRecType = "1";
                string isoUpdCode = " ";
                var field = FIELD.F_91;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    isoUpdCode = fiscMessage.Core().Field(field).ToString();
                }
                string isoCardNbr = "";
                field = FIELD.F_2;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    isoCardNbr = fiscMessage.Core().Field(field).ToString();
                }
                string isoProdType = " ";
                var cardType = CreditCardUtility.FindCardType(isoCardNbr);
                switch (cardType)
                {
                    case CreditCardType.JCB:
                        isoProdType = "J";
                        break;
                    case CreditCardType.MasterCard:
                        isoProdType = "M";
                        break;
                    case CreditCardType.Visa:
                        isoProdType = "V";
                        break;
                }
                string isoActionCode = "";
                field = FIELD.F_92;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    isoActionCode = fiscMessage.Core().Field(field).ToString();
                }
                /// <summary>
                /// Field:S101
                /// NCCC:2Byte
                /// FISC:1Byte
                /// </summary>
                string isoFileName = "";
                field = FIELD.F_101;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    isoFileName = fiscMessage.Core().Field(field).ToString();
                }
                string isoCRBRegion = String.Format("{0,-13}", " ");
                string isoVIPAmt = $"{0:d12}";
                string isoPurgeDte = "";
                field = FIELD.F_14;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    isoPurgeDte = fiscMessage.Core().Field(field).ToString();
                }
                string isoSendDte = String.Format("{0,-8}", " ");
                string isoSendTime = String.Format("{0,-8}", " ");
                string isoResp = String.Format("{0,-2}", " ");
                string isoTraceNbr = "";
                field = FIELD.F_11;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    isoTraceNbr = fiscMessage.Core().Field(field).ToString();
                }
                string isoAddData = String.Format("{0,-54}", " ");
                string tempString = String.Format("{0,-4}{1,-1}{2,-1}{3,-16}{4,-3}{5,-2}{6,-1}{7,-13}{8,-12}{9,-4}{10,-8}{11,-8}{12,-2}{13,-6}{14,-54}",
                                                   isoMessageType,
                                                   isoRecType,
                                                   isoUpdCode,
                                                   isoCardNbr,
                                                   isoProdType,
                                                   isoActionCode,
                                                   isoFileName,
                                                   isoCRBRegion,
                                                   isoVIPAmt,
                                                   isoPurgeDte,
                                                   isoSendDte,
                                                   isoSendTime,
                                                   isoResp,
                                                   isoTraceNbr,
                                                   isoAddData);
                Log.Debug($"ASCII Data[{tempString}], Length[{tempString.Length}]");
                //var ebcdicByteArray = Encoding.GetEncoding("IBM037").GetBytes(tempString);
                var asciiyteArray = Encoding.ASCII.GetBytes(tempString);
                return asciiyteArray;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertFisc0300ToIsoFixed Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the security number.
        /// </summary>
        /// <param name="maxExclusive">The maximum exclusive number(0 ... maxExclusiv - 1.</param>
        /// <returns></returns>
        public static int GetSecurityNumber(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomNumber = new byte[4];
                rng.GetBytes(randomNumber);

                int result = BitConverter.ToInt32(randomNumber, 0) % maxExclusive;
                result = result < 0 ? -result : result; // Ensure non-negative
                if (result >= maxExclusive) result = 0; // Ensure less than maxExclusive
                return result;
            }
        }
    }
}

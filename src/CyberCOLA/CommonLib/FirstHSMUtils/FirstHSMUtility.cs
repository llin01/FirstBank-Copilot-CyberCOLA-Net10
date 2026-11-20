using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using SocketClient;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Com.Cybersoft4u.CommonLib.DBUtils;
using IBM.WMQ;
using Com.Cybersoft4u.TM.FISC.DE48;
using Com.Cybersoft4u.Interchange;

namespace Com.Cybersoft4u.CommonLib.FirstHSMUtils
{
    public static class FirstHSMUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        /// <summary>
        /// F01 Generate CVV / CVV2
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="bankId"></param>
        /// <param name="cardNo"></param>
        /// <param name="expirDate"></param>
        /// <param name="serviceCode"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <param name="cvv"></param>
        /// <returns></returns>
        public static string CallGenerateCVV(string bizId, string bankId, string cardNo, string expirDate, string serviceCode,
                                             out string rtnCode, out string rsnCode, out string cvv)
        {
            try
            {
                var cvv2 = string.Empty;
                ProHSM.Func01(bizId, bankId, cardNo, expirDate, serviceCode, out rtnCode, out rsnCode, out cvv);
                return string.Empty;
            }
            catch (Exception e)
            {
                cvv = string.Empty;
                rtnCode = null;
                rsnCode = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// F03 Encrypt Bank's PIN
        /// </summary> 
        /// <param name="bizId"></param>        
        /// <param name="bankId"></param>     
        /// <param name="cardNo"></param>
        /// <param name="pvkIndex"></param>     
        /// <param name="ppkBizId"></param>        
        /// <param name="ppkKeyId"></param> 
        /// <param name="pinBlockFormat"></param>        
        /// <param name="clearPin"></param>        
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <param name="encPINBlock"></param>
        /// <param name="pvv"></param>
        /// <param name="PinOffset"></param>
        /// <returns></returns>
        public static string EncryptBankPin(string bizId, string bankId, string cardNo, string pvkIndex, string ppkBizId, string ppkKeyId, string pinBlockFormat, string clearPIN,
                                            out string rtnCode, out string rsnCode, out string encPINBlock, out string pvv, out string pinOffset)
        {
            try
            {
                ProHSM.Func03(bizId, bankId, cardNo, pvkIndex, ppkBizId, ppkKeyId, pinBlockFormat, clearPIN, out rtnCode, out rsnCode, out encPINBlock, out pvv, out pinOffset);
                return string.Empty;
            }
            catch (Exception e)
            {
                pvv = string.Empty;
                rtnCode = null;
                rsnCode = null;
                pvv = null;
                encPINBlock = null;
                pinOffset = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }
        /// <summary>
        /// F05 Generate PVV
        /// </summary> 
        /// <param name="bizId"></param>        
        /// <param name="bankId"></param>        
        /// <param name="pvkIndex"></param>        
        /// <param name="cardNo"></param>        
        /// <param name="ppkBizId"></param>        
        /// <param name="pinBlockFormat"></param>        
        /// <param name="encPINBlock"></param>        
        /// <param name="pinOffset"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <param name="pvv"></param>
        /// <param name="rtnPinOffset"></param>
        /// <returns></returns>
        public static string CallGeneratePVV(string bizId, string bankId, string pvkIndex, string cardNo, string ppkBizId, string pinBlockFormat,
                                             string encPINBlock, string pinOffset, out string rtnCode, out string rsnCode, out string pvv, out string pvvOrigin, out string rtnPinOffset)
        {
            try
            {
                ProHSM.Func05(bizId, bankId, pvkIndex, cardNo, ppkBizId, pinBlockFormat, encPINBlock, pinOffset, out rtnCode, out rsnCode, out pvv, out pvvOrigin, out rtnPinOffset);
                Log.Info($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} out rtnCode : [{rtnCode}]  out rsnCode : [{rsnCode}]");
                return string.Empty;
            }
            catch (Exception e)
            {
                rtnCode = null;
                rsnCode = null;
                pvv = null;
                pvvOrigin = null;
                rtnPinOffset = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// Fun07 ARQC Verify
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="bankId"></param>
        /// <param name="keyIndex"></param>
        /// <param name="cdolDataLength"></param>
        /// <param name="cdolData"></param>
        /// <param name="arqcData"></param>
        /// <param name="panData"></param>
        /// <param name="cryptogramVersionNumber"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <returns></returns>
        public static string CallVerifyARQC(string bizId, string bankId, string keyIndex, string cdolDataLength, string cdolData, string arqcData,
            string panData, string cryptogramVersionNumber, out string rtnCode, out string rsnCode)
        {
            try
            {
                ProHSM.Func07(bizId, bankId, keyIndex, cdolDataLength, cdolData, arqcData, panData, cryptogramVersionNumber, out rtnCode, out rsnCode);
                return string.Empty;
            }
            catch (Exception e)
            {
                rtnCode = string.Empty;
                rsnCode = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// Fun08 ARPC Generate
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="bankId"></param>
        /// <param name="keyIndex"></param>
        /// <param name="arqcData"></param>
        /// <param name="arc"></param>
        /// <param name="atc"></param>
        /// <param name="panData"></param>
        /// <param name="cryptogramVersionNumber"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <param name="arpcData"></param>
        /// <returns></returns>
        public static string CallGenerateARPC(string bizId, string bankId, string keyIndex, string arqcData, string arc, string atc, string panData, string cryptogramVersionNumber,
                                          out string rtnCode, out string rsnCode, out string arpcData)
        {
            try
            {
                ProHSM.Func08(bizId, bankId, keyIndex, arqcData, arc, atc, panData, cryptogramVersionNumber,
                             out rtnCode, out rsnCode, out arpcData);
                return string.Empty;
            }
            catch (Exception e)
            {
                arpcData = string.Empty;
                rtnCode = null;
                rsnCode = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// 9. 3D CAVV Generate
        /// </summary>
        /// <param name="bizId"></param>
        /// <param name="bankId"></param>
        /// <param name="keyIndex"></param>
        /// <param name="cardNo"></param>
        /// <param name="expiryData"></param>
        /// <param name="serviceCode"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <param name="cavv"></param>
        /// <returns></returns>
        public static string CallGenerateCAVV(string bizId, string bankId, string keyIndex, string cardNo, string expiryData, string serviceCode,
                                                out string rtnCode, out string rsnCode, out string cavv)
        {
            try
            {
                ProHSM.Func09(bizId, bankId, keyIndex, cardNo, expiryData, serviceCode,
                                   out rtnCode, out rsnCode, out cavv);
                return string.Empty;
            }
            catch (Exception e)
            {
                cavv = string.Empty;
                rtnCode = null;
                rsnCode = null;
                Log.Error($"Call {new StackTrace().GetFrame(0).GetMethod().Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// Fun21 CallChangeReceivingKey
        /// </summary>
        /// <param name="wkBizId"></param>
        /// <param name="zcmkBizId"></param>
		/// <param name="bankId"></param>
        /// <param name="wkKeyId"></param>
        /// <param name="keyLength"></param>
        /// <param name="encKey"></param>
        /// <param name="keySync"></param>
        /// <param name="exporType"></param>
        /// <param name="keyLengthType"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <returns></returns>
        public static string ChangeReceivingKeyKB(string wkBizId, string zcmkBizId, string bankId, string wkKeyId, string keyLength, string encKey, string keySync, string exporType, string keyLengthType, out string rtnCode, out string rsnCode)
        {
            try
            {
                ProHSM.Func21(wkBizId, zcmkBizId, bankId, wkKeyId, keyLength, encKey, keySync, exporType, keyLengthType, out rtnCode, out rsnCode);
                return string.Empty;
            }
            catch (Exception e)
            {
                rtnCode = null;
                rsnCode = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// 13. New Key Replace Old Key
        /// </summary>
        /// <param name="wkBizId"></param>
        /// <param name="bankId"></param>
        /// <param name="wkKeyid"></param>
        /// <param name="replaceMac"></param>
        /// <param name="replaceOPINENC"></param>
        /// <param name="replaceIPINENC"></param>
        /// <param name="replaceTR31"></param>
        /// <param name="rtnCode"></param>
        /// <param name="rsnCode"></param>
        /// <returns></returns>
        public static string CallNewKeyReplaceOldKey(string wkBizId, string bankId, string wkKeyid, string replaceMac, string replaceOPINENC, string replaceIPINENC, string replaceTr31, out string rtnCode, out string rsnCode)
        {
            try
            {
                ProHSM.Func13(wkBizId, bankId, wkKeyid, replaceMac, replaceOPINENC, replaceIPINENC, replaceTr31, out rtnCode, out rsnCode);
                return string.Empty;
            }
            catch (Exception e)
            {
                rtnCode = null;
                rsnCode = null;
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return e.ToString();
            }
        }

        /// <summary>
        /// Create Check ARQC Data
        /// </summary>
        /// <param name="ncccMessage">Request Bic Message</param>
        /// <param name="bankCode"></param>
        /// <param name="codlLength"></param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateARQCData(NcccIso8583 ncccMessage, string bankCode, out int codlLength)
        {
            try
            {
                codlLength = 0;
                string Version = string.Empty;
                const int hsmByteArrayLength = 200;
                byte[] hsmByteArray = Encoding.ASCII.GetBytes(new string('0', hsmByteArrayLength));
                BicTrack2 bicTrack2 = null;

                FIELD field = FIELD.F_35;

                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)ncccMessage.Core().Field(field));
                else
                    return null;

                //field = FIELD.F_63;
                field = ncccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                    if (tokenGroup == null) return (null);
                    var tokenB2 = (TokenB2)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenB2.Id);
                    if (tokenB2 == null) return (null);
                    const int tokenHeaderLength = 10;
                    string tokenB2HeaderData = tokenB2.Encode();
                    string tokenB2Header = tokenB2HeaderData.Substring(0, tokenHeaderLength);
                    string tokenB2Data = tokenB2HeaderData.Substring(tokenHeaderLength, tokenB2HeaderData.Length - tokenHeaderLength);

                    BicB2TokenStruct tokenData = new BicB2TokenStruct();
                    byte[] tokenDataBytes = System.Text.Encoding.ASCII.GetBytes(tokenB2Data);
                    if (tokenDataBytes.Length == SystemConstantData.Token.BicTokenIdB2Length)
                    {
                        tokenData = (BicB2TokenStruct)ColaCommUtility.RawDeserialize(tokenDataBytes, tokenData.GetType());
                    }
                    else
                    {
                        return (null);
                    }

                    // Get 9F10 CVR Data
                    int issApplDataLgth = Convert.ToInt32(Encoding.ASCII.GetString(tokenData.iss_appl_data_lgth));
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       Encoding.ASCII.GetString(tokenData.iss_appl_data),
                                                                       bicTrack2.IsoCardType).Split(';');
                    if (!ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        Log.Error("Process HSMCreateARQCData, Create data9F10 array string Error");
                        return null;
                    }
                    var startPos = 0;
                    var dataLength = 8;
                    string bizId = new string(' ', 8);
                    //organization
                    switch (bicTrack2.IsoCardType)
                    {
                        case CreditCardType.MasterCard:
                            bizId = $"{"MASTER",-8}";
                            break;
                        case CreditCardType.Visa:
                            bizId = $"{"VISA",-8}";
                            break;
                        case CreditCardType.JCB:
                            bizId = $"{"JCB",-8}";
                            break;
                        default:
                            Log.Info("Call HSMCreateARQCData Error,Unknown Card Type");
                            return null;
                    }
                    Array.Copy(Encoding.ASCII.GetBytes(bizId), 0, hsmByteArray, startPos, dataLength);

                    //bankId - 3
                    startPos += dataLength;
                    dataLength = 3;
                    Array.Copy(Encoding.ASCII.GetBytes(bankCode), 0, hsmByteArray, startPos, dataLength);

                    //KeyIndex - 2  
                    if (data9F10.Length == 3 && data9F10[0].Length == 2)
                    {
                        startPos += dataLength;
                        dataLength = data9F10[0].Length;
                        Array.Copy(Encoding.ASCII.GetBytes(data9F10[0]), 0, hsmByteArray, startPos, dataLength);
                    }

                    //ARQC LENGTH 16
                    startPos += dataLength;
                    dataLength = 16;
                    Array.Copy(tokenData.arqc, 0, hsmByteArray, startPos, dataLength);

                    //PAN-DATA+PAN-SEQ 18
                    startPos += dataLength;
                    dataLength = 18;
                    Array.Copy(Encoding.ASCII.GetBytes(bicTrack2.IsoCardNbr + new string('0', dataLength - bicTrack2.IsoCardNbr.Length)), 0, hsmByteArray, startPos, dataLength);

                    //CRYPTOGRAM-VERSION
                    startPos += dataLength;
                    dataLength = 2;
                    string version;
                    switch (data9F10[1])
                    {
                        case "10":
                        case "11":
                            version = "0A";
                            break;
                        case "14":
                            version = "0E";
                            break;
                        default:
                            version = data9F10[1];
                            break;
                    }
                    Array.Copy(Encoding.ASCII.GetBytes(version), 0, hsmByteArray, startPos, dataLength);

                    //CODL data
                    //AMT 9F02 LEN:12
                    codlLength = 0;
                    startPos += dataLength;
                    dataLength = 12;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.amt_auth, 0, hsmByteArray, startPos, dataLength);
                    //AMT_OTHER 9F03 LEN:12
                    startPos += dataLength;
                    dataLength = 12;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.amt_other, 0, hsmByteArray, startPos, dataLength);
                    //COUNTRY 9F1A LEN:4
                    startPos += dataLength;
                    dataLength = 4;
                    codlLength = codlLength + dataLength;
                    byte[] tempArray = Encoding.ASCII.GetBytes(new string('0', dataLength));
                    Array.Copy(tokenData.term_cntry_cde, 0, tempArray, 1, dataLength - 1);
                    Array.Copy(tempArray, 0, hsmByteArray, startPos, dataLength);
                    //TVR 95 LEN:10
                    startPos += dataLength;
                    dataLength = 10;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.tvr, 0, hsmByteArray, startPos, dataLength);
                    //CURRENCY 5F2A LEN:4
                    startPos += dataLength;
                    dataLength = 4;
                    codlLength = codlLength + dataLength;
                    tempArray = Encoding.ASCII.GetBytes(new string('0', dataLength));
                    Array.Copy(tokenData.term_crncy_cde, 0, tempArray, 1, dataLength - 1);
                    Array.Copy(tempArray, 0, hsmByteArray, startPos, dataLength);
                    //TX_DATE 9A LEN:6
                    startPos += dataLength;
                    dataLength = 6;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.tran_dat, 0, hsmByteArray, startPos, dataLength);
                    //TX_TYPE 9C LEN:2
                    startPos += dataLength;
                    dataLength = 2;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.tran_type, 0, hsmByteArray, startPos, dataLength);
                    //UN 9F37 LEN:8
                    startPos += dataLength;
                    dataLength = 8;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.unpredict_num, 0, hsmByteArray, startPos, dataLength);
                    //AIP 82 LEN:4
                    startPos += dataLength;
                    dataLength = 4;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.aip, 0, hsmByteArray, startPos, dataLength);
                    //ATC 9F36 LEN:4
                    startPos += dataLength;
                    dataLength = 4;
                    codlLength = codlLength + dataLength;
                    Array.Copy(tokenData.atc, 0, hsmByteArray, startPos, dataLength);
                    //CVR 9F10 LEN:8
                    if (data9F10.Length == 3)
                    {
                        if (bicTrack2.IsoCardType.Equals(CreditCardType.Visa) && (new[] { "12", "16" }).Contains(data9F10[1]))
                        {
                            startPos += dataLength;
                            dataLength = Convert.ToInt16(Encoding.ASCII.GetString(tokenData.iss_appl_data_lgth)) * 2;
                            string applData = Encoding.ASCII.GetString(tokenData.iss_appl_data).Substring(0, dataLength);
                            codlLength = codlLength + dataLength;
                            Array.Copy(Encoding.ASCII.GetBytes(applData), 0, hsmByteArray, startPos, dataLength);
                        }
                        else
                        {
                            startPos += dataLength;
                            dataLength = data9F10[2].Length;
                            codlLength = codlLength + dataLength;
                            Array.Copy(Encoding.ASCII.GetBytes(data9F10[2]), 0, hsmByteArray, startPos, dataLength);
                        }

                    }
                    return (hsmByteArray);
                }
                return null;
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                codlLength = 0;
                return null;
            }
        }

        /// <summary>
        /// Create Check ARQC Data
        /// </summary>
        /// <param name="fiscMessage">Request ISO Message</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateARQCData(FISC fiscMessage, string bankCode, out int codlLength)
        {
            try
            {
                codlLength = 0;
                string Version = string.Empty;
                const int hsmByteArrayLength = 200;
                byte[] hsmByteArray = Encoding.ASCII.GetBytes(new string('0', hsmByteArrayLength));
                BicTrack2 bicTrack2 = null;

                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}] Data Error");
                    return (null);
                }

                field = FIELD.F_55;
                if (ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}] Data Error");
                    return (null);
                }

                var emvData = fiscMessage.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;
                string emvTagIdData;

                string emvTagIdString = "9F10";
                var data9F10Array = new string[3];
                string emv9F10Data = string.Empty;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    var tagData = emvData[emvTagIdString];
                    if (tagData != null)
                    {
                        emv9F10Data = CommonUtils.HexBinary2HexString(tagData.Value);
                        data9F10Array = ColaCommUtility.FiscGet9F10Data(emv9F10Data.Length, emv9F10Data, bicTrack2.IsoCardType).Split(';');
                    }
                    else
                    {
                        Log.Info($"Call HSMCreateARQCData #55 Decode Error,Field[{emvTagIdString.ToString()}] Data Error");
                        return (null);
                    }
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData #55 ,Field[{emvTagIdString.ToString()}] Data Error");
                    return (null);
                }

                var startPos = 0;
                var dataLength = 8;
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    default:
                        Log.Info("Call GenerateARQC Error,Card Type Error");
                        return (null);
                }
                Array.Copy(Encoding.ASCII.GetBytes(bizId), 0, hsmByteArray, startPos, dataLength);

                //bankId - 3
                startPos += dataLength;
                dataLength = 3;
                Array.Copy(Encoding.ASCII.GetBytes(bankCode), 0, hsmByteArray, startPos, dataLength);

                //KeyIndex --2
                // 0:DERIV - KEY - INDEX,1:CRYPTO - VER - NUM,2:CRD - VERIF - RSLTS
                //CRYPTOGRAM-VERSION
                startPos += dataLength;
                dataLength = 2;
                Array.Copy(Encoding.ASCII.GetBytes(data9F10Array[0]), 0, hsmByteArray, startPos, dataLength);

                //arqcData -16
                emvTagIdString = "9F26";
                int emvTagIdDataLength = 16;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    startPos += dataLength;
                    dataLength = emvTagIdDataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }

                // PAN - DATA + PAN - SEQ 18
                startPos += dataLength;
                dataLength = 18;
                Array.Copy(Encoding.ASCII.GetBytes(bicTrack2.IsoCardNbr + new string('0', dataLength - bicTrack2.IsoCardNbr.Length)), 0, hsmByteArray, startPos, dataLength);

                // 0:DERIV - KEY - INDEX,1:CRYPTO - VER - NUM,2:CRD - VERIF - RSLTS
                //CRYPTOGRAM-VERSION
                startPos += dataLength;
                dataLength = 2;
                string version = new string(' ', dataLength);
                switch (data9F10Array[1])
                {
                    case "10":
                        //case "11":
                        version = "0A";
                        break;
                    case "14":
                        version = "0E";
                        break;
                    default:
                        version = data9F10Array[1];
                        break;
                }
                Array.Copy(Encoding.ASCII.GetBytes(version), 0, hsmByteArray, startPos, dataLength);

                //CDOLData
                //AMT 9F02 LEN:12
                codlLength = 0;
                startPos += dataLength;
                dataLength = 12;
                emvTagIdString = "9F02";
                emvTagIdDataLength = 12;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //OTHER AMT 9F03 LEN:12
                startPos += dataLength;
                dataLength = 12;
                emvTagIdString = "9F03";
                emvTagIdDataLength = 12;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(new string('0', emvTagIdDataLength)), 0, hsmByteArray, startPos, dataLength);
                }
                //COUTNRY CODE 9F1A LEN:4
                startPos += dataLength;
                dataLength = 4;
                emvTagIdString = "9F1A";
                emvTagIdDataLength = 4;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //TVR 95 LEN:10
                startPos += dataLength;
                dataLength = 10;
                emvTagIdString = "95";
                emvTagIdDataLength = 10;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //Tx Currency Code 5F2A LEN:4
                startPos += dataLength;
                dataLength = 4;
                emvTagIdString = "5F2A";
                emvTagIdDataLength = 4;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //Tx Date 9A LEN:6
                startPos += dataLength;
                dataLength = 6;
                emvTagIdString = "9A";
                emvTagIdDataLength = 6;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //Tx Type 9C LEN:2
                startPos += dataLength;
                dataLength = 2;
                emvTagIdString = "9C";
                emvTagIdDataLength = 2;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //UN 9F37 LEN:8
                startPos += dataLength;
                dataLength = 8;
                emvTagIdString = "9F37";
                emvTagIdDataLength = 8;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //AIP 82 LEN:4
                startPos += dataLength;
                dataLength = 4;
                emvTagIdString = "82";
                emvTagIdDataLength = 4;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //ATC 9F36 LEN:4
                startPos += dataLength;
                dataLength = 4;
                emvTagIdString = "9F36";
                emvTagIdDataLength = 4;
                if (emvData.ContainsKey(emvTagIdString))
                {
                    emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    if (emvTagIdData.Length != emvTagIdDataLength)
                    {
                        Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }
                    codlLength = codlLength + dataLength;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);
                }
                else
                {
                    Log.Info($"Call HSMCreateARQCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                    return (null);
                }
                //IAD 9F10 LEN:14
                if (data9F10Array.Length == 3)
                {
                    if (bicTrack2.IsoCardType.Equals(CreditCardType.Visa) && (new[] { "12", "16" }).Contains(data9F10Array[1]))
                    {
                        startPos += dataLength;
                        dataLength = emv9F10Data.Length;
                        codlLength = codlLength + dataLength;
                        Array.Copy(Encoding.ASCII.GetBytes(emv9F10Data), 0, hsmByteArray, startPos, dataLength);
                    }
                    else
                    {
                        startPos += dataLength;
                        dataLength = data9F10Array[2].Length;
                        codlLength = codlLength + dataLength;
                        Array.Copy(Encoding.ASCII.GetBytes(data9F10Array[2]), 0, hsmByteArray, startPos, dataLength);
                    }
                }
                return (hsmByteArray);

            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                codlLength = 0;
                return null;
            }
        }

        /// <summary>
        /// Create Check CVV Data
        /// </summary>
        /// <param name="ncccMessage">Request Bic Message</param>
        /// <param name="option">1:CVV,2:CVV2</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGenerateCVV(NcccIso8583 ncccMessage, string bankCode, int option)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2 = null;
                string entryMode = null;

                //Check P22 Service Entry Node
                FIELD field = FIELD.F_22;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    entryMode = ((string)ncccMessage.Core().Field(field)).Substring(0, 2);
                else
                {
                    Log.Info("Call HSMGenerateCVV Error,Field 22 Not Exist");
                    return null;
                }
                //Check P35 For Track 2
                field = FIELD.F_35;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)ncccMessage.Core().Field(field));
                else
                {
                    Log.Info("Call HSMGenerateCVV Error,Field 35 Not Exist");
                    return null;
                }
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMGenerateCVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                //card No
                messageStringBuilder.Append($"{bicTrack2.IsoCardNbr}");   //PAN Length,PAN Data

                if (string.IsNullOrEmpty(bicTrack2.IsoExpirDte) || bicTrack2.IsoExpirDte.Length != 4)
                {
                    Log.Info("Call HSMGenerateCVV Error,Expire Date Error");
                    return null;
                }

                if (bicTrack2.IsoCardType.Equals(CreditCardType.JCB) && option == 2)
                    messageStringBuilder.Append(bicTrack2.IsoExpirDte.Substring(2, 2) + bicTrack2.IsoExpirDte.Substring(0, 2));
                else
                    messageStringBuilder.Append(bicTrack2.IsoExpirDte.Substring(0, 2) + bicTrack2.IsoExpirDte.Substring(2, 2));

                //Expire Date
                //Decision SVC For Card
                //Service Code
                //IC SVC:999 Other Track 2 Service Code
                //CVV2:000
                if (option == 1)
                {
                    if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                        bicTrack2.IsoExpirDte.Equals(string.Empty) ||
                        bicTrack2.IsoSvc.Equals(string.Empty) ||
                        bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                    {
                        Log.Info("Call HSMGenerateCVV Error,Track2 Data Error");
                        return null;
                    }
                    if ((entryMode.Equals("05")) || (entryMode.Equals("95")) || (entryMode.Equals("07")))
                        messageStringBuilder.Append("999");
                    else
                    {
                        messageStringBuilder.Append(bicTrack2.IsoSvc.Equals(string.Empty) ? "888" : bicTrack2.IsoSvc);
                    }
                }
                else
                {
                    if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                        bicTrack2.IsoExpirDte.Equals(string.Empty) ||
                        bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                    {
                        Log.Info("Call HSMGenerateCVV Error,Track2 Data Error");
                        return null;
                    }
                    messageStringBuilder.Append("000");
                }

                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Check CVV Data(FISC)
        /// </summary>
        /// <param name="FiscMessage">Request Bic Message</param>
        /// <param name="option">1:CVV,2:CVV2</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGenerateCVV(FISC fiscMessage, string bankCode)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2 = null;
                string entryMode = null;

                //Check P22 Service Entry Node
                FIELD field = FIELD.F_22;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    entryMode = ((string)fiscMessage.Core().Field(field)).Substring(0, 2);
                else
                {
                    Log.Info("Call HSMGenerateCVV Error,Field 22 Not Exist");
                    return null;
                }
                //Check P35 For Track 2
                field = FIELD.F_35;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                else
                {
                    field = FIELD.F_45;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                        bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                    else
                    {
                        Log.Info("Call HSMGenerateCVV Error,Field 35 Or 45 Not Exist");
                        return null;
                    }
                }
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMGenerateCVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                //card No
                messageStringBuilder.Append($"{bicTrack2.IsoCardNbr}");   //PAN Length,PAN Data

                if (string.IsNullOrEmpty(bicTrack2.IsoExpirDte) || bicTrack2.IsoExpirDte.Length != 4)
                {
                    Log.Info("Call HSMGenerateCVV Error,Expire Date Error");
                    return null;
                }

                messageStringBuilder.Append(bicTrack2.IsoExpirDte.Substring(0, 2) + bicTrack2.IsoExpirDte.Substring(2, 2));

                //Expire Date
                //Decision SVC For Card
                //Service Code
                //IC SVC:999 Other Track 2 Service Code
                if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                    bicTrack2.IsoExpirDte.Equals(string.Empty) ||
                    bicTrack2.IsoSvc.Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Info("Call HSMGenerateCVV Error,Track2 Data Error");
                    return null;
                }
                if ((entryMode.Equals("05")) || (entryMode.Equals("95")) || (entryMode.Equals("07")))
                    messageStringBuilder.Append("999");
                else
                {
                    messageStringBuilder.Append(bicTrack2.IsoSvc.Equals(string.Empty) ? "888" : bicTrack2.IsoSvc);
                }
                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }


        /// <summary>
        /// Create Check CVV2 Data(FISC)
        /// </summary>
        /// <param name="FiscMessage">Request Bic Message</param>
        /// <param name="option">1:CVV,2:CVV2</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGenerateCVV2(FISC fiscMessage, string bankCode)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2 = null;
                string isoCardNbr = string.Empty;
                string isoExpirDte = string.Empty;
                string calExpirDte = string.Empty;

                // check P2 For CardNbr 
                FIELD field = FIELD.F_2;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    isoCardNbr = (string)fiscMessage.Core().Field(field);

                //Check P35 For Track 2
                field = FIELD.F_14;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    isoExpirDte = (string)fiscMessage.Core().Field(field);
                else
                {
                    field = FIELD.F_35;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                        isoExpirDte = bicTrack2.IsoExpirDte;
                        isoCardNbr = bicTrack2.IsoCardNbr;
                    }
                    else
                    {
                        field = FIELD.F_45;
                        if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                        {
                            bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                            isoExpirDte = bicTrack2.IsoExpirDte;
                            isoCardNbr = bicTrack2.IsoCardNbr;
                        }
                        else
                        {
                            Log.Info("Call HSMGenerateCVV Error,Field  14, 35 Or 45 Not Exist");
                            return null;
                        }
                    }
                }
                string bizId = new string(' ', 8);
                switch (CreditCardUtility.FindCardType(isoCardNbr))
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        calExpirDte = isoExpirDte.Substring(0, 2) + isoExpirDte.Substring(2, 2);
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        calExpirDte = isoExpirDte.Substring(0, 2) + isoExpirDte.Substring(2, 2);
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        calExpirDte = isoExpirDte.Substring(2, 2) + isoExpirDte.Substring(0, 2);
                        break;
                    default:
                        Log.Info("Call HSMGenerateCVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                //card No
                messageStringBuilder.Append($"{isoCardNbr}");   //PAN Length,PAN Data

                if (string.IsNullOrEmpty(isoExpirDte) || isoExpirDte.Length != 4 ||
                    string.IsNullOrEmpty(calExpirDte))
                {
                    Log.Info("Call HSMGenerateCVV Error,Expire Date Error");
                    return null;
                }

                //expirDte
                messageStringBuilder.Append(calExpirDte);

                //CVV2:000
                messageStringBuilder.Append("000");

                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Check PVV Data
        /// </summary>
        /// <param name="ncccMessage">Request Bic Message</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGeneratePVV(NcccIso8583 ncccMessage, string bankCode, string ppkBizId, string pinOffset)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                string pinBlockData = string.Empty;
                BicTrack2 bicTrack2 = null;

                //Check P35 For Track 2
                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)ncccMessage.Core().Field(field));
                else
                {
                    Log.Info("Call HSMGeneratePVV Error,Field 35 Not Exist");
                    return null;
                }

                if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                    bicTrack2.IsoPki.Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Info("Call HSMGeneratePVV Error,Field 35 Format Error");
                    return null;
                }
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMGeneratePVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                field = FIELD.F_52;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    pinBlockData = (string)ncccMessage.Core().Field(field);
                else
                {
                    Log.Info("Call HSMGeneratePVV Error,Field 52 Not Exist");
                    return null;
                }

                if (string.IsNullOrEmpty(bicTrack2.IsoPki) || bicTrack2.IsoPki.Length != 1)
                {
                    Log.Info("Call HSMGeneratePVV Error,PVK Index Error");
                    return null;
                }
                messageStringBuilder.Append(bicTrack2.IsoPki);           //PVK Index
                messageStringBuilder.Append(bicTrack2.IsoCardNbr);       //CARD NO
                messageStringBuilder.Append($"{ppkBizId,-8}");           //ppkBizId
                messageStringBuilder.Append("0");                        //PIN Block Format:ANSI
                messageStringBuilder.Append(pinBlockData);               //PINBlock
                messageStringBuilder.Append($"{pinOffset,-4}");          //PIN OFFSET
                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Check PVV Data(FISC)
        /// </summary>
        /// <param name="FiscMessage">Request Bic Message</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGeneratePVV(FISC fiscMessage, string bankCode, string ppkBizId, string pinOffset)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                string pinBlockData = string.Empty;
                BicTrack2 bicTrack2 = null;

                //Check P35 For Track 2
                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                else
                {
                    field = FIELD.F_45;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                        bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                    else
                    {
                        Log.Info("Call HSMGeneratePVV Error,Field 35 Or 45 Not Exist");
                        return null;
                    }
                }

                if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                    bicTrack2.IsoPki.Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Info("Call HSMGeneratePVV Error,Field 35 or 45 Format Error");
                    return null;
                }
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMGeneratePVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                field = FIELD.F_52;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    pinBlockData = (string)fiscMessage.Core().Field(field);
                else
                {
                    Log.Info("Call HSMGeneratePVV Error,Field 52 Not Exist");
                    return null;
                }

                if (string.IsNullOrEmpty(bicTrack2.IsoPki) || bicTrack2.IsoPki.Length != 1)
                {
                    Log.Info("Call HSMGeneratePVV Error,PVK Index Error");
                    return null;
                }
                messageStringBuilder.Append(bicTrack2.IsoPki);           //PVK Index
                messageStringBuilder.Append(bicTrack2.IsoCardNbr);       //CARD NO
                messageStringBuilder.Append($"{ppkBizId,-8}");           //ppkBizId
                messageStringBuilder.Append("0");                        //PIN Block Format:ANSI
                messageStringBuilder.Append(pinBlockData);               //PINBlock
                messageStringBuilder.Append($"{pinOffset,-4}");          //PIN OFFSET
                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Encrypt Bank's Pin Data
        /// </summary>
        /// <param name="ncccMessage">Request Bic Message</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGenerateEncryptBankPin(NcccIso8583 ncccMessage, string bankCode, string ppkBizId, string clearPin)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2 = null;

                //Check P35 For Track 2
                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)ncccMessage.Core().Field(field));
                else
                {
                    Log.Info("Call HSMGenerateEncryptBankPin Error,Field 35 Not Exist");
                    return null;
                }

                if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                    bicTrack2.IsoPki.Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Info("Call HSMGenerateEncryptBankPin Error,Field 35 Format Error");
                    return null;
                }
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMGenerateEncryptBankPin Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");
                messageStringBuilder.Append(bicTrack2.IsoCardNbr);       //CARD NO
                messageStringBuilder.Append(bicTrack2.IsoPki);           //PVK Index
                messageStringBuilder.Append($"{ppkBizId,-8}");           //ppkBizId
                messageStringBuilder.Append($"{"IWK",-3}");              //ppkBizId
                messageStringBuilder.Append("0");                        //PIN Block Format:ANSI
                messageStringBuilder.Append($"{clearPin,-6}");           //CLEAR PIN
                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());

            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Encrypt Bank's Pin Data(FISC)
        /// </summary>
        /// <param name="fiscMessage">Request Bic Message</param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMGenerateEncryptBankPin(FISC fiscMessage, string bankCode, string ppkBizId, string clearPin)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2 = null;

                //Check P35 For Track 2
                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                else
                {
                    field = FIELD.F_45;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                        bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                    else
                    {
                        Log.Info("Call HSMGenerateEncryptBankPin Error,Field 35 Or 45 Not Exist");
                        return null;
                    }
                }

                if (bicTrack2.IsoCardNbr.Equals(string.Empty) ||
                    bicTrack2.IsoPki.Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Info("Call HSMGenerateEncryptBankPin Error,Field 35 or 45 Format Error");
                    return null;
                }
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMGenerateEncryptBankPin Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");
                messageStringBuilder.Append(bicTrack2.IsoCardNbr);       //CARD NO
                messageStringBuilder.Append(bicTrack2.IsoPki);           //PVK Index
                messageStringBuilder.Append($"{ppkBizId,-8}");           //ppkBizId
                messageStringBuilder.Append($"{"IWK",-3}");              //ppkBizId
                messageStringBuilder.Append("0");                        //PIN Block Format:ANSI
                messageStringBuilder.Append($"{clearPin,-4}");           //CLEAR PIN
                return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());

            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Check CAVV Data
        /// </summary>
        /// <param name="ncccMessage">Request Bic Message</param>
        /// <param name="bankCode"></param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateCAVV(NcccIso8583 ncccMessage, string bankCode)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2;

                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)ncccMessage.Core().Field(field));
                else
                {
                    Log.Error("Call HSMCreateCAVV Error, Track2 Data Error(No F_35 Data)");
                    return null;
                }

                if (bicTrack2.IsoCardNbr.Trim().Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Error("Call HSMCreateCAVV Error, Track2 Data Error");
                    return null;
                }
                //bizId
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMCreateCAVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                //field = FIELD.F_63;
                //if (!ObjectUtils.IsNull(ncccMessage.RawVal(FIELD.F_63)))
                //{
                //    field = FIELD.F_63;
                //}
                field = ncccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                {
                    if (bicTrack2.IsoCardType.Equals(CreditCardType.MasterCard))
                    {
                        var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                        if (tokenGroup == null) return null;
                        var tokenCE = (TokenCE)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenCE.Id);
                        var tokenC0 = (TokenC0)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenC0.Id);
                        if (tokenCE == null)
                        {
                            Log.Error($"Call HSMCreateCAVVV Error, tokenCE Data Is Null");
                            return null;
                        }
                        if (tokenC0 == null)
                        {
                            Log.Error($"Call HSMCreateCAVV Error, tokenC0 Data Is Null");
                            return null;
                        }
                        if (!tokenC0.UcafAuthnCollInd.Equals("2"))
                        {
                            Log.Error($"Call HSMCreateCAVV Error, tokenC0.UcafAuthnCollInd Data Error[{tokenC0.UcafAuthnCollInd}]");
                            return null;
                        }

                        const int tokenHeaderLength = 10;
                        string tokenCEHeaderData = tokenCE.Encode();
                        string tokenCEHeader = tokenCEHeaderData.Substring(0, tokenHeaderLength);
                        string tokenCEData = tokenCEHeaderData.Substring(tokenHeaderLength, tokenCEHeaderData.Length - tokenHeaderLength);
                        //tokenCEData.Substring(2, tokenCEData.Length - 2) 整個字串的第1碼為j 時比照現行邏輯處理不變更
                        //tokenCEData.Substring(2, tokenCEData.Length - 2) 整個字串的的前2碼為kG/kH/kB/kA修改驗算規則如下述
                        byte[] orgBytes = null;
                        string tagHexString;
                        string serviceCode;
                        string expiryDate;
                        string tag43CavvData;
                        string checkString = tokenCEData.Substring(2, 2);
                        if (checkString.Substring(0, 1).Equals("j"))
                        {
                            //Decode Base64 28byte --> 20Byte
                            string base64String;
                            if (tokenCEData.Length >= 30)
                                base64String = tokenCEData.Substring(2, 28);
                            else
                                base64String = tokenCEData.Substring(2, tokenCEData.Length - 2).PadRight(28, ' ');
                            //Unpack 20Byte --> 40 Byte String
                            orgBytes = Convert.FromBase64String(base64String); //update 20200720
                            tagHexString = ColaCommUtility.HexBinary2HexString(orgBytes);

                            //Create SVC
                            var tagStartPos = 0;
                            var tagDataLength = 2;
                            string svcData1 = $"{Convert.ToInt32("0X" + tagHexString.Substring(tagStartPos, tagDataLength), 16):000}".Substring(1, 2);
                            tagStartPos = 20;
                            tagDataLength = 1;
                            string svcData2 = $"{Convert.ToByte(tagHexString.Substring(tagStartPos, tagDataLength)) & 0X0F:0}";
                            serviceCode = svcData2 + svcData1;

                            //Create CVK
                            tagStartPos = 21;
                            tagDataLength = 1;
                            var cvkIndex = tagHexString.Substring(tagStartPos, tagDataLength);

                            //Create Expire Date
                            tagStartPos = 22;
                            tagDataLength = 8;
                            string tempData10 = $"{Convert.ToInt64("0X" + tagHexString.Substring(tagStartPos, tagDataLength), 16):0000000000}";
                            expiryDate = tempData10.Substring(6, 4);

                            //Create CAVV
                            tagStartPos = 30;
                            tagDataLength = 10;
                            string macData = tagHexString.Substring(tagStartPos, tagDataLength);
                            tag43CavvData = macData.Substring(1, 3);
                        }
                        else if (new string[] { "kG", "kH", "kB", "kA" }.Contains(checkString))
                        {
                            //Decode Base64
                            //orgBytes = Convert.FromBase64String(tokenCEData.Substring(4, 4));  //IAV
                            orgBytes = Convert.FromBase64String(tokenCEData.Substring(2, tokenCEData.Length - 2));
                            //Unpack 4Byte --> 8 Byte String
                            tagHexString = ColaCommUtility.HexBinary2HexString(orgBytes);

                            // Create SVC
                            serviceCode = tagHexString.Substring(7, 3);

                            //Create Expire Date
                            expiryDate = bicTrack2.IsoExpirDte;

                            //Create CAVV
                            tag43CavvData = tagHexString.Substring(4, 3);
                        }
                        else
                        {
                            Log.Error($"Call HSMCreateCAVV Error, tokenCE Data Error[{checkString}]");
                            return null;
                        }

                        //keyindex -1 fiexed :01
                        messageStringBuilder.Append("1");

                        //cardNo - 16
                        messageStringBuilder.Append($"{bicTrack2.IsoCardNbr}");   //PAN Data

                        //expiryDate - 4
                        messageStringBuilder.Append(expiryDate);

                        //serviceCode - 3
                        messageStringBuilder.Append(serviceCode);

                        //cavv - 3
                        messageStringBuilder.Append(tag43CavvData);
                        return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
                    }
                    else
                    {
                        //field = FIELD.F_63;
                        field = ncccMessage.GetTokenFieldId();
                        if (!ObjectUtils.IsNull(ncccMessage.RawVal(ncccMessage.GetTokenFieldId())))
                        {
                            //field = FIELD.F_63;
                            field = ncccMessage.GetTokenFieldId();
                        }
                        else if (!ObjectUtils.IsNull(ncccMessage.RawVal(FIELD.F_126)))
                        {
                            field = FIELD.F_126;
                        }
                        var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                        var tokenC6 = (TokenC6)tokenGroup?.GetToken(NcccTransactionConst.PosAdditionalData.TokenC6.Id);
                        if (tokenC6 == null) return null;
                        const int tokenHeaderLength = 10;
                        string tokenC6HeaderData = tokenC6.Encode();
                        string tokenC6Header = tokenC6HeaderData.Substring(0, tokenHeaderLength);
                        string tokenC6Data = tokenC6HeaderData.Substring(tokenHeaderLength, tokenC6HeaderData.Length - tokenHeaderLength);

                        BicC6TokenStruct tokenData = new BicC6TokenStruct();
                        byte[] tokenDataBytes = Encoding.ASCII.GetBytes(tokenC6Data);
                        tokenData = (BicC6TokenStruct)ColaCommUtility.RawDeserialize(tokenDataBytes, tokenData.GetType());
                        string cavvSVC;
                        string cavv;
                        if (bicTrack2.IsoCardType.Equals(CreditCardType.Visa))
                        {
                            if (tokenData.c6_filler[0] == 0x37)
                            {
                                //keyIndex - 1  //KGI 共用CVV函數無法指定CVK
                                //cavvCvk = Encoding.ASCII.GetString(tokenData.c6_key).Substring(1, 1);

                                //cardNo - 16
                                var cvvPan = ColaCommUtility.CreateVisaCAVVPan(ncccMessage, tokenData, bicTrack2);
                                if (string.IsNullOrEmpty(cvvPan) || cvvPan.Length != 16)
                                {
                                    Log.Error($"Call HSMCreateCAVV Error, cvvPan Data Error");
                                    return null;
                                }

                                //keyindex -1 fiexed :01
                                messageStringBuilder.Append("1");
                                //cardNo - 16
                                messageStringBuilder.Append($"{cvvPan}");   //PAN Data
                                //expiryDate - 4
                                messageStringBuilder.Append(tokenData.c6_un);
                                //serviceCode - 3
                                cavvSVC = Encoding.ASCII.GetString(tokenData.c6_arc).Substring(1, 1) + Encoding.ASCII.GetString(tokenData.c6_sarc);
                                messageStringBuilder.Append(cavvSVC);
                                //cavv - 3
                                cavv = Encoding.ASCII.GetString(tokenData.c6_cavvo).Substring(1, 3);
                                messageStringBuilder.Append(cavvSVC);
                            }
                            else
                            {
                                //keyIndex - 1  //KGI 共用CVV函數無法指定CVK
                                //cavvCvk = Encoding.ASCII.GetString(tokenData.c6_key).Substring(1, 1);
                                //expiryDate - 4
                                messageStringBuilder.Append(tokenData.c6_un);
                                //serviceCode - 3
                                cavvSVC = Encoding.ASCII.GetString(tokenData.c6_arc).Substring(1, 1) + Encoding.ASCII.GetString(tokenData.c6_sarc);
                                messageStringBuilder.Append(cavvSVC);
                                //cardNo - 16
                                messageStringBuilder.Append($"{bicTrack2.IsoCardNbr.Length:00}{bicTrack2.IsoCardNbr}");   //PAN Length,PAN Data
                                //cavv - 3
                                cavv = Encoding.ASCII.GetString(tokenData.c6_cavvo).Substring(1, 3);
                                messageStringBuilder.Append(cavv);
                            }
                        }
                        else
                        {
                            ///keyindex -1 fiexed :01
                            messageStringBuilder.Append("1");
                            //cardNo - 16
                            messageStringBuilder.Append($"{bicTrack2.IsoCardNbr}");   //PAN Length,PAN Data
                            //expiryDate - 4
                            messageStringBuilder.Append(tokenData.c6_un);
                            //serviceCode - 3
                            cavvSVC = Encoding.ASCII.GetString(tokenData.c6_arc).Substring(1, 1) + Encoding.ASCII.GetString(tokenData.c6_sarc);
                            messageStringBuilder.Append(cavvSVC);
                            //cavv - 3
                            cavv = Encoding.ASCII.GetString(tokenData.c6_cavvo).Substring(1, 3);
                            //cavv -3
                            messageStringBuilder.Append(cavv);
                        }
                        return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Check CAVV Data(FISC)
        /// </summary>
        /// <param name="fiscMessage">Request Bic Message</param>
        /// <param name="bankCode"></param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateCAVV(FISC fiscMessage, string bankCode)
        {
            try
            {
                var messageStringBuilder = new StringBuilder();
                BicTrack2 bicTrack2;
                string cavvCardNo;

                FIELD field = FIELD.F_35;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                else
                {
                    field = FIELD.F_45;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                        bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                    else
                    {
                        Log.Error("Call HSMCreateCAVV Error, Track2 Data Error(No F_35 or F_45 Data)");
                        return null;
                    }
                }
                cavvCardNo = bicTrack2.IsoCardNbr;

                if (bicTrack2.IsoCardNbr.Trim().Equals(string.Empty) ||
                    bicTrack2.IsoCardType.Equals(CreditCardType.Unknown))
                {
                    Log.Error("Call HSMCreateCAVV Error, Track2 Data Error");
                    return null;
                }

                //bizId
                string bizId = new string(' ', 8);
                switch (bicTrack2.IsoCardType)
                {
                    case CreditCardType.Visa:
                        bizId = $"{"VISA",-8}";
                        break;
                    case CreditCardType.MasterCard:
                        bizId = $"{"MASTER",-8}";
                        break;
                    case CreditCardType.JCB:
                        bizId = $"{"JCB",-8}";
                        break;
                    default:
                        Log.Info("Call HSMCreateCAVV Error,Unknown Card Type");
                        return null;
                }
                messageStringBuilder.Append(bizId);

                //bankId
                messageStringBuilder.Append($"{bankCode,-3}");

                string expiryDate = string.Empty;
                string serviceCode = string.Empty;
                string tag43CavvData = string.Empty;

                field = FIELD.F_48;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    var de48TagData = fiscMessage.RawVal(field).ToString();
                    var de48TagField = new DE48TagField(de48TagData);

                    string de48TagIdString = "43";
                    string isoP48T43;
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                        isoP48T43 = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                    else
                    {
                        Log.Error("Call HSMCreateCAVV Error, isoP48T43 Data Error");
                        return null;
                    }

                    string cvkIndex;
                    int startPos;
                    int dataLength;
                    byte[] orgBytes = null;
                    switch (bicTrack2.IsoCardType)
                    {
                        case CreditCardType.Visa:
                            //This Process For Visa
                            //Unpack 20Byte --> 40 Byte String
                            var tag48HexString = isoP48T43.Substring(2, 40);
                            orgBytes = ColaCommUtility.HexString2HexBinary(tag48HexString);
                            Log.Info($"Call Generate3DCAVV ASC Version {tag48HexString.Substring(30, 1)}");
                            if (tag48HexString.Substring(30, 1).Equals("7"))
                            {
                                de48TagIdString = "42";
                                var isoP48T42EcFlag = string.Empty;
                                //if (isoMessage.GetISOF48DE48Field().FindDE48TagIdExist(de48TagIdString))
                                if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                                {
                                    // Last Character For tempString
                                    string tempString = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                                    isoP48T42EcFlag = tempString.Substring(tempString.Length - 1, 1);
                                    if (!new string[] { "0", "1", "2" }.Contains(isoP48T42EcFlag))
                                    {
                                        Log.Info($"Call Generate3DCAVV ECI Value not in ('0','1','2') {isoP48T42EcFlag}");
                                        return null;
                                    }
                                }
                                //cardNo - 16
                                var mappingEciValue = Convert.ToString(7 - Convert.ToInt16(isoP48T42EcFlag));
                                var cvvPan = ColaCommUtility.CreateVisaCAVVPan(bicTrack2.IsoCardNbr, mappingEciValue, tag48HexString);
                                if (string.IsNullOrEmpty(cvvPan) || cvvPan.Length != 16)
                                {
                                    Log.Error($"Call HSMVerifyCAVV Error, cvvPan Data Error");
                                    return null;
                                }
                                cavvCardNo = cvvPan;
                                //Create SVC
                                startPos = 1;
                                dataLength = 3;
                                serviceCode = tag48HexString.Substring(startPos, dataLength);
                                //Create CVK
                                startPos = 5;
                                dataLength = 1;
                                cvkIndex = tag48HexString.Substring(startPos, dataLength);
                                //Create CAVV
                                startPos = 7;
                                dataLength = 3;
                                tag43CavvData = tag48HexString.Substring(startPos, dataLength);
                                //Create Expire Date
                                startPos = 10;
                                dataLength = 4;
                                expiryDate = tag48HexString.Substring(startPos, dataLength);
                            }
                            else
                            {
                                //Create SVC
                                startPos = 1;
                                dataLength = 3;
                                serviceCode = tag48HexString.Substring(startPos, dataLength);
                                //Create CVK
                                startPos = 5;
                                dataLength = 1;
                                cvkIndex = tag48HexString.Substring(startPos, dataLength);
                                //Create CAVV
                                startPos = 7;
                                dataLength = 3;
                                tag43CavvData = tag48HexString.Substring(startPos, dataLength);
                                //Create Expire Date
                                startPos = 10;
                                dataLength = 4;
                                expiryDate = tag48HexString.Substring(startPos, dataLength);
                            }
                            break;
                        case CreditCardType.MasterCard:
                            //This Process For Master Card
                            string base64String = EbcdicConverter.GetString(isoP48T43);
                            var checkString = base64String.Substring(0, 2);
                            //Decode Base64 28byte --> 20Byte
                            orgBytes = Convert.FromBase64String(base64String);
                            //Unpack 20Byte --> 40 Byte String
                            tag48HexString = ColaCommUtility.HexBinary2HexString(orgBytes);
                            if (new string[] { "kB", "kA" }.Contains(checkString))
                            {
                                Log.Info($"Call Generate3DCAVV ASC2.0 Version,Version value {checkString}");

                                expiryDate = bicTrack2.IsoExpirDte;

                                //Create CVK
                                cvkIndex = "1";

                                //Create CAVV
                                startPos = 4;
                                dataLength = 3;
                                tag43CavvData = tag48HexString.Substring(startPos, dataLength);

                                //Create SVC
                                startPos = 7;
                                dataLength = 3;
                                serviceCode = tag48HexString.Substring(startPos, dataLength);
                            }
                            else
                            {
                                Log.Info($"Call Generate3DCAVV ASC 1.0 Version,Version value {checkString}");
                                //Create SVC
                                startPos = 0;
                                dataLength = 2;
                                string svcData1 = String.Format("{0:000}", Convert.ToInt32("0X" + tag48HexString.Substring(startPos, dataLength), 16)
                                                               ).Substring(1, 2);
                                startPos = 20;
                                dataLength = 1;
                                string svcData2 = String.Format("{0:0}", Convert.ToByte(tag48HexString.Substring(startPos, dataLength)) & 0X0F);
                                serviceCode = svcData2 + svcData1;
                                //Create CVK
                                startPos = 21;
                                dataLength = 1;
                                cvkIndex = tag48HexString.Substring(startPos, dataLength);
                                //Create Expire Date
                                startPos = 22;
                                dataLength = 8;
                                string tempData10 = String.Format("{0:0000000000}", Convert.ToInt64("0X" + tag48HexString.Substring(startPos, dataLength), 16));
                                expiryDate = tempData10.Substring(6, 4);
                                //Create CAVV
                                startPos = 30;
                                dataLength = 10;
                                string macData = tag48HexString.Substring(startPos, dataLength);
                                tag43CavvData = macData.Substring(1, 3);
                            }
                            break;
                        case CreditCardType.JCB:
                            //Unpack 20Byte --> 40 Byte String
                            tag48HexString = isoP48T43.Substring(2, 40);
                            //Create SVC
                            startPos = 1;
                            dataLength = 3;
                            serviceCode = tag48HexString.Substring(startPos, dataLength);
                            //Create CVK
                            startPos = 5;
                            dataLength = 1;
                            cvkIndex = tag48HexString.Substring(startPos, dataLength);
                            //Create CAVV
                            startPos = 7;
                            dataLength = 3;
                            tag43CavvData = tag48HexString.Substring(startPos, dataLength);
                            //Create Expire Date
                            startPos = 10;
                            dataLength = 4;
                            expiryDate = tag48HexString.Substring(startPos, dataLength);
                            break;
                    }

                    ///keyindex -1 fiexed :1
                    messageStringBuilder.Append("1");

                    //cardNo - 16
                    messageStringBuilder.Append($"{bicTrack2.IsoCardNbr}");   //PAN Length,PAN Data

                    //expiryDate - 4
                    messageStringBuilder.Append(expiryDate);

                    //serviceCode - 3
                    messageStringBuilder.Append(serviceCode);

                    //cavv - 3
                    messageStringBuilder.Append(tag43CavvData);

                    return Encoding.ASCII.GetBytes(messageStringBuilder.ToString());
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// Create Check ARPC Data
        /// </summary>
        /// <param name="ncccMessage">Request Bic Message</param>
        /// <param name="archexstring"></param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateARPCData(NcccIso8583 ncccMessage, string bankCode, string archexstring)
        {
            byte[] outputArpc = null;
            var respCode = string.Empty;
            var rtnRsnCode = string.Empty;
            try
            {
                const int hsmByteArrayLength = 57;
                var hsmByteArray = Encoding.ASCII.GetBytes(new string('0', hsmByteArrayLength));
                int startPos;
                int dataLength;
                BicTrack2 bicTrack2 = null;

                var field = FIELD.F_35;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)ncccMessage.Core().Field(field));
                else
                    return null;

                //field = FIELD.F_63;
                field = ncccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                    if (tokenGroup == null) return (hsmByteArray);
                    var tokenB2 = (TokenB2)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenB2.Id);
                    if (tokenB2 == null) return (hsmByteArray);
                    const int tokenHeaderLength = 10;
                    string tokenB2HeaderData = tokenB2.Encode();
                    string tokenB2Data = tokenB2HeaderData.Substring(tokenHeaderLength, tokenB2HeaderData.Length - tokenHeaderLength);

                    BicB2TokenStruct tokenData = new BicB2TokenStruct();
                    byte[] tokenDataBytes = System.Text.Encoding.ASCII.GetBytes(tokenB2Data);
                    if (tokenDataBytes.Length == SystemConstantData.Token.BicTokenIdB2Length)
                    {
                        tokenData = (BicB2TokenStruct)ColaCommUtility.RawDeserialize(tokenDataBytes, tokenData.GetType());
                    }
                    else
                    {
                        return (hsmByteArray);
                    }
                    // Get 9F10 CVR Data
                    int issApplDataLgth = Convert.ToInt32(Encoding.ASCII.GetString(tokenData.iss_appl_data_lgth));
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       Encoding.ASCII.GetString(tokenData.iss_appl_data),
                                                                       bicTrack2.IsoCardType).Split(';');
                    if (!ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        Log.Error("Process HSMCreateARPCData, Create data9F10 array string Error");
                        return (hsmByteArray);
                    }

                    //bizId
                    startPos = 0;
                    dataLength = 8;
                    string bizId = new string(' ', 8);
                    switch (bicTrack2.IsoCardType)
                    {
                        case CreditCardType.MasterCard:
                            bizId = $"{"MASTER",-8}";
                            break;
                        case CreditCardType.Visa:
                            bizId = $"{"VISA",-8}";
                            break;
                        case CreditCardType.JCB:
                            bizId = $"{"JCB",-8}";
                            break;
                        default:
                            Log.Info("Call HSMCreateARPCData Error,Unknown Card Type");
                            return null;
                    }
                    Array.Copy(Encoding.ASCII.GetBytes(bizId), 0, hsmByteArray, startPos, dataLength);

                    //bankId
                    startPos += dataLength;
                    dataLength = 3;
                    Array.Copy(Encoding.ASCII.GetBytes($"{bankCode,-3}"), 0, hsmByteArray, startPos, dataLength);

                    //keyIndex
                    startPos += dataLength;
                    if (data9F10.Length == 3 && data9F10[0].Length == 2)
                    {
                        dataLength = data9F10[0].Length;
                        Array.Copy(Encoding.ASCII.GetBytes(data9F10[0]), 0, hsmByteArray, startPos, dataLength);
                    }
                    else
                    {
                        dataLength = 2;
                    }

                    //ARQC LENGTH 16
                    startPos += dataLength;
                    dataLength = 16;
                    Array.Copy(tokenData.arqc, 0, hsmByteArray, startPos, dataLength);

                    //ARC LENGTH 4
                    startPos += dataLength;
                    dataLength = 4;
                    Array.Copy(Encoding.ASCII.GetBytes(archexstring), 0, hsmByteArray, startPos, dataLength);

                    //ATC LENGTH 4
                    startPos += dataLength;
                    dataLength = 4;
                    Array.Copy(tokenData.atc, 0, hsmByteArray, startPos, dataLength);

                    //PAN-DATA+PAN-SEQ 18
                    startPos += dataLength;
                    dataLength = 18;
                    Array.Copy(Encoding.ASCII.GetBytes(bicTrack2.IsoCardNbr + new string('0', dataLength - bicTrack2.IsoCardNbr.Length)), 0, hsmByteArray, startPos, dataLength);

                    //CRYPTOGRAM-VERSION
                    startPos += dataLength;
                    dataLength = 2;
                    string version;
                    switch (data9F10[1])
                    {
                        case "10":
                            // case "11":
                            version = "0A";
                            break;
                        case "14":
                            version = "0E";
                            break;
                        default:
                            version = data9F10[1];
                            break;
                    }
                    Array.Copy(Encoding.ASCII.GetBytes(version), 0, hsmByteArray, startPos, dataLength);

                }
                Log.Info("Call ARPC HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmByteArray));
                var stringData = Encoding.ASCII.GetString(hsmByteArray);
                var returnString = CallGenerateARPC(stringData.Substring(0, 8),
                                                    stringData.Substring(8, 3),
                                                    stringData.Substring(11, 2),
                                                    stringData.Substring(13, 16),
                                                    stringData.Substring(29, 4),
                                                    stringData.Substring(33, 4),
                                                    stringData.Substring(37, 18),
                                                    stringData.Substring(55, 2),
                                                    out respCode,
                                                    out rtnRsnCode,
                                                    out var arpcData);

                //Log.Info($"CallGenerateARPC input data[{Encoding.ASCII.GetString(hsmInputData)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], returnString[{returnString}]");
                var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                Log.Info($"HSMCreateARPCData Result : {resultMessage}");
                if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                {
                    Log.Info("Call ARPC HSM Return Data Is Null");
                    return outputArpc;
                }
                outputArpc = Encoding.ASCII.GetBytes(respCode + rtnRsnCode + arpcData);
                return (outputArpc);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"HSMCreateARPCData Exception:{ex.Message}");
            }
            return (outputArpc);
        }

        /// <summary>
        /// Create Check ARPC Data
        /// </summary>
        /// <param name="fiscMessage">Request Bic Message</param>
        /// <param name="archexstring"></param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateARPCData(FISC fiscMessage, string bankCode, string archexstring)
        {
            byte[] outputArpc = null;
            var respCode = string.Empty;
            var rtnRsnCode = string.Empty;
            try
            {
                const int hsmByteArrayLength = 57;
                var hsmByteArray = Encoding.ASCII.GetBytes(new string('0', hsmByteArrayLength));
                int startPos;
                int dataLength;
                BicTrack2 bicTrack2 = null;

                var field = FIELD.F_35;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    bicTrack2 = new BicTrack2((string)fiscMessage.Core().Field(field));
                else
                    return null;

                field = FIELD.F_55;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                {
                    var emvData = fiscMessage.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;
                    string emvTagIdString = "9F10";
                    string field9F10;
                    int emvTagIdDataLength = 14;

                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        field9F10 = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                    }
                    else
                    {
                        Log.Info($"Call HSMCreateARPCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }

                    string[] data9F10 = FISCDataUtility.ISOGet9F10Data(field9F10.Length, field9F10, bicTrack2.IsoCardType).Split(';');
                    if (data9F10.Length != 3)
                    {
                        Log.Info($"Call HSMCreateARPCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                        return (null);
                    }

                    //bizId
                    startPos = 0;
                    dataLength = 8;
                    string bizId = new string(' ', 8);
                    switch (bicTrack2.IsoCardType)
                    {
                        case CreditCardType.MasterCard:
                            bizId = $"{"MASTER",-8}";
                            break;
                        case CreditCardType.Visa:
                            bizId = $"{"VISA",-8}";
                            break;
                        case CreditCardType.JCB:
                            bizId = $"{"JCB",-8}";
                            break;
                        default:
                            Log.Info("Call HSMCreateARPCData Error,Unknown Card Type");
                            return null;
                    }
                    Array.Copy(Encoding.ASCII.GetBytes(bizId), 0, hsmByteArray, startPos, dataLength);

                    //bankId
                    startPos += dataLength;
                    dataLength = 3;
                    Array.Copy(Encoding.ASCII.GetBytes($"{bankCode,-3}"), 0, hsmByteArray, startPos, dataLength);

                    //keyIndex
                    startPos += dataLength;
                    if (data9F10.Length == 3 && data9F10[0].Length == 2)
                    {
                        dataLength = data9F10[0].Length;
                        Array.Copy(Encoding.ASCII.GetBytes(data9F10[0]), 0, hsmByteArray, startPos, dataLength);
                    }
                    else
                    {
                        dataLength = 2;
                    }

                    //ARQC LENGTH 16
                    string emvTagIdData = new string(' ', 16);
                    emvTagIdString = "9F26";
                    emvTagIdDataLength = 16;
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                        if (emvTagIdData.Length != emvTagIdDataLength)
                        {
                            Log.Info($"Call HSMCreateARPCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                            return (null);
                        }
                    }
                    startPos += dataLength;
                    dataLength = 16;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);

                    //ARC LENGTH 4
                    startPos += dataLength;
                    dataLength = 4;
                    Array.Copy(Encoding.ASCII.GetBytes(archexstring), 0, hsmByteArray, startPos, dataLength);

                    //ATC LENGTH 4
                    emvTagIdData = new string(' ', 4);
                    emvTagIdString = "9F36";
                    emvTagIdDataLength = 4;
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        emvTagIdData = CommonUtils.HexBinary2HexString(emvData[emvTagIdString].Value);
                        if (emvTagIdData.Length != emvTagIdDataLength)
                        {
                            Log.Info($"Call HSMCreateARPCData Error,Field[{field.ToString()}],TagId[{emvTagIdString}] Data Error");
                            return (null);
                        }
                    }
                    startPos += dataLength;
                    dataLength = 4;
                    Array.Copy(Encoding.ASCII.GetBytes(emvTagIdData), 0, hsmByteArray, startPos, dataLength);

                    //PAN-DATA+PAN-SEQ 18
                    startPos += dataLength;
                    dataLength = 18;
                    Array.Copy(Encoding.ASCII.GetBytes(bicTrack2.IsoCardNbr + new string('0', dataLength - bicTrack2.IsoCardNbr.Length)), 0, hsmByteArray, startPos, dataLength);

                    //CRYPTOGRAM-VERSION
                    startPos += dataLength;
                    dataLength = 2;
                    string version;
                    switch (data9F10[1])
                    {
                        case "10":
                            // case "11":
                            version = "0A";
                            break;
                        case "14":
                            version = "0E";
                            break;
                        default:
                            version = data9F10[1];
                            break;
                    }
                    Array.Copy(Encoding.ASCII.GetBytes(version), 0, hsmByteArray, startPos, dataLength);

                }
                Log.Info("Call ARPC HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmByteArray));
                var stringData = Encoding.ASCII.GetString(hsmByteArray);
                var returnString = CallGenerateARPC(stringData.Substring(0, 8),
                                                    stringData.Substring(8, 3),
                                                    stringData.Substring(11, 2),
                                                    stringData.Substring(13, 16),
                                                    stringData.Substring(29, 4),
                                                    stringData.Substring(33, 4),
                                                    stringData.Substring(37, 18),
                                                    stringData.Substring(55, 2),
                                                    out respCode,
                                                    out rtnRsnCode,
                                                    out var arpcData);

                //Log.Info($"CallGenerateARPC input data[{Encoding.ASCII.GetString(hsmInputData)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], returnString[{returnString}]");
                var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                Log.Info($"HSMCreateARPCData Result : {resultMessage}");
                if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                {
                    Log.Info("Call ARPC HSM Return Data Is Null");
                    return outputArpc;
                }
                outputArpc = Encoding.ASCII.GetBytes(respCode + rtnRsnCode + arpcData);
                return (outputArpc);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"HSMCreateARPCData Exception:{ex.Message}");
            }
            return (outputArpc);
        }

        /// <summary>
        /// Create Change Receive IWKAWK
        /// </summary>
        /// <param name="tr31PinBlock"></param>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMCreateChangeReceiveIWKAWK(string tr31PinBlock, string wkBizId, string bankCode, string keySync)
        {
            try
            {
                const int inputLength = 200;
                byte[] inputData = Encoding.ASCII.GetBytes(new string('0', inputLength));
                int startPos;
                int dataLength;

                //wkBizId
                startPos = 0;
                dataLength = 8;
                Array.Copy(Encoding.ASCII.GetBytes($"{wkBizId,-8}"), 0, inputData, startPos, dataLength);

                //zcmkBizId
                startPos += dataLength;
                dataLength = 8;
                Array.Copy(Encoding.ASCII.GetBytes($"{wkBizId,-8}"), 0, inputData, startPos, dataLength);

                //bankId
                startPos += dataLength;
                dataLength = 3;
                Array.Copy(Encoding.ASCII.GetBytes($"{bankCode,-3}"), 0, inputData, startPos, dataLength);

                //wkKeyId
                startPos += dataLength;
                dataLength = 3;
                Array.Copy(Encoding.ASCII.GetBytes("IWK"), 0, inputData, startPos, dataLength);

                //keySync
                startPos += dataLength;
                dataLength = 4;
                Array.Copy(Encoding.ASCII.GetBytes($"{keySync,-4}"), 0, inputData, startPos, dataLength);

                //exportType: fixed :3 
                startPos += dataLength;
                dataLength = 1;
                Array.Copy(Encoding.ASCII.GetBytes("3"), 0, inputData, startPos, dataLength);

                //keyLengthType: fixed :2 
                startPos += dataLength;
                dataLength = 1;
                Array.Copy(Encoding.ASCII.GetBytes("2"), 0, inputData, startPos, dataLength);

                //keyLength
                startPos += dataLength;
                dataLength = 3;
                var tr31PinBlockLength = tr31PinBlock.Length.ToString().PadLeft(3, '0');
                Array.Copy(Encoding.ASCII.GetBytes(tr31PinBlockLength), 0, inputData, startPos, dataLength);

                //encKey
                startPos += dataLength;
                byte[] encryKey = Encoding.ASCII.GetBytes(tr31PinBlock);
                dataLength = encryKey.Length;
                Array.Copy(encryKey, 0, inputData, startPos, dataLength);

                return (inputData);
            }
            catch (Exception e)
            {
                Log.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{e.Message}]");
                return null;
            }
        }

        /// <summary>
        /// New Key Replace
        /// </summary>
        /// <returns>
        /// Create Byte Array
        /// </returns>
        public static byte[] HSMNewKeyReplaceIWKAWK(string wkBizId, string bankCode)
        {
            const int inputLength = 18;
            byte[] inputData = Encoding.ASCII.GetBytes(new string('0', inputLength));
            int startPos;
            int dataLength;

            //wkBizId
            startPos = 0;
            dataLength = 8;
            Array.Copy(Encoding.ASCII.GetBytes($"{wkBizId,-8}"), 0, inputData, startPos, dataLength);

            //bankId
            startPos += dataLength;
            dataLength = 3;
            Array.Copy(Encoding.ASCII.GetBytes($"{bankCode,-3}"), 0, inputData, startPos, dataLength);

            //wkKeyid
            startPos += dataLength;
            dataLength = 3;
            Array.Copy(Encoding.ASCII.GetBytes("IWK"), 0, inputData, startPos, dataLength);

            //ReplaceMac
            startPos += dataLength;
            dataLength = 1;
            Array.Copy(Encoding.ASCII.GetBytes("0"), 0, inputData, startPos, dataLength);

            //ReplaceOPINENC
            startPos += dataLength;
            dataLength = 1;
            Array.Copy(Encoding.ASCII.GetBytes("0"), 0, inputData, startPos, dataLength);

            //ReplaceIPINENC
            startPos += dataLength;
            dataLength = 1;
            Array.Copy(Encoding.ASCII.GetBytes("1"), 0, inputData, startPos, dataLength);

            //ReplaceTR31
            startPos += dataLength;
            dataLength = 1;
            Array.Copy(Encoding.ASCII.GetBytes("1"), 0, inputData, startPos, dataLength);

            return (inputData);
        }

        /// <summary>
        /// NCCC Check Flow For HSM
        /// </summary>
        /// <param name="ncccMessage">Request BIC Message</param>
        /// <param name="hsmCheckFlag">HSM SUIP Check Flag</param>
        /// <param name="bankId"></param>
        /// <param name="ncccAuthInfoData">Require Data For Authorization</param>
        public static void HSMCheckFlow(NcccIso8583 ncccMessage, string hsmCheckFlag, string bankId, ref NcccAuthInfoStruct ncccAuthInfoData)
        {
            byte[] hsmInputData;
            int codllength = 0;
            string pvvDobGenPvv = string.Empty;
            string respCode;
            string rtnRsnCode;
            string stringData;
            string returnString;
            //Initial Check CVV
            bool isARQCCheck, isCVVCheck, isCVV2Check, isCAVVCheck, isPVVCheck, isPVVDobCheck;
            try
            {
                //Decision SUIP Check Flag For Input String 0:ARQC 1:CVV 2:CVV2 3:CAVV 4:PVV 5:PVVDob
                if ((hsmCheckFlag == null) || (hsmCheckFlag.Length != 6))
                {
                    isARQCCheck = false;
                    isCVVCheck = false;
                    isCVV2Check = false;
                    isCAVVCheck = false;
                    isPVVCheck = false;
                    isPVVDobCheck = false;
                }
                else
                {
                    isARQCCheck = hsmCheckFlag.Substring(0, 1).ToUpper().Equals("Y");

                    isCVVCheck = hsmCheckFlag.Substring(1, 1).ToUpper().Equals("Y");

                    isCVV2Check = hsmCheckFlag.Substring(2, 1).ToUpper().Equals("Y");

                    isCAVVCheck = hsmCheckFlag.Substring(3, 1).ToUpper().Equals("Y");

                    isPVVCheck = hsmCheckFlag.Substring(4, 1).ToUpper().Equals("Y");

                    isPVVDobCheck = hsmCheckFlag.Substring(5, 1).ToUpper().Equals("Y");
                }
                FIELD field;
                //Check ARQC(Token B2)
                if (isARQCCheck)
                {
                    //field = FIELD.F_63;
                    field = ncccMessage.GetTokenFieldId();
                    if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    {
                        var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                        var tokenB2 = (TokenB2)tokenGroup?.GetToken(NcccTransactionConst.PosAdditionalData.TokenB2.Id);
                        if (tokenB2 != null)
                        {
                            hsmInputData = HSMCreateARQCData(ncccMessage, bankId, out codllength);
                            if (hsmInputData == null)
                            {
                                ncccAuthInfoData.arqcCheck = 'Y';
                                ncccAuthInfoData.arqcChkResult = 'N';
                                Log.Info("Call ARQC HSM Input Data Is Null");
                                return;
                            }
                            Log.Info("Call ARQC HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                            stringData = Encoding.ASCII.GetString(hsmInputData);
                            returnString = CallVerifyARQC(stringData.Substring(0, 8),
                                                            stringData.Substring(8, 3),
                                                            stringData.Substring(11, 2),
                                                            codllength.ToString().PadLeft(2, '0'),
                                                            stringData.Substring(49, codllength),
                                                            stringData.Substring(13, 16),
                                                            stringData.Substring(29, 18),
                                                            stringData.Substring(47, 2),
                                                            out respCode,
                                                            out rtnRsnCode);

                            //Log.Info($"CallGenerateARQC input data[{Encoding.ASCII.GetString(hsmInputData)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], returnString[{returnString}]");
                            var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                            Log.Info($"CallVerifyARQC Result : {resultMessage}");
                            if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                            {
                                ncccAuthInfoData.arqcCheck = 'Y';
                                ncccAuthInfoData.arqcChkResult = 'N';
                                return;
                            }
                            else
                            {
                                ncccAuthInfoData.arqcCheck = 'Y';
                                ncccAuthInfoData.arqcChkResult = 'Y';
                            }
                        }
                    }
                }
                //Initial Check CVV
                //Check CVV & P22 Service Entry Node
                if (isCVVCheck)
                {
                    field = FIELD.F_22;
                    string entryMode;
                    if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    {
                        entryMode = ((string)ncccMessage.Core().Field(field)).Substring(0, 2);
                        if ((entryMode.Equals("02")) ||
                            (entryMode.Equals("05")) ||
                            (entryMode.Equals("07")) ||
                            (entryMode.Equals("90")) ||
                            (entryMode.Equals("91")) ||
                            (entryMode.Equals("95")))
                        {
                            field = FIELD.F_35;
                            if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                            {
                                hsmInputData = HSMGenerateCVV(ncccMessage, bankId, 1);
                                if (hsmInputData == null)
                                {
                                    ncccAuthInfoData.cvvCheck = 'Y';
                                    ncccAuthInfoData.cvvChkResult = 'N';
                                    Log.Info("Call CVV HSM Input Data Is Null");
                                    return;
                                }
                                Log.Info("Call CVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                                stringData = Encoding.ASCII.GetString(hsmInputData);
                                returnString = CallGenerateCVV(stringData.Substring(0, 8),
                                                               stringData.Substring(8, 3),
                                                               stringData.Substring(11, 16),
                                                               stringData.Substring(27, 4),
                                                               stringData.Substring(31, 3),
                                                               out respCode,
                                                               out rtnRsnCode,
                                                               out var cvv);

                                //Log.Info($"CallGenerateCVV input data[{Encoding.ASCII.GetString(byteArray)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], cvv[{cvv}], returnString[{returnString}]");
                                var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                                Log.Info($"CallGenerateCVV Result : {resultMessage}");
                                if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                                {
                                    ncccAuthInfoData.cvvCheck = 'Y';
                                    ncccAuthInfoData.cvvChkResult = 'N';
                                    return;
                                }
                                if (cvv.Equals(ncccAuthInfoData.isoP35Cvv))
                                {
                                    ncccAuthInfoData.cvvCheck = 'Y';
                                    ncccAuthInfoData.cvvChkResult = 'Y';
                                }
                                else
                                {
                                    ncccAuthInfoData.cvvCheck = 'Y';
                                    ncccAuthInfoData.cvvChkResult = 'N';
                                    return;
                                }
                            }
                        }
                    }
                }
                //Initial Check CVV2
                //Check CVV2 Token C0
                if (isCVV2Check)
                {
                    //field = FIELD.F_63;
                    field = ncccMessage.GetTokenFieldId();
                    if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    {
                        var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                        if (tokenGroup != null)
                        {
                            var tokenC0 = (TokenC0)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenC0.Id);
                            if (tokenC0 != null)
                            {
                                if (ncccAuthInfoData.tokenC0Data.CvdFldPresent.Equals("1"))
                                {
                                    hsmInputData = HSMGenerateCVV(ncccMessage, bankId, 2);
                                    if (hsmInputData == null)
                                    {
                                        ncccAuthInfoData.cvv2Check = 'Y';
                                        ncccAuthInfoData.cvv2ChkResult = 'N';
                                        Log.Info("Call CVV2 HSM Input Data Is Null");
                                        return;
                                    }
                                    Log.Info("Call CVV2 HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                                    stringData = Encoding.ASCII.GetString(hsmInputData);
                                    returnString = CallGenerateCVV(stringData.Substring(0, 8),
                                                                   stringData.Substring(8, 3),
                                                                   stringData.Substring(11, 16),
                                                                   stringData.Substring(27, 4),
                                                                   stringData.Substring(31, 3),
                                                                   out respCode,
                                                                   out rtnRsnCode,
                                                                   out var cvv);
                                    var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                                    Log.Info($"CallGenerateCVV Result : {resultMessage}");
                                    if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                                    {
                                        ncccAuthInfoData.cvv2Check = 'Y';
                                        ncccAuthInfoData.cvv2ChkResult = 'N';
                                        Log.Info("Call CVV2 HSM Return Data Is Null");
                                        return;
                                    }
                                    if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                                    {
                                        ncccAuthInfoData.cvv2Check = 'Y';
                                        ncccAuthInfoData.cvv2ChkResult = 'N';
                                        return;
                                    }
                                    if (cvv.Equals(ncccAuthInfoData.tokenC0Data.CvdFld.Substring(0, 3)))
                                    {
                                        ncccAuthInfoData.cvv2Check = 'Y';
                                        ncccAuthInfoData.cvv2ChkResult = 'Y';
                                    }
                                    else
                                    {
                                        ncccAuthInfoData.cvv2Check = 'Y';
                                        ncccAuthInfoData.cvv2ChkResult = 'N';
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                //Initial Check CAVV
                //Check CAVV Token CE & C6
                if (isCAVVCheck)
                {
                    field = FIELD.F_22;
                    string entryMode;
                    if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    {
                        entryMode = ((string)ncccMessage.Core().Field(field)).Substring(0, 2);
                        if ((entryMode.Equals("01")) || (entryMode.Equals("81")))
                        {
                            //field = FIELD.F_63;
                            field = ncccMessage.GetTokenFieldId();
                            if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                            {
                                var tokenGroup = (TokenGroup)ncccMessage.Core().GetOthersValByKey(field.ToString());
                                if (tokenGroup != null)
                                {
                                    var tokenCE = (TokenCE)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenCE.Id);
                                    var tokenC6 = (TokenC6)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenC6.Id);
                                    if ((tokenCE != null) || (tokenC6 != null))
                                    {
                                        hsmInputData = HSMCreateCAVV(ncccMessage, bankId);
                                        if (hsmInputData == null)
                                        {
                                            ncccAuthInfoData.cavvCheck = 'Y';
                                            ncccAuthInfoData.cavvChkResult = 'N';
                                            Log.Info("Call CAVV HSM Input Data Is Null");
                                            return;
                                        }
                                        Log.Info("Call CAVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                                        stringData = Encoding.ASCII.GetString(hsmInputData);
                                        var cavvdata = stringData.Substring(stringData.Length - 3, 3);
                                        returnString = CallGenerateCAVV(stringData.Substring(0, 8),
                                                                       stringData.Substring(8, 3),
                                                                       stringData.Substring(11, 1),
                                                                       stringData.Substring(12, 16),
                                                                       stringData.Substring(28, 4),
                                                                       stringData.Substring(32, 3),
                                                                       out respCode,
                                                                       out rtnRsnCode,
                                                                       out var cavv);

                                        //Log.Info($"CallGenerateCAVV input data[{Encoding.ASCII.GetString(byteArray)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], cvv[{cvv}], returnString[{returnString}]");
                                        var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                                        Log.Info($"CallGenerateCAVV Result : {resultMessage}");
                                        if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                                        {
                                            ncccAuthInfoData.cavvCheck = 'Y';
                                            ncccAuthInfoData.cavvChkResult = 'N';
                                            return;
                                        }
                                        if (cavvdata.Equals(cavv))
                                        {
                                            ncccAuthInfoData.cvv2Check = 'Y';
                                            ncccAuthInfoData.cvv2ChkResult = 'Y';
                                        }
                                        else
                                        {
                                            ncccAuthInfoData.cvv2Check = 'Y';
                                            ncccAuthInfoData.cvv2ChkResult = 'N';
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                //Initial Check PVV
                //Check PVV
                if (isPVVCheck)
                {
                    field = FIELD.F_52;
                    if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    {
                        string[] returnStrings = StoreProcUtility.SelectCardholderPin(ncccAuthInfoData.isoP2CardNbr, "1");
                        if (returnStrings == null)
                        {
                            ncccAuthInfoData.pvvCheck = 'Y';
                            ncccAuthInfoData.pvvChkResult = null;
                            return;
                        }
                        hsmInputData = HSMGeneratePVV(ncccMessage, bankId, "NCCC", returnStrings[0]);
                        if (hsmInputData == null)
                        {
                            ncccAuthInfoData.pvvCheck = 'Y';
                            ncccAuthInfoData.pvvChkResult = null;
                            return;
                        }
                        Log.Info("Call PVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                        stringData = Encoding.ASCII.GetString(hsmInputData);
                        returnString = CallGeneratePVV(stringData.Substring(0, 8),
                                                       stringData.Substring(8, 3),
                                                       stringData.Substring(11, 1),
                                                       stringData.Substring(12, 16),
                                                       stringData.Substring(28, 8),
                                                       stringData.Substring(36, 1),
                                                       stringData.Substring(37, 16),
                                                       stringData.Substring(53, 4),
                                                       out respCode,
                                                       out rtnRsnCode,
                                                       out var pvv,
                                                       out var pvvOrigin,
                                                       out var checkPinOffset);
                        var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                        Log.Info($"CallGeneratePVV Result : {resultMessage}");
                        if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                        {
                            ncccAuthInfoData.pvvCheck = 'Y';
                            ncccAuthInfoData.pvvChkResult = null;
                            Log.Info("Call PVV HSM Return Data Is Null");
                            return;
                        }
                        pvvDobGenPvv = pvv;
                        ncccAuthInfoData.pvvCheck = 'Y';
                        ncccAuthInfoData.pvvChkResult = checkPinOffset;
                        if (!pvvOrigin.Equals(ncccAuthInfoData.isoP35Pvv)) ncccAuthInfoData.pvvChkResult = "9999";
                        // if (!checkPinOffset.Equals("0000")) ncccAuthInfoData.pvvChkResult = "N";
                    }
                    else
                    {
                        ncccAuthInfoData.pvvCheck = 'N';
                        ncccAuthInfoData.pvvChkResult = null;
                    }
                }

                //Initial Check PVVDob
                //Check PVV
                if (isPVVDobCheck)
                {
                    field = FIELD.F_52;
                    if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                    {
                        string pinBlockData = string.Empty;
                        field = FIELD.F_52;
                        if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                            pinBlockData = (string)ncccMessage.Core().Field(field);

                        if ((!string.IsNullOrEmpty(ncccAuthInfoData.isoP18MccCode)) && (ncccAuthInfoData.isoP18MccCode == "4011"))
                        {
                            //get clean pin from store procedure and call hsm get PVV
                            string[] returnStrings = StoreProcUtility.SelectCardholderPin(ncccAuthInfoData.isoP2CardNbr, "2");
                            if (returnStrings == null)
                            {
                                ncccAuthInfoData.pvvDobCheck = 'Y';
                                ncccAuthInfoData.pvvDobChkResult = null;
                                return;
                            }
                            //GET PVV
                            hsmInputData = HSMGenerateEncryptBankPin(ncccMessage, bankId, "NCCC", returnStrings[0]);
                            if (hsmInputData == null)
                            {
                                ncccAuthInfoData.pvvDobCheck = 'Y';
                                ncccAuthInfoData.pvvDobChkResult = null;
                                return;
                            }
                            Log.Info("Call HSMGenerateEncryptBankPin HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                            stringData = Encoding.ASCII.GetString(hsmInputData);
                            returnString = EncryptBankPin(stringData.Substring(0, 8),
                                                           stringData.Substring(8, 3),
                                                           stringData.Substring(11, 16),
                                                           stringData.Substring(27, 1),
                                                           stringData.Substring(28, 8),
                                                           stringData.Substring(36, 3),
                                                           stringData.Substring(39, 1),
                                                           stringData.Substring(40, 6),
                                                           out respCode,
                                                           out rtnRsnCode,
                                                           out var encPinBlock,
                                                           out var pvv,
                                                           out var pinOffset);
                            var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                            Log.Info($"EncryptBankPin Result : {resultMessage}");
                            if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                            {
                                ncccAuthInfoData.pvvDobCheck = 'Y';
                                ncccAuthInfoData.pvvDobChkResult = null;
                                Log.Info("Call EncryptBankPin HSM Return Data Is Null");
                                return;
                            }
                            //if (!pinBlockData.Equals(encPinBlock)) 
                            if (!pvv.Equals(pvvDobGenPvv))
                            {
                                ncccAuthInfoData.pvvDobCheck = 'Y';
                                ncccAuthInfoData.pvvDobChkResult = "N";
                                return;
                            }
                            ncccAuthInfoData.pvvDobCheck = 'Y';
                            ncccAuthInfoData.pvvDobChkResult = "Y";
                        }
                        else
                        {
                            ncccAuthInfoData.pvvDobCheck = 'N';
                            ncccAuthInfoData.pvvDobChkResult = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"HSMCheckFlow Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// FISC Check Flow For HSM
        /// </summary>
        /// <param name="fiscMessage">Request BIC Message</param>
        /// <param name="hsmCheckFlag">HSM SUIP Check Flag</param>
        /// <param name="bankId"></param>
        /// <param name="fiscAuthInfoData">Require Data For Authorization</param>
        public static void HSMCheckFlow(FISC fiscMessage, string hsmCheckFlag, string bankId, ref FiscAuthInfoStruct fiscAuthInfoData)
        {
            byte[] hsmInputData;
            int codllength = 0;
            string pvvDobGenPvv = string.Empty;
            string respCode;
            string rtnRsnCode;
            string stringData;
            string returnString;
            //Initial Check CVV
            bool isARQCCheck, isCVVCheck, isCVV2Check, isCAVVCheck, isPVVCheck, isPVVDobCheck;
            try
            {
                //Decision SUIP Check Flag For Input String 0:ARQC 1:CVV 2:CVV2 3:CAVV 4:PVV 5:PVVDob
                if ((hsmCheckFlag == null) || (hsmCheckFlag.Length != 6))
                {
                    isARQCCheck = false;
                    isCVVCheck = false;
                    isCVV2Check = false;
                    isCAVVCheck = false;
                    isPVVCheck = false;
                    isPVVDobCheck = false;
                }
                else
                {
                    isARQCCheck = hsmCheckFlag.Substring(0, 1).ToUpper().Equals("Y");

                    isCVVCheck = hsmCheckFlag.Substring(1, 1).ToUpper().Equals("Y");

                    isCVV2Check = hsmCheckFlag.Substring(2, 1).ToUpper().Equals("Y");

                    isCAVVCheck = hsmCheckFlag.Substring(3, 1).ToUpper().Equals("Y");

                    isPVVCheck = hsmCheckFlag.Substring(4, 1).ToUpper().Equals("Y");

                    isPVVDobCheck = hsmCheckFlag.Substring(5, 1).ToUpper().Equals("Y");
                }
                FIELD field;
                //Check ARQC
                if (isARQCCheck)
                {
                    field = FIELD.F_55;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        hsmInputData = HSMCreateARQCData(fiscMessage, bankId, out codllength);
                        if (hsmInputData == null)
                        {
                            fiscAuthInfoData.arqcCheck = 'Y';
                            fiscAuthInfoData.arqcChkResult = 'N';
                            Log.Info("Call ARQC HSM Input Data Is Null");
                            return;
                        }
                        Log.Info("Call ARQC HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                        stringData = Encoding.ASCII.GetString(hsmInputData);
                        returnString = CallVerifyARQC(stringData.Substring(0, 8),
                                                      stringData.Substring(8, 3),
                                                      stringData.Substring(11, 2),
                                                      codllength.ToString().PadLeft(2, '0'),
                                                      stringData.Substring(49, codllength),
                                                      stringData.Substring(13, 16),
                                                      stringData.Substring(29, 18),
                                                      stringData.Substring(47, 2),
                                                      out respCode,
                                                      out rtnRsnCode);

                        //Log.Info($"CallGenerateARQC input data[{Encoding.ASCII.GetString(hsmInputData)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], returnString[{returnString}]");
                        var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                        Log.Info($"CallVerifyARQC Result : {resultMessage}");
                        if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                        {
                            fiscAuthInfoData.arqcCheck = 'Y';
                            fiscAuthInfoData.arqcChkResult = 'N';
                            return;
                        }
                        else
                        {
                            fiscAuthInfoData.arqcCheck = 'Y';
                            fiscAuthInfoData.arqcChkResult = 'Y';
                        }
                    }
                    else
                    {
                        fiscAuthInfoData.arqcCheck = 'N';
                        fiscAuthInfoData.arqcChkResult = 'N';
                    }
                }
                //Initial Check CVV
                //Check CVV & P22 Service Entry Node
                if (isCVVCheck)
                {
                    field = FIELD.F_22;
                    string entryMode;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        entryMode = ((string)fiscMessage.Core().Field(field)).Substring(0, 2);
                        if ((entryMode.Equals("02")) ||
                            (entryMode.Equals("05")) ||
                            (entryMode.Equals("07")) ||
                            (entryMode.Equals("90")) ||
                            (entryMode.Equals("91")) ||
                            (entryMode.Equals("95")))
                        {
                            field = FIELD.F_35;
                            if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                            {
                                hsmInputData = HSMGenerateCVV(fiscMessage, bankId);
                                if (hsmInputData == null)
                                {
                                    fiscAuthInfoData.cvvCheck = 'Y';
                                    fiscAuthInfoData.cvvChkResult = 'N';
                                    Log.Info("Call CVV HSM Input Data Is Null");
                                    return;
                                }
                                Log.Info("Call CVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                                stringData = Encoding.ASCII.GetString(hsmInputData);
                                returnString = CallGenerateCVV(stringData.Substring(0, 8),
                                                               stringData.Substring(8, 3),
                                                               stringData.Substring(11, 16),
                                                               stringData.Substring(27, 4),
                                                               stringData.Substring(31, 3),
                                                               out respCode,
                                                               out rtnRsnCode,
                                                               out var cvv);

                                //Log.Info($"CallGenerateCVV input data[{Encoding.ASCII.GetString(byteArray)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], cvv[{cvv}], returnString[{returnString}]");
                                var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                                Log.Info($"CallGenerateCVV Result : {resultMessage}");
                                if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                                {
                                    fiscAuthInfoData.cvvCheck = 'Y';
                                    fiscAuthInfoData.cvvChkResult = 'N';
                                    return;
                                }
                                if (cvv.Equals(fiscAuthInfoData.isoP35OrP45Cvv))
                                {
                                    fiscAuthInfoData.cvvCheck = 'Y';
                                    fiscAuthInfoData.cvvChkResult = 'Y';
                                }
                                else
                                {
                                    fiscAuthInfoData.cvvCheck = 'Y';
                                    fiscAuthInfoData.cvvChkResult = 'N';
                                    return;
                                }
                            }
                        }
                    }
                }
                //Initial Check CVV2
                if (isCVV2Check)
                {
                    //CVV2 #48TAG92
                    field = FIELD.F_48;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        var de48TagData = fiscMessage.RawVal(field).ToString();
                        var de48TagField = new DE48TagField(de48TagData);

                        string de48TagIdString = "92";
                        string isoP48T92 = string.Empty;
                        if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                            isoP48T92 = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);

                        if (isoP48T92.Length == 3)
                        {
                            hsmInputData = HSMGenerateCVV2(fiscMessage, bankId);
                            if (hsmInputData == null)
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'N';
                                Log.Info("Call CVV2 HSM Input Data Is Null");
                                return;
                            }
                            Log.Info("Call CVV2 HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                            stringData = Encoding.ASCII.GetString(hsmInputData);
                            returnString = CallGenerateCVV(stringData.Substring(0, 8),
                                                           stringData.Substring(8, 3),
                                                           stringData.Substring(11, 16),
                                                           stringData.Substring(27, 4),
                                                           stringData.Substring(31, 3),
                                                           out respCode,
                                                           out rtnRsnCode,
                                                           out var cvv);
                            var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                            Log.Info($"CallGenerateCVV Result : {resultMessage}");
                            if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'N';
                                Log.Info("Call CVV2 HSM Return Data Is Null");
                                return;
                            }
                            if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'N';
                                return;
                            }
                            if (cvv.Equals(isoP48T92))
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'Y';
                            }
                            else
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'N';
                                return;
                            }
                        }
                        else
                        {
                            fiscAuthInfoData.cvv2Check = 'N';
                            fiscAuthInfoData.cvv2ChkResult = 'N';
                        }
                    }
                    else
                    {
                        fiscAuthInfoData.cvv2Check = 'N';
                        fiscAuthInfoData.cvv2ChkResult = 'N';
                    }
                }
                //Initial Check CAVV
                if (isCAVVCheck)
                {
                    field = FIELD.F_48;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        var de48TagData = fiscMessage.RawVal(field).ToString();
                        var de48TagField = new DE48TagField(de48TagData);

                        string de48TagIdString = "43";
                        if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                        {
                            hsmInputData = HSMCreateCAVV(fiscMessage, bankId);
                            if (hsmInputData == null)
                            {
                                fiscAuthInfoData.cavvCheck = 'Y';
                                fiscAuthInfoData.cavvChkResult = 'N';
                                Log.Info("Call CAVV HSM Input Data Is Null");
                                return;
                            }
                            Log.Info("Call CAVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                            stringData = Encoding.ASCII.GetString(hsmInputData);
                            var cavvdata = stringData.Substring(stringData.Length - 3, 3);
                            returnString = CallGenerateCAVV(stringData.Substring(0, 8),
                                                           stringData.Substring(8, 3),
                                                           stringData.Substring(11, 1),
                                                           stringData.Substring(12, 16),
                                                           stringData.Substring(28, 4),
                                                           stringData.Substring(32, 3),
                                                           out respCode,
                                                           out rtnRsnCode,
                                                           out var cavv);

                            //Log.Info($"CallGenerateCAVV input data[{Encoding.ASCII.GetString(byteArray)}], respCode[{respCode}], rtnRsnCode[{rtnRsnCode}], cvv[{cvv}], returnString[{returnString}]");
                            var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                            Log.Info($"CallGenerateCAVV Result : {resultMessage}");
                            if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                            {
                                fiscAuthInfoData.cavvCheck = 'Y';
                                fiscAuthInfoData.cavvChkResult = 'N';
                                return;
                            }
                            if (cavvdata.Equals(cavv))
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'Y';
                            }
                            else
                            {
                                fiscAuthInfoData.cvv2Check = 'Y';
                                fiscAuthInfoData.cvv2ChkResult = 'N';
                                return;
                            }
                        }
                        else
                        {
                            fiscAuthInfoData.cavvCheck = 'N';
                            fiscAuthInfoData.cavvChkResult = 'N';
                        }
                    }
                    else
                    {
                        fiscAuthInfoData.cavvCheck = 'N';
                        fiscAuthInfoData.cavvChkResult = 'N';
                    }
                }
                //Initial Check PVV
                //Check PVV
                if (isPVVCheck)
                {
                    field = FIELD.F_52;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        string[] returnStrings = StoreProcUtility.SelectCardholderPin(fiscAuthInfoData.isoP2CardNbr, "1");
                        if (returnStrings == null)
                        {
                            fiscAuthInfoData.pvvCheck = 'Y';
                            fiscAuthInfoData.pvvChkResult = null;
                            return;
                        }
                        hsmInputData = HSMGeneratePVV(fiscMessage, bankId, "FISC", returnStrings[0]);
                        if (hsmInputData == null)
                        {
                            fiscAuthInfoData.pvvCheck = 'Y';
                            fiscAuthInfoData.pvvChkResult = null;
                            return;
                        }
                        Log.Info("Call PVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                        stringData = Encoding.ASCII.GetString(hsmInputData);
                        returnString = CallGeneratePVV(stringData.Substring(0, 8),
                                                       stringData.Substring(8, 3),
                                                       stringData.Substring(11, 1),
                                                       stringData.Substring(12, 16),
                                                       stringData.Substring(28, 8),
                                                       stringData.Substring(36, 1),
                                                       stringData.Substring(37, 16),
                                                       stringData.Substring(53, 4),
                                                       out respCode,
                                                       out rtnRsnCode,
                                                       out var pvv,
                                                       out var pvvOrigin,
                                                       out var checkPinOffset);
                        var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                        Log.Info($"CallGeneratePVV Result : {resultMessage}");
                        if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                        {
                            fiscAuthInfoData.pvvCheck = 'Y';
                            fiscAuthInfoData.pvvChkResult = null;
                            Log.Info("Call PVV HSM Return Data Is Null");
                            return;
                        }
                        fiscAuthInfoData.pvvCheck = 'Y';
                        fiscAuthInfoData.pvvChkResult = checkPinOffset;
                        pvvDobGenPvv = pvv;
                        if (!pvvOrigin.Equals(fiscAuthInfoData.isoP35OrP45Pvv)) fiscAuthInfoData.pvvChkResult = "9999";
                        // if (!checkPinOffset.Equals("0000")) fiscAuthInfoData.pvvChkResult = "N";
                    }
                    else
                    {
                        fiscAuthInfoData.pvvCheck = 'N';
                        fiscAuthInfoData.pvvChkResult = null;
                    }
                }

                //Initial Check PVVDob
                //Check PVV
                if (isPVVDobCheck)
                {
                    field = FIELD.F_52;
                    if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                    {
                        string pinBlockData = string.Empty;
                        field = FIELD.F_52;
                        if (!ObjectUtils.IsNull(fiscMessage.RawVal(field)))
                            pinBlockData = (string)fiscMessage.Core().Field(field);

                        if ((!string.IsNullOrEmpty(fiscAuthInfoData.isoP18MccCode)) && (fiscAuthInfoData.isoP18MccCode == "4011"))
                        {
                            //get clean pin from store procedure and call hsm get PVV
                            string[] returnStrings = StoreProcUtility.SelectCardholderPin(fiscAuthInfoData.isoP2CardNbr, "2");
                            if (returnStrings == null)
                            {
                                fiscAuthInfoData.pvvDobCheck = 'Y';
                                fiscAuthInfoData.pvvDobChkResult = null;
                                return;
                            }
                            //GET PVV
                            hsmInputData = HSMGenerateEncryptBankPin(fiscMessage, bankId, "FISC", returnStrings[0]);
                            if (hsmInputData == null)
                            {
                                fiscAuthInfoData.pvvDobCheck = 'Y';
                                fiscAuthInfoData.pvvDobChkResult = null;
                                return;
                            }
                            Log.Info("Call PVV HSM Input Data[{0}]", Encoding.ASCII.GetString(hsmInputData));
                            stringData = Encoding.ASCII.GetString(hsmInputData);
                            returnString = EncryptBankPin(stringData.Substring(0, 8),
                                                           stringData.Substring(8, 3),
                                                           stringData.Substring(11, 16),
                                                           stringData.Substring(27, 1),
                                                           stringData.Substring(28, 8),
                                                           stringData.Substring(36, 3),
                                                           stringData.Substring(39, 1),
                                                           stringData.Substring(40, 6),
                                                           out respCode,
                                                           out rtnRsnCode,
                                                           out var encPinBlock,
                                                           out var pvv,
                                                           out var pinOffset);
                            var resultMessage = ColaCommUtility.GetHSMReasonCodeMessage(respCode, rtnRsnCode, returnString);
                            Log.Info($"EncryptBankPin Result : {resultMessage}");
                            if (string.IsNullOrEmpty(respCode) || !respCode.Equals("00"))
                            {
                                fiscAuthInfoData.pvvDobCheck = 'Y';
                                fiscAuthInfoData.pvvDobChkResult = null;
                                Log.Info("Call EncryptBankPin HSM Return Data Is Null");
                                return;
                            }
                            //if (!pinBlockData.Equals(encPinBlock)) 
                            if (!pvv.Equals(pvvDobGenPvv))
                            {
                                fiscAuthInfoData.pvvDobCheck = 'Y';
                                fiscAuthInfoData.pvvDobChkResult = "N";
                                return;
                            }
                            fiscAuthInfoData.pvvDobCheck = 'Y';
                            fiscAuthInfoData.pvvDobChkResult = "Y";
                        }
                        else
                        {
                            fiscAuthInfoData.pvvDobCheck = 'N';
                            fiscAuthInfoData.pvvDobChkResult = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"HSMCheckFlow Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// SVCS Check Flow For HSM
        /// </summary>
        /// <param name="svcsMessage">Request BIC Message</param>
        /// <param name="hsmCheckFlag">HSM SUIP Check Flag</param>
        /// <param name="bankId"></param>
        /// <param name="svcsAuthInfoData">Require Data For Authorization</param>
        public static void HSMCheckFlow(SVCS svcsMessage, string hsmCheckFlag, string bankId, ref EsvcAuthInfoStruct svcsAuthInfoData)
        {
        }

        /// <summary>
        /// TWINC Check Flow For HSM
        /// </summary>
        /// <param name="twinMessage"></param>
        /// <param name="hsmCheckFlag">HSM SUIP Check Flag</param>
        /// <param name="bankId"></param>
        /// <param name="ncccAuthInfoData">Require Data For Authorization</param>
        public static void HSMCheckFlow(TWINIso8583 twinMessage, string hsmCheckFlag, string bankId, ref NcccAuthInfoStruct ncccAuthInfoData)
        {
        }
    }
}

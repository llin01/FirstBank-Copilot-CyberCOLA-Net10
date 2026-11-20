using Com.Cybersoft4u.CommonLib.FirstHSMUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Creates the B5 arpc.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="data9F10Array">The data9 F10 array.</param>
        /// <param name="archexString"></param>
        /// <param name="arpcString">The arpc string.</param>
        public static void CreateB5ARPC(NcccIso8583 requestNcccMessage,
                                        NcccAuthInfoStruct authInfoData,
                                        string[] authReturnString,
                                        string[] data9F10Array,
                                        out string archexString,
                                        out string arpcString)
        {
            archexString = string.Empty;
            arpcString = string.Empty;
            try
            {
                if (authReturnString == null)
                    return;
                if (!Is9F10DataValid(data9F10Array))
                    return;
                if (authInfoData.tokenB2Data?.Arqc == null)
                    return;
                if (authInfoData.isoP35Svc == null)
                    return;
                if (!new string[] { "2", "6" }.Contains(authInfoData.isoP35Svc.Substring(0, 1)))
                    return;
                var creditCardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                var cvr = ColaCommUtility.HexString2BinString(data9F10Array[2]);
                if (cvr.Substring(14, 1).Equals("1") && creditCardType.Equals(CreditCardType.MasterCard))
                {
                    if (authReturnString[2] != null && authReturnString[2].Equals("A"))
                    {
                        archexString = "0012";
                    }
                    else if (authReturnString[2] != null && authReturnString[2].Equals("R"))
                    {
                        archexString = "0010";
                    }
                    else
                    {
                        archexString = "0000";
                    }
                    //arqcStringBuilder.Append(new String('0', 12));
                }
                else
                {
                    archexString = ColaCommUtility.HexBinary2HexString(Encoding.ASCII.GetBytes(authReturnString[1]));
                }
                //string arqcString = HexStringConverter.FromByteArray(Encoding.ASCII.GetBytes(authReturnString[1])) + new string('0', 12);
                //call GenerateARPC
                //byte[] byteArray = CommonLib.HSMUtils.HSMClient.HSMGenerateARPC(requestNcccMessage, SystemParameterInfo.BankInfo.BankCode, arqcString);
                //string stringData = Encoding.ASCII.GetString(byteArray);
                //var returnString = CommonLib.HSMUtils.HSMClient.CallGenerateARPC(stringData.Substring(0, 8),
                //                                                                 stringData.Substring(8, 3),
                //                                                                 stringData.Substring(11, 2),
                //                                                                 stringData.Substring(13, 16),
                //                                                                 stringData.Substring(29, 16),
                //                                                                 stringData.Substring(45, 4),
                //                                                                 stringData.Substring(49, 18),
                //                                                                 stringData.Substring(67, 2),
                //                                                                 out var rtnCode,
                //                                                                 out var rsnCode,
                //                                                                 out var arpcData);
                //
                byte[] byteArray = FirstHSMUtility.HSMCreateARPCData(requestNcccMessage, SystemParameterInfo.BankInfo.BankCode, archexString);
                if (byteArray == null)
                {
                    Log.Error("HSMCreateARPCData Error");
                    return;
                }
                string stringData = Encoding.ASCII.GetString(byteArray);
                arpcString = stringData.Substring(10, stringData.Length - 10);
                Log.Info($"CallGenerateARPC rtnCode[{stringData.Substring(0, 2)}], rsnCode[{stringData.Substring(2, 8)}],arcData[{archexString}], arpcData[{arpcString}], returnString[{stringData}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateB5ARPC Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Creates the B5 ARPC(FISC).
        /// </summary>
        /// <param name="requestFiscMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="data9F10Array">The data9 F10 array.</param>
        /// <param name="archexString"></param>
        /// <param name="arpcString">The arpc string.</param>
        public static void CreateB5ARPC(FISC requestFiscMessage,
                                        FiscAuthInfoStruct authInfoData,
                                        string[] authReturnString,
                                        string[] data9F10Array,
                                        out string archexString,
                                        out string arpcString)
        {
            archexString = string.Empty;
            arpcString = string.Empty;
            try
            {
                if (authReturnString == null)
                    return;
                if (!Is9F10DataValid(data9F10Array))
                    return;
                if (authInfoData.isoP55ARQC == null)
                    return;
                if (authInfoData.isoP35OrP45Svc == null)
                    return;
                if (!new string[] { "2", "6" }.Contains(authInfoData.isoP35OrP45Svc.Substring(0, 1)))
                    return;
                var creditCardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                var cvr = ColaCommUtility.HexString2BinString(data9F10Array[2]);
                if (cvr.Substring(14, 1).Equals("1") && creditCardType.Equals(CreditCardType.MasterCard))
                {
                    if (authReturnString[2] != null && authReturnString[2].Equals("A"))
                    {
                        archexString = "0012";
                    }
                    else if (authReturnString[2] != null && authReturnString[2].Equals("R"))
                    {
                        archexString = "0010";
                    }
                    else
                    {
                        archexString = "0000";
                    }
                    //arqcStringBuilder.Append(new String('0', 12));
                }
                else
                {
                    archexString = ColaCommUtility.HexBinary2HexString(Encoding.ASCII.GetBytes(authReturnString[1]));
                }

                byte[] byteArray = FirstHSMUtility.HSMCreateARPCData(requestFiscMessage, SystemParameterInfo.BankInfo.BankCode, archexString);
                if (byteArray == null)
                {
                    Log.Error("HSMCreateARPCData Error");
                    return;
                }
                string stringData = Encoding.ASCII.GetString(byteArray);
                arpcString = stringData.Substring(10, stringData.Length - 10);
                Log.Info($"CallGenerateARPC rtnCode[{stringData.Substring(0, 2)}], rsnCode[{stringData.Substring(2, 8)}],arcData[{archexString}], arpcData[{arpcString}], returnString[{stringData}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateB5ARPC Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Get Token B2 Issuer Application Data
        /// </summary>
        /// <param name="issApplDataLgth">Issuer Application Data Length</param>
        /// <param name="issApplData">Issuer Application Data</param>
        /// <param name="creditCardType">CardType</param>
        /// <returns>
        /// BIC Field String
        /// </returns>
        public static string BicGet9F10Data(int issApplDataLgth, string issApplData, CreditCardType creditCardType)
        {
            StringBuilder retnData = new StringBuilder("");
            int dataLength = 0;
            if (creditCardType.Equals(CreditCardType.JCB) || creditCardType.Equals(CreditCardType.Visa))
            {
                //issApplData change decimal to hexadecimal
                //dataLength = Convert.ToInt32(issApplData.Substring(0, 2));
                dataLength = Convert.ToInt32(issApplData.Substring(0, 2), 16);
                if (issApplDataLgth != (dataLength + 1))
                    return (retnData.ToString());
                retnData.Append(issApplData.Substring(2, 2));
                retnData.Append(";");
                retnData.Append(issApplData.Substring(4, 2));
                retnData.Append(";");
                retnData.Append(creditCardType.Equals(CreditCardType.JCB) ? issApplData.Substring(6, 10) : issApplData.Substring(6, 8));
                return (retnData.ToString());
            }
            if (creditCardType.Equals(CreditCardType.MasterCard))
            {
                //issApplData change decimal to hexadecimal
                //dataLength = Convert.ToInt32(issApplData.Substring(0, 2));
                dataLength = Convert.ToInt32(issApplData.Substring(0, 2), 16);

                if (issApplDataLgth == (dataLength + 1))
                {
                    //SELECT 2
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(6, 8));
                    return (retnData.ToString());
                }
                if (issApplDataLgth > 8)
                {
                    //SELECT MChip 4
                    retnData.Append(issApplData.Substring(0, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(4, 12));
                    return (retnData.ToString());
                }
                //SELECT MChip Lite 2.1
                retnData.Append(issApplData.Substring(0, 2));
                retnData.Append(";");
                retnData.Append(issApplData.Substring(2, 2));
                retnData.Append(";");
                retnData.Append(issApplData.Substring(4, 8));
                return (retnData.ToString());
            }
            return (retnData.ToString());
        }


        /// <summary>
        /// Get Token #55 tag9F10 Issuer Application Data
        /// </summary>
        /// <param name="issApplDataLgth">Issuer Application Data Length</param>
        /// <param name="issApplData">Issuer Application Data</param>
        /// <param name="creditCardType">CardType</param>
        /// <returns>
        /// BIC Field String
        /// </returns>
        public static string FiscGet9F10Data(int issApplDataLgth, string issApplData, CreditCardType creditCardType)
        {
            StringBuilder retnData = new StringBuilder("");
            int dataLength = 0;
            if (creditCardType.Equals(CreditCardType.JCB) || creditCardType.Equals(CreditCardType.Visa))
            {
                //issApplData change decimal to hexadecimal
                //dataLength = Convert.ToInt32(issApplData.Substring(0, 2));
                dataLength = Convert.ToInt32(issApplData.Substring(0, 2), 16) * 2;
                if (issApplDataLgth != (dataLength + 2))
                    return (retnData.ToString());
                if (creditCardType.Equals(CreditCardType.Visa) && (dataLength == 62))
                {
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(6, 12));
                }
                else
                {
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(';');
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(';');
                    retnData.Append(creditCardType.Equals(CreditCardType.JCB) ? issApplData.Substring(6, 10) : issApplData.Substring(6, 8));
                }
                return (retnData.ToString());
            }
            if (creditCardType.Equals(CreditCardType.MasterCard))
            {
                dataLength = Convert.ToInt32(issApplData.Substring(0, 2), 16) * 2;

                if (issApplDataLgth == (dataLength + 2))
                {
                    //SELECT 2
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(';');
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(';');
                    retnData.Append(issApplData.Substring(6, 8));
                    return (retnData.ToString());
                }
                if (issApplDataLgth > 8)
                {
                    //SELECT MChip 4
                    retnData.Append(issApplData.Substring(0, 2));
                    retnData.Append(';');
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(';');
                    retnData.Append(issApplData.Substring(4, 12));
                    return (retnData.ToString());
                }
                //SELECT MChip Lite 2.1
                retnData.Append(issApplData.Substring(0, 2));
                retnData.Append(';');
                retnData.Append(issApplData.Substring(2, 2));
                retnData.Append(';');
                retnData.Append(issApplData.Substring(4, 8));
                return (retnData.ToString());
            }
            return (retnData.ToString());
        }


        public static bool Is9F10DataValid(string[] data9F10Array)
        {
            try
            {
                if (data9F10Array == null) return false;
                if (data9F10Array.Length != 3) return false;
                foreach (var data in data9F10Array)
                {
                    if (string.IsNullOrEmpty(data))
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Is9F10DataValid Exception:{ex.Message}");
                return false;
            }
        }
        /// <summary></summary>
        /// <param name="bicAuthInfoData"></param>
        public static string[] Get9F10Array(NcccAuthInfoStruct bicAuthInfoData)
        {
            try
            {
                if (bicAuthInfoData.tokenB2Data?.IssApplDataLgth == null)
                {
                    return null;
                }
                int issApplDataLgth = Convert.ToInt32(bicAuthInfoData.tokenB2Data.IssApplDataLgth);
                var cardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                   bicAuthInfoData.tokenB2Data.IssApplData,
                                                                   cardType).Split(';');
                return data9F10;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Get9F10Array Exception:{ex.Message}");
                return null;
            }

        }

        /// <summary>
        /// CreateNccc0200TokenData Create BIC Field 57 Or 63 Authorization Data For Response Data
        /// Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
        /// authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
        /// authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
        /// authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
        /// authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
        /// authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
        /// authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
        /// authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
        /// authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
        /// authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <param name="data9F10Array"></param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNccc0200TokenData(NcccIso8583 requestNcccMessage,
                                                      NcccAuthInfoStruct authInfoData,
                                                      string[] authReturnString,
                                                      string[] data9F10Array)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            const int authReturnStringLength = 23;
            try
            {
                string arcString = string.Empty;
                string arpcString = string.Empty;
                //CreateB5ARPC(requestNcccMessage, authInfoData, authReturnString, data9F10Array, out arcString, out arpcString);

                //Add Input Have Token Data
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        if (tokenId.Equals("B5"))
                        {
                            Log.Debug($"Modify Token B5 Process arpc data [{arpcString}], arc [{arcString}],authReturnString length[{authReturnString.Length.ToString()}],authReturnString[01][{authReturnString[01].ToString()}]");
                            if (authReturnString.Length == authReturnStringLength && (authReturnString[01] != null) && (!string.IsNullOrEmpty(arpcString)))
                            {
                                if ((authReturnString[01].Length == 2) && (arpcString.Length == 16))
                                {
                                    string tempString = string.Format(SystemConstantData.CommonData.NcccTokenB5StringFormat,
                                                                      Convert.ToInt32(tokenData.Substring(0, 4)),
                                                                      arpcString,
                                                                      arcString.Substring(0, 4),
                                                                      tokenData.Substring(24, 12),
                                                                      tokenData.Substring(36, 6));
                                    Log.Info($"Modify Token B5 DataString [{tempString}]");
                                    tokenSortedList.Add(tokenId, tempString);
                                    createTokenFlag = true;
                                }
                            }
                        }
                        else
                        {
                            tokenSortedList.Add(tokenId, tokenData);
                            createTokenFlag = true;
                        }
                    }
                }
                //Recheck B5
                tokenId = "B5";
                if (!tokenSortedList.ContainsKey(tokenId))
                {
                    Log.Debug($"Generate Token B5 Process arpc data [{arpcString}], arc [{arcString}],authReturnString length[{authReturnString.Length.ToString()}],authReturnString[01][{authReturnString[01].ToString()}]");
                    if (authReturnString.Length == authReturnStringLength && (authReturnString[01] != null) && (!string.IsNullOrEmpty(arpcString)))
                    {
                        Log.Debug($"Generate Token B5 For Data authReturnString[01].Length [{authReturnString[01].Length.ToString()}], arpc length [{arpcString.Length.ToString()}]");
                        if ((authReturnString[01].Length == 2) && (arpcString.Length == 16))
                        {
                            string tempString = string.Format(SystemConstantData.CommonData.NcccTokenB5StringFormat,
                                                              10,
                                                              arpcString,
                                                              arcString.Substring(0, 4),
                                                              new string('0', 12),
                                                              "N    3");
                            Log.Info($"Generate Token B5 DataString [{tempString}]");
                            tokenSortedList.Add(tokenId, tempString);
                            createTokenFlag = true;
                        }
                    }
                }
                //Check Token 04 Data
                tokenId = "04";
                if (!string.IsNullOrEmpty(authReturnString[04]) &&
                    !string.IsNullOrEmpty(authReturnString[05]) &&
                    tokenSortedList.ContainsKey(tokenId))
                {
                    if ((authReturnString[04].Length == 1) && (authReturnString[05].Length == 1))
                    {
                        field = FIELD.F_35;
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            //Remove tokenId and recreate
                            tokenSortedList.Remove(tokenId);
                            tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccToken04StringFormat,
                                                                       authReturnString[04],
                                                                       " ",
                                                                       authReturnString[05],
                                                                       " ",
                                                                       "Y",
                                                                       " "));
                            createTokenFlag = true;
                        }
                        else
                        {
                            //Remove tokenId and recreate
                            tokenSortedList.Remove(tokenId);
                            tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccToken04StringFormat,
                                                                       authReturnString[04],
                                                                       " ",
                                                                       authReturnString[05],
                                                                       " ",
                                                                       "N",
                                                                       " "));
                            createTokenFlag = true;
                        }
                    }
                }
                //Check Token C0 Data
                tokenId = "C0";
                if (tokenSortedList.ContainsKey(tokenId))
                {
                    //Remove tokenId and recreate
                    tokenSortedList.Remove(tokenId);
                    string tempString = string.Format(SystemConstantData.CommonData.NcccTokenC0StringFormat,
                                                      authInfoData.tokenC0Data.CvdFld,
                                                      " ",
                                                      authInfoData.tokenC0Data.MotoOrECFlag,
                                                      " ",
                                                      authInfoData.tokenC0Data.CvdFldPresent,
                                                      authInfoData.tokenC0Data.Visa3DSInd,
                                                      authInfoData.tokenC0Data.UcafAuthnCollInd,
                                                      " ",
                                                      authReturnString[06]);
                    tokenSortedList.Add(tokenId, tempString);
                    createTokenFlag = true;

                }
                //Check Token C5 Data
                field = requestNcccMessage.GetTokenFieldId();
                dynamic valueTokenC5 = ((TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString())).GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                tokenId = "C5";
                if (valueTokenC5 != null)
                {
                    //Check token Id C5 Exist For Remove
                    if (tokenSortedList.ContainsKey(tokenId))
                        tokenSortedList.Remove(tokenId);
                    if (valueTokenC5 is TokenNCCCInstallmentC5 c6)
                    {
                        tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccTokenC5InstallmentStringFormat,
                                                                   c6.MerchantId,
                                                                   c6.StoreId,
                                                                   c6.TransactionId,
                                                                   c6.ReferenceId,
                                                                   c6.GoodsId,
                                                                   c6.Personal,
                                                                   c6.Pin,
                                                                   c6.EcFlag,
                                                                   c6.OrderNum,               //Order Number
                                                                   authReturnString[07],      //installment-tx-indicator
                                                                   authReturnString[08],      //installment-tx-resp-code
                                                                   " ",
                                                                   c6.PeriodNumber,
                                                                   authReturnString[09],      //down-payments
                                                                   authReturnString[10],      //installment-payments
                                                                   authReturnString[11]));    //formality-fee
                        createTokenFlag = true;
                    }
                    else if (valueTokenC5 is TokenNCCCRoyaltyC5 c5)
                    {
                        tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccTokenC5RebateStringFormat,
                                                                   c5.MerchantId,
                                                                   c5.StoreId,
                                                                   c5.TransactionId,
                                                                   c5.ReferenceId,
                                                                   c5.GoodsId,
                                                                   c5.Personal,
                                                                   c5.Pin,
                                                                   c5.EcFlag,
                                                                   c5.SettlementFlag,
                                                                   c5.OrderNum,
                                                                   authReturnString[16],
                                                                   authReturnString[08],
                                                                   authReturnString[12],
                                                                   authReturnString[13],
                                                                   authReturnString[14],
                                                                   authReturnString[15]));
                        createTokenFlag = true;
                    }
                }

                if (createTokenFlag)
                {
                    //For Skip B5 Token Data
                    //tokenId = "B5";
                    //if (tokenSortedList.ContainsKey(tokenId))
                    //    tokenSortedList.Remove(tokenId);
                    //For Skip B5 Token Data
                    //string tokenData = new BicTokenField(tokenSortedList).GetTokenData();
                    //return (string.Format("{0:000}{1}", tokenData.Length, tokenData));
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateNccc0200TokenData Exception:{ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// CreateNccc0200TknAuthTokenData Create BIC Field 57 Or 63 Tokenization Data For Response Data
        /// Return String Arrays Information
        // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@t04ErrorFlg
        // authReturnString[04]:@t04CrdVeryFlg       , authReturnString[05]:@tC5InstTxResp
        // authReturnString[06]:@tC5DownPayment      , authReturnString[07]:@tC5InstPayment
        // authReturnString[08]:@tC5InstFee          , authReturnString[09]:@tC5PointRedemption
        // authReturnString[10]:@tC5SignPoint        , authReturnString[11]:@tC5PointBalance
        // authReturnString[12]:@tC5PaidCreditAmt    , authReturnString[13]:@tC5LoyaltyTxInd
        // authReturnString[14]:@vdLinkNbr           , authReturnString[15]:@vdLinkNbrSeq
        // authReturnString[16]:@vdActionCode        , authReturnString[17]:@vdBlockAmt
        /// Description
        // @isoP38AuthCode          	varchar(06) 	Output	 --p38
        // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        // @isoP39RspAction             varchar(01)     Output	 --對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可)
        // @t04ErrorFlg	                varchar(01) 	Output	 Field 63 Token04:Error-flag Token04.ErrorFlg
        // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 63 Token04:Crd-very-flag Token04.CrdVeryFlg
        // @tC5InstTxResp       	    varchar(02) 	Output	 Field 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
        // @tC5DownPayment      	    varchar(08) 	Output	 Field 63 Token C5 Down Payment<分期,首期>
        // @tC5InstPayment      	    varchar(08) 	Output	 Field 63 Token C5 Installment Payment<分期, 每期>
        // @tC5InstFee          	    varchar(06) 	Output	 Field 63 Token C5 Formality-Fee<分期, 手續費/利息>
        // @tC5PointRedemption  	    varchar(08) 	Output	 Field 63 Token C5 Point of Redemption<紅利,折扺點數>
        // @tC5SignPoint        	    varchar(01) 	Output	 Field 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
        // @tC5PointBalance     	    varchar(08) 	Output	 Field 63 Token C5 Point of Balance<紅利, 餘點>
        // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
        // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 63 Token C5 <紅利註記>
        // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
        // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
        // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <param name="data9F10Array"></param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNccc0200TknAuthTokenData(NcccIso8583 requestNcccMessage,
                                                             NcccAuthInfoStruct authInfoData,
                                                             string[] authReturnString)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            try
            {
                //Add Input Have Token Data
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        tokenSortedList.Add(tokenId, tokenData);
                        createTokenFlag = true;
                    }
                }
                //Check Token 04 Data
                tokenId = "04";
                if (!string.IsNullOrEmpty(authReturnString[03]) &&
                    !string.IsNullOrEmpty(authReturnString[04]) &&
                    tokenSortedList.ContainsKey(tokenId))
                {
                    if ((authReturnString[03].Length == 1) && (authReturnString[04].Length == 1))
                    {
                        field = FIELD.F_35;
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            //Remove tokenId and recreate
                            tokenSortedList.Remove(tokenId);
                            tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccToken04StringFormat,
                                                                       authReturnString[03],
                                                                       " ",
                                                                       authReturnString[04],
                                                                       " ",
                                                                       "Y",
                                                                       " "));
                            createTokenFlag = true;
                        }
                        else
                        {
                            //Remove tokenId and recreate
                            tokenSortedList.Remove(tokenId);
                            tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccToken04StringFormat,
                                                                       authReturnString[03],
                                                                       " ",
                                                                       authReturnString[04],
                                                                       " ",
                                                                       "N",
                                                                       " "));
                            createTokenFlag = true;
                        }
                    }
                }
                //Check Token C5 Data
                field = requestNcccMessage.GetTokenFieldId();
                dynamic valueTokenC5 = ((TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString())).GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                tokenId = "C5";
                if (valueTokenC5 != null)
                {
                    //Check token Id C5 Exist For Remove
                    if (tokenSortedList.ContainsKey(tokenId))
                        tokenSortedList.Remove(tokenId);
                    if (valueTokenC5 is TokenNCCCInstallmentC5 c6)
                    {
                        tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccTokenC5InstallmentStringFormat,
                                                                   c6.MerchantId,
                                                                   c6.StoreId,
                                                                   c6.TransactionId,
                                                                   c6.ReferenceId,
                                                                   c6.GoodsId,
                                                                   c6.Personal,
                                                                   c6.Pin,
                                                                   c6.EcFlag,
                                                                   c6.OrderNum,               //Order Number
                                                                   c6.InstallmentTxInd,       //installment-tx-indicator
                                                                   authReturnString[05],      //installment-tx-resp-code
                                                                   " ",
                                                                   c6.PeriodNumber,
                                                                   authReturnString[06],      //down-payments
                                                                   authReturnString[07],      //installment-payments
                                                                   authReturnString[08]));    //formality-fee
                        createTokenFlag = true;
                    }
                    else if (valueTokenC5 is TokenNCCCRoyaltyC5 c5)
                    {
                        tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccTokenC5RebateStringFormat,
                                                                   c5.MerchantId,
                                                                   c5.StoreId,
                                                                   c5.TransactionId,
                                                                   c5.ReferenceId,
                                                                   c5.GoodsId,
                                                                   c5.Personal,
                                                                   c5.Pin,
                                                                   c5.EcFlag,
                                                                   c5.SettlementFlag,
                                                                   c5.OrderNum,
                                                                   authReturnString[13],
                                                                   authReturnString[05],
                                                                   authReturnString[09],
                                                                   authReturnString[10],
                                                                   authReturnString[11],
                                                                   authReturnString[12]));
                        createTokenFlag = true;
                    }
                }

                if (createTokenFlag)
                {
                    //For Skip B5 Token Data
                    //tokenId = "B5";
                    //if (tokenSortedList.ContainsKey(tokenId))
                    //    tokenSortedList.Remove(tokenId);
                    //For Skip B5 Token Data
                    //string tokenData = new BicTokenField(tokenSortedList).GetTokenData();
                    //return (string.Format("{0:000}{1}", tokenData.Length, tokenData));
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ8();
                                var tokenQ8 = (TokenQ8)newtokenGroup.Tokens[tokenId];
                                tokenQ8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ9.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ9();
                                var tokenQ9 = (TokenQ9)newtokenGroup.Tokens[tokenId];
                                tokenQ9.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenS8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenS8();
                                var tokenS8 = (TokenS8)newtokenGroup.Tokens[tokenId];
                                tokenS8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenU7();
                                var tokenU7 = (TokenU7)newtokenGroup.Tokens[tokenId];
                                tokenU7.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateNccc0200TknAuthTokenData Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// CreateNccc0200TknActiveTokenData Create BIC Field 57 Or 63 Tokenization Active Response Data
        /// Nccc0200TknActiveProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@t04ErrorFlg         , authReturnString[03]:@t04CrdVeryFlg
        /// authReturnString[04]:@tQ8Tag16EMail       , authReturnString[05]:@tQ8Tag29IssConfigId
        /// authReturnString[06]:@tQ8Tag36PanSeq      , authReturnString[07]:@tQ8Tag51ChVfyMethod
        /// authReturnString[08]:@tQ8Tag56CvmErrCde   , authReturnString[09]:@tQ8Tag58CvmRsp
        /// authReturnString[10]:@tQ8TagQEPhone       , authReturnString[11]:@tQ8TagQGRiskLvlResult
        /// authReturnString[12]:@tknTxType           
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @t04ErrorFlg	            varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
        /// @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
        /// @tQ8Tag16EMail              varchar(64) 	Output   Field 57 or 63 Token Q8 SubToken Id 16 Acct E-Mail
        /// @tQ8Tag29IssConfigId        varchar(30) 	Output	 Field 57 or 63 Token Q8 SubToken Id 29 Issuer Production Configuration Id
        /// @tQ8Tag36PanSeq       	    varchar(10) 	Output	 Field 57 or 63 Token Q8 SubToken Id 36 Card Sequence Number
        /// @tQ8Tag51ChVfyMethod      	varchar(186) 	Output	 Field 57 or 63 Token Q8 SubToken Id 51 CardHolder Verification Method
        /// @tQ8Tag56CvmErrCde          varchar(08) 	Output	 Field 57 or 63 Token Q8 SubToken Id 56 Check Eligibility error code
        /// @tQ8Tag58CvmRsp             varchar(02) 	Output	 Field 57 or 63 Token Q8 SubToken Id 58 Contactless use
        /// @tQ8TagQEPhone      	    varchar(15) 	Output	 Field 57 or 63 Token Q8 SubToken Id QE Mobile Phone number
        /// @tQ8TagQGRiskLvlResult      varchar(01) 	Output	 Field 57 or 63 Token Q8 SubToken Id QG Risk Level Result
        /// @tknTxType  	            varchar(02) 	Output
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNccc0200TknActiveTokenData(NcccIso8583 requestNcccMessage,
                                                                NcccAuthInfoStruct authInfoData,
                                                                string[] authReturnString)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            try
            {
                //Add Input Have Token Data
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        tokenSortedList.Add(tokenId, tokenData);
                        createTokenFlag = true;
                    }
                }
                //Check Token 04 Data
                tokenId = "04";
                if (!string.IsNullOrEmpty(authReturnString[02]) &&
                    !string.IsNullOrEmpty(authReturnString[03]) &&
                    tokenSortedList.ContainsKey(tokenId))
                {
                    if ((authReturnString[02].Length == 1) && (authReturnString[03].Length == 1))
                    {
                        field = FIELD.F_35;
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            //Remove tokenId and recreate
                            tokenSortedList.Remove(tokenId);
                            tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccToken04StringFormat,
                                                                       authReturnString[02],
                                                                       " ",
                                                                       authReturnString[03],
                                                                       " ",
                                                                       "Y",
                                                                       " "));
                            createTokenFlag = true;
                        }
                        else
                        {
                            //Remove tokenId and recreate
                            tokenSortedList.Remove(tokenId);
                            tokenSortedList.Add(tokenId, string.Format(SystemConstantData.CommonData.NcccToken04StringFormat,
                                                                       authReturnString[02],
                                                                       " ",
                                                                       authReturnString[03],
                                                                       " ",
                                                                       "N",
                                                                       " "));
                            createTokenFlag = true;
                        }
                    }
                }
                //Check Token Q8 Data
                tokenId = "Q8";
                if (tokenSortedList.ContainsKey(tokenId))
                {
                    //Remove tokenId and recreate
                    tokenSortedList.Remove(tokenId);
                    var updateTokenQ8TLV = authInfoData.tokenQ8Data.GetSubTokenData();
                    string subTLVId = "16";
                    if (!string.IsNullOrEmpty(authReturnString[04]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[04];

                    subTLVId = "29";
                    if (!string.IsNullOrEmpty(authReturnString[05]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[05];

                    subTLVId = "36";
                    if (!string.IsNullOrEmpty(authReturnString[06]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[06];

                    subTLVId = "51";
                    if (!string.IsNullOrEmpty(authReturnString[07]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[07];

                    subTLVId = "56";
                    if (!string.IsNullOrEmpty(authReturnString[08]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[08];

                    subTLVId = "58";
                    if (!string.IsNullOrEmpty(authReturnString[09]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[09];

                    subTLVId = "QE";
                    if (!string.IsNullOrEmpty(authReturnString[10]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[10];

                    subTLVId = "QG";
                    if (!string.IsNullOrEmpty(authReturnString[11]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[11];

                    var tempString = PackTokenQ8Data(updateTokenQ8TLV);
                    tokenSortedList.Add(tokenId, tempString);
                    createTokenFlag = true;

                }

                //Check Token Q9 Data
                //tokenId = "Q9";
                //if (tokenSortedList.ContainsKey(tokenId))
                //{
                //    //Remove tokenId and recreate
                //    tokenSortedList.Remove(tokenId);
                //    ///////update add create Q9
                //    //tokenSortedList.Add(tokenId, tempString);
                //    createTokenFlag = true;
                //
                //}

                if (createTokenFlag)
                {
                    //For Skip B5 Token Data
                    //tokenId = "B5";
                    //if (tokenSortedList.ContainsKey(tokenId))
                    //    tokenSortedList.Remove(tokenId);
                    //For Skip B5 Token Data
                    //string tokenData = new BicTokenField(tokenSortedList).GetTokenData();
                    //return (string.Format("{0:000}{1}", tokenData.Length, tokenData));
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ8();
                                var tokenQ8 = (TokenQ8)newtokenGroup.Tokens[tokenId];
                                tokenQ8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ9.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ9();
                                var tokenQ9 = (TokenQ9)newtokenGroup.Tokens[tokenId];
                                tokenQ9.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenS8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenS8();
                                var tokenS8 = (TokenS8)newtokenGroup.Tokens[tokenId];
                                tokenS8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenU7();
                                var tokenU7 = (TokenU7)newtokenGroup.Tokens[tokenId];
                                tokenU7.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateNccc0200TknActiveTokenData Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// CreateNccc0200TknOtpTokenData Create BIC Field 57 Or 63 Token Data For Tokenization OTP Response Data
        /// Nccc0200TknOtp Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@tQ8Tag07Ac          , authReturnString[03]:@otpSend
        /// authReturnString[04]:@tokenMsg            , authReturnString[05]:@custName 
        /// authReturnString[06]:@cellPhone
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @tQ8Tag07Ac                 varchar(12) 	Output   Field 57 or 63 Token Q8 SubToken Id 07 Activity Code
        /// @otpSend                    varchar(01) 	Output	 簡訊使用
        /// @tokenMsg       	        varchar(100) 	Output	 簡訊使用
        /// @custName      	            varchar(20) 	Output	 簡訊使用
        /// @cardholderCellPhone      	varchar(20) 	Output	 簡訊使用
        /// <summary/>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNccc0200TknOtpTokenData(NcccIso8583 requestNcccMessage,
                                                         NcccAuthInfoStruct authInfoData,
                                                         string[] authReturnString)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            try
            {
                //Add Input Have Token Data
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        tokenSortedList.Add(tokenId, tokenData);
                        createTokenFlag = true;
                    }
                }
                //Check Token Q8 Data
                tokenId = "Q8";
                if (tokenSortedList.ContainsKey(tokenId))
                {
                    //Remove tokenId and recreate
                    tokenSortedList.Remove(tokenId);
                    var updateTokenQ8TLV = authInfoData.tokenQ8Data.GetSubTokenData();
                    string subTLVId = "07";
                    if (!string.IsNullOrEmpty(authReturnString[02]))
                        updateTokenQ8TLV[subTLVId] = authReturnString[02];
                    var tempString = PackTokenQ8Data(updateTokenQ8TLV);
                    tokenSortedList.Add(tokenId, tempString);
                    createTokenFlag = true;

                }

                if (createTokenFlag)
                {
                    //For Skip B5 Token Data
                    //tokenId = "B5";
                    //if (tokenSortedList.ContainsKey(tokenId))
                    //    tokenSortedList.Remove(tokenId);
                    //For Skip B5 Token Data
                    //string tokenData = new BicTokenField(tokenSortedList).GetTokenData();
                    //return (string.Format("{0:000}{1}", tokenData.Length, tokenData));
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ8();
                                var tokenQ8 = (TokenQ8)newtokenGroup.Tokens[tokenId];
                                tokenQ8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ9.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ9();
                                var tokenQ9 = (TokenQ9)newtokenGroup.Tokens[tokenId];
                                tokenQ9.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenS8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenS8();
                                var tokenS8 = (TokenS8)newtokenGroup.Tokens[tokenId];
                                tokenS8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenU7();
                                var tokenU7 = (TokenU7)newtokenGroup.Tokens[tokenId];
                                tokenU7.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateNccc0200TknOtpTokenData Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// CreateNcccHceC5TokenData Create BIC Field 57 Or 63 Authorization Data For Response Data
        /// Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode    , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@tC5InstTxResp     , authReturnString[03]:@tC5DownPayment
        /// authReturnString[04]:@tC5InstPayment    , authReturnString[05]:@tC5InstFee
        /// authReturnString[06]:@tC5PointRedemption , authReturnString[07]:@tC5SignPoint
        /// authReturnString[08]:@tC5PointBalance   , authReturnString[09]:@tC5PaidCreditAmt
        /// authReturnString[10]:@tC5LoyaltyTxInd
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNcccHceC5TokenData(NcccIso8583 requestNcccMessage,
                                                    NcccAuthInfoStruct authInfoData,
                                                    string[] authReturnString)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            try
            {
                //this for Nccc HCE(0200,0220) TokenAuth(0220) , authReturnString.Length = 10
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        if (tokenId.Equals("C5"))
                        {
                            //Check Token C05 Data
                            dynamic valueTokenC5 = ((TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString())).GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                            tokenId = "C5";
                            if (valueTokenC5 != null)
                            {
                                //Check token Id C5 Exist For Remove
                                if (tokenSortedList.ContainsKey(tokenId))
                                    tokenSortedList.Remove(tokenId);
                                if (valueTokenC5 is TokenNCCCInstallmentC5)
                                {
                                    var c5 = valueTokenC5 as TokenNCCCInstallmentC5;
                                    tokenSortedList.Add(tokenId, String.Format(SystemConstantData.CommonData.NcccTokenC5InstallmentStringFormat,
                                                                               c5.MerchantId,
                                                                               c5.StoreId,
                                                                               c5.TransactionId,
                                                                               c5.ReferenceId,
                                                                               c5.GoodsId,
                                                                               c5.Personal,
                                                                               c5.Pin,
                                                                               c5.EcFlag,
                                                                               c5.OrderNum,
                                                                               c5.InstallmentTxInd,
                                                                               authReturnString[02],
                                                                               " ",
                                                                               c5.PeriodNumber,
                                                                               authReturnString[03],
                                                                               authReturnString[04],
                                                                               authReturnString[05]));
                                    createTokenFlag = true;
                                }
                                else if (valueTokenC5 is TokenNCCCRoyaltyC5)
                                {
                                    var c5 = valueTokenC5 as TokenNCCCRoyaltyC5;
                                    tokenSortedList.Add(tokenId, String.Format(SystemConstantData.CommonData.NcccTokenC5RebateStringFormat,
                                                                               c5.MerchantId,
                                                                               c5.StoreId,
                                                                               c5.TransactionId,
                                                                               c5.ReferenceId,
                                                                               c5.GoodsId,
                                                                               c5.Personal,
                                                                               c5.Pin,
                                                                               c5.EcFlag,
                                                                               c5.SettlementFlag,
                                                                               c5.OrderNum,
                                                                               authReturnString[10],
                                                                               authReturnString[02],
                                                                               authReturnString[06],
                                                                               authReturnString[07],
                                                                               authReturnString[08],
                                                                               authReturnString[09]));
                                    createTokenFlag = true;
                                }
                            }
                        }
                        else
                        {
                            tokenSortedList.Add(tokenId, tokenData);
                            createTokenFlag = true;
                        }
                    }
                }
                if (createTokenFlag)
                {
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenU7();
                                var tokenU7 = (TokenU7)newtokenGroup.Tokens[tokenId];
                                tokenU7.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $" CreateNcccHceC5TokenData Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// CreateNcccC5TokenData Create BIC Field 57 Or 63 Authorization Data For Response Data
        /// Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode    , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@tC5InstTxResp     , authReturnString[03]:@tC5DownPayment
        /// authReturnString[04]:@tC5InstPayment    , authReturnString[05]:@tC5InstFee
        /// authReturnString[06]:@tC5PointRedemption , authReturnString[07]:@tC5SignPoint
        /// authReturnString[08]:@tC5PointBalance   , authReturnString[09]:@tC5PaidCreditAmt
        /// authReturnString[10]:@tC5LoyaltyTxInd
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNcccC5TokenData(NcccIso8583 requestNcccMessage,
                                                    NcccAuthInfoStruct authInfoData,
                                                    string[] authReturnString)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            try
            {
                //this for Nccc HCE(0200,0220) TokenAuth(0220) , authReturnString.Length = 15
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        if (tokenId.Equals("C5"))
                        {
                            //Check Token C05 Data
                            dynamic valueTokenC5 = ((TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString())).GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                            tokenId = "C5";
                            if (valueTokenC5 != null)
                            {
                                //Check token Id C5 Exist For Remove
                                if (tokenSortedList.ContainsKey(tokenId))
                                    tokenSortedList.Remove(tokenId);
                                if (valueTokenC5 is TokenNCCCInstallmentC5)
                                {
                                    var c5 = valueTokenC5 as TokenNCCCInstallmentC5;
                                    tokenSortedList.Add(tokenId, String.Format(SystemConstantData.CommonData.NcccTokenC5InstallmentStringFormat,
                                                                               c5.MerchantId,
                                                                               c5.StoreId,
                                                                               c5.TransactionId,
                                                                               c5.ReferenceId,
                                                                               c5.GoodsId,
                                                                               c5.Personal,
                                                                               c5.Pin,
                                                                               c5.EcFlag,
                                                                               c5.OrderNum,
                                                                               c5.InstallmentTxInd,
                                                                               authReturnString[03],
                                                                               " ",
                                                                               c5.PeriodNumber,
                                                                               authReturnString[04],
                                                                               authReturnString[05],
                                                                               authReturnString[06]));
                                    createTokenFlag = true;
                                }
                                else if (valueTokenC5 is TokenNCCCRoyaltyC5)
                                {
                                    var c5 = valueTokenC5 as TokenNCCCRoyaltyC5;
                                    tokenSortedList.Add(tokenId, String.Format(SystemConstantData.CommonData.NcccTokenC5RebateStringFormat,
                                                                               c5.MerchantId,
                                                                               c5.StoreId,
                                                                               c5.TransactionId,
                                                                               c5.ReferenceId,
                                                                               c5.GoodsId,
                                                                               c5.Personal,
                                                                               c5.Pin,
                                                                               c5.EcFlag,
                                                                               c5.SettlementFlag,
                                                                               c5.OrderNum,
                                                                               authReturnString[11],
                                                                               authReturnString[03],
                                                                               authReturnString[07],
                                                                               authReturnString[08],
                                                                               authReturnString[09],
                                                                               authReturnString[10]));
                                    createTokenFlag = true;
                                }
                            }
                        }
                        else
                        {
                            tokenSortedList.Add(tokenId, tokenData);
                            createTokenFlag = true;
                        }
                    }
                }
                if (createTokenFlag)
                {
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenU7();
                                var tokenU7 = (TokenU7)newtokenGroup.Tokens[tokenId];
                                tokenU7.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $" CreateNcccC5TokenData Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// CreateNccc0220TknAuthTokenData Create BIC Field 57 Or 63 For Tokenization Authorization Response Data
        // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        // authReturnString[02]:@@isoP39RspAction    , authReturnString[03]:@tC5InstTxResp
        // authReturnString[04]:@tC5DownPayment      , authReturnString[05]:@tC5InstPayment
        // authReturnString[06]:@tC5InstFee          , authReturnString[07]:@tC5PointRedemption
        // authReturnString[08]:@tC5SignPoint        , authReturnString[09]:@tC5PointBalance
        // authReturnString[10]:@tC5PaidCreditAmt    , authReturnString[11]:@tC5LoyaltyTxInd
        // authReturnString[12]:@vdLinkNbr           , authReturnString[13]:@vdLinkNbrSeq
        // authReturnString[14]:@vdActionCode        , authReturnString[15]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction            varchar(01)     Output 	 --對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可)
        /// @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
        /// @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
        /// @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
        /// @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
        /// @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
        /// @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
        /// @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
        /// @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
        /// @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
        /// @vdLinkNbr                  varchar(20)     Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq               varchar(02)     Output   VD 圈存帳戶序號
        /// @vdActionCode               varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                 varchar(15) 	Output   VD 圈存解圈金額
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message Data</param>
        /// <param name="authInfoData">For Authorization Data</param>
        /// <param name="authReturnString">Cola System Return String Arrays</param>
        /// <returns>
        /// BIC P57(Include 4 Bytes Length(LLLLVR)) Or P63 Field String(Include 3 Bytes Length(LLLVR))
        /// </returns>
        public static string CreateNccc0220TknAuthTokenData(NcccIso8583 requestNcccMessage,
                                                            NcccAuthInfoStruct authInfoData,
                                                            string[] authReturnString)
        {
            SortedList<string, string> tokenSortedList = new SortedList<string, string>();
            string tokenId;
            FIELD field;
            bool createTokenFlag = false;
            try
            {
                //Add Input Have Token Data
                //field = FIELD.F_63;
                field = requestNcccMessage.GetTokenFieldId();
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString());
                    const int tokenHeaderLength = 10;
                    foreach (var data in tokenGroup.Tokens)
                    {
                        tokenId = data.Key;
                        string tokenHeaderData = data.Value.Encode();
                        string tokenHeader = tokenHeaderData.Substring(0, tokenHeaderLength);
                        string tokenData = tokenHeaderData.Substring(tokenHeaderLength, tokenHeaderData.Length - tokenHeaderLength);
                        if (tokenId.Equals("C5"))
                        {
                            //Check Token C05 Data
                            dynamic valueTokenC5 = ((TokenGroup)requestNcccMessage.Core().GetOthersValByKey(field.ToString())).GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                            tokenId = "C5";
                            if (valueTokenC5 != null)
                            {
                                //Check token Id C5 Exist For Remove
                                if (tokenSortedList.ContainsKey(tokenId))
                                    tokenSortedList.Remove(tokenId);
                                if (valueTokenC5 is TokenNCCCInstallmentC5)
                                {
                                    var c5 = valueTokenC5 as TokenNCCCInstallmentC5;
                                    tokenSortedList.Add(tokenId, String.Format(SystemConstantData.CommonData.NcccTokenC5InstallmentStringFormat,
                                                                               c5.MerchantId,
                                                                               c5.StoreId,
                                                                               c5.TransactionId,
                                                                               c5.ReferenceId,
                                                                               c5.GoodsId,
                                                                               c5.Personal,
                                                                               c5.Pin,
                                                                               c5.EcFlag,
                                                                               c5.OrderNum,
                                                                               c5.InstallmentTxInd,
                                                                               authReturnString[03],
                                                                               " ",
                                                                               c5.PeriodNumber,
                                                                               authReturnString[04],
                                                                               authReturnString[05],
                                                                               authReturnString[06]));
                                    createTokenFlag = true;
                                }
                                else if (valueTokenC5 is TokenNCCCRoyaltyC5)
                                {
                                    var c5 = valueTokenC5 as TokenNCCCRoyaltyC5;
                                    tokenSortedList.Add(tokenId, String.Format(SystemConstantData.CommonData.NcccTokenC5RebateStringFormat,
                                                                               c5.MerchantId,
                                                                               c5.StoreId,
                                                                               c5.TransactionId,
                                                                               c5.ReferenceId,
                                                                               c5.GoodsId,
                                                                               c5.Personal,
                                                                               c5.Pin,
                                                                               c5.EcFlag,
                                                                               c5.SettlementFlag,
                                                                               c5.OrderNum,
                                                                               authReturnString[11],
                                                                               authReturnString[03],
                                                                               authReturnString[07],
                                                                               authReturnString[08],
                                                                               authReturnString[09],
                                                                               authReturnString[10]));
                                    createTokenFlag = true;
                                }
                            }
                        }
                        else
                        {
                            tokenSortedList.Add(tokenId, tokenData);
                            createTokenFlag = true;
                        }
                    }
                }
                if (createTokenFlag)
                {
                    //For Skip B5 Token Data
                    //tokenId = "B5";
                    //if (tokenSortedList.ContainsKey(tokenId))
                    //    tokenSortedList.Remove(tokenId);
                    //For Skip B5 Token Data
                    //string tokenData = new BicTokenField(tokenSortedList).GetTokenData();
                    //return (String.Format("{0:000}{1}", tokenData.Length, tokenData));
                    var newtokenGroup = new TokenGroup();
                    foreach (var token in tokenSortedList)
                    {
                        tokenId = token.Key;
                        string tokenData = token.Value;
                        switch (tokenId)
                        {
                            case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                newtokenGroup.Tokens[tokenId] = new Token04();
                                var token04 = (Token04)newtokenGroup.Tokens[tokenId];
                                token04.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                newtokenGroup.Tokens[tokenId] = new Token20();
                                var token20 = (Token20)newtokenGroup.Tokens[tokenId];
                                token20.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB2();
                                var tokenB2 = (TokenB2)newtokenGroup.Tokens[tokenId];
                                tokenB2.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB3();
                                var tokenB3 = (TokenB3)newtokenGroup.Tokens[tokenId];
                                tokenB3.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB4();
                                var tokenB4 = (TokenB4)newtokenGroup.Tokens[tokenId];
                                tokenB4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB5();
                                var tokenB5 = (TokenB5)newtokenGroup.Tokens[tokenId];
                                tokenB5.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenB6();
                                var tokenB6 = (TokenB6)newtokenGroup.Tokens[tokenId];
                                tokenB6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC0();
                                var tokenC0 = (TokenC0)newtokenGroup.Tokens[tokenId];
                                tokenC0.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC4();
                                var tokenC4 = (TokenC4)newtokenGroup.Tokens[tokenId];
                                tokenC4.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                if (tokenData.Length == 144)
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCInstallmentC5();
                                    var tokenC5Installment = (TokenNCCCInstallmentC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Installment.DataDecode(tokenData);
                                }
                                else
                                {
                                    newtokenGroup.Tokens[tokenId] = new TokenNCCCRoyaltyC5();
                                    var tokenC5Royalty = (TokenNCCCRoyaltyC5)newtokenGroup.Tokens[tokenId];
                                    tokenC5Royalty.DataDecode(tokenData);
                                }
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenC6.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenC6();
                                var tokenC6 = (TokenC6)newtokenGroup.Tokens[tokenId];
                                tokenC6.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCE();
                                var tokenCE = (TokenCE)newtokenGroup.Tokens[tokenId];
                                tokenCE.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenCH();
                                var tokenCH = (TokenCH)newtokenGroup.Tokens[tokenId];
                                tokenCH.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ8();
                                var tokenQ8 = (TokenQ8)newtokenGroup.Tokens[tokenId];
                                tokenQ8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenQ9.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenQ9();
                                var tokenQ9 = (TokenQ9)newtokenGroup.Tokens[tokenId];
                                tokenQ9.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenS8.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenS8();
                                var tokenS8 = (TokenS8)newtokenGroup.Tokens[tokenId];
                                tokenS8.DataDecode(tokenData);
                                break;
                            case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                                newtokenGroup.Tokens[tokenId] = new TokenU7();
                                var tokenU7 = (TokenU7)newtokenGroup.Tokens[tokenId];
                                tokenU7.DataDecode(tokenData);
                                break;
                        }
                    }
                    return (newtokenGroup.Encode());
                }
                return (string.Empty);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $" CreateNccc0220TokenData Exception:{ex.Message}");

                return null;
            }
        }
    }
}

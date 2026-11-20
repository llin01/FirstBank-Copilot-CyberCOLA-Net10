using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.DBUtils
{
    public static class StoreProcUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        /// <summary>
        /// Nccc0200AuthProcess Process Message Type 0200 Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="data9F10Array"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200AuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] data9F10Array)
        {
            const int storeProcedureParameterCount = 90;
            const int outputParaCount = 23;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoP52PinBlock;
                paraValues[26] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[27] = bicAuthInfoData.isoS127CellPhone;
                paraValues[28] = bicAuthInfoData.isoS127Dob;
                paraValues[29] = bicAuthInfoData.isoS127Pws;
                paraValues[30] = null;
                paraValues[31] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[32] = bicAuthInfoData.isoS127MpFlag;
                paraValues[33] = bicAuthInfoData.isoS127Id;
                paraValues[34] = bicAuthInfoData.arqcCheck;
                paraValues[35] = bicAuthInfoData.arqcChkResult;
                paraValues[36] = bicAuthInfoData.cvvCheck;
                paraValues[37] = bicAuthInfoData.cvvChkResult;
                paraValues[38] = bicAuthInfoData.cvv2Check;
                paraValues[39] = bicAuthInfoData.cvv2ChkResult;
                paraValues[40] = bicAuthInfoData.cavvCheck;
                paraValues[41] = bicAuthInfoData.cavvChkResult;
                paraValues[42] = bicAuthInfoData.pvvCheck;
                paraValues[43] = bicAuthInfoData.pvvChkResult;
                paraValues[44] = bicAuthInfoData.pvvDobCheck;
                paraValues[45] = bicAuthInfoData.pvvDobChkResult;


                if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                    paraValues[46] = null;
                else
                    paraValues[46] = bicAuthInfoData.token04Data.ErrorFlg;

                if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                    paraValues[47] = null;
                else
                    paraValues[47] = bicAuthInfoData.token04Data.CrdVeryFlg;

                if (bicAuthInfoData.tokenB0Data?.OnBehalf == null)
                    paraValues[48] = null;
                else
                {
                    string dataString = bicAuthInfoData.tokenB0Data.OnBehalf;
                    var dataLength = dataString.Length;
                    int startPosition = 0;
                    StringBuilder sb = new StringBuilder();
                    while (startPosition < dataLength)
                    {
                        var copyDataLength = 0;
                        if ((startPosition + 3) > dataLength)
                            copyDataLength = dataLength - startPosition;
                        else
                            copyDataLength = 3;
                        if (startPosition == 0)
                        {
                            sb.Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        else
                        {
                            sb.Append(";")
                              .Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        startPosition += 3;
                    }
                    paraValues[48] = sb.ToString();
                }

                var b2StartIndex = 49;
                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[b2StartIndex] = null;
                else
                    paraValues[b2StartIndex] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.Atc == null)
                    paraValues[b2StartIndex + 1] = null;
                else
                    paraValues[b2StartIndex + 1] = bicAuthInfoData.tokenB2Data.Atc;

                if (bicAuthInfoData.tokenB2Data?.IssApplDataLgth == null)
                {
                    paraValues[b2StartIndex + 2] = null;
                    paraValues[b2StartIndex + 5] = null;
                    paraValues[b2StartIndex + 6] = null;
                }
                else
                {
                    int issApplDataLgth = Convert.ToInt32(bicAuthInfoData.tokenB2Data.IssApplDataLgth);
                    var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       bicAuthInfoData.tokenB2Data.IssApplData,
                                                                       creditCardType).Split(';');
                    if (ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = ColaCommUtility.HexString2BinString(data9F10[2]);
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = data9F10[0];
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = data9F10[1];
                        data9F10.CopyTo(data9F10Array, 0);
                    }
                    else
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = null;
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = null;
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = null;
                    }
                }

                if (bicAuthInfoData.tokenB2Data?.Arqc == null)
                    paraValues[b2StartIndex + 3] = null;
                else
                    paraValues[b2StartIndex + 3] = bicAuthInfoData.tokenB2Data.Arqc;

                if (bicAuthInfoData.tokenB2Data?.UnpredictNum == null)
                    paraValues[b2StartIndex + 4] = null;
                else
                    paraValues[b2StartIndex + 4] = bicAuthInfoData.tokenB2Data.UnpredictNum;

                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[56] = null;
                else
                    paraValues[56] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[57] = null;
                else
                    paraValues[57] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[58] = null;
                else
                    paraValues[58] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[59] = null;
                else
                    paraValues[59] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[60] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[60] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[62] = null;
                else
                    paraValues[62] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[63] = null;
                else
                    paraValues[63] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                    paraValues[64] = null;
                else
                    paraValues[64] = bicAuthInfoData.tokenC0Data.CavvAavResults;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[65] = null;
                else
                    paraValues[65] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[66] = null;
                else
                    paraValues[66] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[67] = null;
                else
                    paraValues[67] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[68] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[68] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[68] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[69] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxnRespCde;
                else
                    paraValues[69] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[70] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).SettlementFlag;
                else
                    paraValues[70] = null;

                if ((bicAuthInfoData.tokenC5Data) is TokenNCCCInstallmentC5)
                    paraValues[71] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).PeriodNumber;
                else
                    paraValues[71] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[72] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[72] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[73] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[73] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[74] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[74] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[75] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[75] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[76] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[76] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[77] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[77] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[78] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[78] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[79] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[79] = null;

                paraValues[80] = null;
                paraValues[81] = null;
                paraValues[82] = null;
                paraValues[83] = null;
                paraValues[84] = null;
                paraValues[85] = null;
                paraValues[86] = null;
                paraValues[87] = null;

                paraValues[88] = bicAuthInfoData.isoS127MerchVfy;

                if (bicAuthInfoData.tokenB4Data?.ArqcVerify == null)
                    paraValues[89] = null;
                else
                    paraValues[89] = bicAuthInfoData.tokenB4Data.ArqcVerify;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200AuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
                // authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
                // authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
                // authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
                // authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
                // authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
                // authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
                // authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
                // authReturnString[18]:@icashCardNbr        , authReturnString[19]:@vdLinkNbr
                // authReturnString[20]:@vdLinkNbrSeq        , authReturnString[21]:@vdActionCode
                // authReturnString[22]:@vdBlockAmt

                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoS127NegDelFlag       	varchar(01)	    Output	 --NegFile刪除註記 initial 時請放null即可註記為D時, 傳送一筆MessageType :0300 Detele Neg File
                // @t04ErrorFlg	                varchar(01) 	Output	 Field 63 Token04:Error-flag Token04.ErrorFlg
                // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 63 Token04:Crd-very-flag Token04.CrdVeryFlg
                // @tC0CavvResult               varchar(01) 	Output   Field 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
                // @tC5InstTxInd        	    varchar(01) 	Output	 Field 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
                // @tC5InstTxResp       	    varchar(02) 	Output	 Field 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
                // @tC5DownPayment      	    varchar(08) 	Output	 Field 63 Token C5 Down Payment<分期,首期>
                // @tC5InstPayment      	    varchar(08) 	Output	 Field 63 Token C5 Installment Payment<分期, 每期>
                // @tC5InstFee          	    varchar(06) 	Output	 Field 63 Token C5 Formality-Fee<分期, 手續費/利息>
                // @tC5PointRedemption  	    varchar(08) 	Output	 Field 63 Token C5 Point of Redemption<紅利,折扺點數>
                // @tC5SignPoint        	    varchar(01) 	Output	 Field 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
                // @tC5PointBalance     	    varchar(08) 	Output	 Field 63 Token C5 Point of Balance<紅利, 餘點>
                // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
                // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 63 Token C5 <紅利註記>
                // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
                // @icashCardNbr                varchar(20)    	OUTPUT   ICASH 刪除STIP 檔案
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0200AuthProcess Process Message Type 0200 Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="data9F10Array"></param>
        /// <param name="authReturnString"></param>
        /// <param name="responseXML"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200AuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] data9F10Array, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 90;
            const int outputParaCount = 23;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                if (!string.IsNullOrEmpty(authReturnString[0]))
                    paraValues[17] = authReturnString[0]; //改以第一道電文的#38帶入
                else
                    paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoP52PinBlock;
                paraValues[26] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[27] = bicAuthInfoData.isoS127CellPhone;
                paraValues[28] = bicAuthInfoData.isoS127Dob;
                paraValues[29] = bicAuthInfoData.isoS127Pws;
                paraValues[30] = null;
                paraValues[31] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[32] = bicAuthInfoData.isoS127MpFlag;
                paraValues[33] = bicAuthInfoData.isoS127Id;
                paraValues[34] = bicAuthInfoData.arqcCheck;
                paraValues[35] = bicAuthInfoData.arqcChkResult;
                paraValues[36] = bicAuthInfoData.cvvCheck;
                paraValues[37] = bicAuthInfoData.cvvChkResult;
                paraValues[38] = bicAuthInfoData.cvv2Check;
                paraValues[39] = bicAuthInfoData.cvv2ChkResult;
                paraValues[40] = bicAuthInfoData.cavvCheck;
                paraValues[41] = bicAuthInfoData.cavvChkResult;
                paraValues[42] = bicAuthInfoData.pvvCheck;
                paraValues[43] = bicAuthInfoData.pvvChkResult;
                paraValues[44] = bicAuthInfoData.pvvDobCheck;
                paraValues[45] = bicAuthInfoData.pvvDobChkResult;

                if (authReturnString[04] != null)
                    paraValues[46] = authReturnString[04]; //改以第一道電文回覆值帶入
                else
                {
                    if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = bicAuthInfoData.token04Data.ErrorFlg;
                }

                if (!string.IsNullOrEmpty(authReturnString[05]))
                    paraValues[47] = authReturnString[05]; //改以第一道電文回覆值帶入
                else
                {
                    if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                        paraValues[47] = null;
                    else
                        paraValues[47] = bicAuthInfoData.token04Data.CrdVeryFlg;
                }

                if (bicAuthInfoData.tokenB0Data?.OnBehalf == null)
                    paraValues[48] = null;
                else
                {
                    string dataString = bicAuthInfoData.tokenB0Data.OnBehalf;
                    var dataLength = dataString.Length;
                    int startPosition = 0;
                    StringBuilder sb = new StringBuilder();
                    while (startPosition < dataLength)
                    {
                        var copyDataLength = 0;
                        if ((startPosition + 3) > dataLength)
                            copyDataLength = dataLength - startPosition;
                        else
                            copyDataLength = 3;
                        if (startPosition == 0)
                        {
                            sb.Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        else
                        {
                            sb.Append(";")
                              .Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        startPosition += 3;
                    }
                    paraValues[48] = sb.ToString();
                }

                var b2StartIndex = 49;
                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[b2StartIndex] = null;
                else
                    paraValues[b2StartIndex] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.Atc == null)
                    paraValues[b2StartIndex + 1] = null;
                else
                    paraValues[b2StartIndex + 1] = bicAuthInfoData.tokenB2Data.Atc;

                if (bicAuthInfoData.tokenB2Data?.IssApplDataLgth == null)
                {
                    paraValues[b2StartIndex + 2] = null;
                    paraValues[b2StartIndex + 5] = null;
                    paraValues[b2StartIndex + 6] = null;
                }
                else
                {
                    int issApplDataLgth = Convert.ToInt32(bicAuthInfoData.tokenB2Data.IssApplDataLgth);
                    var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       bicAuthInfoData.tokenB2Data.IssApplData,
                                                                       creditCardType).Split(';');
                    if (ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = ColaCommUtility.HexString2BinString(data9F10[2]);
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = data9F10[0];
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = data9F10[1];
                        data9F10.CopyTo(data9F10Array, 0);
                    }
                    else
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = null;
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = null;
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = null;
                    }
                }

                if (bicAuthInfoData.tokenB2Data?.Arqc == null)
                    paraValues[b2StartIndex + 3] = null;
                else
                    paraValues[b2StartIndex + 3] = bicAuthInfoData.tokenB2Data.Arqc;

                if (bicAuthInfoData.tokenB2Data?.UnpredictNum == null)
                    paraValues[b2StartIndex + 4] = null;
                else
                    paraValues[b2StartIndex + 4] = bicAuthInfoData.tokenB2Data.UnpredictNum;

                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[56] = null;
                else
                    paraValues[56] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[57] = null;
                else
                    paraValues[57] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[58] = null;
                else
                    paraValues[58] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[59] = null;
                else
                    paraValues[59] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[60] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[60] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;


                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[62] = null;
                else
                    paraValues[62] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[63] = null;
                else
                    paraValues[63] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (authReturnString[06] != null)
                    paraValues[64] = authReturnString[06]; //改以第一道電文回覆值帶入
                else
                {
                    if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                        paraValues[64] = null;
                    else
                        paraValues[64] = bicAuthInfoData.tokenC0Data.CavvAavResults;
                }

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[65] = null;
                else
                    paraValues[65] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[66] = null;
                else
                    paraValues[66] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[67] = null;
                else
                    paraValues[67] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[68] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[68] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[68] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[69] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxnRespCde;
                else
                    paraValues[69] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[70] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).SettlementFlag;
                else
                    paraValues[70] = null;

                if ((bicAuthInfoData.tokenC5Data) is TokenNCCCInstallmentC5)
                    paraValues[71] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).PeriodNumber;
                else
                    paraValues[71] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[72] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[72] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[73] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[73] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[74] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[74] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[75] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[75] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[76] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[76] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[77] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[77] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[78] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[78] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[79] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[79] = null;

                if (!string.IsNullOrEmpty(authReturnString[17]))
                    paraValues[80] = authReturnString[17];
                else
                    paraValues[80] = null;
                paraValues[81] = null;

                if (responseXML.TxRs == null)
                {
                    paraValues[82] = authReturnString[19];
                    paraValues[83] = authReturnString[20];
                    paraValues[84] = authReturnString[21];
                    paraValues[87] = 0;
                }
                else
                {
                    paraValues[82] = responseXML.TxRs.AcctNo;
                    paraValues[83] = responseXML.TxRs.CardNo;
                    paraValues[84] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[87] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[87] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[85] = authReturnString[22];
                paraValues[86] = responseXML.ResponseHeader.StatusCode;

                paraValues[88] = bicAuthInfoData.isoS127MerchVfy;

                if (bicAuthInfoData.tokenB4Data?.ArqcVerify == null)
                    paraValues[89] = null;
                else
                    paraValues[89] = bicAuthInfoData.tokenB4Data.ArqcVerify;


                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200AuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
                // authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
                // authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
                // authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
                // authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
                // authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
                // authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
                // authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
                // authReturnString[18]:@icashCardNbr        , authReturnString[19]:@vdLinkNbr
                // authReturnString[20]:@vdLinkNbrSeq        , authReturnString[21]:@vdActionCode
                // authReturnString[22]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoS127NegDelFlag       	varchar(01)	    Output	 --NegFile刪除註記 initial 時請放null即可註記為D時, 傳送一筆MessageType :0300 Detele Neg File
                // @t04ErrorFlg	                varchar(01) 	Output	 Field 63 Token04:Error-flag Token04.ErrorFlg
                // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 63 Token04:Crd-very-flag Token04.CrdVeryFlg
                // @tC0CavvResult               varchar(01) 	Output   Field 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
                // @tC5InstTxInd        	    varchar(01) 	Output	 Field 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
                // @tC5InstTxResp       	    varchar(02) 	Output	 Field 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
                // @tC5DownPayment      	    varchar(08) 	Output	 Field 63 Token C5 Down Payment<分期,首期>
                // @tC5InstPayment      	    varchar(08) 	Output	 Field 63 Token C5 Installment Payment<分期, 每期>
                // @tC5InstFee          	    varchar(06) 	Output	 Field 63 Token C5 Formality-Fee<分期, 手續費/利息>
                // @tC5PointRedemption  	    varchar(08) 	Output	 Field 63 Token C5 Point of Redemption<紅利,折扺點數>
                // @tC5SignPoint        	    varchar(01) 	Output	 Field 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
                // @tC5PointBalance     	    varchar(08) 	Output	 Field 63 Token C5 Point of Balance<紅利, 餘點>
                // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
                // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 63 Token C5 <紅利註記>
                // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
                // @icashCardNbr                varchar(20)    	OUTPUT   ICASH 刪除STIP 檔案
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額


                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }
        /// <summary>
        /// Nccc0200TknActiveProcess Process Message Type 0200 Token Active Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200TknActiveProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 64;
            const int outputParaCount = 13;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknActiveProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = bicAuthInfoData.isoP41TermNbr;
                paraValues[17] = bicAuthInfoData.isoP42Merchant;
                paraValues[18] = bicAuthInfoData.isoP43MerName;
                paraValues[19] = bicAuthInfoData.isoP43MerCity;
                paraValues[20] = bicAuthInfoData.isoP43LocCountry;
                paraValues[21] = bicAuthInfoData.cvv2Check;
                paraValues[22] = bicAuthInfoData.cvv2ChkResult;

                if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                    paraValues[23] = null;
                else
                    paraValues[23] = bicAuthInfoData.token04Data.ErrorFlg;

                if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                    paraValues[24] = null;
                else
                    paraValues[24] = bicAuthInfoData.token04Data.CrdVeryFlg;

                if (bicAuthInfoData.tokenF4Data?.WalletIndData == null)
                    paraValues[25] = null;
                else
                    paraValues[25] = bicAuthInfoData.tokenF4Data.WalletIndData;

                var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                if (bicAuthInfoData.tokenQ8Data == null)
                {
                    paraValues[26] = 0;
                    paraValues[27] = null;
                    paraValues[28] = null;
                    paraValues[29] = 0;
                    paraValues[30] = 0;
                    paraValues[31] = null;
                    paraValues[32] = null;
                    paraValues[33] = null;
                    paraValues[34] = null;
                    paraValues[35] = null;
                    paraValues[36] = null;
                    paraValues[37] = null;
                    paraValues[38] = null;
                    paraValues[39] = null;
                    paraValues[40] = null;
                    paraValues[41] = null;
                    paraValues[42] = null;
                    paraValues[43] = null;
                    paraValues[44] = null;
                    paraValues[45] = null;
                    paraValues[46] = null;
                    paraValues[47] = null;
                    paraValues[48] = null;
                    paraValues[49] = null;
                    paraValues[50] = null;
                    paraValues[51] = null;
                    paraValues[52] = null;
                    paraValues[53] = null;
                    paraValues[54] = null;
                    paraValues[55] = null;
                    paraValues[56] = null;
                    paraValues[57] = null;
                    paraValues[58] = null;
                    paraValues[59] = null;
                }
                else
                {
                    var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                    if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                    {
                        paraValues[26] = 0;
                        paraValues[27] = null;
                        paraValues[28] = null;
                        paraValues[29] = 0;
                        paraValues[30] = 0;
                        paraValues[31] = null;
                        paraValues[32] = null;
                        paraValues[33] = null;
                        paraValues[34] = null;
                        paraValues[35] = null;
                        paraValues[36] = null;
                        paraValues[37] = null;
                        paraValues[38] = null;
                        paraValues[39] = null;
                        paraValues[40] = null;
                        paraValues[41] = null;
                        paraValues[42] = null;
                        paraValues[43] = null;
                        paraValues[44] = null;
                        paraValues[45] = null;
                        paraValues[46] = null;
                        paraValues[47] = null;
                        paraValues[48] = null;
                        paraValues[49] = null;
                        paraValues[50] = null;
                        paraValues[51] = null;
                        paraValues[52] = null;
                        paraValues[53] = null;
                        paraValues[54] = null;
                        paraValues[55] = null;
                        paraValues[56] = null;
                        paraValues[57] = null;
                        paraValues[58] = null;
                        paraValues[59] = null;
                    }
                    else
                    {
                        int arrayIndex = 26;
                        string subTokenId = "02";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 27;
                        subTokenId = "04";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 28;
                        subTokenId = "05";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 29;
                        subTokenId = "09";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 30;
                        subTokenId = "10";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 31;
                        subTokenId = "11";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 32;
                        subTokenId = "12";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 33;
                        subTokenId = "13";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 34;
                        subTokenId = "16";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 35;
                        subTokenId = "18";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 36;
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                            case CreditCardType.MasterCard:
                                subTokenId = "19";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            case CreditCardType.JCB:
                                subTokenId = "QF";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 37;
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                                subTokenId = "20";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            case CreditCardType.MasterCard:
                            case CreditCardType.JCB:
                                subTokenId = "34";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 38;
                        subTokenId = "21";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 39;
                        subTokenId = "22";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 40;
                        subTokenId = "27";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 41;
                        subTokenId = "28";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 42;
                        subTokenId = "29";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 43;
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                                subTokenId = "38";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            case CreditCardType.MasterCard:
                                subTokenId = "30";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 44;
                        subTokenId = "36";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 45;
                        subTokenId = "40";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 46;
                        subTokenId = "50";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 47;
                        subTokenId = "51";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }
                        //update
                        arrayIndex = 48;
                        subTokenId = "56";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 49;
                        subTokenId = "58";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 50;
                        subTokenId = "59";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 51;
                        subTokenId = "60";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 52;
                        subTokenId = "62";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 53;
                        subTokenId = "63";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 54;
                        subTokenId = "Q3";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 55;
                        subTokenId = "Q4";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 56;
                        subTokenId = "Q6";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 57;
                        subTokenId = "QA";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 58;
                        subTokenId = "QE";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 59;
                        subTokenId = "QG";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }
                    }
                }

                if (bicAuthInfoData.tokenQ9Data == null)
                    paraValues[60] = null;
                else
                {
                    bicAuthInfoData.tokenQ9Data.SubDataDecode();
                    switch (creditCardType)
                    {
                        case CreditCardType.Visa:
                            paraValues[60] = bicAuthInfoData.tokenQ9Data?.VisaMsgRsnCde;
                            break;
                        case CreditCardType.MasterCard:
                            paraValues[60] = bicAuthInfoData.tokenQ9Data?.MastercardAdvcDetlCde;
                            break;
                        default:
                            paraValues[60] = null;
                            break;
                    }
                }

                if (bicAuthInfoData.tokenS8Data == null)
                {
                    paraValues[61] = null;
                    paraValues[62] = null;
                }
                else
                {
                    paraValues[61] = bicAuthInfoData.tokenS8Data?.AcctNum;
                    paraValues[62] = bicAuthInfoData.tokenS8Data?.ExpDat;
                }

                paraValues[63] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200TknActiveProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200TknActiveProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200TknActiveProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@t04ErrorFlg         , authReturnString[03]:@t04CrdVeryFlg
                // authReturnString[04]:@tQ8Tag16EMail       , authReturnString[05]:@tQ8Tag29IssConfigId
                // authReturnString[06]:@tQ8Tag36PanSeq      , authReturnString[07]:@tQ8Tag51ChVfyMethod
                // authReturnString[08]:@tQ8Tag56CvmErrCde   , authReturnString[09]:@tQ8Tag58CvmRsp
                // authReturnString[10]:@tQ8TagQEPhone       , authReturnString[11]:@tQ8TagQGRiskLvlResult
                // authReturnString[12]:@tknTxType          
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @t04ErrorFlg	                varchar(01) 	Output	 Field 63 Token04:Error-flag Token04.ErrorFlg
                // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 63 Token04:Crd-very-flag Token04.CrdVeryFlg
                // @tQ8Tag16EMail               varchar(64) 	Output   Field 63 Token Q8 SubToken Id 16 Acct E-Mail
                // @tQ8Tag29IssConfigId         varchar(30) 	Output	 Field 63 Token Q8 SubToken Id 29 Issuer Production Configuration Id
                // @tQ8Tag36PanSeq       	    varchar(10) 	Output	 Field 63 Token Q8 SubToken Id 36 Card Sequence Number
                // @tQ8Tag51ChVfyMethod      	varchar(186) 	Output	 Field 63 Token Q8 SubToken Id 51 CardHolder Verification Method
                // @tQ8Tag56CvmErrCde           varchar(08) 	Output	 Field 63 Token Q8 SubToken Id 56 Check Eligibility error code
                // @tQ8Tag58CvmRsp              varchar(02) 	Output	 Field 63 Token Q8 SubToken Id 58 Contactless use
                // @tQ8TagQEPhone      	        varchar(15) 	Output	 Field 63 Token Q8 SubToken Id QE Mobile Phone number
                // @tQ8TagQGRiskLvlResult       varchar(01) 	Output	 Field 63 Token Q8 SubToken Id QG Risk Level Result
                // @tknTxType  	                varchar(02) 	Output

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknActiveProcess, returnStrings, 4, 10));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200TknActiveProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200TknActiveProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0200TknAuthProcess Process Message Type 0200 Tokenization Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200TknAuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 70;
            const int outputParaCount = 18;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;   //isoP39RspAction
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoP52PinBlock;
                paraValues[26] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[27] = bicAuthInfoData.isoS127Pws;
                paraValues[28] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[29] = bicAuthInfoData.isoS127MpFlag;
                paraValues[30] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.token04Data.ErrorFlg;

                if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.token04Data.CrdVeryFlg;

                if (bicAuthInfoData.tokenB0Data?.OnBehalf == null)
                    paraValues[33] = null;
                else
                {
                    string dataString = bicAuthInfoData.tokenB0Data.OnBehalf;
                    var dataLength = dataString.Length;
                    int startPosition = 0;
                    StringBuilder sb = new StringBuilder();
                    while (startPosition < dataLength)
                    {
                        var copyDataLength = 0;
                        if ((startPosition + 3) > dataLength)
                            copyDataLength = dataLength - startPosition;
                        else
                            copyDataLength = 3;
                        if (startPosition == 0)
                        {
                            sb.Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        else
                        {
                            sb.Append(";")
                              .Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        startPosition += 3;
                    }
                    paraValues[33] = sb.ToString();
                }

                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.Atc == null)
                    paraValues[35] = null;
                else
                    paraValues[35] = bicAuthInfoData.tokenB2Data.Atc;


                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[36] = null;
                else
                    paraValues[36] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[37] = null;
                else
                    paraValues[37] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[38] = null;
                else
                    paraValues[38] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.ArqcVerify == null)
                    paraValues[39] = null;
                else
                    paraValues[39] = bicAuthInfoData.tokenB4Data.ArqcVerify;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[40] = null;
                else
                    paraValues[40] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[41] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[41] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[42] = null;
                else
                    paraValues[42] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[43] = null;
                else
                    paraValues[43] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[44] = null;
                else
                    paraValues[44] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                    paraValues[45] = null;
                else
                    paraValues[45] = bicAuthInfoData.tokenC0Data.CavvAavResults;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[46] = null;
                else
                    paraValues[46] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[47] = null;
                else
                    paraValues[47] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[48] = null;
                else
                    paraValues[48] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[49] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[49] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[49] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[50] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                else
                    paraValues[50] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[51] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                else
                    paraValues[51] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[52] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                else
                    paraValues[52] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[53] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[53] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[54] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[54] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[55] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[55] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[56] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[56] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[57] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[57] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[58] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[58] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[59] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[59] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[60] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[60] = null;

                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenS8Data?.AcctNum;

                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[62] = null;
                else
                    paraValues[62] = bicAuthInfoData.tokenS8Data?.ExpDat;

                if (bicAuthInfoData.tokenQ8Data == null)
                    paraValues[63] = null;
                else
                {
                    var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                    if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                    {
                        paraValues[63] = null;
                    }
                    else
                    {
                        string subTokenId = "59";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                            paraValues[63] = tokenQ8TLV[subTokenId];
                        else
                            paraValues[63] = null;
                    }
                }

                paraValues[64] = null;
                paraValues[65] = null;
                paraValues[66] = null;
                paraValues[67] = null;
                paraValues[68] = null;
                paraValues[69] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200TknAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200TknAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200TknAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@t04ErrorFlg
                // authReturnString[04]:@t04CrdVeryFlg       , authReturnString[05]:@tC5InstTxResp
                // authReturnString[06]:@tC5DownPayment      , authReturnString[07]:@tC5InstPayment
                // authReturnString[08]:@tC5InstFee          , authReturnString[09]:@tC5PointRedemption
                // authReturnString[10]:@tC5SignPoint        , authReturnString[11]:@tC5PointBalance
                // authReturnString[12]:@tC5PaidCreditAmt    , authReturnString[13]:@tC5LoyaltyTxInd
                // authReturnString[14]:@vdLinkNbr           , authReturnString[15]:@vdLinkNbrSeq
                // authReturnString[16]:@vdActionCode        , authReturnString[17]:@vdBlockAmt
                // Description
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

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200TknAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200TknAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0200TknAuthProcess Process Message Type 0200 Tokenization Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"></param>
        /// <param name="responseXML"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200TknAuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 70;
            const int outputParaCount = 18;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoP52PinBlock;
                paraValues[26] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[27] = bicAuthInfoData.isoS127Pws;
                paraValues[28] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[29] = bicAuthInfoData.isoS127MpFlag;
                paraValues[30] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.token04Data.ErrorFlg;

                if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.token04Data.CrdVeryFlg;

                if (bicAuthInfoData.tokenB0Data?.OnBehalf == null)
                    paraValues[33] = null;
                else
                {
                    string dataString = bicAuthInfoData.tokenB0Data.OnBehalf;
                    var dataLength = dataString.Length;
                    int startPosition = 0;
                    StringBuilder sb = new StringBuilder();
                    while (startPosition < dataLength)
                    {
                        var copyDataLength = 0;
                        if ((startPosition + 3) > dataLength)
                            copyDataLength = dataLength - startPosition;
                        else
                            copyDataLength = 3;
                        if (startPosition == 0)
                        {
                            sb.Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        else
                        {
                            sb.Append(";")
                              .Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        startPosition += 3;
                    }
                    paraValues[33] = sb.ToString();
                }

                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.Atc == null)
                    paraValues[35] = null;
                else
                    paraValues[35] = bicAuthInfoData.tokenB2Data.Atc;


                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[36] = null;
                else
                    paraValues[36] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[37] = null;
                else
                    paraValues[37] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[38] = null;
                else
                    paraValues[38] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.ArqcVerify == null)
                    paraValues[39] = null;
                else
                    paraValues[39] = bicAuthInfoData.tokenB4Data.ArqcVerify;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[40] = null;
                else
                    paraValues[40] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[41] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[41] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[42] = null;
                else
                    paraValues[42] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[43] = null;
                else
                    paraValues[43] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[44] = null;
                else
                    paraValues[44] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                    paraValues[45] = null;
                else
                    paraValues[45] = bicAuthInfoData.tokenC0Data.CavvAavResults;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[46] = null;
                else
                    paraValues[46] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[47] = null;
                else
                    paraValues[47] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[48] = null;
                else
                    paraValues[48] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[49] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[49] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[49] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[50] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                else
                    paraValues[50] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[51] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                else
                    paraValues[51] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[52] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                else
                    paraValues[52] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[53] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[53] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[54] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[54] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[55] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[55] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[56] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[56] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[57] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[57] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[58] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[58] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[59] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[59] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[60] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[60] = null;

                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenS8Data?.AcctNum;

                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[62] = null;
                else
                    paraValues[62] = bicAuthInfoData.tokenS8Data?.ExpDat;

                if (bicAuthInfoData.tokenQ8Data == null)
                    paraValues[63] = null;
                else
                {
                    var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                    if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                    {
                        paraValues[63] = null;
                    }
                    else
                    {
                        string subTokenId = "59";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                            paraValues[63] = tokenQ8TLV[subTokenId];
                        else
                            paraValues[63] = null;
                    }
                }

                if (responseXML.TxRs == null)
                {
                    paraValues[64] = authReturnString[14];
                    paraValues[65] = authReturnString[15];
                    paraValues[66] = authReturnString[16];
                    paraValues[69] = 0;
                }
                else
                {
                    paraValues[64] = responseXML.TxRs.AcctNo;
                    paraValues[65] = responseXML.TxRs.CardNo;
                    paraValues[66] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[69] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[69] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[67] = authReturnString[17];
                paraValues[68] = responseXML.ResponseHeader.StatusCode;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200TknAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200TknAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200TknAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     ,
                // authReturnString[03]:@t04ErrorFlg         , authReturnString[04]:@t04CrdVeryFlg
                // authReturnString[05]:@tC5InstTxResp       , authReturnString[06]:@tC5DownPayment
                // authReturnString[07]:@tC5InstPayment      , authReturnString[08]:@tC5InstFee
                // authReturnString[09]:@tC5PointRedemption  , authReturnString[10]:@tC5SignPoint
                // authReturnString[11]:@tC5PointBalance     , authReturnString[12]:@tC5PaidCreditAmt
                // authReturnString[13]:@tC5LoyaltyTxInd     , authReturnString[14]:@vdLinkNbr 
                // authReturnString[15]:@vdLinkNbrSeq        , authReturnString[16]:@vdActionCode
                // authReturnString[17]:@vdBlockAmt
                // Description
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

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200TknAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200TknAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0200TknOtpProcess Process Message Type 0200 Tokenz1ation Otp Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200TknOtpProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 37;
            const int outputParaCount = 8;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknOtpProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = bicAuthInfoData.isoP41TermNbr;
                paraValues[17] = bicAuthInfoData.isoP42Merchant;
                paraValues[18] = bicAuthInfoData.isoP43MerName;
                paraValues[19] = bicAuthInfoData.isoP43MerCity;
                paraValues[20] = bicAuthInfoData.isoP43LocCountry;
                if (bicAuthInfoData.tokenF4Data == null)
                    paraValues[21] = null;
                else
                {
                    paraValues[21] = bicAuthInfoData.tokenF4Data?.WalletIndData;
                }
                var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                if (bicAuthInfoData.tokenQ8Data == null)
                {
                    paraValues[22] = null;
                    paraValues[23] = null;
                    paraValues[24] = null;
                    paraValues[25] = null;
                    paraValues[26] = null;
                    paraValues[27] = null;
                    paraValues[28] = null;
                    paraValues[29] = null;
                    paraValues[30] = null;
                    paraValues[31] = null;
                    paraValues[32] = null;
                    paraValues[33] = null;
                    paraValues[34] = null;
                    paraValues[35] = null;
                }
                else
                {
                    var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                    if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                    {
                        paraValues[22] = null;
                        paraValues[23] = null;
                        paraValues[24] = null;
                        paraValues[25] = null;
                        paraValues[26] = null;
                        paraValues[27] = null;
                        paraValues[28] = null;
                        paraValues[29] = null;
                        paraValues[30] = null;
                        paraValues[31] = null;
                        paraValues[32] = null;
                        paraValues[33] = null;
                        paraValues[34] = null;
                        paraValues[35] = null;
                    }
                    else
                    {
                        int arrayIndex = 22;
                        string subTokenId = "07";

                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 23;
                        subTokenId = "08";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 24;
                        subTokenId = "27";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 25;
                        subTokenId = "28";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 26;
                        switch (creditCardType)
                        {
                            case CreditCardType.MasterCard:
                                subTokenId = "30";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                                }
                                break;
                            case CreditCardType.Visa:
                                subTokenId = "38";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 27;
                        subTokenId = "50";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 28;
                        subTokenId = "Q4";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 29;
                        subTokenId = "QA";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 30;
                        subTokenId = "QF";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                    }
                }

                if (bicAuthInfoData.tokenQ9Data == null)
                    paraValues[31] = null;
                else
                {
                    bicAuthInfoData.tokenQ9Data.SubDataDecode();
                    switch (creditCardType)
                    {
                        case CreditCardType.Visa:
                            paraValues[31] = bicAuthInfoData.tokenQ9Data?.VisaMsgRsnCde;
                            break;
                        case CreditCardType.MasterCard:
                            paraValues[31] = bicAuthInfoData.tokenQ9Data?.MastercardAdvcDetlCde;
                            break;
                        default:
                            paraValues[31] = null;
                            break;
                    }
                }

                paraValues[32] = null;
                paraValues[33] = null;
                paraValues[34] = null;
                paraValues[35] = null;
                paraValues[36] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200TknOtpProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200TknOtpProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200TknOtpProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@tQ8Tag07Ac          , authReturnString[03]:@otpSend
                // authReturnString[04]:@tokenMsg            , authReturnString[05]:@custName
                // authReturnString[06]:@cellPhone           , authReturnString[07]:@custSeq
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @tQ8Tag07Ac                  varchar(12) 	Output   Field 63 Token Q8 SubToken Id 07 Activity Code
                // @otpSend                     varchar(01) 	Output	 簡訊使用
                // @tokenMsg       	            varchar(100) 	Output	 簡訊使用
                // @custName        	        varchar(20) 	Output	 簡訊使用
                // @cellPhone               	varchar(20) 	Output	 簡訊使用
                // @custSeq                	    varchar(20) 	Output	 簡訊使用

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknOtpProcess, returnStrings, 2, 4, 5, 6, 7));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200TknOtpProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200TknOtpProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0200TknEventProcess Process Message Type 0200  Tokenization Event Process For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200TknEventProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 62;
            const int outputParaCount = 7;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknEventProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP7TxDte;
                paraValues[05] = bicAuthInfoData.isoP7TxTime;
                paraValues[06] = bicAuthInfoData.isoP11TraceNo;
                paraValues[07] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[08] = bicAuthInfoData.isoP37RefNbr;
                paraValues[09] = bicAuthInfoData.isoP38AuthCode;
                paraValues[10] = bicAuthInfoData.isoP39Rsp;
                paraValues[11] = null;
                if (bicAuthInfoData.tokenF4Data?.WalletIndData != null)
                    paraValues[11] = bicAuthInfoData.tokenF4Data.WalletIndData;

                var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                if (bicAuthInfoData.tokenQ8Data == null)
                {
                    paraValues[12] = 0;
                    paraValues[13] = null;
                    paraValues[14] = null;
                    paraValues[15] = 0;
                    paraValues[16] = 0;
                    paraValues[17] = null;
                    paraValues[18] = null;
                    paraValues[19] = null;
                    paraValues[20] = null;
                    paraValues[21] = null;
                    paraValues[22] = null;
                    paraValues[23] = null;
                    paraValues[24] = null;
                    paraValues[25] = null;
                    paraValues[26] = null;
                    paraValues[27] = null;
                    paraValues[28] = null;
                    paraValues[29] = null;
                    paraValues[30] = null;
                    paraValues[31] = null;
                    paraValues[32] = null;
                    paraValues[33] = null;
                    paraValues[34] = null;
                    paraValues[35] = null;
                    paraValues[36] = null;
                    paraValues[37] = null;
                    paraValues[38] = null;
                    paraValues[39] = null;
                    paraValues[40] = null;
                    paraValues[41] = null;
                    paraValues[42] = null;
                    paraValues[43] = null;
                    paraValues[44] = null;
                    paraValues[45] = null;
                    paraValues[46] = null;
                    paraValues[47] = null;
                    paraValues[48] = null;
                    paraValues[49] = null;
                    paraValues[50] = null;
                    paraValues[51] = null;
                    paraValues[52] = null;
                    paraValues[53] = null;
                }
                else
                {
                    var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                    if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                    {
                        paraValues[12] = 0;
                        paraValues[13] = null;
                        paraValues[14] = null;
                        paraValues[15] = 0;
                        paraValues[16] = 0;
                        paraValues[17] = null;
                        paraValues[18] = null;
                        paraValues[19] = null;
                        paraValues[20] = null;
                        paraValues[21] = null;
                        paraValues[22] = null;
                        paraValues[23] = null;
                        paraValues[24] = null;
                        paraValues[25] = null;
                        paraValues[26] = null;
                        paraValues[27] = null;
                        paraValues[28] = null;
                        paraValues[29] = null;
                        paraValues[30] = null;
                        paraValues[31] = null;
                        paraValues[32] = null;
                        paraValues[33] = null;
                        paraValues[34] = null;
                        paraValues[35] = null;
                        paraValues[36] = null;
                        paraValues[37] = null;
                        paraValues[38] = null;
                        paraValues[39] = null;
                        paraValues[40] = null;
                        paraValues[41] = null;
                        paraValues[42] = null;
                        paraValues[43] = null;
                        paraValues[44] = null;
                        paraValues[45] = null;
                        paraValues[46] = null;
                        paraValues[47] = null;
                        paraValues[48] = null;
                        paraValues[49] = null;
                        paraValues[50] = null;
                        paraValues[51] = null;
                        paraValues[52] = null;
                        paraValues[53] = null;
                    }
                    else
                    {
                        int arrayIndex = 12;
                        string subTokenId = "02";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 13;
                        subTokenId = "04";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 14;
                        subTokenId = "05";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 15;
                        subTokenId = "09";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 16;
                        subTokenId = "10";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 17;
                        subTokenId = "11";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 18;
                        subTokenId = "12";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 19;
                        subTokenId = "13";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 20;
                        subTokenId = "16";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 21;
                        subTokenId = "18";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 22;
                        subTokenId = "19";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 23;
                        subTokenId = "20";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 24;
                        subTokenId = "21";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 25;
                        subTokenId = "22";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 26;
                        subTokenId = "23";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 27;
                        subTokenId = "24";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 28;
                        subTokenId = "25";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 29;
                        subTokenId = "26";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 30;
                        subTokenId = "27";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 31;
                        subTokenId = "28";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 32;
                        subTokenId = "29";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 33;
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                                subTokenId = "38";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            case CreditCardType.MasterCard:
                                subTokenId = "30";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 34;
                        subTokenId = "31";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 35;
                        subTokenId = "32";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 36;
                        subTokenId = "33";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 37;
                        subTokenId = "36";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 38;
                        subTokenId = "37";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 39;
                        subTokenId = "39";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 40;
                        subTokenId = "40";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 41;
                        subTokenId = "56";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 42;
                        subTokenId = "57";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 43;
                        subTokenId = "58";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 44;
                        subTokenId = "59";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 45;
                        subTokenId = "60";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 46;
                        subTokenId = "62";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 47;
                        subTokenId = "63";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 48;
                        subTokenId = "Q3";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 49;
                        subTokenId = "Q4";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 50;
                        subTokenId = "Q6";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 51;
                        subTokenId = "Q9";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 52;
                        subTokenId = "QA";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 53;
                        subTokenId = "QG";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }
                    }
                }

                if (bicAuthInfoData.tokenQ9Data == null)
                    paraValues[54] = null;
                else
                {
                    bicAuthInfoData.tokenQ9Data.SubDataDecode();
                    switch (creditCardType)
                    {
                        case CreditCardType.Visa:
                            paraValues[54] = bicAuthInfoData.tokenQ9Data?.VisaMsgRsnCde;
                            break;
                        case CreditCardType.MasterCard:
                            paraValues[54] = bicAuthInfoData.tokenQ9Data?.MastercardAdvcDetlCde;
                            break;
                        default:
                            paraValues[54] = null;
                            break;
                    }
                }

                paraValues[55] = null;
                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[55] = null;
                else
                    paraValues[55] = bicAuthInfoData.tokenS8Data?.AcctNum;

                paraValues[56] = null;
                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[56] = null;
                else
                    paraValues[56] = bicAuthInfoData.tokenS8Data?.ExpDat;

                paraValues[57] = null;
                paraValues[58] = null;
                paraValues[59] = null;
                paraValues[60] = null;
                paraValues[61] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start NCCC0200TknEventProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   NCCC0200TknEventProcess Display Store Procedure Input Data  ---");
                }

                // NCCC0200TknEventProcess Return String Arrays Information
                // Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@otpSend             , authReturnString[03]:@tokenMsg
                // authReturnString[04]:@custName            , authReturnString[05]:@cellPhone
                // authReturnString[06]:@custSeq
                // Description
                // @isoP38AuthCode              varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @otpSend                     varchar(01) 	Output	 簡訊使用
                // @tokenMsg         	        varchar(100) 	Output	 簡訊使用
                // @custName      	            varchar(20) 	Output	 簡訊使用
                // @cellPhone      	            varchar(20) 	Output	 簡訊使用
                // @custSeq      	            varchar(20) 	Output	 簡訊使用

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200TknEventProcess, returnStrings, 3, 4, 5, 6));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NCCC0200TknEventProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"NCCC0200TknEventProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0200HceAuthProcess Process Message Type 0200 Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="data9F10Array">The data9 F10 array.</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0200HceAuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] data9F10Array)
        {
            const int storeProcedureParameterCount = 62;
            const int outputParaCount = 11;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200HceAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = bicAuthInfoData.isoP41TermNbr;
                paraValues[20] = bicAuthInfoData.isoP42Merchant;
                paraValues[21] = bicAuthInfoData.isoP43MerName;
                paraValues[22] = bicAuthInfoData.isoP43MerCity;
                paraValues[23] = bicAuthInfoData.isoP43LocCountry;
                paraValues[24] = bicAuthInfoData.isoP52PinBlock;
                paraValues[25] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[26] = bicAuthInfoData.isoS127Pws;
                paraValues[27] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[28] = bicAuthInfoData.isoS127MpFlag;
                paraValues[29] = bicAuthInfoData.isoS127Id;
                paraValues[30] = bicAuthInfoData.pvvCheck;
                paraValues[31] = bicAuthInfoData.pvvChkResult;

                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.IssApplDataLgth == null)
                {
                    paraValues[33] = null;
                }
                else
                {
                    int issApplDataLgth = Convert.ToInt32(bicAuthInfoData.tokenB2Data.IssApplDataLgth);
                    var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       bicAuthInfoData.tokenB2Data.IssApplData,
                                                                       creditCardType).Split(';');
                    if (ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[33] = ColaCommUtility.HexString2BinString(data9F10[2]);
                    }
                    else
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[33] = null;
                    }
                }

                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[35] = null;
                else
                    paraValues[35] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[36] = null;
                else
                    paraValues[36] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[37] = null;
                else
                    paraValues[37] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[38] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[38] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[39] = null;
                else
                    paraValues[39] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[40] = null;
                else
                    paraValues[40] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[41] = null;
                else
                    paraValues[41] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                    paraValues[42] = null;
                else
                    paraValues[42] = bicAuthInfoData.tokenC0Data.CavvAavResults;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[43] = null;
                else
                    paraValues[43] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[44] = null;
                else
                    paraValues[44] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[45] = null;
                else
                    paraValues[45] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[46] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[46] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[46] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[47] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxnRespCde;
                else
                    paraValues[47] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[48] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).SettlementFlag;
                else
                    paraValues[48] = null;

                if ((bicAuthInfoData.tokenC5Data) is TokenNCCCInstallmentC5)
                    paraValues[49] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).PeriodNumber;
                else
                    paraValues[49] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[50] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[50] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[51] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[51] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[52] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[52] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[53] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[53] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[54] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[54] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[55] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[55] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[56] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[56] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[57] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[57] = null;

                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[58] = null;
                else
                    paraValues[58] = bicAuthInfoData.tokenS8Data?.AcctNum;

                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[59] = null;
                else
                    paraValues[59] = bicAuthInfoData.tokenS8Data?.ExpDat;

                if (bicAuthInfoData.tokenS8Data?.AcVeryRslt == null)
                    paraValues[60] = null;
                else
                    paraValues[60] = bicAuthInfoData.tokenS8Data?.AcVeryRslt;

                if (bicAuthInfoData.tokenS8Data?.FraudChkRslt == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenS8Data?.FraudChkRslt;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0200HceAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0200HceAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0200HceAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@tC5InstTxResp       , authReturnString[03]:@tC5DownPayment
                // authReturnString[04]:@tC5InstPayment      , authReturnString[05]:@tC5InstFee
                // authReturnString[06]:@tC5PointRedemption  , authReturnString[07]:@tC5SignPoint
                // authReturnString[08]:@tC5PointBalance     , authReturnString[09]:@tC5PaidCreditAmt
                // authReturnString[10]:@tC5LoyaltyTxInd     
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
                // @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
                // @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
                // @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
                // @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
                // @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
                // @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
                // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
                // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0200HceAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0200HceAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0200HceAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0220AuthProcess Process Message Type 0220 Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0220AuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 53;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[26] = bicAuthInfoData.isoS127Pws;
                paraValues[27] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[28] = bicAuthInfoData.isoS127MpFlag;
                paraValues[29] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[30] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[30] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[33] = null;
                else
                    paraValues[33] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[35] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[35] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[35] = null;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[44] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[44] = null;
                    else
                        paraValues[44] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[45] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[45] = null;
                    else
                        paraValues[45] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[46] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }
                paraValues[47] = null;
                paraValues[48] = null;
                paraValues[49] = null;
                paraValues[50] = null;
                paraValues[51] = null;
                paraValues[52] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0220AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0220AuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0220AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq        , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0220AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0220AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0220AuthProcess Process Message Type 0220 Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="responseXML">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0220AuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 53;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[26] = bicAuthInfoData.isoS127Pws;
                paraValues[27] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[28] = bicAuthInfoData.isoS127MpFlag;
                paraValues[29] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[30] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[30] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[33] = null;
                else
                    paraValues[33] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[35] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[35] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[35] = null;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[44] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[44] = null;
                    else
                        paraValues[44] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[45] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[45] = null;
                    else
                        paraValues[45] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[46] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }
                if (responseXML.TxRs == null)
                {
                    paraValues[47] = authReturnString[03];
                    paraValues[48] = authReturnString[04];
                    paraValues[49] = authReturnString[05];
                    paraValues[52] = 0;
                }
                else
                {
                    paraValues[47] = responseXML.TxRs.AcctNo;
                    paraValues[48] = responseXML.TxRs.CardNo;
                    paraValues[49] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[52] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[52] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[50] = authReturnString[06];
                paraValues[51] = responseXML.ResponseHeader.StatusCode;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0220AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0220AuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0220AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq        , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0220AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0220AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0220TknAuthProcess Process Message Type 0220 Tokenization Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0220TknAuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 56;
            const int outputParaCount = 16;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220TknAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[26] = bicAuthInfoData.isoS127Pws;
                paraValues[27] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[28] = bicAuthInfoData.isoS127MpFlag;
                paraValues[29] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[30] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[30] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[33] = null;
                else
                    paraValues[33] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[35] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[35] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[35] = null;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[44] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[44] = null;
                    else
                        paraValues[44] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[45] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[45] = null;
                    else
                        paraValues[45] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[46] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }

                paraValues[47] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.TransactionId == null)
                        paraValues[47] = null;
                    else
                        paraValues[47] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.TransactionId;
                }
                else if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.TransactionId == null)
                        paraValues[47] = null;
                    else
                        paraValues[47] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).TransactionId;
                }

                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[48] = null;
                else
                    paraValues[48] = bicAuthInfoData.tokenS8Data?.AcctNum;

                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[49] = null;
                else
                    paraValues[49] = bicAuthInfoData.tokenS8Data?.ExpDat;

                paraValues[50] = null;
                paraValues[51] = null;
                paraValues[52] = null;
                paraValues[53] = null;
                paraValues[54] = null;
                paraValues[55] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0220TknAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0220TknAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0220TknAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@tC5InstTxResp
                // authReturnString[04]:@tC5DownPayment      , authReturnString[05]:@tC5InstPayment
                // authReturnString[06]:@tC5InstFee          , authReturnString[07]:@tC5PointRedemption
                // authReturnString[08]:@tC5SignPoint        , authReturnString[09]:@tC5PointBalance
                // authReturnString[10]:@tC5PaidCreditAmt    , authReturnString[11]:@tC5LoyaltyTxInd
                // authReturnString[12]:@vdLinkNbr           , authReturnString[13]:@vdLinkNbrSeq
                // authReturnString[14]:@vdActionCode        , authReturnString[15]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction		        varchar(01)     Output 	 --對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可) 20231117
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

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220TknAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0220TknAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0220TknAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0220TknAuthProcess Process Message Type 0220 Tokenization Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"></param>
        /// <param name="responseXML"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0220TknAuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 56;
            const int outputParaCount = 16;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220TknAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[26] = bicAuthInfoData.isoS127Pws;
                paraValues[27] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[28] = bicAuthInfoData.isoS127MpFlag;
                paraValues[29] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[30] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[30] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[33] = null;
                else
                    paraValues[33] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[35] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[35] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[35] = null;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[44] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[44] = null;
                    else
                        paraValues[44] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[45] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[45] = null;
                    else
                        paraValues[45] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[46] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }

                paraValues[47] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.TransactionId == null)
                        paraValues[47] = null;
                    else
                        paraValues[47] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.TransactionId;
                }
                else if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.TransactionId == null)
                        paraValues[47] = null;
                    else
                        paraValues[47] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).TransactionId;
                }

                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[48] = null;
                else
                    paraValues[48] = bicAuthInfoData.tokenS8Data?.AcctNum;

                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[49] = null;
                else
                    paraValues[49] = bicAuthInfoData.tokenS8Data?.ExpDat;

                if (responseXML.TxRs == null)
                {
                    paraValues[50] = authReturnString[12];
                    paraValues[51] = authReturnString[13];
                    paraValues[52] = authReturnString[14];
                    paraValues[55] = 0;
                }
                else
                {
                    paraValues[50] = responseXML.TxRs.AcctNo;
                    paraValues[51] = responseXML.TxRs.CardNo;
                    paraValues[52] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[55] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[55] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[53] = authReturnString[15];
                paraValues[54] = responseXML.ResponseHeader.StatusCode;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0220TknAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0220TknAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0220TknAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@tC5InstTxResp
                // authReturnString[04]:@tC5DownPayment      , authReturnString[05]:@tC5InstPayment
                // authReturnString[06]:@tC5InstFee          , authReturnString[07]:@tC5PointRedemption
                // authReturnString[08]:@tC5SignPoint        , authReturnString[09]:@tC5PointBalance
                // authReturnString[10]:@tC5PaidCreditAmt    , authReturnString[11]:@tC5LoyaltyTxInd
                // authReturnString[12]:@vdLinkNbr           , authReturnString[13]:@vdLinkNbrSeq
                // authReturnString[14]:@vdActionCode        , authReturnString[15]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction		        varchar(01)     Output 	 --對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可) 20231117
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

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220TknAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0220TknAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0220TknAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0220HceAuthProcess Process Message Type 0220 Authorization Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0220HceAuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 49;
            const int outputParaCount = 11;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220HceAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = bicAuthInfoData.isoP41TermNbr;
                paraValues[20] = bicAuthInfoData.isoP42Merchant;
                paraValues[21] = bicAuthInfoData.isoP43MerName;
                paraValues[22] = bicAuthInfoData.isoP43MerCity;
                paraValues[23] = bicAuthInfoData.isoP43LocCountry;
                paraValues[24] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[25] = bicAuthInfoData.isoS127Pws;
                paraValues[26] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[27] = bicAuthInfoData.isoS127MpFlag;
                paraValues[28] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[29] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[29] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[30] = null;
                else
                    paraValues[30] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[33] = null;
                else
                    paraValues[33] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[34] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[34] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[34] = null;
                }
                paraValues[35] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[35] = null;
                    else
                        paraValues[35] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[44] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[44] = null;
                    else
                        paraValues[44] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[45] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[45] = null;
                    else
                        paraValues[45] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }

                paraValues[46] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.TransactionId == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.TransactionId;
                }
                else if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.TransactionId == null)
                        paraValues[46] = null;
                    else
                        paraValues[46] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).TransactionId;
                }

                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[47] = null;
                else
                    paraValues[47] = bicAuthInfoData.tokenS8Data?.AcctNum;

                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[48] = null;
                else
                    paraValues[48] = bicAuthInfoData.tokenS8Data?.ExpDat;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0220HceAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0220HceAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0220HceAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@tC5InstTxResp       , authReturnString[03]:@tC5DownPayment
                // authReturnString[04]:@tC5InstPayment      , authReturnString[05]:@tC5InstFee
                // authReturnString[06]:@tC5PointRedemption  , authReturnString[07]:@tC5SignPoint
                // authReturnString[08]:@tC5PointBalance     , authReturnString[09]:@tC5PaidCreditAmt
                // authReturnString[10]:@tC5LoyaltyTxInd
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 Or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
                // @tC5DownPayment      	    varchar(08) 	Output	 Field 57 Or 63 Token C5 Down Payment<分期,首期>
                // @tC5InstPayment      	    varchar(08) 	Output	 Field 57 Or 63 Token C5 Installment Payment<分期, 每期>
                // @tC5InstFee          	    varchar(06) 	Output	 Field 57 Or 63 Token C5 Formality-Fee<分期, 手續費/利息>
                // @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 Or 63 Token C5 Point of Redemption<紅利,折扺點數>
                // @tC5SignPoint        	    varchar(01) 	Output	 Field 57 Or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
                // @tC5PointBalance     	    varchar(08) 	Output	 Field 57 Or 63 Token C5 Point of Balance<紅利, 餘點>
                // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 Or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
                // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 Or 63 Token C5 <紅利註記>

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0220HceAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0220HceAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0220HceAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0420AuthProcess Process Message Type 0420 Reversal Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0420AuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 37;
            const int outputParaCount = 6;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = null;
                paraValues[17] = bicAuthInfoData.isoP41TermNbr;
                paraValues[18] = bicAuthInfoData.isoP42Merchant;
                paraValues[19] = bicAuthInfoData.isoP43MerName;
                paraValues[20] = bicAuthInfoData.isoP43MerCity;
                paraValues[21] = bicAuthInfoData.isoP43LocCountry;
                paraValues[22] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[23] = bicAuthInfoData.isoS127Pws;
                paraValues[24] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[25] = bicAuthInfoData.isoS127MpFlag;
                paraValues[26] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[27] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[27] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[28] = null;
                else
                    paraValues[28] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                paraValues[29] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[29] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[29] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[29] = null;
                }
                paraValues[30] = null;
                paraValues[31] = null;
                paraValues[32] = null;
                paraValues[33] = null;
                paraValues[34] = null;
                paraValues[35] = null;
                paraValues[36] = null;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[30] = null;
                    else
                        paraValues[30] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0420AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0420AuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0420AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkNbr           , authReturnString[03]:@vdLinkNbrSeq
                // authReturnString[04]:@vdActionCode        , authReturnString[05]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0420AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0420AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0420AuthProcess Process Message Type 0420 Reversal Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"></param>
        /// <param name="responseXML"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0420AuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 37;
            const int outputParaCount = 6;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = null;
                paraValues[17] = bicAuthInfoData.isoP41TermNbr;
                paraValues[18] = bicAuthInfoData.isoP42Merchant;
                paraValues[19] = bicAuthInfoData.isoP43MerName;
                paraValues[20] = bicAuthInfoData.isoP43MerCity;
                paraValues[21] = bicAuthInfoData.isoP43LocCountry;
                paraValues[22] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[23] = bicAuthInfoData.isoS127Pws;
                paraValues[24] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[25] = bicAuthInfoData.isoS127MpFlag;
                paraValues[26] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[27] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[27] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[28] = null;
                else
                    paraValues[28] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                paraValues[29] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[29] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[29] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[29] = null;
                }
                paraValues[30] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[30] = null;
                    else
                        paraValues[30] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                if (responseXML.TxRs == null)
                {
                    paraValues[31] = authReturnString[02];
                    paraValues[32] = authReturnString[03];
                    paraValues[33] = authReturnString[04];
                    paraValues[36] = 0;
                }
                else
                {
                    paraValues[31] = responseXML.TxRs.AcctNo;
                    paraValues[32] = responseXML.TxRs.CardNo;
                    paraValues[33] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[36] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[36] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[34] = authReturnString[05];
                paraValues[35] = responseXML.ResponseHeader.StatusCode;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0420AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0420AuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0420AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkNbr           , authReturnString[03]:@vdLinkNbrSeq
                // authReturnString[04]:@vdActionCode        , authReturnString[05]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0420AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0420AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0420TknActiveProcesss Process Message Type 0420 Tokenization Active Reversal Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0420TknActiveProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 23;
            const int outputParaCount = 1;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420TknActiveProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = bicAuthInfoData.isoP41TermNbr;
                paraValues[17] = bicAuthInfoData.isoP42Merchant;
                paraValues[18] = bicAuthInfoData.isoP43MerName;
                paraValues[19] = bicAuthInfoData.isoP43MerCity;
                paraValues[20] = bicAuthInfoData.isoP43LocCountry;
                var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                {
                    paraValues[21] = null;
                    paraValues[22] = null;
                }
                else
                {
                    int arrayIndex = 21;
                    string subTokenId = "19";
                    if (tokenQ8TLV.ContainsKey(subTokenId))
                    {
                        paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        ;
                    }
                    else
                    {
                        paraValues[arrayIndex] = null;
                    }

                    arrayIndex = 22;
                    subTokenId = "27";
                    if (tokenQ8TLV.ContainsKey(subTokenId))
                    {
                        paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        ;
                    }
                    else
                    {
                        paraValues[arrayIndex] = null;
                    }
                }

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0420TknActiveProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0420TknActiveProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0420TknActiveProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420TknActiveProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0420TknActiveProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0420TknActiveProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0420TknAuthProcess Process Message Type 0420 Tokenization Reversal Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0420TknAuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 39;
            const int outputParaCount = 6;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420TknAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = null;
                paraValues[17] = bicAuthInfoData.isoP41TermNbr;
                paraValues[18] = bicAuthInfoData.isoP42Merchant;
                paraValues[19] = bicAuthInfoData.isoP43MerName;
                paraValues[20] = bicAuthInfoData.isoP43MerCity;
                paraValues[21] = bicAuthInfoData.isoP43LocCountry;
                paraValues[22] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[23] = bicAuthInfoData.isoS127Pws;
                paraValues[24] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[25] = bicAuthInfoData.isoS127MpFlag;
                paraValues[26] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[27] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[27] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[28] = null;
                else
                    paraValues[28] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                paraValues[29] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[29] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[29] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[29] = null;
                }

                paraValues[30] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[30] = null;
                    else
                        paraValues[30] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }

                paraValues[31] = null;
                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenS8Data?.AcctNum;

                paraValues[32] = null;
                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenS8Data?.ExpDat;

                paraValues[33] = null;
                paraValues[34] = null;
                paraValues[35] = null;
                paraValues[36] = null;
                paraValues[37] = null;
                paraValues[38] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0420TknAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0420TknAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0420TknAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkNbr           , authReturnString[03]:@vdLinkNbrSeq
                // authReturnString[04]:@vdActionCode        , authReturnString[05]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 #39 initial 時請放null即可
                // isoP39RspAction		        varchar(01)     Output 	 --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420TknAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0420TknAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0420TknAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0420TknAuthProcess Process Message Type 0420 Tokenization Reversal Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"></param>
        /// <param name="responseXML"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0420TknAuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 39;
            const int outputParaCount = 6;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420TknAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = null;
                paraValues[17] = bicAuthInfoData.isoP41TermNbr;
                paraValues[18] = bicAuthInfoData.isoP42Merchant;
                paraValues[19] = bicAuthInfoData.isoP43MerName;
                paraValues[20] = bicAuthInfoData.isoP43MerCity;
                paraValues[21] = bicAuthInfoData.isoP43LocCountry;
                paraValues[22] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[23] = bicAuthInfoData.isoS127Pws;
                paraValues[24] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[25] = bicAuthInfoData.isoS127MpFlag;
                paraValues[26] = bicAuthInfoData.isoS127Id;

                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[27] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[27] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[28] = null;
                else
                    paraValues[28] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                paraValues[29] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[29] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[29] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[29] = null;
                }

                paraValues[30] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[30] = null;
                    else
                        paraValues[30] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }

                paraValues[31] = null;
                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenS8Data?.AcctNum;

                paraValues[32] = null;
                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenS8Data?.ExpDat;

                paraValues[33] = null;
                paraValues[34] = null;
                paraValues[35] = null;
                paraValues[36] = null;
                paraValues[37] = null;
                paraValues[38] = null;

                if (responseXML.TxRs == null)
                {
                    paraValues[33] = authReturnString[02];
                    paraValues[34] = authReturnString[03];
                    paraValues[35] = authReturnString[04];
                    paraValues[38] = 0;
                }
                else
                {
                    paraValues[33] = responseXML.TxRs.AcctNo;
                    paraValues[34] = responseXML.TxRs.CardNo;
                    paraValues[35] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[38] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[38] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[36] = authReturnString[05];
                paraValues[37] = responseXML.ResponseHeader.StatusCode;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0420TknAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0420TknAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0420TknAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkNbr           , authReturnString[03]:@vdLinkNbrSeq
                // authReturnString[04]:@vdActionCode        , authReturnString[05]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 #39 initial 時請放null即可
                // isoP39RspAction		        varchar(01)     Output 	 --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420TknAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0420TknAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0420TknAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0420HceAuthProcess Process Message Type 0420 Tokenization Reversal Flow For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0420HceAuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 32;
            const int outputParaCount = 1;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420HceAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = bicAuthInfoData.isoP41TermNbr;
                paraValues[17] = bicAuthInfoData.isoP42Merchant;
                paraValues[18] = bicAuthInfoData.isoP43MerName;
                paraValues[19] = bicAuthInfoData.isoP43MerCity;
                paraValues[20] = bicAuthInfoData.isoP43LocCountry;
                paraValues[21] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[22] = bicAuthInfoData.isoS127Pws;
                paraValues[23] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[24] = bicAuthInfoData.isoS127MpFlag;
                paraValues[25] = bicAuthInfoData.isoS127Id;
                if (bicAuthInfoData.tokenBEData?.Amount == null)
                    paraValues[26] = null;
                else
                {
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(bicAuthInfoData.tokenBEData?.OrigCurrencyCode))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(bicAuthInfoData.tokenBEData?.OrigCurrencyCode);
                    }
                    paraValues[26] = Convert.ToDecimal(bicAuthInfoData.tokenBEData.Amount) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                if (bicAuthInfoData.tokenBEData?.OrigCurrencyCode == null)
                    paraValues[27] = null;
                else
                    paraValues[27] = bicAuthInfoData.tokenBEData.OrigCurrencyCode;

                paraValues[28] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[28] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[28] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[28] = null;
                }

                paraValues[29] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[29] = null;
                    else
                        paraValues[29] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }

                paraValues[30] = null;
                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[30] = null;
                else
                    paraValues[30] = bicAuthInfoData.tokenS8Data?.AcctNum;

                paraValues[31] = null;
                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenS8Data?.ExpDat;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Nccc0420HceAuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Nccc0420HceAuthProcess Display Store Procedure Input Data  ---");
                }

                // Nccc0420HceAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0420HceAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Nccc0420HceAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Nccc0420HceAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Nccc0620TknEventProcess Process Message Type 0620  Tokenization Event Process For NCCC
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Nccc0620TknEventProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 61;
            const int outputParaCount = 6;

            try
            {

                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureNCCC0620TknEventProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP7TxDte;
                paraValues[05] = bicAuthInfoData.isoP7TxTime;
                paraValues[06] = bicAuthInfoData.isoP11TraceNo;
                paraValues[07] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[08] = bicAuthInfoData.isoP37RefNbr;
                paraValues[09] = bicAuthInfoData.isoP39Rsp;
                paraValues[10] = null;
                if (bicAuthInfoData.tokenF4Data?.WalletIndData != null)
                    paraValues[10] = bicAuthInfoData.tokenF4Data.WalletIndData;

                var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                if (bicAuthInfoData.tokenQ8Data == null)
                {
                    paraValues[11] = 0;
                    paraValues[12] = null;
                    paraValues[13] = null;
                    paraValues[14] = 0;
                    paraValues[15] = 0;
                    paraValues[16] = null;
                    paraValues[17] = null;
                    paraValues[18] = null;
                    paraValues[19] = null;
                    paraValues[20] = null;
                    paraValues[21] = null;
                    paraValues[22] = null;
                    paraValues[23] = null;
                    paraValues[24] = null;
                    paraValues[25] = null;
                    paraValues[26] = null;
                    paraValues[27] = null;
                    paraValues[28] = null;
                    paraValues[29] = null;
                    paraValues[30] = null;
                    paraValues[31] = null;
                    paraValues[32] = null;
                    paraValues[33] = null;
                    paraValues[34] = null;
                    paraValues[35] = null;
                    paraValues[36] = null;
                    paraValues[37] = null;
                    paraValues[38] = null;
                    paraValues[39] = null;
                    paraValues[40] = null;
                    paraValues[41] = null;
                    paraValues[42] = null;
                    paraValues[43] = null;
                    paraValues[44] = null;
                    paraValues[45] = null;
                    paraValues[46] = null;
                    paraValues[47] = null;
                    paraValues[48] = null;
                    paraValues[49] = null;
                    paraValues[50] = null;
                    paraValues[51] = null;
                    paraValues[52] = null;
                }
                else
                {
                    var tokenQ8TLV = bicAuthInfoData.tokenQ8Data.GetSubTokenData();
                    if ((tokenQ8TLV == null) || (tokenQ8TLV.Count == 0))
                    {
                        paraValues[11] = 0;
                        paraValues[12] = null;
                        paraValues[13] = null;
                        paraValues[14] = 0;
                        paraValues[15] = 0;
                        paraValues[16] = null;
                        paraValues[17] = null;
                        paraValues[18] = null;
                        paraValues[19] = null;
                        paraValues[20] = null;
                        paraValues[21] = null;
                        paraValues[22] = null;
                        paraValues[23] = null;
                        paraValues[24] = null;
                        paraValues[25] = null;
                        paraValues[26] = null;
                        paraValues[27] = null;
                        paraValues[28] = null;
                        paraValues[29] = null;
                        paraValues[30] = null;
                        paraValues[31] = null;
                        paraValues[32] = null;
                        paraValues[33] = null;
                        paraValues[34] = null;
                        paraValues[35] = null;
                        paraValues[36] = null;
                        paraValues[37] = null;
                        paraValues[38] = null;
                        paraValues[39] = null;
                        paraValues[40] = null;
                        paraValues[41] = null;
                        paraValues[42] = null;
                        paraValues[43] = null;
                        paraValues[44] = null;
                        paraValues[45] = null;
                        paraValues[46] = null;
                        paraValues[47] = null;
                        paraValues[48] = null;
                        paraValues[49] = null;
                        paraValues[50] = null;
                        paraValues[51] = null;
                        paraValues[52] = null;
                    }
                    else
                    {
                        int arrayIndex = 11;
                        string subTokenId = "02";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 12;
                        subTokenId = "04";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId];
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 13;
                        subTokenId = "05";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 14;
                        subTokenId = "09";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 15;
                        subTokenId = "10";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            var subTokenIdData = tokenQ8TLV[subTokenId];
                            if (int.TryParse(subTokenIdData, out var number))
                                paraValues[arrayIndex] = number;
                            else
                                paraValues[arrayIndex] = 0;
                        }
                        else
                        {
                            paraValues[arrayIndex] = 0;
                        }

                        arrayIndex = 16;
                        subTokenId = "11";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 17;
                        subTokenId = "12";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 18;
                        subTokenId = "13";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 19;
                        subTokenId = "16";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 20;
                        subTokenId = "18";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 21;
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                            case CreditCardType.MasterCard:
                                subTokenId = "19";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            case CreditCardType.JCB:
                                subTokenId = "QF";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 22;
                        subTokenId = "20";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 23;
                        subTokenId = "21";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 24;
                        subTokenId = "22";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 25;
                        subTokenId = "23";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 26;
                        subTokenId = "24";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 27;
                        subTokenId = "25";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 28;
                        subTokenId = "26";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 29;
                        subTokenId = "27";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 30;
                        subTokenId = "28";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 31;
                        subTokenId = "29";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 32;
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                                subTokenId = "38";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            case CreditCardType.MasterCard:
                                subTokenId = "30";
                                if (tokenQ8TLV.ContainsKey(subTokenId))
                                {
                                    paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                                }
                                else
                                {
                                    paraValues[arrayIndex] = null;
                                }
                                break;
                            default:
                                paraValues[arrayIndex] = null;
                                break;
                        }

                        arrayIndex = 33;
                        subTokenId = "31";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 34;
                        subTokenId = "32";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 35;
                        subTokenId = "33";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 36;
                        subTokenId = "36";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 37;
                        subTokenId = "37";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 38;
                        subTokenId = "39";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 39;
                        subTokenId = "40";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 40;
                        subTokenId = "56";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 41;
                        subTokenId = "57";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 42;
                        subTokenId = "58";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 43;
                        subTokenId = "59";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 44;
                        subTokenId = "60";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 45;
                        subTokenId = "62";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 46;
                        subTokenId = "63";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 47;
                        subTokenId = "Q3";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 48;
                        subTokenId = "Q4";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 49;
                        subTokenId = "Q6";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 50;
                        subTokenId = "Q9";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 51;
                        subTokenId = "QA";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }

                        arrayIndex = 52;
                        subTokenId = "QG";
                        if (tokenQ8TLV.ContainsKey(subTokenId))
                        {
                            paraValues[arrayIndex] = tokenQ8TLV[subTokenId]; ;
                        }
                        else
                        {
                            paraValues[arrayIndex] = null;
                        }
                    }
                }

                if (bicAuthInfoData.tokenQ9Data == null)
                    paraValues[53] = null;
                else
                {
                    bicAuthInfoData.tokenQ9Data.SubDataDecode();
                    switch (creditCardType)
                    {
                        case CreditCardType.Visa:
                            paraValues[53] = bicAuthInfoData.tokenQ9Data?.VisaMsgRsnCde;
                            break;
                        case CreditCardType.MasterCard:
                            paraValues[53] = bicAuthInfoData.tokenQ9Data?.MastercardAdvcDetlCde;
                            break;
                        default:
                            paraValues[53] = null;
                            break;
                    }
                }

                paraValues[54] = null;
                if (bicAuthInfoData.tokenS8Data?.AcctNum == null)
                    paraValues[54] = null;
                else
                    paraValues[54] = bicAuthInfoData.tokenS8Data?.AcctNum;

                paraValues[55] = null;
                if (bicAuthInfoData.tokenS8Data?.ExpDat == null)
                    paraValues[55] = null;
                else
                    paraValues[55] = bicAuthInfoData.tokenS8Data?.ExpDat;

                paraValues[56] = null;
                paraValues[57] = null;
                paraValues[58] = null;
                paraValues[59] = null;
                paraValues[60] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start NCCC0620TknEventProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   NCCC0620TknEventProcess Display Store Procedure Input Data  ---");
                }

                // NCCC0620TknEventProcess Return String Arrays Information
                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@otpSend
                // authReturnString[02]:@tokenMsg            , authReturnString[03]:@custName 
                // authReturnString[04]:@cellPhone           , authReturnString[05]:@custSeq
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @otpSend                     varchar(01) 	Output	 簡訊使用
                // @tokenMsg       	            varchar(100) 	Output	 簡訊使用
                // @custName      	            varchar(20) 	Output	 簡訊使用
                // @cellPhone      	            varchar(20) 	Output	 簡訊使用
                // @custSeq      	            varchar(20) 	Output	 簡訊使用

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureNCCC0620TknEventProcess, returnStrings, 2, 3, 4, 5));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NCCC0620TknEventProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"NCCC0620TknEventProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Fisc0100AuthProcess Process Message Type 0100 Authorization Flow
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0100AuthProcess(FiscAuthInfoStruct fiscAuthInfoData)
        {
            const int storeProcedureParameterCount = 79;
            const int outputParaCount = 19;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0100AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP10ConvertRte;
                paraValues[09] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                if (fiscAuthInfoData.isoP35OrP45ExpirDte == null)
                    paraValues[14] = fiscAuthInfoData.isoP14ExpirDte;
                else
                    paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                //paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP35OrP45Svc;
                paraValues[16] = fiscAuthInfoData.isoP35OrP45Pvv;
                paraValues[17] = fiscAuthInfoData.isoP35OrP45Pki;
                paraValues[18] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[19] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[20] = fiscAuthInfoData.isoP39Rsp;
                paraValues[21] = null;
                paraValues[22] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[23] = fiscAuthInfoData.isoP42Merchant;
                paraValues[24] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[25] = fiscAuthInfoData.isoP48T42;
                paraValues[26] = fiscAuthInfoData.isoP48T43;
                paraValues[27] = fiscAuthInfoData.isoP48T45;
                paraValues[28] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[29] = fiscAuthInfoData.isoP51BillingCurrency;
                paraValues[30] = fiscAuthInfoData.isoP52PinBlock;
                if (fiscAuthInfoData.isoP55TVR == null)
                    paraValues[31] = null;
                else
                {
                    string tempString = ColaCommUtility.HexString2BinString(fiscAuthInfoData.isoP55TVR);
                    paraValues[31] = tempString;
                }
                if (fiscAuthInfoData.isoP55ATC == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = fiscAuthInfoData.isoP55ATC;

                if (fiscAuthInfoData.isoP55CVR == null)
                    paraValues[33] = null;
                else
                {
                    string tempString = ColaCommUtility.HexString2BinString(fiscAuthInfoData.isoP55CVR);
                    paraValues[33] = tempString;
                }

                if (fiscAuthInfoData.isoP55MDK == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = fiscAuthInfoData.isoP55MDK;

                if (fiscAuthInfoData.isoP55VerNum == null)
                    paraValues[35] = null;
                else
                    paraValues[35] = fiscAuthInfoData.isoP55VerNum;

                if (fiscAuthInfoData.isoP55ROC == null)
                    paraValues[36] = null;
                else
                    paraValues[36] = fiscAuthInfoData.isoP55ROC;

                if (fiscAuthInfoData.isoP55ARQC == null)
                    paraValues[37] = null;
                else
                    paraValues[37] = fiscAuthInfoData.isoP55ARQC;

                if (fiscAuthInfoData.isoP55Un == null)
                    paraValues[38] = null;
                else
                    paraValues[38] = fiscAuthInfoData.isoP55Un;

                paraValues[39] = fiscAuthInfoData.isoP58T21LoyaltyNbr;
                paraValues[40] = fiscAuthInfoData.isoP58T21LoyaltyInd;
                paraValues[41] = fiscAuthInfoData.isoP58T21TxResp;
                paraValues[42] = fiscAuthInfoData.isoP58T21SignPoint;
                paraValues[43] = fiscAuthInfoData.isoP58T21PointBalance;
                paraValues[44] = fiscAuthInfoData.isoP58T21PointRedemption;
                paraValues[45] = fiscAuthInfoData.isoP58T21PaidCAmt;
                paraValues[46] = fiscAuthInfoData.isoP58T67;
                paraValues[47] = fiscAuthInfoData.isoP58T68;
                paraValues[48] = fiscAuthInfoData.isoP58T87CellPhone;
                paraValues[49] = fiscAuthInfoData.isoP58T87Dob;
                paraValues[50] = fiscAuthInfoData.isoP61PosChPresInd;
                paraValues[51] = fiscAuthInfoData.isoP61TermEntryCap;
                paraValues[52] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[53] = fiscAuthInfoData.isoS95ReplaceAmt;
                paraValues[54] = fiscAuthInfoData.isoS112InstTxInd;
                paraValues[55] = fiscAuthInfoData.isoS112InstPeriod;
                paraValues[56] = fiscAuthInfoData.isoS112DownPayment;
                paraValues[57] = fiscAuthInfoData.isoS112InstPayment;
                paraValues[58] = fiscAuthInfoData.isoS112InstFee;
                paraValues[59] = fiscAuthInfoData.isoS112InstTxResp;
                paraValues[60] = fiscAuthInfoData.arqcCheck;
                paraValues[61] = fiscAuthInfoData.arqcChkResult;
                paraValues[62] = fiscAuthInfoData.cvvCheck;
                paraValues[63] = fiscAuthInfoData.cvvChkResult;
                paraValues[64] = fiscAuthInfoData.cvv2Check;
                paraValues[65] = fiscAuthInfoData.cvv2ChkResult;
                paraValues[66] = fiscAuthInfoData.cavvCheck;
                paraValues[67] = fiscAuthInfoData.cavvChkResult;
                paraValues[68] = fiscAuthInfoData.pvvCheck;
                paraValues[69] = fiscAuthInfoData.pvvChkResult;
                paraValues[70] = fiscAuthInfoData.pvvDobCheck;
                paraValues[71] = fiscAuthInfoData.pvvDobChkResult;
                paraValues[72] = null;
                paraValues[73] = null;
                paraValues[74] = null;
                paraValues[75] = null;
                paraValues[76] = null;
                paraValues[77] = null;
                paraValues[78] = null;

                // Fisc0100AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
                // authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
                // authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
                // authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
                // authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
                // authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
                // authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
                // authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
                // authReturnString[18]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoP48T45               	varchar(01)	    Output	 --CAVV Result
                // @isoP58T21LoyaltyInd	        varchar(01) 	Output	 Field 58 紅利註記
                // @isoP58T21TxResp           	varchar(02) 	Output	 Field 58 紅利回覆碼
                // @isoP58T21SignPoint          varchar(01) 	Output   Field 58 <紅利, 餘點正負項>
                // @isoP58T21PointBalance       varchar(10) 	Output	 Field 58 <紅利, 餘點>
                // @isoP58T21PointRedemption    varchar(10) 	Output	 Field 58 <紅利,折扺點數>
                // @isoP58T21PaidCAmt      	    varchar(12) 	Output	 Field 58  <紅利,實際支付金額>
                // @isoS112DownPayment      	varchar(08) 	Output	 Field 112 首期期金
                // @isoS112InstPayment          varchar(08) 	Output	 Field 112 每期期金
                // @isoS112InstFee  	        varchar(06) 	Output	 Field 112 手續費
                // @isoS112InstTxResp        	varchar(02) 	Output	 Field 112 分期回應碼
                // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0100AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Fisc0100AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(FiscAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(fiscAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(fiscAuthInfoData)}\n");
                    }

                    Log.Trace($"Fisc0100AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Fisc0100AuthProcess Process Message Type 0100 Authorization Flow
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="responseXML">The response XML.</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0100AuthProcess(FiscAuthInfoStruct fiscAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 79;
            const int outputParaCount = 19;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0100AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP10ConvertRte;
                paraValues[09] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                if (fiscAuthInfoData.isoP35OrP45ExpirDte == null)
                    paraValues[14] = fiscAuthInfoData.isoP14ExpirDte;
                else
                    paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                //paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP35OrP45Svc;
                paraValues[16] = fiscAuthInfoData.isoP35OrP45Pvv;
                paraValues[17] = fiscAuthInfoData.isoP35OrP45Pki;
                paraValues[18] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[19] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[20] = fiscAuthInfoData.isoP39Rsp;
                paraValues[21] = null;
                paraValues[22] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[23] = fiscAuthInfoData.isoP42Merchant;
                paraValues[24] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[25] = fiscAuthInfoData.isoP48T42;
                paraValues[26] = fiscAuthInfoData.isoP48T43;
                paraValues[27] = fiscAuthInfoData.isoP48T45;
                paraValues[28] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[29] = fiscAuthInfoData.isoP51BillingCurrency;
                paraValues[30] = fiscAuthInfoData.isoP52PinBlock;
                if (fiscAuthInfoData.isoP55TVR == null)
                    paraValues[31] = null;
                else
                {
                    string tempString = ColaCommUtility.HexString2BinString(fiscAuthInfoData.isoP55TVR);
                    paraValues[31] = tempString;
                }
                if (fiscAuthInfoData.isoP55ATC == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = fiscAuthInfoData.isoP55ATC;

                if (fiscAuthInfoData.isoP55CVR == null)
                    paraValues[33] = null;
                else
                {
                    string tempString = ColaCommUtility.HexString2BinString(fiscAuthInfoData.isoP55CVR);
                    paraValues[33] = tempString;
                }

                if (fiscAuthInfoData.isoP55MDK == null)
                    paraValues[34] = null;
                else
                    paraValues[34] = fiscAuthInfoData.isoP55MDK;

                if (fiscAuthInfoData.isoP55VerNum == null)
                    paraValues[35] = null;
                else
                    paraValues[35] = fiscAuthInfoData.isoP55VerNum;

                if (fiscAuthInfoData.isoP55ROC == null)
                    paraValues[36] = null;
                else
                    paraValues[36] = fiscAuthInfoData.isoP55ROC;

                if (fiscAuthInfoData.isoP55ARQC == null)
                    paraValues[37] = null;
                else
                    paraValues[37] = fiscAuthInfoData.isoP55ARQC;

                if (fiscAuthInfoData.isoP55Un == null)
                    paraValues[38] = null;
                else
                    paraValues[38] = fiscAuthInfoData.isoP55Un;

                paraValues[39] = fiscAuthInfoData.isoP58T21LoyaltyNbr;
                paraValues[40] = fiscAuthInfoData.isoP58T21LoyaltyInd;
                paraValues[41] = fiscAuthInfoData.isoP58T21TxResp;
                paraValues[42] = fiscAuthInfoData.isoP58T21SignPoint;
                paraValues[43] = fiscAuthInfoData.isoP58T21PointBalance;
                paraValues[44] = fiscAuthInfoData.isoP58T21PointRedemption;
                paraValues[45] = fiscAuthInfoData.isoP58T21PaidCAmt;
                paraValues[46] = fiscAuthInfoData.isoP58T67;
                paraValues[47] = fiscAuthInfoData.isoP58T68;
                paraValues[48] = fiscAuthInfoData.isoP58T87CellPhone;
                paraValues[49] = fiscAuthInfoData.isoP58T87Dob;
                paraValues[50] = fiscAuthInfoData.isoP61PosChPresInd;
                paraValues[51] = fiscAuthInfoData.isoP61TermEntryCap;
                paraValues[52] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[53] = fiscAuthInfoData.isoS95ReplaceAmt;
                paraValues[54] = fiscAuthInfoData.isoS112InstTxInd;
                paraValues[55] = fiscAuthInfoData.isoS112InstPeriod;
                paraValues[56] = fiscAuthInfoData.isoS112DownPayment;
                paraValues[57] = fiscAuthInfoData.isoS112InstPayment;
                paraValues[58] = fiscAuthInfoData.isoS112InstFee;
                paraValues[59] = fiscAuthInfoData.isoS112InstTxResp;
                paraValues[60] = fiscAuthInfoData.arqcCheck;
                paraValues[61] = fiscAuthInfoData.arqcChkResult;
                paraValues[62] = fiscAuthInfoData.cvvCheck;
                paraValues[63] = fiscAuthInfoData.cvvChkResult;
                paraValues[64] = fiscAuthInfoData.cvv2Check;
                paraValues[65] = fiscAuthInfoData.cvv2ChkResult;
                paraValues[66] = fiscAuthInfoData.cavvCheck;
                paraValues[67] = fiscAuthInfoData.cavvChkResult;
                paraValues[68] = fiscAuthInfoData.pvvCheck;
                paraValues[69] = fiscAuthInfoData.pvvChkResult;
                paraValues[70] = fiscAuthInfoData.pvvDobCheck;
                paraValues[71] = fiscAuthInfoData.pvvDobChkResult;
                paraValues[72] = null;
                if (responseXML.TxRs == null)
                {
                    paraValues[73] = authReturnString[15];
                    paraValues[74] = authReturnString[16];
                    paraValues[75] = authReturnString[17];
                    paraValues[77] = responseXML.ResponseHeader.StatusCode;
                    paraValues[78] = 0;
                    //if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                    //    paraValues[78] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    //else
                    //    paraValues[78] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                else
                {
                    paraValues[73] = responseXML.TxRs.AcctNo;
                    paraValues[74] = responseXML.TxRs.CardNo;
                    paraValues[75] = responseXML.TxRs.FuncCode;
                    paraValues[77] = responseXML.ResponseHeader.StatusCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[78] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[78] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);

                }
                paraValues[76] = authReturnString[18];

                // Fisc0100AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
                // authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
                // authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
                // authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
                // authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
                // authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
                // authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
                // authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
                // authReturnString[18]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoP48T45              	    varchar(01)	    Output	 --CAVV Result
                // @isoP58T21LoyaltyInd	        varchar(01) 	Output	 Field 58 紅利註記
                // @isoP58T21TxResp           	varchar(02) 	Output	 Field 58 紅利回覆碼
                // @isoP58T21SignPoint          varchar(01) 	Output   Field 58 <紅利, 餘點正負項>
                // @isoP58T21PointBalance       varchar(10) 	Output	 Field 58 <紅利, 餘點>
                // @isoP58T21PointRedemption    varchar(10) 	Output	 Field 58 <紅利,折扺點數>
                // @isoP58T21PaidCAmt      	    varchar(12) 	Output	 Field 58  <紅利,實際支付金額>
                // @isoS112DownPayment      	varchar(08) 	Output	 Field 112 首期期金
                // @isoS112InstPayment          varchar(08) 	Output	 Field 112 每期期金
                // @isoS112InstFee  	        varchar(06) 	Output	 Field 112 手續費
                // @isoS112InstTxResp        	varchar(02) 	Output	 Field 112 分期回應碼
                // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0100AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Fisc0100AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(FiscAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(fiscAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(fiscAuthInfoData)}\n");
                    }

                    Log.Trace($"Fisc0100AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Fisc0100HceAuthProcess Process Message Type 0100 Authorization Flow For HCE Onus
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0100HceAuthProcess(FiscAuthInfoStruct fiscAuthInfoData)
        {
            const int storeProcedureParameterCount = 49;
            const int outputParaCount = 14;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0100HceAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP10ConvertRte;
                paraValues[09] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                if (fiscAuthInfoData.isoP35OrP45ExpirDte == null)
                    paraValues[14] = fiscAuthInfoData.isoP14ExpirDte;
                else
                    paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP35OrP45Svc;
                paraValues[16] = fiscAuthInfoData.isoP35OrP45Pvv;
                paraValues[17] = fiscAuthInfoData.isoP35OrP45Pki;
                paraValues[18] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[19] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[20] = fiscAuthInfoData.isoP39Rsp;
                paraValues[21] = null;
                paraValues[22] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[23] = fiscAuthInfoData.isoP42Merchant;
                paraValues[24] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[25] = fiscAuthInfoData.isoP48T42;
                paraValues[26] = fiscAuthInfoData.isoP48T43;
                paraValues[27] = fiscAuthInfoData.isoP48T45;
                paraValues[28] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[29] = fiscAuthInfoData.isoP51BillingCurrency;
                paraValues[30] = fiscAuthInfoData.isoP58T21LoyaltyNbr;
                paraValues[31] = fiscAuthInfoData.isoP58T21LoyaltyInd;
                paraValues[32] = fiscAuthInfoData.isoP58T21TxResp;
                paraValues[33] = fiscAuthInfoData.isoP58T21SignPoint;
                paraValues[34] = fiscAuthInfoData.isoP58T21PointBalance;
                paraValues[35] = fiscAuthInfoData.isoP58T21PointRedemption;
                paraValues[36] = fiscAuthInfoData.isoP58T21PaidCAmt;
                paraValues[37] = fiscAuthInfoData.isoP58T32Tpan;
                paraValues[38] = fiscAuthInfoData.isoP58T32ExpDat;
                paraValues[39] = fiscAuthInfoData.isoP58T32AcVeryRslt;
                paraValues[40] = fiscAuthInfoData.isoP58T32FraudChkRslt;
                paraValues[41] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[42] = fiscAuthInfoData.isoS95ReplaceAmt;
                paraValues[43] = fiscAuthInfoData.isoS112InstTxInd;
                paraValues[44] = fiscAuthInfoData.isoS112InstPeriod;
                paraValues[45] = fiscAuthInfoData.isoS112DownPayment;
                paraValues[46] = fiscAuthInfoData.isoS112InstPayment;
                paraValues[47] = fiscAuthInfoData.isoS112InstFee;
                paraValues[48] = fiscAuthInfoData.isoS112InstTxResp;

                // Fisc0100HceAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
                // authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
                // authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
                // authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
                // authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
                // authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoP48T45               	varchar(01)	    Output	 --CAVV Result
                // @isoP58T21LoyaltyInd	        varchar(01) 	Output	 Field 58 紅利註記
                // @isoP58T21TxResp           	varchar(02) 	Output	 Field 58 紅利回覆碼
                // @isoP58T21SignPoint          varchar(01) 	Output   Field 58 <紅利, 餘點正負項>
                // @isoP58T21PointBalance       varchar(10) 	Output	 Field 58 <紅利, 餘點>
                // @isoP58T21PointRedemption    varchar(10) 	Output	 Field 58 <紅利,折扺點數>
                // @isoP58T21PaidCAmt      	    varchar(12) 	Output	 Field 58  <紅利,實際支付金額>
                // @isoS112DownPayment      	varchar(08) 	Output	 Field 112 首期期金
                // @isoS112InstPayment          varchar(08) 	Output	 Field 112 每期期金
                // @isoS112InstFee  	        varchar(06) 	Output	 Field 112 手續費
                // @isoS112InstTxResp        	varchar(02) 	Output	 Field 112 分期回應碼

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0100HceAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Fisc0100HceAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(FiscAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(fiscAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(fiscAuthInfoData)}\n");
                    }

                    Log.Trace($"Fisc0100HceAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Fisc0120AuthProcess Process Message Type 0120 Authorization Flow
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0120AuthProcess(FiscAuthInfoStruct fiscAuthInfoData)
        {
            const int storeProcedureParameterCount = 34;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0120AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[09] = fiscAuthInfoData.isoP14ExpirDte;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP35OrP45Svc;
                paraValues[16] = fiscAuthInfoData.isoP35OrP45Pvv;
                paraValues[17] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[18] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[19] = fiscAuthInfoData.isoP39Rsp;
                paraValues[20] = null;
                paraValues[21] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[22] = fiscAuthInfoData.isoP42Merchant;
                paraValues[23] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[24] = fiscAuthInfoData.isoP48T42;
                paraValues[25] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[26] = fiscAuthInfoData.isoP58T69MPflag;
                paraValues[27] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[28] = null;
                paraValues[29] = null;
                paraValues[30] = null;
                paraValues[31] = null;
                paraValues[32] = null;
                paraValues[33] = null;

                // Fisc0120AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction       , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq          , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0120AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Exception:" + ex.Message);
                return (null);
            }
        }

        // <summary>
        /// <summary>
        /// Fisc0120 the authentication process.
        /// </summary>
        /// <param name="fiscAuthInfoData">The fisc authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="responseXML">The response XML.</param>
        /// <returns></returns>
        public static string[] Fisc0120AuthProcess(FiscAuthInfoStruct fiscAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 34;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0120AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[09] = fiscAuthInfoData.isoP14ExpirDte;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP35OrP45Svc;
                paraValues[16] = fiscAuthInfoData.isoP35OrP45Pvv;
                paraValues[17] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[18] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[19] = fiscAuthInfoData.isoP39Rsp;
                paraValues[20] = null;
                paraValues[21] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[22] = fiscAuthInfoData.isoP42Merchant;
                paraValues[23] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[24] = fiscAuthInfoData.isoP48T42;
                paraValues[25] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[26] = fiscAuthInfoData.isoP58T69MPflag;
                if (responseXML.TxRs == null)
                {
                    paraValues[28] = authReturnString[03];
                    paraValues[29] = authReturnString[04];
                    paraValues[30] = authReturnString[05];
                    paraValues[33] = 0;
                }
                else
                {
                    paraValues[28] = responseXML.TxRs.AcctNo;
                    paraValues[29] = responseXML.TxRs.CardNo;
                    paraValues[30] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[33] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[33] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[31] = authReturnString[06];
                paraValues[32] = responseXML.ResponseHeader.StatusCode;

                // Fisc0120AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction       , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq          , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0120AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Exception:" + ex.Message);
                return (null);
            }
        }

        /// <summary>
        /// Fisc0120HceAuthProcess Process Message Type 0120 Authorization Flow For Hce
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0120HceAuthProcess(FiscAuthInfoStruct fiscAuthInfoData)
        {
            const int storeProcedureParameterCount = 32;
            const int outputParaCount = 3;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0120HceAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[09] = fiscAuthInfoData.isoP14ExpirDte;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP35OrP45Svc;
                paraValues[16] = fiscAuthInfoData.isoP35OrP45Pvv;
                paraValues[17] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[18] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[19] = fiscAuthInfoData.isoP39Rsp;
                paraValues[20] = null;
                paraValues[21] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[22] = fiscAuthInfoData.isoP42Merchant;
                paraValues[23] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[24] = fiscAuthInfoData.isoP48T42;
                paraValues[25] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[26] = fiscAuthInfoData.isoP58T32Tpan;
                paraValues[27] = fiscAuthInfoData.isoP58T32ExpDat;
                paraValues[28] = fiscAuthInfoData.isoP58T32AcVeryRslt;
                paraValues[29] = fiscAuthInfoData.isoP58T32FraudChkRslt;
                paraValues[30] = fiscAuthInfoData.isoP58T69MPflag;
                paraValues[31] = fiscAuthInfoData.isoP61VisaAcctVfy;

                // Fisc0120HceAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction       
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0120HceAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Exception:" + ex.Message);
                return (null);
            }
        }


        /// <summary>
        /// Fisc0420AuthProcess Process Message Type 0420 Reversal Flow
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0420AuthProcess(FiscAuthInfoStruct fiscAuthInfoData)
        {
            const int storeProcedureParameterCount = 43;
            const int outputParaCount = 12;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0420AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[09] = fiscAuthInfoData.isoP14ExpirDte;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[16] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[17] = fiscAuthInfoData.isoP39Rsp;
                paraValues[18] = fiscAuthInfoData.isoP39Rsp;
                paraValues[19] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[20] = fiscAuthInfoData.isoP42Merchant;
                paraValues[21] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[22] = fiscAuthInfoData.isoP48T42;
                paraValues[23] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[24] = fiscAuthInfoData.isoP58T21LoyaltyNbr;
                paraValues[25] = fiscAuthInfoData.isoP58T21LoyaltyInd;
                paraValues[26] = fiscAuthInfoData.isoP58T21TxResp;
                paraValues[27] = fiscAuthInfoData.isoP58T21SignPoint;
                paraValues[28] = fiscAuthInfoData.isoP58T21PointBalance;
                paraValues[29] = fiscAuthInfoData.isoP58T21PointRedemption;
                paraValues[30] = fiscAuthInfoData.isoP58T21PaidCAmt;
                paraValues[31] = fiscAuthInfoData.isoP58T69MPflag;
                paraValues[32] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[33] = fiscAuthInfoData.isoS95ReplaceAmt;
                paraValues[34] = fiscAuthInfoData.isoS112InstTxInd;
                paraValues[35] = fiscAuthInfoData.isoS112InstPeriod;
                paraValues[36] = fiscAuthInfoData.isoS112InstTxResp;
                paraValues[37] = null;
                paraValues[38] = null;
                paraValues[39] = null;
                paraValues[40] = null;
                paraValues[41] = null;
                paraValues[42] = null;

                // Fisc0420AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode            , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction           , authReturnString[03]:@isoP58T21TxResp
                // authReturnString[04]:@isoP58T21SignPoint        , authReturnString[05]:@isoP58T21PointBalance
                // authReturnString[06]:@isoP58T21PointRedemption  , authReturnString[07]:@isoP58T21PaidCAmt
                // authReturnString[08]:@vdLinkNbr                 , authReturnString[09]:@vdLinkNbrSeq
                // authReturnString[10]:@vdActionCode              , authReturnString[11]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoP58T21TxResp             varchar(02)     Output,  --紅利回覆碼
                // @isoP58T21SignPoint          varchar(01)     Output,  --< 紅利, 餘點正負項 >
                // @isoP58T21PointBalance       varchar(10)     Output,  --< 紅利, 餘點 >
                // @isoP58T21PointRedemption    varchar(10)     Output,  --< 紅利,折扺點數 >
                // @isoP58T21PaidCAmt           varchar(12)     Output,  --< 紅利,實際支付金額 >
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Exception:" + ex.Message);
                return (null);
            }
        }


        /// <summary>
        /// Fisc0420AuthProcess Process Message Type 0420 Reversal Flow
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="responseXML">The response XML.</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0420AuthProcess(FiscAuthInfoStruct fiscAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 43;
            const int outputParaCount = 12;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0420AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[09] = fiscAuthInfoData.isoP14ExpirDte;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[16] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[17] = fiscAuthInfoData.isoP39Rsp;
                paraValues[18] = fiscAuthInfoData.isoP39Rsp;
                paraValues[19] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[20] = fiscAuthInfoData.isoP42Merchant;
                paraValues[21] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[22] = fiscAuthInfoData.isoP48T42;
                paraValues[23] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[24] = fiscAuthInfoData.isoP58T21LoyaltyNbr;
                paraValues[25] = fiscAuthInfoData.isoP58T21LoyaltyInd;
                paraValues[26] = fiscAuthInfoData.isoP58T21TxResp;
                paraValues[27] = fiscAuthInfoData.isoP58T21SignPoint;
                paraValues[28] = fiscAuthInfoData.isoP58T21PointBalance;
                paraValues[29] = fiscAuthInfoData.isoP58T21PointRedemption;
                paraValues[30] = fiscAuthInfoData.isoP58T21PaidCAmt;
                paraValues[31] = fiscAuthInfoData.isoP58T69MPflag;
                paraValues[32] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[33] = fiscAuthInfoData.isoS95ReplaceAmt;
                paraValues[34] = fiscAuthInfoData.isoS112InstTxInd;
                paraValues[35] = fiscAuthInfoData.isoS112InstPeriod;
                paraValues[36] = fiscAuthInfoData.isoS112InstTxResp;
                if (responseXML.TxRs == null)
                {
                    paraValues[37] = authReturnString[08];
                    paraValues[38] = authReturnString[09];
                    paraValues[39] = authReturnString[10];
                    paraValues[42] = 0;
                }
                else
                {
                    paraValues[37] = responseXML.TxRs.AcctNo;
                    paraValues[38] = responseXML.TxRs.CardNo;
                    paraValues[39] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[42] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[42] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[40] = authReturnString[11];
                paraValues[41] = responseXML.ResponseHeader.StatusCode;
                // Fisc0420AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode            , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction           , authReturnString[03]:@isoP58T21TxResp
                // authReturnString[04]:@isoP58T21SignPoint        , authReturnString[05]:@isoP58T21PointBalance
                // authReturnString[06]:@isoP58T21PointRedemption  , authReturnString[07]:@isoP58T21PaidCAmt
                // authReturnString[08]:@vdLinkNbr                 , authReturnString[09]:@vdLinkNbrSeq
                // authReturnString[10]:@vdActionCode              , authReturnString[11]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoP58T21TxResp             varchar(02)     Output,  --紅利回覆碼
                // @isoP58T21SignPoint          varchar(01)     Output,  --< 紅利, 餘點正負項 >
                // @isoP58T21PointBalance       varchar(10)     Output,  --< 紅利, 餘點 >
                // @isoP58T21PointRedemption    varchar(10)     Output,  --< 紅利,折扺點數 >
                // @isoP58T21PaidCAmt           varchar(12)     Output,  --< 紅利,實際支付金額 >
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Exception:" + ex.Message);
                return (null);
            }
        }

        /// <summary>
        /// Fisc0420HceAuthProcess Process Message Type 0420 Reversal Flow For Hce
        /// </summary>
        /// <param name="fiscAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Fisc0420HceAuthProcess(FiscAuthInfoStruct fiscAuthInfoData)
        {
            const int storeProcedureParameterCount = 41;
            const int outputParaCount = 8;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureFISC0420HceAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = fiscAuthInfoData.tranLogKey;
                paraValues[01] = fiscAuthInfoData.isoMessageType;
                paraValues[02] = fiscAuthInfoData.isoP2CardNbr;
                paraValues[03] = fiscAuthInfoData.isoP3ProcessCde;
                paraValues[04] = fiscAuthInfoData.isoP4Amt;
                paraValues[05] = fiscAuthInfoData.isoP6Amt;
                paraValues[06] = fiscAuthInfoData.isoP7TxDte;
                paraValues[07] = fiscAuthInfoData.isoP7TxTime;
                paraValues[08] = fiscAuthInfoData.isoP11TraceNo;
                paraValues[09] = fiscAuthInfoData.isoP14ExpirDte;
                paraValues[10] = fiscAuthInfoData.isoP18MccCode;
                paraValues[11] = fiscAuthInfoData.isoP19AcqCountry;
                paraValues[12] = fiscAuthInfoData.isoP22Em;
                paraValues[13] = fiscAuthInfoData.isoP32BinIca;
                paraValues[14] = fiscAuthInfoData.isoP35OrP45ExpirDte;
                paraValues[15] = fiscAuthInfoData.isoP37RefNbr;
                paraValues[16] = fiscAuthInfoData.isoP38AuthCode;
                paraValues[17] = fiscAuthInfoData.isoP39Rsp;
                paraValues[18] = fiscAuthInfoData.isoP39Rsp;
                paraValues[19] = fiscAuthInfoData.isoP41TermNbr;
                paraValues[20] = fiscAuthInfoData.isoP42Merchant;
                paraValues[21] = fiscAuthInfoData.isoP43MerInfo;
                paraValues[22] = fiscAuthInfoData.isoP48T42;
                paraValues[23] = fiscAuthInfoData.isoP49CurrencyCode;
                paraValues[24] = fiscAuthInfoData.isoP58T21LoyaltyNbr;
                paraValues[25] = fiscAuthInfoData.isoP58T21LoyaltyInd;
                paraValues[26] = fiscAuthInfoData.isoP58T21TxResp;
                paraValues[27] = fiscAuthInfoData.isoP58T21SignPoint;
                paraValues[28] = fiscAuthInfoData.isoP58T21PointBalance;
                paraValues[29] = fiscAuthInfoData.isoP58T21PointRedemption;
                paraValues[30] = fiscAuthInfoData.isoP58T21PaidCAmt;
                paraValues[31] = fiscAuthInfoData.isoP58T32Tpan;
                paraValues[32] = fiscAuthInfoData.isoP58T32ExpDat;
                paraValues[33] = fiscAuthInfoData.isoP58T32AcVeryRslt;
                paraValues[34] = fiscAuthInfoData.isoP58T32FraudChkRslt;
                paraValues[35] = fiscAuthInfoData.isoP58T69MPflag;
                paraValues[36] = fiscAuthInfoData.isoP61VisaAcctVfy;
                paraValues[37] = fiscAuthInfoData.isoS95ReplaceAmt;
                paraValues[38] = fiscAuthInfoData.isoS112InstTxInd;
                paraValues[39] = fiscAuthInfoData.isoS112InstPeriod;
                paraValues[40] = fiscAuthInfoData.isoS112InstTxResp;

                // Fisc0420HceAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode            , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction           , authReturnString[03]:@isoP58T21TxResp
                // authReturnString[04]:@isoP58T21SignPoint        , authReturnString[05]:@isoP58T21PointBalance
                // authReturnString[06]:@isoP58T21PointRedemption  , authReturnString[07]:@isoP58T21PaidCAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoP58T21TxResp             varchar(02)     Output,  --紅利回覆碼
                // @isoP58T21SignPoint          varchar(01)     Output,  --< 紅利, 餘點正負項 >
                // @isoP58T21PointBalance       varchar(10)     Output,  --< 紅利, 餘點 >
                // @isoP58T21PointRedemption    varchar(10)     Output,  --< 紅利,折扺點數 >
                // @isoP58T21PaidCAmt           varchar(12)     Output,  --< 紅利,實際支付金額 >

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureFISC0420HceAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Exception:" + ex.Message);
                return (null);
            }
        }

        /// <summary>
        /// Twin0100AuthProcess Process Message Type 0200 Authorization Flow For TWIN
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="data9F10Array"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Twin0100AuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] data9F10Array)
        {
            const int storeProcedureParameterCount = 85;
            const int outputParaCount = 22;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureTWIN0100AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoP52PinBlock;
                paraValues[26] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[27] = bicAuthInfoData.isoS127CellPhone;
                paraValues[28] = bicAuthInfoData.isoS127Dob;
                paraValues[29] = bicAuthInfoData.isoS127Pws;
                paraValues[30] = null;
                paraValues[31] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[32] = bicAuthInfoData.isoS127MpFlag;
                paraValues[33] = bicAuthInfoData.isoS127Id;
                paraValues[34] = bicAuthInfoData.arqcCheck;
                paraValues[35] = bicAuthInfoData.arqcChkResult;
                paraValues[36] = bicAuthInfoData.cvvCheck;
                paraValues[37] = bicAuthInfoData.cvvChkResult;
                paraValues[38] = bicAuthInfoData.cvv2Check;
                paraValues[39] = bicAuthInfoData.cvv2ChkResult;
                paraValues[40] = bicAuthInfoData.cavvCheck;
                paraValues[41] = bicAuthInfoData.cavvChkResult;
                paraValues[42] = bicAuthInfoData.pvvCheck;
                paraValues[43] = bicAuthInfoData.pvvChkResult;
                paraValues[44] = bicAuthInfoData.pvvDobCheck;
                paraValues[45] = bicAuthInfoData.pvvDobChkResult;

                if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                    paraValues[46] = null;
                else
                    paraValues[46] = bicAuthInfoData.token04Data.ErrorFlg;

                if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                    paraValues[47] = null;
                else
                    paraValues[47] = bicAuthInfoData.token04Data.CrdVeryFlg;

                if (bicAuthInfoData.tokenB0Data?.OnBehalf == null)
                    paraValues[48] = null;
                else
                {
                    string dataString = bicAuthInfoData.tokenB0Data.OnBehalf;
                    var dataLength = dataString.Length;
                    int startPosition = 0;
                    StringBuilder sb = new StringBuilder();
                    while (startPosition < dataLength)
                    {
                        var copyDataLength = 0;
                        if ((startPosition + 3) > dataLength)
                            copyDataLength = dataLength - startPosition;
                        else
                            copyDataLength = 3;
                        if (startPosition == 0)
                        {
                            sb.Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        else
                        {
                            sb.Append(";")
                              .Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        startPosition += 3;
                    }
                    paraValues[48] = sb.ToString();
                }

                var b2StartIndex = 49;
                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[b2StartIndex] = null;
                else
                    paraValues[b2StartIndex] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.Atc == null)
                    paraValues[b2StartIndex + 1] = null;
                else
                    paraValues[b2StartIndex + 1] = bicAuthInfoData.tokenB2Data.Atc;

                if (bicAuthInfoData.tokenB2Data?.IssApplDataLgth == null)
                {
                    paraValues[b2StartIndex + 2] = null;
                    paraValues[b2StartIndex + 5] = null;
                    paraValues[b2StartIndex + 6] = null;
                }
                else
                {
                    int issApplDataLgth = Convert.ToInt32(bicAuthInfoData.tokenB2Data.IssApplDataLgth);
                    var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       bicAuthInfoData.tokenB2Data.IssApplData,
                                                                       creditCardType).Split(';');
                    if (ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = ColaCommUtility.HexString2BinString(data9F10[2]);
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = data9F10[0];
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = data9F10[1];
                        data9F10.CopyTo(data9F10Array, 0);
                    }
                    else
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = null;
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = null;
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = null;
                    }
                }

                if (bicAuthInfoData.tokenB2Data?.Arqc == null)
                    paraValues[b2StartIndex + 3] = null;
                else
                    paraValues[b2StartIndex + 3] = bicAuthInfoData.tokenB2Data.Arqc;

                if (bicAuthInfoData.tokenB2Data?.UnpredictNum == null)
                    paraValues[b2StartIndex + 4] = null;
                else
                    paraValues[b2StartIndex + 4] = bicAuthInfoData.tokenB2Data.UnpredictNum;

                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[56] = null;
                else
                    paraValues[56] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[57] = null;
                else
                    paraValues[57] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[58] = null;
                else
                    paraValues[58] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[59] = null;
                else
                    paraValues[59] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[60] = null;
                else
                    paraValues[60] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                    paraValues[62] = null;
                else
                    paraValues[62] = bicAuthInfoData.tokenC0Data.CavvAavResults;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[63] = null;
                else
                    paraValues[63] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[64] = null;
                else
                    paraValues[64] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[65] = null;
                else
                    paraValues[65] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[66] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[66] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[66] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[67] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxnRespCde;
                else
                    paraValues[67] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[68] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).SettlementFlag;
                else
                    paraValues[68] = null;

                if ((bicAuthInfoData.tokenC5Data) is TokenNCCCInstallmentC5)
                    paraValues[69] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).PeriodNumber;
                else
                    paraValues[69] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[70] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[70] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[71] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[71] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[72] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[72] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[73] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[73] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[74] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[74] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[75] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[75] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[76] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[76] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[77] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[77] = null;

                paraValues[78] = null;
                paraValues[79] = null;
                paraValues[80] = null;
                paraValues[81] = null;
                paraValues[82] = null;
                paraValues[83] = null;
                paraValues[84] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Twin0100AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Twin0100AuthProcess Display Store Procedure Input Data  ---");
                }

                // Twin0100AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
                // authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
                // authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
                // authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
                // authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
                // authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
                // authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
                // authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
                // authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
                // authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoS127NegDelFlag       	varchar(01)	    Output	 --NegFile刪除註記 initial 時請放null即可註記為D時, 傳送一筆MessageType :0300 Detele Neg File
                // @t04ErrorFlg	                varchar(01) 	Output	 Field 63 Token04:Error-flag Token04.ErrorFlg
                // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 63 Token04:Crd-very-flag Token04.CrdVeryFlg
                // @tC0CavvResult               varchar(01) 	Output   Field 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
                // @tC5InstTxInd        	    varchar(01) 	Output	 Field 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
                // @tC5InstTxResp       	    varchar(02) 	Output	 Field 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
                // @tC5DownPayment      	    varchar(08) 	Output	 Field 63 Token C5 Down Payment<分期,首期>
                // @tC5InstPayment      	    varchar(08) 	Output	 Field 63 Token C5 Installment Payment<分期, 每期>
                // @tC5InstFee          	    varchar(06) 	Output	 Field 63 Token C5 Formality-Fee<分期, 手續費/利息>
                // @tC5PointRedemption  	    varchar(08) 	Output	 Field 63 Token C5 Point of Redemption<紅利,折扺點數>
                // @tC5SignPoint        	    varchar(01) 	Output	 Field 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
                // @tC5PointBalance     	    varchar(08) 	Output	 Field 63 Token C5 Point of Balance<紅利, 餘點>
                // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
                // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 63 Token C5 <紅利註記>
                // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureTWIN0100AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Twin0100AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Twin0100AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Twin0120AuthProcess Process Message Type 0220 Authorization Flow For TWIN
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Twin0120AuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 44;
            const int outputParaCount = 2;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureTWIN0120AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = bicAuthInfoData.isoP41TermNbr;
                paraValues[20] = bicAuthInfoData.isoP42Merchant;
                paraValues[21] = bicAuthInfoData.isoP43MerName;
                paraValues[22] = bicAuthInfoData.isoP43MerCity;
                paraValues[23] = bicAuthInfoData.isoP43LocCountry;
                paraValues[24] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[25] = bicAuthInfoData.isoS127Pws;
                paraValues[26] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[27] = bicAuthInfoData.isoS127MpFlag;
                paraValues[28] = bicAuthInfoData.isoS127Id;
                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[29] = null;
                else
                    paraValues[29] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[30] = null;
                else
                    paraValues[30] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[32] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[32] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[32] = null;
                }
                paraValues[33] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[33] = null;
                    else
                        paraValues[33] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[34] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[34] = null;
                    else
                        paraValues[34] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[35] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[35] = null;
                    else
                        paraValues[35] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Twin0120AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Twin0120AuthProcess Display Store Procedure Input Data  ---");
                }

                // Twin0120AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureTWIN0120AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Twin0120AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Twin0120AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Twin0200AuthProcess Process Message Type 0200 Authorization Flow For TWIN
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <param name="data9F10Array"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Twin0200AuthProcess(NcccAuthInfoStruct bicAuthInfoData, string[] data9F10Array)
        {
            const int storeProcedureParameterCount = 85;
            const int outputParaCount = 22;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureTWIN0200AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP35Svc;
                paraValues[14] = bicAuthInfoData.isoP35Pvv;
                paraValues[15] = bicAuthInfoData.isoP35Pvki;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = null;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoP52PinBlock;
                paraValues[26] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[27] = bicAuthInfoData.isoS127CellPhone;
                paraValues[28] = bicAuthInfoData.isoS127Dob;
                paraValues[29] = bicAuthInfoData.isoS127Pws;
                paraValues[30] = null;
                paraValues[31] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[32] = bicAuthInfoData.isoS127MpFlag;
                paraValues[33] = bicAuthInfoData.isoS127Id;
                paraValues[34] = bicAuthInfoData.arqcCheck;
                paraValues[35] = bicAuthInfoData.arqcChkResult;
                paraValues[36] = bicAuthInfoData.cvvCheck;
                paraValues[37] = bicAuthInfoData.cvvChkResult;
                paraValues[38] = bicAuthInfoData.cvv2Check;
                paraValues[39] = bicAuthInfoData.cvv2ChkResult;
                paraValues[40] = bicAuthInfoData.cavvCheck;
                paraValues[41] = bicAuthInfoData.cavvChkResult;
                paraValues[42] = bicAuthInfoData.pvvCheck;
                paraValues[43] = bicAuthInfoData.pvvChkResult;
                paraValues[44] = bicAuthInfoData.pvvDobCheck;
                paraValues[45] = bicAuthInfoData.pvvDobChkResult;

                if (bicAuthInfoData.token04Data?.ErrorFlg == null)
                    paraValues[46] = null;
                else
                    paraValues[46] = bicAuthInfoData.token04Data.ErrorFlg;

                if (bicAuthInfoData.token04Data?.CrdVeryFlg == null)
                    paraValues[47] = null;
                else
                    paraValues[47] = bicAuthInfoData.token04Data.CrdVeryFlg;

                if (bicAuthInfoData.tokenB0Data?.OnBehalf == null)
                    paraValues[48] = null;
                else
                {
                    string dataString = bicAuthInfoData.tokenB0Data.OnBehalf;
                    var dataLength = dataString.Length;
                    int startPosition = 0;
                    StringBuilder sb = new StringBuilder();
                    while (startPosition < dataLength)
                    {
                        var copyDataLength = 0;
                        if ((startPosition + 3) > dataLength)
                            copyDataLength = dataLength - startPosition;
                        else
                            copyDataLength = 3;
                        if (startPosition == 0)
                        {
                            sb.Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        else
                        {
                            sb.Append(";")
                              .Append(dataString.Substring(startPosition, copyDataLength));
                        }
                        startPosition += 3;
                    }
                    paraValues[48] = sb.ToString();
                }

                var b2StartIndex = 49;
                if (bicAuthInfoData.tokenB2Data?.Tvr == null)
                    paraValues[b2StartIndex] = null;
                else
                    paraValues[b2StartIndex] = ColaCommUtility.HexString2BinString(bicAuthInfoData.tokenB2Data.Tvr);

                if (bicAuthInfoData.tokenB2Data?.Atc == null)
                    paraValues[b2StartIndex + 1] = null;
                else
                    paraValues[b2StartIndex + 1] = bicAuthInfoData.tokenB2Data.Atc;

                if (bicAuthInfoData.tokenB2Data?.IssApplDataLgth == null)
                {
                    paraValues[b2StartIndex + 2] = null;
                    paraValues[b2StartIndex + 5] = null;
                    paraValues[b2StartIndex + 6] = null;
                }
                else
                {
                    int issApplDataLgth = Convert.ToInt32(bicAuthInfoData.tokenB2Data.IssApplDataLgth);
                    var creditCardType = CreditCardUtility.FindCardType(bicAuthInfoData.isoP2CardNbr);
                    string[] data9F10 = ColaCommUtility.BicGet9F10Data(issApplDataLgth,
                                                                       bicAuthInfoData.tokenB2Data.IssApplData,
                                                                       creditCardType).Split(';');
                    if (ColaCommUtility.Is9F10DataValid(data9F10))
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = ColaCommUtility.HexString2BinString(data9F10[2]);
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = data9F10[0];
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = data9F10[1];
                        data9F10.CopyTo(data9F10Array, 0);
                    }
                    else
                    {
                        //tB2CVR = data9F10[2];
                        paraValues[b2StartIndex + 2] = null;
                        //tB2DerivKeyIndex
                        paraValues[b2StartIndex + 5] = null;
                        //tB2CryptoVerNum
                        paraValues[b2StartIndex + 6] = null;
                    }
                }

                if (bicAuthInfoData.tokenB2Data?.Arqc == null)
                    paraValues[b2StartIndex + 3] = null;
                else
                    paraValues[b2StartIndex + 3] = bicAuthInfoData.tokenB2Data.Arqc;

                if (bicAuthInfoData.tokenB2Data?.UnpredictNum == null)
                    paraValues[b2StartIndex + 4] = null;
                else
                    paraValues[b2StartIndex + 4] = bicAuthInfoData.tokenB2Data.UnpredictNum;

                if (bicAuthInfoData.tokenB4Data?.PtSrvEntryMde == null)
                    paraValues[56] = null;
                else
                    paraValues[56] = bicAuthInfoData.tokenB4Data.PtSrvEntryMde;

                if (bicAuthInfoData.tokenB4Data?.TermEntryCap == null)
                    paraValues[57] = null;
                else
                    paraValues[57] = bicAuthInfoData.tokenB4Data.TermEntryCap;

                if (bicAuthInfoData.tokenB4Data?.LastEmvStat == null)
                    paraValues[58] = null;
                else
                    paraValues[58] = bicAuthInfoData.tokenB4Data.LastEmvStat;

                if (bicAuthInfoData.tokenB4Data?.RsnOnLineCde == null)
                    paraValues[59] = null;
                else
                    paraValues[59] = bicAuthInfoData.tokenB4Data.RsnOnLineCde;

                if (bicAuthInfoData.tokenC0Data?.MotoOrECFlag == null)
                    paraValues[60] = null;
                else
                    paraValues[60] = bicAuthInfoData.tokenC0Data.MotoOrECFlag;

                if (bicAuthInfoData.tokenC0Data?.UcafAuthnCollInd == null)
                    paraValues[61] = null;
                else
                    paraValues[61] = bicAuthInfoData.tokenC0Data.UcafAuthnCollInd;

                if (bicAuthInfoData.tokenC0Data?.CavvAavResults == null)
                    paraValues[62] = null;
                else
                    paraValues[62] = bicAuthInfoData.tokenC0Data.CavvAavResults;

                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[63] = null;
                else
                    paraValues[63] = bicAuthInfoData.tokenC4Data?.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[64] = null;
                else
                    paraValues[64] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[65] = null;
                else
                    paraValues[65] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[66] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[66] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).LoyaltyTxInd;
                    else
                        paraValues[66] = null;
                }

                if (bicAuthInfoData.tokenC5Data is TokenNCCCInstallmentC5)
                    paraValues[67] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).InstallmentTxnRespCde;
                else
                    paraValues[67] = null;

                if (bicAuthInfoData.tokenC5Data is TokenNCCCRoyaltyC5)
                    paraValues[68] = ((TokenNCCCRoyaltyC5)(bicAuthInfoData.tokenC5Data)).SettlementFlag;
                else
                    paraValues[68] = null;

                if ((bicAuthInfoData.tokenC5Data) is TokenNCCCInstallmentC5)
                    paraValues[69] = ((TokenNCCCInstallmentC5)(bicAuthInfoData.tokenC5Data)).PeriodNumber;
                else
                    paraValues[69] = null;

                var tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[70] = tokenInstallmentC5.DownPayment;
                else
                    paraValues[70] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[71] = tokenInstallmentC5.InstallmentPayments;
                else
                    paraValues[71] = null;

                tokenInstallmentC5 = bicAuthInfoData.tokenC5Data as TokenNCCCInstallmentC5;
                if (tokenInstallmentC5 != null)
                    paraValues[72] = tokenInstallmentC5.FormalityFee;
                else
                    paraValues[72] = null;

                var tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[73] = tokenRoyaltyC5.PointsOfRedemption;
                else
                    paraValues[73] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[74] = tokenRoyaltyC5.SignPointOfBalance;
                else
                    paraValues[74] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[75] = tokenRoyaltyC5.PointOfBalance;
                else
                    paraValues[75] = null;

                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[76] = tokenRoyaltyC5.PaidCreditAmount;
                else
                    paraValues[76] = null;
                tokenRoyaltyC5 = bicAuthInfoData.tokenC5Data as TokenNCCCRoyaltyC5;
                if (tokenRoyaltyC5 != null)
                    paraValues[77] = tokenRoyaltyC5.LoyaltyTxInd;
                else
                    paraValues[77] = null;

                paraValues[78] = null;
                paraValues[79] = null;
                paraValues[80] = null;
                paraValues[81] = null;
                paraValues[82] = null;
                paraValues[83] = null;
                paraValues[84] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Twin0200AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Twin0200AuthProcess Display Store Procedure Input Data  ---");
                }

                // Twin0200AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
                // authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
                // authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
                // authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
                // authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
                // authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
                // authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
                // authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
                // authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
                // authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @isoS127NegDelFlag       	varchar(01)	    Output	 --NegFile刪除註記 initial 時請放null即可註記為D時, 傳送一筆MessageType :0300 Detele Neg File
                // @t04ErrorFlg	                varchar(01) 	Output	 Field 63 Token04:Error-flag Token04.ErrorFlg
                // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 63 Token04:Crd-very-flag Token04.CrdVeryFlg
                // @tC0CavvResult               varchar(01) 	Output   Field 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
                // @tC5InstTxInd        	    varchar(01) 	Output	 Field 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
                // @tC5InstTxResp       	    varchar(02) 	Output	 Field 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
                // @tC5DownPayment      	    varchar(08) 	Output	 Field 63 Token C5 Down Payment<分期,首期>
                // @tC5InstPayment      	    varchar(08) 	Output	 Field 63 Token C5 Installment Payment<分期, 每期>
                // @tC5InstFee          	    varchar(06) 	Output	 Field 63 Token C5 Formality-Fee<分期, 手續費/利息>
                // @tC5PointRedemption  	    varchar(08) 	Output	 Field 63 Token C5 Point of Redemption<紅利,折扺點數>
                // @tC5SignPoint        	    varchar(01) 	Output	 Field 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
                // @tC5PointBalance     	    varchar(08) 	Output	 Field 63 Token C5 Point of Balance<紅利, 餘點>
                // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
                // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 63 Token C5 <紅利註記>
                // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureTWIN0200AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Twin0200AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Twin0200AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Twin0220AuthProcess Process Message Type 0220 Authorization Flow For TWIN
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Twin0220AuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 51;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureTWIN0220AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageHeader;
                paraValues[02] = bicAuthInfoData.isoMessageType;
                paraValues[03] = bicAuthInfoData.isoP2CardNbr;
                paraValues[04] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[05] = bicAuthInfoData.isoP4Amt;
                paraValues[06] = bicAuthInfoData.isoP7TxDte;
                paraValues[07] = bicAuthInfoData.isoP7TxTime;
                paraValues[08] = bicAuthInfoData.isoP11TraceNo;
                paraValues[09] = bicAuthInfoData.isoP18MccCode;
                paraValues[10] = bicAuthInfoData.isoP22Em;
                paraValues[11] = bicAuthInfoData.isoP25CondCode;
                paraValues[12] = bicAuthInfoData.isoP32BinIca;
                paraValues[13] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[14] = bicAuthInfoData.isoP35Svc;
                paraValues[15] = bicAuthInfoData.isoP35Pvv;
                paraValues[16] = bicAuthInfoData.isoP37RefNbr;
                paraValues[17] = bicAuthInfoData.isoP38AuthCode;
                paraValues[18] = bicAuthInfoData.isoP39Rsp;
                paraValues[19] = bicAuthInfoData.isoP39Rsp;
                paraValues[20] = bicAuthInfoData.isoP41TermNbr;
                paraValues[21] = bicAuthInfoData.isoP42Merchant;
                paraValues[22] = bicAuthInfoData.isoP43MerName;
                paraValues[23] = bicAuthInfoData.isoP43MerCity;
                paraValues[24] = bicAuthInfoData.isoP43LocCountry;
                paraValues[25] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[26] = bicAuthInfoData.isoS127Pws;
                paraValues[27] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[28] = bicAuthInfoData.isoS127MpFlag;
                paraValues[29] = bicAuthInfoData.isoS127Id;
                if (bicAuthInfoData.tokenC4Data?.TermAttendInd == null)
                    paraValues[30] = null;
                else
                    paraValues[30] = bicAuthInfoData.tokenC4Data.TermAttendInd;

                if (bicAuthInfoData.tokenC4Data?.ChActvtInd == null)
                    paraValues[31] = null;
                else
                    paraValues[31] = bicAuthInfoData.tokenC4Data.ChActvtInd;

                if (bicAuthInfoData.tokenC4Data?.TermInputCapInd == null)
                    paraValues[32] = null;
                else
                    paraValues[32] = bicAuthInfoData.tokenC4Data.TermInputCapInd;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[33] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[33] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[33] = null;
                }
                paraValues[34] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentTxnRespCde == null)
                        paraValues[34] = null;
                    else
                        paraValues[34] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxnRespCde;
                }
                paraValues[35] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[35] = null;
                    else
                        paraValues[35] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }
                paraValues[36] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.PeriodNumber == null)
                        paraValues[36] = null;
                    else
                        paraValues[36] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).PeriodNumber;
                }
                paraValues[37] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.DownPayment == null)
                        paraValues[37] = null;
                    else
                        paraValues[37] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).DownPayment;
                }

                paraValues[38] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.InstallmentPayments == null)
                        paraValues[38] = null;
                    else
                        paraValues[38] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentPayments;
                }

                paraValues[39] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                {
                    if (((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data)?.FormalityFee == null)
                        paraValues[39] = null;
                    else
                        paraValues[39] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).FormalityFee;
                }

                paraValues[40] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointsOfRedemption == null)
                        paraValues[40] = null;
                    else
                        paraValues[40] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointsOfRedemption;
                }

                paraValues[41] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SignPointOfBalance == null)
                        paraValues[41] = null;
                    else
                        paraValues[41] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SignPointOfBalance;
                }

                paraValues[42] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PointOfBalance == null)
                        paraValues[42] = null;
                    else
                        paraValues[42] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PointOfBalance;
                }

                paraValues[43] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.PaidCreditAmount == null)
                        paraValues[43] = null;
                    else
                        paraValues[43] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).PaidCreditAmount;
                }

                paraValues[44] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd == null)
                        paraValues[44] = null;
                    else
                        paraValues[44] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.LoyaltyTxInd;
                }
                paraValues[45] = null;
                paraValues[46] = null;
                paraValues[47] = null;
                paraValues[48] = null;
                paraValues[49] = null;
                paraValues[50] = null;

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Twin0220AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Twin0220AuthProcess Display Store Procedure Input Data  ---");
                }

                // Twin0220AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq        , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP38AuthCode          	varchar(06) 	Output	 --p38
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureTWIN0220AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Twin0220AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Twin0220AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Twin0420AuthProcess Process Message Type 0420 Reversal Flow For TWIN
        /// </summary>
        /// <param name="bicAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Twin0420AuthProcess(NcccAuthInfoStruct bicAuthInfoData)
        {
            const int storeProcedureParameterCount = 35;
            const int outputParaCount = 6;

            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureTWIN0420AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = bicAuthInfoData.tranLogKey;
                paraValues[01] = bicAuthInfoData.isoMessageType;
                paraValues[02] = bicAuthInfoData.isoP2CardNbr;
                paraValues[03] = bicAuthInfoData.isoP3ProcessCde;
                paraValues[04] = bicAuthInfoData.isoP4Amt;
                paraValues[05] = bicAuthInfoData.isoP7TxDte;
                paraValues[06] = bicAuthInfoData.isoP7TxTime;
                paraValues[07] = bicAuthInfoData.isoP11TraceNo;
                paraValues[08] = bicAuthInfoData.isoP18MccCode;
                paraValues[09] = bicAuthInfoData.isoP22Em;
                paraValues[10] = bicAuthInfoData.isoP25CondCode;
                paraValues[11] = bicAuthInfoData.isoP32BinIca;
                paraValues[12] = bicAuthInfoData.isoP35ExpirDte;
                paraValues[13] = bicAuthInfoData.isoP37RefNbr;
                paraValues[14] = bicAuthInfoData.isoP38AuthCode;
                paraValues[15] = bicAuthInfoData.isoP39Rsp;
                paraValues[16] = null;
                paraValues[17] = bicAuthInfoData.isoP41TermNbr;
                paraValues[18] = bicAuthInfoData.isoP42Merchant;
                paraValues[19] = bicAuthInfoData.isoP43MerName;
                paraValues[20] = bicAuthInfoData.isoP43MerCity;
                paraValues[21] = bicAuthInfoData.isoP43LocCountry;
                paraValues[22] = bicAuthInfoData.isoS95ReplaceAmt;
                paraValues[23] = bicAuthInfoData.isoS127Pws;
                paraValues[24] = bicAuthInfoData.isoS127EdcFunction;
                paraValues[25] = bicAuthInfoData.isoS127MpFlag;
                paraValues[26] = bicAuthInfoData.isoS127Id;

                paraValues[7] = null;
                if (bicAuthInfoData.c5InstallRebateFlag.Equals('I'))
                    paraValues[27] = ((TokenNCCCInstallmentC5)bicAuthInfoData.tokenC5Data).InstallmentTxInd;
                else
                {
                    if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                        paraValues[27] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).LoyaltyTxInd;
                    else
                        paraValues[27] = null;
                }
                paraValues[28] = null;
                paraValues[29] = null;
                paraValues[30] = null;
                paraValues[31] = null;
                paraValues[32] = null;
                paraValues[33] = null;
                paraValues[34] = null;

                if (bicAuthInfoData.c5InstallRebateFlag.Equals('R'))
                {
                    if (((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data)?.SettlementFlag == null)
                        paraValues[27] = null;
                    else
                        paraValues[27] = ((TokenNCCCRoyaltyC5)bicAuthInfoData.tokenC5Data).SettlementFlag;
                }

                if (Log.IsTraceEnabled)
                {
                    Log.Trace("--- Start Twin0420AuthProcess Display Store Procedure Input Data ---");
                    for (var index = 0; index < paraValues.Length; index++)
                    {
                        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                    }
                    Log.Trace("--- End   Twin0420AuthProcess Display Store Procedure Input Data  ---");
                }

                // Twin0420AuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp           , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkNbr           , authReturnString[03]:@vdLinkNbrSeq
                // authReturnString[04]:@vdActionCode        , authReturnString[05]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
                // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureTWIN0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Twin0420AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(bicAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(bicAuthInfoData)}\n");
                    }

                    Log.Trace($"Twin0420AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// Esvc0100AutProcesss Process Message Type 0100 Authorization Flow For ETicket
        /// </summary>
        /// <param name="esvcAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Esvc0100AuthProcess(EsvcAuthInfoStruct esvcAuthInfoData)
        {
            const int storeProcedureParameterCount = 30;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureESVC0100AuthProcess);
                var paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = esvcAuthInfoData.tranLogKey;
                paraValues[01] = esvcAuthInfoData.isoMessageType;
                paraValues[02] = esvcAuthInfoData.isoP2PidNbr;
                paraValues[03] = esvcAuthInfoData.isoP3ProcessCde;
                paraValues[04] = esvcAuthInfoData.isoP4Amt;
                paraValues[05] = esvcAuthInfoData.isoP7TxDte;
                paraValues[06] = esvcAuthInfoData.isoP7TxTime;
                paraValues[07] = esvcAuthInfoData.isoP11TraceNo;
                paraValues[08] = esvcAuthInfoData.isoP14ExpirDte;
                paraValues[09] = esvcAuthInfoData.isoP32BinIca;
                paraValues[10] = esvcAuthInfoData.isoP36NTID;
                paraValues[11] = esvcAuthInfoData.isoP37RefNbr;
                paraValues[12] = esvcAuthInfoData.isoP39Rsp;
                paraValues[13] = null;
                paraValues[14] = esvcAuthInfoData.isoP41TermNbr;
                paraValues[15] = esvcAuthInfoData.isoP42Merchant;
                paraValues[16] = esvcAuthInfoData.isoP45SAMId;
                paraValues[17] = esvcAuthInfoData.isoP58T06;
                paraValues[18] = esvcAuthInfoData.isoP58T07;
                paraValues[19] = esvcAuthInfoData.isoP58T10;
                paraValues[20] = esvcAuthInfoData.isoP61T03;
                paraValues[21] = esvcAuthInfoData.isoP61T04; ;
                paraValues[22] = esvcAuthInfoData.isoP63SystemId;
                paraValues[23] = null;
                paraValues[24] = null;
                paraValues[25] = null;
                paraValues[26] = null;
                paraValues[27] = null;
                paraValues[28] = null;
                paraValues[29] = null;
                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp       , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkCreditNbr , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq	 , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction             varchar(01)     Output   --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkCreditNbr             varchar(20)     Output   --VD 圈存信用卡號
                // @vdLinkNbr                   varchar(20)     Output   --VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   --VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   --VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15)     Output   --VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureESVC0100AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Esvc0100AuthProcess Exception");

                return (null);
            }
        }

        /// <summary>
        /// Esvc0100AutProcesss Process Message Type 0100 Authorization Flow For ETicket
        /// </summary>
        /// <param name="esvcAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"></param>
        /// <param name="responseXML"></param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Esvc0100AuthProcess(EsvcAuthInfoStruct esvcAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 30;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureESVC0100AuthProcess);
                var paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = esvcAuthInfoData.tranLogKey;
                paraValues[01] = esvcAuthInfoData.isoMessageType;
                paraValues[02] = esvcAuthInfoData.isoP2PidNbr;
                paraValues[03] = esvcAuthInfoData.isoP3ProcessCde;
                paraValues[04] = esvcAuthInfoData.isoP4Amt;
                paraValues[05] = esvcAuthInfoData.isoP7TxDte;
                paraValues[06] = esvcAuthInfoData.isoP7TxTime;
                paraValues[07] = esvcAuthInfoData.isoP11TraceNo;
                paraValues[08] = esvcAuthInfoData.isoP14ExpirDte;
                paraValues[09] = esvcAuthInfoData.isoP32BinIca;
                paraValues[10] = esvcAuthInfoData.isoP36NTID;
                paraValues[11] = esvcAuthInfoData.isoP37RefNbr;
                paraValues[12] = esvcAuthInfoData.isoP39Rsp;
                paraValues[13] = null;
                paraValues[14] = esvcAuthInfoData.isoP41TermNbr;
                paraValues[15] = esvcAuthInfoData.isoP42Merchant;
                paraValues[16] = esvcAuthInfoData.isoP45SAMId;
                paraValues[17] = esvcAuthInfoData.isoP58T06;
                paraValues[18] = esvcAuthInfoData.isoP58T07;
                paraValues[19] = esvcAuthInfoData.isoP58T10;
                paraValues[20] = esvcAuthInfoData.isoP61T03;
                paraValues[21] = esvcAuthInfoData.isoP61T04; ;
                paraValues[22] = esvcAuthInfoData.isoP63SystemId;
                if (responseXML.TxRs == null)
                {
                    paraValues[23] = authReturnString[2];
                    paraValues[24] = authReturnString[3];
                    paraValues[25] = authReturnString[4];
                    paraValues[26] = authReturnString[5];
                    paraValues[29] = 0;
                }
                else
                {
                    paraValues[23] = responseXML.TxRs.CreditNo;
                    paraValues[24] = responseXML.TxRs.AcctNo;
                    paraValues[25] = responseXML.TxRs.CardNo;
                    paraValues[26] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[29] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[29] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }

                paraValues[28] = responseXML.ResponseHeader.StatusCode;
                paraValues[27] = authReturnString[6];

                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp , authReturnString[01]:@isoP39RspAction

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureESVC0100AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Esvc0100AuthProcess Exception");

                return (null);
            }
        }

        /// <summary>
        /// Esvc0120AutProcesss Process Message Type 0120 Authorization Flow For ETicket
        /// <param name="esvcAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Esvc0120AuthProcess(EsvcAuthInfoStruct esvcAuthInfoData)
        {
            const int storeProcedureParameterCount = 28;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureESVC0120AuthProcess);
                var paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = esvcAuthInfoData.tranLogKey;
                paraValues[01] = esvcAuthInfoData.isoMessageType;
                paraValues[02] = esvcAuthInfoData.isoP2PidNbr;
                paraValues[03] = esvcAuthInfoData.isoP3ProcessCde;
                paraValues[04] = esvcAuthInfoData.isoP4Amt;
                paraValues[05] = esvcAuthInfoData.isoP7TxDte;
                paraValues[06] = esvcAuthInfoData.isoP7TxTime;
                paraValues[07] = esvcAuthInfoData.isoP11TraceNo;
                paraValues[08] = esvcAuthInfoData.isoP32BinIca;
                paraValues[09] = esvcAuthInfoData.isoP36NTID;
                paraValues[10] = esvcAuthInfoData.isoP37RefNbr;
                paraValues[11] = esvcAuthInfoData.isoP39Rsp;
                paraValues[12] = null;
                paraValues[13] = esvcAuthInfoData.isoP41TermNbr;
                paraValues[14] = esvcAuthInfoData.isoP42Merchant;
                paraValues[15] = esvcAuthInfoData.isoP45SAMId;
                paraValues[16] = esvcAuthInfoData.isoP58T06;
                paraValues[17] = esvcAuthInfoData.isoP58T07;
                paraValues[18] = esvcAuthInfoData.isoP61T03;
                paraValues[19] = esvcAuthInfoData.isoP61T04; ;
                paraValues[20] = esvcAuthInfoData.isoP63SystemId;
                paraValues[21] = null;
                paraValues[22] = null;
                paraValues[23] = null;
                paraValues[24] = null;
                paraValues[25] = null;
                paraValues[26] = null;
                paraValues[27] = null;

                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp       , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkCreditNbr , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq	 , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction             varchar(01)     Output   --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkCreditNbr             varchar(20)     Output   --VD 圈存信用卡號
                // @vdLinkNbr                   varchar(20)     Output   --VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   --VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   --VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15)     Output   --VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureESVC0120AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Esvc0120AuthProcess Exception");

                return (null);
            }
        }

        /// <summary>
        /// Esvc0120AutProcesss Process Message Type 0120 Authorization Flow For ETicket
        /// <param name="esvcAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"> </param>  
        /// <param name="responseXML">  </param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Esvc0120AuthProcess(EsvcAuthInfoStruct esvcAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 28;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureESVC0120AuthProcess);
                var paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = esvcAuthInfoData.tranLogKey;
                paraValues[01] = esvcAuthInfoData.isoMessageType;
                paraValues[02] = esvcAuthInfoData.isoP2PidNbr;
                paraValues[03] = esvcAuthInfoData.isoP3ProcessCde;
                paraValues[04] = esvcAuthInfoData.isoP4Amt;
                paraValues[05] = esvcAuthInfoData.isoP7TxDte;
                paraValues[06] = esvcAuthInfoData.isoP7TxTime;
                paraValues[07] = esvcAuthInfoData.isoP11TraceNo;
                paraValues[08] = esvcAuthInfoData.isoP32BinIca;
                paraValues[09] = esvcAuthInfoData.isoP36NTID;
                paraValues[10] = esvcAuthInfoData.isoP37RefNbr;
                paraValues[11] = esvcAuthInfoData.isoP39Rsp;
                paraValues[12] = null;
                paraValues[13] = esvcAuthInfoData.isoP41TermNbr;
                paraValues[14] = esvcAuthInfoData.isoP42Merchant;
                paraValues[15] = esvcAuthInfoData.isoP45SAMId;
                paraValues[16] = esvcAuthInfoData.isoP58T06;
                paraValues[17] = esvcAuthInfoData.isoP58T07;
                paraValues[18] = esvcAuthInfoData.isoP61T03;
                paraValues[19] = esvcAuthInfoData.isoP61T04; ;
                paraValues[20] = esvcAuthInfoData.isoP63SystemId;
                if (responseXML.TxRs == null)
                {
                    paraValues[21] = authReturnString[2];
                    paraValues[22] = authReturnString[3];
                    paraValues[23] = authReturnString[4];
                    paraValues[24] = authReturnString[5];
                    paraValues[25] = authReturnString[6];
                    paraValues[27] = 0;
                }
                else
                {
                    paraValues[21] = responseXML.TxRs.CreditNo;
                    paraValues[22] = responseXML.TxRs.AcctNo;
                    paraValues[23] = responseXML.TxRs.CardNo;
                    paraValues[24] = responseXML.TxRs.FuncCode;
                    paraValues[25] = authReturnString[6];
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[27] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[27] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);

                }
                paraValues[26] = responseXML.ResponseHeader.StatusCode;

                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp       , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkCreditNbr , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq	 , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction             varchar(01)     Output   --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkCreditNbr             varchar(20)     Output   --VD 圈存信用卡號
                // @vdLinkNbr                   varchar(20)     Output   --VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   --VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   --VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15)     Output   --VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureESVC0120AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Esvc0120AuthProcess Exception");

                return (null);
            }
        }

        //// <summary>
        /// Esvc0120AutProcesss Process Message Type 0420 Authorization Flow For ETicket
        /// <param name="esvcAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Esvc0420AuthProcess(EsvcAuthInfoStruct esvcAuthInfoData)
        {
            const int storeProcedureParameterCount = 31;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureESVC0420AuthProcess);
                var paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = esvcAuthInfoData.tranLogKey;
                paraValues[01] = esvcAuthInfoData.isoMessageType;
                paraValues[02] = esvcAuthInfoData.isoP2PidNbr;
                paraValues[03] = esvcAuthInfoData.isoP3ProcessCde;
                paraValues[04] = esvcAuthInfoData.isoP4Amt;
                paraValues[05] = esvcAuthInfoData.isoP7TxDte;
                paraValues[06] = esvcAuthInfoData.isoP7TxTime;
                paraValues[07] = esvcAuthInfoData.isoP11TraceNo;
                paraValues[08] = esvcAuthInfoData.isoP14ExpirDte;
                paraValues[09] = esvcAuthInfoData.isoP32BinIca;
                paraValues[10] = esvcAuthInfoData.isoP36NTID;
                paraValues[11] = esvcAuthInfoData.isoP37RefNbr;
                paraValues[12] = esvcAuthInfoData.isoP39Rsp;
                paraValues[13] = null;
                paraValues[14] = esvcAuthInfoData.isoP41TermNbr;
                paraValues[15] = esvcAuthInfoData.isoP42Merchant;
                paraValues[16] = esvcAuthInfoData.isoP45SAMId;
                paraValues[17] = esvcAuthInfoData.isoP63SystemId;
                paraValues[18] = esvcAuthInfoData.isoS90MessageType;
                paraValues[19] = esvcAuthInfoData.isoS90TxDte;
                paraValues[20] = esvcAuthInfoData.isoS90TxTime;
                paraValues[21] = esvcAuthInfoData.isoS90Trace;
                paraValues[22] = esvcAuthInfoData.isoS90BinIca;
                paraValues[23] = esvcAuthInfoData.isoS90RefNbr;
                paraValues[24] = null;
                paraValues[25] = null;
                paraValues[26] = null;
                paraValues[27] = null;
                paraValues[28] = null;
                paraValues[29] = null;
                paraValues[30] = null;

                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp       , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkCreditNbr , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq	 , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction             varchar(01)     Output   --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkCreditNbr             varchar(20)     Output   --VD 圈存信用卡號
                // @vdLinkNbr                   varchar(20)     Output   --VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   --VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   --VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15)     Output   --VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureESVC0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Esvc0420AuthProcess Exception");

                return (null);
            }
        }

        //// <summary>
        /// Esvc0120AutProcesss Process Message Type 0420 Authorization Flow For ETicket
        /// <param name="esvcAuthInfoData">Authorization Data Record</param>
        /// <param name="authReturnString"> </param>
        /// <param name="responseXML"> </param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] Esvc0420AuthProcess(EsvcAuthInfoStruct esvcAuthInfoData, string[] authReturnString, VDMessage.ResponseXML responseXML)
        {
            const int storeProcedureParameterCount = 31;
            const int outputParaCount = 7;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureESVC0420AuthProcess);
                var paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = esvcAuthInfoData.tranLogKey;
                paraValues[01] = esvcAuthInfoData.isoMessageType;
                paraValues[02] = esvcAuthInfoData.isoP2PidNbr;
                paraValues[03] = esvcAuthInfoData.isoP3ProcessCde;
                paraValues[04] = esvcAuthInfoData.isoP4Amt;
                paraValues[05] = esvcAuthInfoData.isoP7TxDte;
                paraValues[06] = esvcAuthInfoData.isoP7TxTime;
                paraValues[07] = esvcAuthInfoData.isoP11TraceNo;
                paraValues[08] = esvcAuthInfoData.isoP14ExpirDte;
                paraValues[09] = esvcAuthInfoData.isoP32BinIca;
                paraValues[10] = esvcAuthInfoData.isoP36NTID;
                paraValues[11] = esvcAuthInfoData.isoP37RefNbr;
                paraValues[12] = esvcAuthInfoData.isoP39Rsp;
                paraValues[13] = null;
                paraValues[14] = esvcAuthInfoData.isoP41TermNbr;
                paraValues[15] = esvcAuthInfoData.isoP42Merchant;
                paraValues[16] = esvcAuthInfoData.isoP45SAMId;
                paraValues[17] = esvcAuthInfoData.isoP63SystemId;
                paraValues[18] = esvcAuthInfoData.isoS90MessageType;
                paraValues[19] = esvcAuthInfoData.isoS90TxDte;
                paraValues[20] = esvcAuthInfoData.isoS90TxTime;
                paraValues[21] = esvcAuthInfoData.isoS90Trace;
                paraValues[22] = esvcAuthInfoData.isoS90BinIca;
                paraValues[23] = esvcAuthInfoData.isoS90RefNbr;
                if (responseXML.TxRs == null)
                {
                    paraValues[24] = authReturnString[2];
                    paraValues[25] = authReturnString[3];
                    paraValues[26] = authReturnString[4];
                    paraValues[27] = authReturnString[5];
                    paraValues[30] = 0;
                }
                else
                {
                    paraValues[24] = responseXML.TxRs.CreditNo;
                    paraValues[25] = responseXML.TxRs.AcctNo;
                    paraValues[26] = responseXML.TxRs.CardNo;
                    paraValues[27] = responseXML.TxRs.FuncCode;
                    if (!responseXML.TxRs.AvaiBalSign.Equals("+"))
                        paraValues[30] = Convert.ToDecimal(responseXML.TxRs.AvaiBal) * -1;
                    else
                        paraValues[30] = Convert.ToDecimal(responseXML.TxRs.AvaiBal);
                }
                paraValues[28] = authReturnString[6];
                paraValues[29] = responseXML.ResponseHeader.StatusCode;

                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp       , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@vdLinkCreditNbr , authReturnString[03]:@vdLinkNbr
                // authReturnString[04]:@vdLinkNbrSeq	 , authReturnString[05]:@vdActionCode
                // authReturnString[06]:@vdBlockAmt
                // Description
                // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
                // @isoP39RspAction             varchar(01)     Output   --對映於P39的授權回覆動作 A/D/R/P
                // @vdLinkCreditNbr             varchar(20)     Output   --VD 圈存信用卡號
                // @vdLinkNbr                   varchar(20)     Output   --VD 圈存銀行帳戶
                // @vdLinkNbrSeq                varchar(02)     Output   --VD 圈存帳戶序號
                // @vdActionCode                varchar(01)     Output   --VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
                // @vdBlockAmt                  varchar(15)     Output   --VD 圈存解圈金額

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureESVC0420AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Esvc0420AuthProcess Exception");

                return (null);
            }
        }

        /// <summary>
        /// Get Auth. Transaction Data For SMS Use
        /// </summary>
        /// <param name="storeProcedureName">Using Store Procedure Name</param>
        /// <param name="startDateTime">Set Start Key Value</param>
        /// <param name="getMaxCount">Get The Top Max. Record</param>
        public static DataTable SMSGetProcessAuthLogByStartKey(string storeProcedureName, string startDateTime, int getMaxCount)
        {
            try
            {
                var ds = new DataSet();
                using (var conn = new SqlConnection(ColaCommUtility.GetConnectionString() + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
                {
                    using (var sqlCmd = new SqlCommand())
                    {
                        sqlCmd.CommandText = storeProcedureName;
                        sqlCmd.Connection = conn;
                        sqlCmd.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter("@StartTranLogKey", SqlDbType.VarChar)
                        {
                            Size = 22,
                            Direction = ParameterDirection.Input,
                            Value = ParameterSanitizer.Sanitize(startDateTime)
                        };
                        sqlCmd.Parameters.Add(param);

                        param = new SqlParameter("@GetMaxCount", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Input,
                            Value = ParameterSanitizer.Sanitize(getMaxCount)
                        };
                        sqlCmd.Parameters.Add(param);

                        param = new SqlParameter("@ReturnCode", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output,
                            Value = -1
                        };
                        sqlCmd.Parameters.Add(param);
                        conn.Open();
                        var adapter = new SqlDataAdapter(sqlCmd);
                        adapter.Fill(ds);
                        conn.Close();
                    }
                }
                return (ds.Tables[0]);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SMS_GetProcessAuthLog Exception:{ex.Message}");
                return null;
            }

        }

        /// <summary>
        /// Set Process_Flag to 'Y' For Processed
        /// </summary>
        /// <param name="dataTable">Processed Key Value</param>
        /// <param name="storeProcedureName">Using Store Procedure Name</param>
        /// <param name="dataBaseDefineTranLogKeyListTranLogKeyName">Database User Define DataType Name</param>
        public static int SMSUpdateProcessAuthLog(DataTable dataTable, string storeProcedureName, string dataBaseDefineTranLogKeyListTranLogKeyName, string dataBaseDefineTranLogKeyListRtnCodeName)
        {
            int returnCode = 1;
            try
            {
                DataTable dt = new DataTable();
                var column = new DataColumn
                {
                    DataType = Type.GetType("System.String"),
                    ColumnName = dataBaseDefineTranLogKeyListTranLogKeyName
                };
                dt.Columns.Add(column);
                column = new DataColumn
                {
                    DataType = Type.GetType("System.String"),
                    ColumnName = dataBaseDefineTranLogKeyListRtnCodeName
                };
                dt.Columns.Add(column);
                foreach (DataRow source in dataTable.Rows)
                {
                    DataRow destRow = dt.NewRow();
                    destRow[dataBaseDefineTranLogKeyListTranLogKeyName] = source[0];
                    destRow[dataBaseDefineTranLogKeyListRtnCodeName] = source[1];
                    dt.Rows.Add(destRow);
                }
                using (var conn = new SqlConnection(ColaCommUtility.GetConnectionString() + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
                {
                    conn.Open();
                    using (SqlCommand sqlCmd = conn.CreateCommand())
                    {
                        sqlCmd.CommandText = storeProcedureName;
                        sqlCmd.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter
                        {
                            ParameterName = "@TranLogKeyList",
                            SqlDbType = SqlDbType.Structured,
                            Direction = ParameterDirection.Input,
                            TypeName = "[dbo].[TranLogKeyList]",
                            Value = dt
                        };
                        sqlCmd.Parameters.Add(param);

                        param = new SqlParameter
                        {
                            ParameterName = "@ReturnCode",
                            SqlDbType = SqlDbType.Int,
                            Direction = ParameterDirection.Output
                        };
                        sqlCmd.Parameters.Add(param);
                        sqlCmd.ExecuteNonQuery();
                        returnCode = (int)sqlCmd.Parameters["@ReturnCode"].Value;
                    }
                    conn.Close();
                }
                return (returnCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SMSUpdateProcessAuthLog Exception:{ex.Message}");
                return (returnCode);
            }
        }

        /// <summary>
        /// QR0200AuthProcess Process Message Type 0200 Authorization Flow For QRCode
        /// </summary>
        /// <param name="ncccAuthInfoData">Authorization Data Record</param>
        /// <param name="disPatchSwitch">NCCC,NCPS</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] QR0200AuthProcess(NcccAuthInfoStruct ncccAuthInfoData)
        {
            const int storeProcedureParameterCount = 22;
            const int outputParaCount = 4;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureQR0200AuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = ncccAuthInfoData.tranLogKey;
                paraValues[01] = ncccAuthInfoData.isoMessageType;
                paraValues[02] = ncccAuthInfoData.isoP2CardNbr;
                paraValues[03] = ncccAuthInfoData.isoP3ProcessCde;
                paraValues[04] = ncccAuthInfoData.isoP4Amt;
                paraValues[05] = ncccAuthInfoData.isoP7TxDte;
                paraValues[06] = ncccAuthInfoData.isoP7TxTime;
                paraValues[07] = ncccAuthInfoData.isoP11TraceNo;
                paraValues[08] = ncccAuthInfoData.isoP18MccCode;
                paraValues[09] = ncccAuthInfoData.isoP22Em;
                paraValues[10] = ncccAuthInfoData.isoP25CondCode;
                paraValues[11] = ncccAuthInfoData.isoP35ExpirDte;
                paraValues[12] = ncccAuthInfoData.isoP37RefNbr;
                paraValues[13] = ncccAuthInfoData.isoP38AuthCode;
                paraValues[14] = ncccAuthInfoData.isoP39Rsp;
                paraValues[15] = null;
                paraValues[16] = ncccAuthInfoData.isoP41TermNbr;
                paraValues[17] = ncccAuthInfoData.isoP42Merchant;
                paraValues[18] = ncccAuthInfoData.isoP43MerName;
                paraValues[19] = ncccAuthInfoData.isoP43MerCity;
                paraValues[20] = ncccAuthInfoData.isoP43LocCountry;
                paraValues[21] = null;

                //if (Log.IsTraceEnabled)
                //{
                //    Log.Trace("--- Start QR0200AuthProcess Display Store Procedure Input Data ---");
                //    for (var index = 0; index < paraValues.Length; index++)
                //    {
                //        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                //    }
                //    Log.Trace("--- End   QR0200AuthProcess Display Store Procedure Input Data  ---");
                //}

                // Return String Arrays Information
                // authReturnString[00]:@isoP39Rsp         , authReturnString[01]:@isoP39RspAction
                // authReturnString[02]:@disPatchSwitch
                // Description
                // @isoP35ExpirDte              varchar(04)     --P35  有效期限
                // @isoP39Rsp               	varchar(02) 	--P39
                // @isoP39RspAction	            varchar(01)     --對映於P39的授權回覆動作 A/D/R/P
                // @disPatchSwitch           	varchar(01)     --依回覆值傳送對映單位

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureQR0200AuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"QR0200AuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(ncccAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(ncccAuthInfoData)}\n");
                    }

                    Log.Trace($"QR0200AuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }

        /// <summary>
        /// QRUpdTxAuthProcess Process For Update QRCode Transaction
        /// </summary>
        /// <param name="ncccAuthInfoData">Authorization Data Record</param>
        /// <returns>
        /// Return Output String Array
        /// </returns>
        public static string[] QR0200UpdTxAuthProcess(NcccAuthInfoStruct ncccAuthInfoData)
        {
            const int storeProcedureParameterCount = 11;
            const int outputParaCount = 3;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureQR0200UpdTxAuthProcess);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = ncccAuthInfoData.tranLogKey;
                paraValues[01] = ncccAuthInfoData.isoMessageType;
                paraValues[02] = ncccAuthInfoData.isoP2CardNbr;
                paraValues[03] = ncccAuthInfoData.isoP4Amt;
                paraValues[04] = ncccAuthInfoData.isoP7TxDte;
                paraValues[05] = ncccAuthInfoData.isoP7TxTime;
                paraValues[06] = ncccAuthInfoData.isoP11TraceNo;
                paraValues[07] = ncccAuthInfoData.isoP37RefNbr;
                paraValues[08] = ncccAuthInfoData.isoP38AuthCode;
                paraValues[09] = ncccAuthInfoData.isoP39Rsp;
                paraValues[10] = null;

                //if (Log.IsTraceEnabled)
                //{
                //    Log.Trace("--- Start QR0200UpdTxAuthProcess Display Store Procedure Input Data ---");
                //    for (var index = 0; index < paraValues.Length; index++)
                //    {
                //        Log.Trace($"Index[{index,00}], Data[{paraValues[index]}]");
                //    }
                //    Log.Trace("--- End   QR0200UpdTxAuthProcess Display Store Procedure Input Data  ---");
                //}

                // QRUpdTxAuthProcess Return String Arrays Information
                // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
                // authReturnString[02]:@isoP39RspAction
                // Description
                // @isoP38AuthCode              varchar(06) 	--P38
                // @isoP39Rsp	                varchar(02)     --P39
                // @isoP39RspAction           	varchar(01)     --對映於P39的授權回覆動作 A/D/R/P

                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if (returnStrings != null)
                {
                    Log.Info(ColaCommUtility.DisplaySpReturnStrings(SystemConstantData.StoreProcedureName.StoreProcedureQR0200UpdTxAuthProcess, returnStrings));
                }
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"QR0200UpdTxAuthProcess Exception:{ex.Message}");
                //This Display Information For Trace
                if (Log.IsTraceEnabled)
                {
                    System.Reflection.FieldInfo[] fields = typeof(NcccAuthInfoStruct).GetFields();
                    var stringBuilder = new StringBuilder();
                    foreach (var field in fields)
                    {
                        if (field.GetValue(ncccAuthInfoData) == null) continue;
                        stringBuilder.Append($"{field.Name} : {field.GetValue(ncccAuthInfoData)}\n");
                    }

                    Log.Trace($"QR0200UpdTxAuthProcess Exception -> Data[{stringBuilder}]");
                }
                return null;
            }
        }


        /// <summary>
        /// Get COLA PASYSTF System Information
        /// </summary>
        /// <param name="buString">BU Primary Key</param>
        /// <param name="colaPasystfInfo">Reference Of Cola Pasystf Structure</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>

        public static int SelectColaPasystf(string buString, ref ColaPasystfInfoStruct colaPasystfInfo)
        {
            colaPasystfInfo.Bu = string.Empty;
            colaPasystfInfo.SystWorkSw = string.Empty;
            colaPasystfInfo.Holiday = string.Empty;
            colaPasystfInfo.TodayProcessDt = string.Empty;
            colaPasystfInfo.PrevProcessDt = string.Empty;
            colaPasystfInfo.NextProcessDt = string.Empty;
            colaPasystfInfo.LstBatchDt = string.Empty;
            colaPasystfInfo.NCCCSystemDt = string.Empty;
            colaPasystfInfo.NCCCSystemUpdDt = string.Empty;
            colaPasystfInfo.NCCCIwkCheckDigits = string.Empty;
            colaPasystfInfo.NCCCIwkChangeDt = string.Empty;
            colaPasystfInfo.FISCSystemDt = string.Empty;
            colaPasystfInfo.FISCSystemUpdDt = string.Empty;
            colaPasystfInfo.FISCIwkCheckDigits = string.Empty;
            colaPasystfInfo.FISCIwkChangeDt = string.Empty;
            colaPasystfInfo.TWINSystemDt = string.Empty;
            colaPasystfInfo.TWINSystemUpdDt = string.Empty;
            colaPasystfInfo.TWINIwkCheckDigits = string.Empty;
            colaPasystfInfo.TWINIwkChangeDt = string.Empty;
            colaPasystfInfo.PostResult = string.Empty;
            colaPasystfInfo.MntDt = string.Empty;
            colaPasystfInfo.MntUser = string.Empty;
            const int storeProcedureParameterCount = 24;
            const int outputParaCount = 23;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureSelectPasystf);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = buString;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    colaPasystfInfo.Bu = returnStrings[1];
                    colaPasystfInfo.SystWorkSw = returnStrings[2];
                    colaPasystfInfo.Holiday = returnStrings[3];
                    colaPasystfInfo.TodayProcessDt = returnStrings[4];
                    colaPasystfInfo.PrevProcessDt = returnStrings[5];
                    colaPasystfInfo.NextProcessDt = returnStrings[6];
                    colaPasystfInfo.LstBatchDt = returnStrings[7];
                    colaPasystfInfo.NCCCSystemDt = returnStrings[8];
                    colaPasystfInfo.NCCCSystemUpdDt = returnStrings[9];
                    colaPasystfInfo.NCCCIwkCheckDigits = returnStrings[10];
                    colaPasystfInfo.NCCCIwkChangeDt = returnStrings[11];

                    colaPasystfInfo.FISCSystemDt = returnStrings[12];
                    colaPasystfInfo.FISCSystemUpdDt = returnStrings[13];
                    colaPasystfInfo.FISCIwkCheckDigits = returnStrings[14];
                    colaPasystfInfo.FISCIwkChangeDt = returnStrings[15];

                    colaPasystfInfo.TWINSystemDt = returnStrings[16];
                    colaPasystfInfo.TWINSystemUpdDt = returnStrings[17];
                    colaPasystfInfo.TWINIwkCheckDigits = returnStrings[18];
                    colaPasystfInfo.TWINIwkChangeDt = returnStrings[19];

                    colaPasystfInfo.PostResult = returnStrings[20];

                    colaPasystfInfo.MntDt = returnStrings[21];
                    colaPasystfInfo.MntUser = returnStrings[22];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Select Cola_Pasystf Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SelectColaPasystf Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// Update COLA PASYSTF System Information
        /// </summary>
        /// <param name="buString">BU Primary Key</param>
        /// <param name="option">1:Update SYSTEM_DTE Other:Update IWK_CHECK_DIGITS </param>
        /// <param name="updateDataString">Reference Of Cola Paysystf Structure</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int UpdateColaPasystf(string buString, int option, string updateDataString)
        {
            const int storeProcedureParameterCount = 4;
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureUpdatePasystf);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = buString;
                paraValues[01] = option;
                paraValues[02] = updateDataString;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Update Cola_Pasystf Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "UpdateColaPasystf Exception");

                return (-2);
            }
        }

        /// <summary>
        /// Insert BIC Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="ncccMessage">BIC Message Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        NcccIso8583 ncccMessage)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[4] = ncccMessage.Header().Encode();
                paraValues[5] = ncccMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(ncccMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in ncccMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// Insert SVCS Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="svcsMessage">BIC Message Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        SVCS svcsMessage)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[4] = svcsMessage.Header().Encode();
                paraValues[5] = svcsMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(svcsMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in svcsMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// Insert ICASH Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="icashMessage">BIC Message Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        ICASH icashMessage)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[4] = icashMessage.Header().Encode();
                paraValues[5] = icashMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(icashMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in icashMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// Insert IPASS Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="ipassMessage">BIC Message Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        IPASS ipassMessage)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[4] = ipassMessage.Header().Encode();
                paraValues[5] = ipassMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(ipassMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in ipassMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// Insert FISC Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="fiscMessage">BIC Message Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        FISC fiscMessage)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[4] = string.Empty;          //header not null
                paraValues[5] = fiscMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(fiscMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in fiscMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception");

                return (-2);
            }
        }

        /// <summary>
        /// Insert TWIN Card Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="twinMessage">BIC Message Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        TWINIso8583 twinMessage)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[4] = string.Empty;          //header not null
                paraValues[5] = twinMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(twinMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in twinMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception");

                return (-2);
            }
        }


        /// <summary>
        /// Insert ISO Data Transaction Data To COLA_TRAN_LOG Table
        /// </summary>
        /// <param name="tranUniqueKey">Transaction Log Key</param>
        /// <param name="direction">Send Or Receive</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="isoData">BIC Message Raw Data</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertTranLog(string tranUniqueKey,
                                        string direction,
                                        string sessionName,
                                        byte[] isoData)
        {
            const int storeProcedureParameterCount = 136; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                if ((isoData == null) || (isoData.Length < 2))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              (new StackTrace(true)).GetFrame(0).GetMethod().Name,
                              "InsertTranLog",
                              "Message Data InValid");
                    return (-1);
                }
                int messageLength = ColaCommUtility.BinaryToInteger(ColaCommUtility.GetSubBytes(isoData, 0, 2));
                if ((messageLength == 0) || (messageLength != (isoData.Length - 2)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "InsertTranLog Data Length:" + messageLength,
                              "Message Data Length InValid");
                    return (-2);
                }
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertTranLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                NcccIso8583 ncccMessage = new NcccIso8583(isoData);
                paraValues[0] = tranUniqueKey;
                paraValues[1] = direction;
                paraValues[2] = sessionName;
                paraValues[3] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                paraValues[4] = ncccMessage.Header().Encode();
                paraValues[5] = ncccMessage.Mti();
                paraValues[6] = HexStringConverter.FromByteArray(ncccMessage.Bitmap());
                int startIndex = 6;
                foreach (var pair in ncccMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    var bytes = value as byte[];
                    if (bytes != null)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        paraValues[startIndex + FIELDMethods.Val(key)] = tempString;
                    }
                    else
                    {
                        paraValues[startIndex + FIELDMethods.Val(key)] = value;
                    }
                }
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCodeString = returnStrings[0];
                    return (Convert.ToInt32(retnCodeString));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert Cola_Tran_Log Table Error");
                return (-4);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "InsertTranLog Exception:{ex.Message}");

                return (-5);
            }
        }

        /// <summary>
        /// Update Session Table For Monitor Session Process
        /// </summary>
        /// <param name="inputOption">Option For Start Or Other</param>
        /// <param name="sessionName">Session Name</param>
        /// <param name="sessionType">Session Type(M or T)</param>
        /// <param name="currentProcessDate">Current Process Date Time(yyyy-MM-dd HH:mm:ss)</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int UpdateSession(int inputOption, string sessionName, char sessionType, string currentProcessDate)
        {
            const int storeProcedureParameterCount = 5; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString() + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting,
                                                               SystemConstantData.StoreProcedureName.StoreProcedureUpdateSession);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = inputOption;
                paraValues[1] = sessionName;
                paraValues[2] = sessionType;
                paraValues[3] = currentProcessDate;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Update Cola_Session Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "UpdateSession Exception");

                return (-2);
            }
        }

        /// <summary>
        /// Get COLA PASYSTF System Information
        /// </summary>
        /// <param name="messageIdString">BU Primary Key</param>
        /// <param name="serviceRouterLogInfo">Reference Of Cola ServiceRouterLogInfo Structure</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int SelectServiceRouterLog(string messageIdString, ref ServiceRouterLogInfoStruct serviceRouterLogInfo)
        {
            serviceRouterLogInfo.MessageId = string.Empty;
            serviceRouterLogInfo.ProcessDatetime = string.Empty;
            serviceRouterLogInfo.SourceEndpoint = string.Empty;
            serviceRouterLogInfo.DestinationEndpoint = string.Empty;
            serviceRouterLogInfo.CreateDatetime = string.Empty;
            serviceRouterLogInfo.ReturnDatetime = string.Empty;
            serviceRouterLogInfo.Category = string.Empty;
            serviceRouterLogInfo.CommandCode = string.Empty;
            serviceRouterLogInfo.InterchangeData = string.Empty;
            const int storeProcedureParameterCount = 11;
            const int outputParaCount = 10;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureSelectServiceRouterLog);
                Object[] paraValues = new object[storeProcedureParameterCount];

                paraValues[00] = messageIdString;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    serviceRouterLogInfo.MessageId = returnStrings[1];
                    serviceRouterLogInfo.ProcessDatetime = returnStrings[2];
                    serviceRouterLogInfo.SourceEndpoint = returnStrings[3];
                    serviceRouterLogInfo.DestinationEndpoint = returnStrings[4];
                    serviceRouterLogInfo.CreateDatetime = returnStrings[5];
                    serviceRouterLogInfo.ReturnDatetime = returnStrings[6];
                    serviceRouterLogInfo.Category = returnStrings[7];
                    serviceRouterLogInfo.CommandCode = returnStrings[8];
                    serviceRouterLogInfo.InterchangeData = returnStrings[9];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Select Cola_Pasystf Table Error");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SelectServiceRouterLog Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// Get Auth. Transaction Data For SMS Use
        /// </summary>
        /// <param name="messageId">Set Message Id Key Value</param>
        /// <returns></returns>
        public static DataTable GetServiceRouterLogByMessageId(string messageId)
        {
            try
            {
                var ds = new DataSet();
                using (var conn = new SqlConnection(ColaCommUtility.GetConnectionString() + ";" + SystemConstantData.CommonData.ColumnEncryptionSetting))
                {
                    using (var sqlCmd = new SqlCommand())
                    {
                        sqlCmd.CommandText = SystemConstantData.StoreProcedureName.StoreProcedureGetServiceRouterLogByMessageId;
                        sqlCmd.Connection = conn;
                        sqlCmd.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter("@MessageId", SqlDbType.VarChar)
                        {
                            Size = 22,
                            Direction = ParameterDirection.Input,
                            Value = messageId
                        };
                        sqlCmd.Parameters.Add(param);

                        param = new SqlParameter("@ReturnCode", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output,
                            Value = -1
                        };
                        sqlCmd.Parameters.Add(param);
                        conn.Open();
                        var adapter = new SqlDataAdapter(sqlCmd);
                        adapter.Fill(ds);
                        conn.Close();
                    }
                }
                return (ds.Tables[0]);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"GetServiceRouterLogByMessageId Exception:{ex.Message}");
                return null;
            }

        }

        ///  <summary>
        ///  Transfer CountryCode to Numeric
        ///  </summary>
        /// <param name="countryCode"></param>
        /// <returns>
        ///  Return Cols(0:Successful,Not 0:Fail)
        ///  </returns>
        public static string[] Country2Numeric(string countryCode)
        {
            const int storeProcedureParameterCount = 3;
            const int outputParaCount = 2;
            try
            {
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureCountry2Numeric);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[00] = countryCode;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings == null) || (returnStrings.Length != outputParaCount))
                {
                    return null;
                }
                Log.Info("Convert country code from [{0}] -- Output is[{1}]--[{2}]", countryCode, Convert.ToString(returnStrings[0]), Convert.ToString(returnStrings[1]));
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Country2Numeric Exception:");

                return null;
            }
        }

        /// <summary>
        /// Insert BIC Transaction Data To COLA_SERVICE_ROUTER_LOG Table
        /// </summary>
        /// <param name="interchangeMessage">The interchange message.</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static int InsertServiceRouterLog(InterchangeMessage interchangeMessage)
        {
            const int storeProcedureParameterCount = 10; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                if (interchangeMessage == null)
                {
                    Log.Error($"InsertServiceRouterLog, Input interchangeMessage is null");
                    return -9;
                }
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureInsertServiceRouterLog);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[0] = interchangeMessage.MessageId;
                paraValues[1] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffff");
                paraValues[2] = interchangeMessage.SourceEndPoint;
                paraValues[3] = interchangeMessage.DestinationEndPoint;
                if (string.IsNullOrEmpty(interchangeMessage.CreateDateTime))
                {
                    paraValues[4] = null;
                }
                else
                {
                    paraValues[4] = interchangeMessage.CreateDateTime;
                }
                if (string.IsNullOrEmpty(interchangeMessage.ReturnDateTime))
                {
                    paraValues[5] = null;
                }
                else
                {
                    paraValues[5] = interchangeMessage.ReturnDateTime;
                }
                paraValues[6] = interchangeMessage.Category;
                paraValues[7] = interchangeMessage.CommandCode;
                if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                {
                    if (interchangeMessage.InterchangeData == null)
                    {
                        paraValues[8] = "No Data";
                    }
                    else
                    {
                        if (interchangeMessage.InterchangeData.Length > 0)
                        {
                            paraValues[8] = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        }
                        else
                        {
                            paraValues[8] = "No Data";
                        }
                    }
                }
                else
                {
                    if (interchangeMessage.InterchangeData == null)
                    {
                        paraValues[8] = "No Data";
                    }
                    else
                    {
                        if (interchangeMessage.InterchangeData.Length > 0)
                        {
                            paraValues[8] = HexStringConverter.FromByteArray(interchangeMessage.InterchangeData);
                        }
                        else
                        {
                            paraValues[8] = "No Data";
                        }
                    }
                }

                paraValues[9] = null;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings != null) && (returnStrings.Length == outputParaCount))
                {
                    string retnCode = returnStrings[0];
                    return (Convert.ToInt32(retnCode));
                }
                Log.Fatal((new StackTrace(true)).GetFrame(0).GetMethod().Name + ",Error:" + "Insert dbo.COLA_SERVICE_ROUTER_LOG");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"InsertServiceRouterLog Exception:{ex.Message}");

                return (-2);
            }
        }

        /// <summary>
        /// 取得持卡人生日
        /// </summary>
        /// <param name="cardNbr">The card NBR.</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static string[] SelectCardholderDob(string cardNbr)
        {
            const int storeProcedureParameterCount = 2; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                if (string.IsNullOrEmpty(cardNbr))
                {
                    Log.Error($"SelectCardholderDob, Input cardNbr invalid");
                    return null;
                }
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureSelectCardholderDob);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[00] = cardNbr;
                paraValues[01] = null;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings == null) || (returnStrings.Length != outputParaCount))
                {
                    return null;
                }
                Log.Info("SelectCardholderDob [{0}] is successfully", cardNbr);
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SelectCardholderDob Exception:{ex.Message}");

                return null;
            }
        }

        /// <summary>
        /// 取得持卡人密碼(option = 1 pin_offset , 2=生日)
        /// </summary>
        /// <param name="cardNbr">The card NBR.</param>
        /// <param name="option">The option.</param>
        /// <returns>
        /// Return Cols(0:Successful,Not 0:Fail)
        /// </returns>
        public static string[] SelectCardholderPin(string cardNbr, string option)
        {
            const int storeProcedureParameterCount = 3; //Total Include Input And OutPut
            const int outputParaCount = 1;
            try
            {
                if (string.IsNullOrEmpty(cardNbr))
                {
                    Log.Error($"SelectCardholderDob, Input cardNbr invalid");
                    return null;
                }
                //Initial Store Procedure Parameter
                DbStoreProc dbStoreProcedure = new DbStoreProc(ColaCommUtility.GetConnectionString(),
                                                               SystemConstantData.StoreProcedureName.StoreProcedureSelectCardholderPin);
                Object[] paraValues = new object[storeProcedureParameterCount];
                paraValues[00] = cardNbr;
                paraValues[01] = option;
                paraValues[02] = null;
                string[] returnStrings = dbStoreProcedure.ExecProcOutput(outputParaCount, paraValues);
                if ((returnStrings == null) || (returnStrings.Length != outputParaCount))
                {
                    return null;
                }
                Log.Info("SelectCardholderPin [{0}] is successfully", cardNbr);
                return (returnStrings);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SelectCardholderPin Exception:{ex.Message}");

                return null;
            }
        }
    }
}

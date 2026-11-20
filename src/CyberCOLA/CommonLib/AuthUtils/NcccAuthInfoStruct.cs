using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using System.Diagnostics;
using Com.Cybersoft4u.TM.ISO8583.MCOLA.DataElement;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public struct NcccAuthInfoStruct
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        public string tranLogKey;            //Transaction Log Unique Key
        public string isoMessageHeader;      //messageHeader
        public string isoMessageType;        //messageType
        public string isoP2CardNbr;          //P2
        public string isoP3ProcessCde;       //P3
        public decimal isoP4Amt;             //p4
        public decimal isoP6Amt;             //p6
        public string isoP7TxDte;            //p7   mmdd
        public string isoP7TxTime;           //p7   hhmmss
        public string isoP11TraceNo;         //p11  trace no public string isoS127EdcFunction
        public string isoP18MccCode;         //P18
        public string isoP19AcqCountry;      //P19  Country Code
        public string isoP22Em;              //p22
        public string isoP25CondCode;        //p25  Condition Code
        public string isoP32BinIca;          //p32  收單行代碼
        public string isoP35ExpirDte;        //p35  有效期限
        public string isoP35Svc;             //p35  SVC Code
        public string isoP35Pvv;             //p35  PVV
        public string isoP35Pvki;            //p35  PVKI
        public string isoP35Cvv;
        public string isoP37RefNbr;          //p37  Reference Number
        public string isoP38AuthCode;        //p38
        public string isoP39Rsp;             //p39
        public string isoP41TermNbr;         //p41  終端機編號
        public string isoP42Merchant;        //p42  特店號碼
        public string isoP43MerName;         //p43  特店名稱
        public string isoP43MerCity;         //p43  特店城市
        public string isoP43LocCountry;      //p43  Country Code 國別
        public DE48Data de48Data;            //p48
        public string twinP48SubAcquirerId; //TWIN p48
        public string twinP48NetworkRefId;  //TWIN p48
        public string isoP49TxCode;          //p49  P4的幣別
        public string isoP52PinBlock;        //p52
        public TWINFIELD_55 twinP55ChipCardData;   //TWIN P55
        public string isoP60NewPin;          //P60
        public string twinP61PosData;        //TWIN p61
        public TWINTLV twinP63AdditionData;  //TWIN p63
        public string isoP57_P63InsInd;      //P57 or P63
        public string isoS90MessType;        //S90  Message Type
        public string isoS90RefNbr;          //S90  RRN
        public string isoS90TxDte;           //S90  Transaction Date
        public string isoS90TxTime;          //S90  Transaction Time
        public decimal isoS95ReplaceAmt;     //S95
        public string isoS127CellPhone;      //S127 手機         20201111
        public string isoS127Dob;            //S127 生日         20201111
        public string isoS127EdcFunction;    //S127 EDC FUNCTION
        public string isoS127Pws;            //S127 開卡密碼
        public string isoS127Id;             //S127 身份証字號
        public string isoS127PayPws;         //S127 Payment Password
        public string isoS127EcFlag;         //S127 EDC Flag
        public string isoS127MpFlag;         //S127 MP FLAG
        public string isoS127MerchVfy;       //S127 Merchant Verify : CBR 20241017
        //Des Function
        public char arqcCheck;               //ARQC驗証 Y:有驗証 N:未驗証
        public char arqcChkResult;           //Y : ARQC驗証通過  N:ARQC驗証失敗
        public char cvvCheck;                //CVV 驗証 Y:有驗証 N:未驗証
        public char cvvChkResult;            //Y : CVV 驗証通過  N:CVV 驗証失敗
        public char cvv2Check;               //CVV2 驗証 Y:有驗証 N:未驗証
        public char cvv2ChkResult;           //Y : CVV2 驗証通過  N:CVV2 驗証失敗
        public char cavvCheck;               //CAVV 驗証 Y:有驗証 N:未驗証
        public char cavvChkResult;           //Y : CAVV  驗証通過  N:CAVV 驗証失敗
        public char pvvCheck;                //PVV 驗証 Y:有驗証 N:未驗証
        public string pvvChkResult;          //Field 52 create PVV
        public char pvvDobCheck;             //Y: 算回PVV值 N:不用算回PVV值 
        public string pvvDobChkResult;       //僅@isoP18MccCode=4011時, 呼叫執Sp:Cola_SelectCardholderDob-->取得CLEAR PIN 呼叫hsm(EncryptBankPin) 算回PVV值放回
        public char c5InstallRebateFlag;    //C5 installment Or Rebate Transaction I:Installment R:Rebate,N:No C5 Data
        //Token Data
        public Token04 token04Data;
        public Token17 token17Data;
        public Token20 token20Data;
        public TokenB0 tokenB0Data;
        public TokenB2 tokenB2Data;
        public TokenB3 tokenB3Data;
        public TokenB4 tokenB4Data;
        public TokenB5 tokenB5Data;
        public TokenBE tokenBEData;
        public TokenC0 tokenC0Data;
        public TokenC4 tokenC4Data;
        public dynamic tokenC5Data;
        public TokenCH tokenCHData;
        public TokenF1 tokenF1Data;
        public TokenF4 tokenF4Data;
        public TokenQ8 tokenQ8Data;
        public TokenQ9 tokenQ9Data;
        public TokenQR tokenQRData;
        public TokenS8 tokenS8Data;
        //public string t04ErrorFlg;
        //public string t04CrdVeryFlg;
        //public string tB2TVR;                //->將A轉成1010
        //public string tB2ATC;
        //public string tB2CVR;                //->將A轉成1010
        //public string tB4PtSrvEm;
        //public string tB4TermEntryCap;
        //public string tB4LastEmvStat;
        //public string tB4ROC;
        //public string tC0EcFlag;
        //public string tC0UcafAuthInd;
        //public string tC0CavvResult;
        //public string tC4TermAttendInd;
        //public string tC4ChActVtInd;
        //public string tC4TermInputCap;
        //public string tC5InstTxInd;
        //public string tC5InstTxResp;
        //public string tC5SettlementFlag;
        //public string tC5InstPeriod;
        //public string tC5DownPayment;
        //public string tC5InstPayment;
        //public string tC5InstFee;
        //public string tC5PointRedemption;
        //public string tC5SignPoint;
        //public string tC5PointBalance;
        //public string tC5PaidCreditAmt;
        public NcccAuthInfoStruct(NcccIso8583 requestNcccIso8583) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2CardNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.000; //decimal point p49
                isoP6Amt = (decimal)0.000; //decimal point p51
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP18MccCode = null;
                isoP19AcqCountry = null;
                isoP22Em = null;
                isoP25CondCode = null;
                isoP32BinIca = null;
                isoP35ExpirDte = null;
                isoP35Svc = null;
                isoP35Pvki = null;
                isoP35Pvv = null;
                isoP35Cvv = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP43MerName = null;
                isoP43MerCity = null;
                isoP43LocCountry = null;
                de48Data = null;
                isoP49TxCode = null;
                isoP52PinBlock = null;
                twinP55ChipCardData = null;
                isoP57_P63InsInd = null;
                twinP63AdditionData = null;
                isoS90MessType = null;
                isoS90RefNbr = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS95ReplaceAmt = (decimal)0.00;
                isoS127CellPhone = null;
                isoS127Dob = null;
                isoS127EdcFunction = null;
                isoS127Pws = null;
                isoS127Id = null;
                isoS127PayPws = null;
                isoS127EcFlag = null;
                isoS127MpFlag = null;
                isoS127MerchVfy = null;
                arqcCheck = 'N';
                arqcChkResult = 'N';
                cvvCheck = 'N';
                cvvChkResult = 'N';
                cvv2Check = 'N';
                cvv2ChkResult = 'N';
                cavvCheck = 'N';
                cavvChkResult = 'N';
                pvvCheck = 'N';
                pvvChkResult = null;
                pvvDobCheck = 'N';
                pvvDobChkResult = null;
                c5InstallRebateFlag = 'N';
                token04Data = null;
                token17Data = null;
                token20Data = null;
                tokenB0Data = null;
                tokenB2Data = null;
                tokenB3Data = null;
                tokenB4Data = null;
                tokenB5Data = null;
                tokenBEData = null;
                tokenC0Data = null;
                tokenC4Data = null;
                tokenC5Data = null;
                tokenCHData = null;
                tokenF1Data = null;
                tokenF4Data = null;
                tokenQ8Data = null;
                tokenQ9Data = null;
                tokenQRData = null;
                tokenS8Data = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestNcccIso8583.Header().Encode();
                isoMessageType = requestNcccIso8583.Mti();
                var fieldPos = FIELD.F_2;
                isoP2CardNbr = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP2CardNbr = requestNcccIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    fieldPos = FIELD.F_35;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                    {
                        string track2Data = requestNcccIso8583.RawVal(fieldPos).ToString();
                        //int dataStartPos = 2;//include 2 variable length
                        int dataStartPos = 0;
                        int delimiterPos = 0;
                        string cardNbr = string.Empty;
                        delimiterPos = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                        if (delimiterPos < 0)
                        {
                            cardNbr = track2Data;
                        }
                        else
                        {
                            cardNbr = track2Data.Substring(dataStartPos, delimiterPos - dataStartPos);
                        }

                        isoP2CardNbr = cardNbr;
                    }
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_49;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestNcccIso8583.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP4Amt = Convert.ToDecimal(requestNcccIso8583.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_6;
                isoP6Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_51;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestNcccIso8583.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP6Amt = Convert.ToDecimal(requestNcccIso8583.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestNcccIso8583.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_11;
                isoP11TraceNo = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_18;
                isoP18MccCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP18MccCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_19;
                isoP19AcqCountry = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP19AcqCountry = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_22;
                isoP22Em = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP22Em = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_25;
                isoP25CondCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP25CondCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_35;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string track2Data = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int track2DataLength = track2Data.Length;
                    int startIndex = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                    if (startIndex > 0)
                    {
                        startIndex++;
                        int dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35ExpirDte = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Svc = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 1;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Pvki = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Pvv = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Cvv = track2Data.Substring(startIndex, dataLength);
                    }
                }
                else
                {
                    isoP35ExpirDte = null;

                    isoP35Svc = null;

                    isoP35Pvki = null;

                    isoP35Pvv = null;

                    isoP35Cvv = null;
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    if ((isoMessageType.Substring(1, 1).Equals("1")) ||
                        (isoMessageType.Substring(1, 1).Equals("2")))
                    {
                        isoP38AuthCode = ColaCommUtility.GetApproveCode();
                    }
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestNcccIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    var fieldOtherPos = FIELD.F_48;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldOtherPos)))
                    {
                        if (requestNcccIso8583.RawVal(fieldOtherPos).ToString().Length == 27)
                            isoP42Merchant = requestNcccIso8583.RawVal(fieldOtherPos).ToString().Substring(0, 19).Trim();
                    }
                }

                fieldPos = FIELD.F_43;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string merchInfo = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int startIndex = 0;
                    int dataLength = 22;
                    isoP43MerName = merchInfo.Substring(startIndex, dataLength);

                    startIndex = 22;
                    dataLength = 13;
                    isoP43MerCity = merchInfo.Substring(startIndex, dataLength);

                    startIndex = 38;
                    dataLength = 2;
                    isoP43LocCountry = merchInfo.Substring(startIndex, dataLength);
                }
                else
                {
                    isoP43MerName = null;

                    isoP43MerCity = null;

                    isoP43LocCountry = null;
                }

                fieldPos = FIELD.F_48;
                de48Data = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    if (requestNcccIso8583.RawVal(fieldPos).ToString().Length == 76)
                        de48Data = new DE48Data(requestNcccIso8583.RawVal(fieldPos).ToString());
                }
                fieldPos = FIELD.F_49;
                isoP49TxCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP49TxCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_52;
                isoP52PinBlock = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP52PinBlock = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                //fieldPos = FIELD.F_63;
                fieldPos = requestNcccIso8583.GetTokenFieldId();
                isoP57_P63InsInd = null;
                string c5TokenData = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    var tokenGroup = (TokenGroup)requestNcccIso8583.Core().GetOthersValByKey(fieldPos.ToString());
                    if (tokenGroup != null)
                    {
                        foreach (var t in tokenGroup.Tokens)
                        {
                            switch (t.Key)
                            {
                                case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                    token04Data = (Token04)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.Token17.Id:
                                    token17Data = (Token17)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                    token20Data = (Token20)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB0.Id:
                                    tokenB0Data = (TokenB0)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                    tokenB2Data = (TokenB2)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                    tokenB3Data = (TokenB3)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                    tokenB4Data = (TokenB4)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                    tokenB5Data = (TokenB5)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenBE.Id:
                                    tokenBEData = (TokenBE)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                    tokenC0Data = (TokenC0)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                    tokenC4Data = (TokenC4)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                    var c5 = t.Value as TokenNCCCRoyaltyC5;
                                    if (c5 != null)
                                    {
                                        tokenC5Data = c5;
                                        c5InstallRebateFlag = 'R';
                                    }
                                    else
                                    {
                                        tokenC5Data = (TokenNCCCInstallmentC5)t.Value;
                                        c5InstallRebateFlag = 'I';
                                    }

                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                    tokenCHData = (TokenCH)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenF1.Id:
                                    tokenF1Data = (TokenF1)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenF4.Id:
                                    tokenF4Data = (TokenF4)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenQ8.Id:
                                    tokenQ8Data = (TokenQ8)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenQ9.Id:
                                    tokenQ9Data = (TokenQ9)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenQR.Id:
                                    tokenQRData = (TokenQR)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenS8.Id:
                                    tokenS8Data = (TokenS8)t.Value;
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(c5TokenData))
                    {
                        if (c5TokenData.Length == 144)
                            isoP57_P63InsInd = c5TokenData.Substring(90, 1);
                    }
                }

                fieldPos = FIELD.F_95;
                isoS95ReplaceAmt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoS95ReplaceAmt = Convert.ToDecimal(requestNcccIso8583.RawVal(fieldPos).ToString().Substring(0, 12)) / (decimal)100.00;
                }

                fieldPos = FIELD.F_127;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string edcData = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int leadingDataLength = 3;
                    int edcDataLength = edcData.Length;

                    int startIndex = 3 - leadingDataLength;
                    int dataLength = 10;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127CellPhone = edcData.Substring(startIndex, dataLength);

                    startIndex = 13 - leadingDataLength;
                    dataLength = 8;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127Dob = edcData.Substring(startIndex, dataLength);

                    startIndex = 21 - leadingDataLength;
                    dataLength = 3;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127MerchVfy = edcData.Substring(startIndex, dataLength);

                    startIndex = 41 - leadingDataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127EdcFunction = edcData.Substring(startIndex, dataLength);

                    startIndex = 54 - leadingDataLength;
                    dataLength = 6;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127Pws = edcData.Substring(startIndex, dataLength);

                    startIndex = 60 - leadingDataLength;
                    dataLength = 10;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127Id = edcData.Substring(startIndex, dataLength);

                    startIndex = 70 - leadingDataLength;
                    dataLength = 4;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127PayPws = edcData.Substring(startIndex, dataLength);

                    startIndex = 74 - leadingDataLength;
                    dataLength = 1;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127EcFlag = edcData.Substring(startIndex, dataLength);

                    startIndex = 75 - leadingDataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127MpFlag = edcData.Substring(startIndex, dataLength);
                }
                else
                {
                    isoS127CellPhone = null;

                    isoS127Dob = null;

                    isoS127EdcFunction = null;

                    isoS127Pws = null;

                    isoS127Id = null;

                    isoS127PayPws = null;

                    isoS127EcFlag = null;

                    isoS127MpFlag = null;

                    isoS127MerchVfy = null;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccAuthInfoStruct-1 Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        /// <summary></summary>
		/// <param name="requestNcccIso8583"></param>
		/// <param name="txKeySeq"></param>
		public NcccAuthInfoStruct(NcccIso8583 requestNcccIso8583, FCB9GQ.FCB9GQ_REQ reqJSONObject, string txKeySeq) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = txKeySeq;
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2CardNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP18MccCode = null;
                isoP19AcqCountry = null;
                isoP22Em = null;
                isoP25CondCode = null;
                isoP32BinIca = null;
                isoP35ExpirDte = null;
                isoP35Svc = null;
                isoP35Pvki = null;
                isoP35Pvv = null;
                isoP35Cvv = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP43MerName = null;
                isoP43MerCity = null;
                isoP43LocCountry = null;
                de48Data = null;
                twinP48SubAcquirerId = null;
                twinP48NetworkRefId = null;
                isoP49TxCode = null;
                isoP52PinBlock = null;
                twinP55ChipCardData = null;
                isoP57_P63InsInd = null;
                twinP63AdditionData = null;
                isoS90MessType = null;
                isoS90RefNbr = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS95ReplaceAmt = (decimal)0.00;
                isoS127CellPhone = null;
                isoS127Dob = null;
                isoS127EdcFunction = null;
                isoS127Pws = null;
                isoS127Id = null;
                isoS127PayPws = null;
                isoS127EcFlag = null;
                isoS127MpFlag = null;
                arqcCheck = 'N';
                arqcChkResult = 'N';
                cvvCheck = 'N';
                cvvChkResult = 'N';
                cvv2Check = 'N';
                cvv2ChkResult = 'N';
                cavvCheck = 'N';
                cavvChkResult = 'N';
                pvvCheck = 'N';
                pvvChkResult = null;
                pvvDobCheck = 'N';
                pvvDobChkResult = null;
                c5InstallRebateFlag = 'N';
                token04Data = null;
                token17Data = null;
                token20Data = null;
                tokenB0Data = null;
                tokenB2Data = null;
                tokenB3Data = null;
                tokenB4Data = null;
                tokenB5Data = null;
                tokenC0Data = null;
                tokenC4Data = null;
                tokenC5Data = null;
                tokenCHData = null;
                tokenF1Data = null;
                tokenF4Data = null;
                tokenQ8Data = null;
                tokenQ9Data = null;
                tokenQRData = null;
                tokenS8Data = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestNcccIso8583.Header().Encode();
                isoMessageType = requestNcccIso8583.Mti();
                var fieldPos = FIELD.F_2;
                isoP2CardNbr = null;

                if (!ObjectUtils.IsNull(reqJSONObject.BODY.CardNo))
                {
                    isoP2CardNbr = reqJSONObject.BODY.CardNo;
                }
                else
                {
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                    {
                        isoP2CardNbr = requestNcccIso8583.RawVal(fieldPos).ToString();
                    }
                    else
                    {
                        fieldPos = FIELD.F_35;
                        if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                        {
                            string track2Data = requestNcccIso8583.RawVal(fieldPos).ToString();
                            //int dataStartPos = 2;//include 2 variable length
                            int dataStartPos = 0;
                            int delimiterPos = 0;
                            string cardNbr = string.Empty;
                            delimiterPos = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                            if (delimiterPos < 0)
                            {
                                cardNbr = track2Data;
                            }
                            else
                            {
                                cardNbr = track2Data.Substring(dataStartPos, delimiterPos - dataStartPos);
                            }

                            isoP2CardNbr = cardNbr;
                        }
                    }
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_49;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestNcccIso8583.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP4Amt = Convert.ToDecimal(requestNcccIso8583.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_6;
                isoP6Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_51;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestNcccIso8583.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP6Amt = Convert.ToDecimal(requestNcccIso8583.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestNcccIso8583.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_11;
                isoP11TraceNo = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_18;
                isoP18MccCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP18MccCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_19;
                isoP19AcqCountry = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP19AcqCountry = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_22;
                isoP22Em = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP22Em = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_25;
                isoP25CondCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP25CondCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_35;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string track2Data = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int track2DataLength = track2Data.Length;
                    int startIndex = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                    if (startIndex > 0)
                    {
                        startIndex++;
                        int dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35ExpirDte = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Svc = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 1;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Pvki = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Pvv = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Cvv = track2Data.Substring(startIndex, dataLength);
                    }
                }
                else
                {
                    isoP35ExpirDte = null;

                    isoP35Svc = null;

                    isoP35Pvki = null;

                    isoP35Pvv = null;

                    isoP35Cvv = null;
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    if ((isoMessageType.Substring(1, 1).Equals("1")) ||
                        (isoMessageType.Substring(1, 1).Equals("2")))
                    {
                        isoP38AuthCode = ColaCommUtility.GetApproveCode();
                    }
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestNcccIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    var fieldOtherPos = FIELD.F_48;
                    if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldOtherPos)))
                    {
                        if (requestNcccIso8583.RawVal(fieldOtherPos).ToString().Length == 27)
                            isoP42Merchant = requestNcccIso8583.RawVal(fieldOtherPos).ToString().Substring(0, 19).Trim();
                    }
                }

                fieldPos = FIELD.F_43;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string merchInfo = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int startIndex = 0;
                    int dataLength = 22;
                    isoP43MerName = merchInfo.Substring(startIndex, dataLength);

                    startIndex = 22;
                    dataLength = 13;
                    isoP43MerCity = merchInfo.Substring(startIndex, dataLength);

                    startIndex = 38;
                    dataLength = 2;
                    isoP43LocCountry = merchInfo.Substring(startIndex, dataLength);
                }
                else
                {
                    isoP43MerName = null;

                    isoP43MerCity = null;

                    isoP43LocCountry = null;
                }

                fieldPos = FIELD.F_48;
                de48Data = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    if (requestNcccIso8583.RawVal(fieldPos).ToString().Length == 76)
                        de48Data = new DE48Data(requestNcccIso8583.RawVal(fieldPos).ToString());
                }

                fieldPos = FIELD.F_49;
                isoP49TxCode = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP49TxCode = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_52;
                isoP52PinBlock = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoP52PinBlock = requestNcccIso8583.RawVal(fieldPos).ToString();
                }

                //fieldPos = FIELD.F_63;
                fieldPos = requestNcccIso8583.GetTokenFieldId();
                isoP57_P63InsInd = null;
                string c5TokenData = null;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    var tokenGroup = (TokenGroup)requestNcccIso8583.Core().GetOthersValByKey(fieldPos.ToString());
                    if (tokenGroup != null)
                    {
                        foreach (var t in tokenGroup.Tokens)
                        {
                            switch (t.Key)
                            {
                                case NcccTransactionConst.PosAdditionalData.Token04.Id:
                                    token04Data = (Token04)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.Token17.Id:
                                    token17Data = (Token17)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.Token20.Id:
                                    token20Data = (Token20)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB0.Id:
                                    tokenB0Data = (TokenB0)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                                    tokenB2Data = (TokenB2)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                                    tokenB3Data = (TokenB3)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                                    tokenB4Data = (TokenB4)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                                    tokenB5Data = (TokenB5)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                                    tokenC0Data = (TokenC0)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenC4.Id:
                                    tokenC4Data = (TokenC4)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                                    var c5 = t.Value as TokenNCCCRoyaltyC5;
                                    if (c5 != null)
                                    {
                                        tokenC5Data = c5;
                                        c5InstallRebateFlag = 'R';
                                    }
                                    else
                                    {
                                        tokenC5Data = (TokenNCCCInstallmentC5)t.Value;
                                        c5InstallRebateFlag = 'I';
                                    }

                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenCH.Id:
                                    tokenCHData = (TokenCH)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenF1.Id:
                                    tokenF1Data = (TokenF1)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenF4.Id:
                                    tokenF4Data = (TokenF4)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenQ8.Id:
                                    tokenQ8Data = (TokenQ8)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenQ9.Id:
                                    tokenQ9Data = (TokenQ9)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenQR.Id:
                                    tokenQRData = (TokenQR)t.Value;
                                    break;
                                case NcccTransactionConst.PosAdditionalData.TokenS8.Id:
                                    tokenS8Data = (TokenS8)t.Value;
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(c5TokenData))
                    {
                        if (c5TokenData.Length == 144)
                            isoP57_P63InsInd = c5TokenData.Substring(90, 1);
                    }
                }

                fieldPos = FIELD.F_90;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string originalDataElement = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int startIndex = 0;
                    int dataLength = 4;
                    isoS90MessType = originalDataElement.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 12;
                    isoS90RefNbr = originalDataElement.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 4;
                    isoS90TxDte = originalDataElement.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 8;
                    isoS90TxTime = originalDataElement.Substring(startIndex, dataLength);
                }

                fieldPos = FIELD.F_95;
                isoS95ReplaceAmt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    isoS95ReplaceAmt = Convert.ToDecimal(requestNcccIso8583.RawVal(fieldPos).ToString().Substring(0, 12)) / (decimal)100.00;
                }

                fieldPos = FIELD.F_127;
                if (!ObjectUtils.IsNull(requestNcccIso8583.RawVal(fieldPos)))
                {
                    string edcData = requestNcccIso8583.RawVal(fieldPos).ToString();
                    int leadingDataLength = 3;
                    int edcDataLength = edcData.Length;

                    int startIndex = 3 - leadingDataLength;
                    int dataLength = 10;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127CellPhone = edcData.Substring(startIndex, dataLength);

                    startIndex = 13 - leadingDataLength;
                    dataLength = 8;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127Dob = edcData.Substring(startIndex, dataLength);

                    startIndex = 21 - leadingDataLength;
                    dataLength = 3;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127MerchVfy = edcData.Substring(startIndex, dataLength);

                    startIndex = 41 - leadingDataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127EdcFunction = edcData.Substring(startIndex, dataLength);

                    startIndex = 54 - leadingDataLength;
                    dataLength = 6;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127Pws = edcData.Substring(startIndex, dataLength);

                    startIndex = 60 - leadingDataLength;
                    dataLength = 10;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127Id = edcData.Substring(startIndex, dataLength);

                    startIndex = 70 - leadingDataLength;
                    dataLength = 4;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127PayPws = edcData.Substring(startIndex, dataLength);

                    startIndex = 74 - leadingDataLength;
                    dataLength = 1;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127EcFlag = edcData.Substring(startIndex, dataLength);

                    startIndex = 75 - leadingDataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= edcDataLength)
                        isoS127MpFlag = edcData.Substring(startIndex, dataLength);
                }
                else
                {
                    isoS127CellPhone = null;

                    isoS127Dob = null;

                    isoS127EdcFunction = null;

                    isoS127Pws = null;

                    isoS127Id = null;

                    isoS127PayPws = null;

                    isoS127EcFlag = null;

                    isoS127MpFlag = null;

                    isoS127MerchVfy = null;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccAuthInfoStruct-2 Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        /// <summary></summary>
        /// <param name="requestTwinIso8583"></param>
        public NcccAuthInfoStruct(TWINIso8583 requestTwinIso8583) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2CardNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.000; //decimal point p49
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP18MccCode = null;
                isoP19AcqCountry = null;
                isoP22Em = null;
                isoP25CondCode = null;
                isoP32BinIca = null;
                isoP35ExpirDte = null;
                isoP35Svc = null;
                isoP35Pvki = null;
                isoP35Pvv = null;
                isoP35Cvv = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP43MerName = null;
                isoP43MerCity = null;
                isoP43LocCountry = null;
                de48Data = null;
                twinP48SubAcquirerId = null;
                twinP48NetworkRefId = null;
                isoP49TxCode = null;
                isoP52PinBlock = null;
                twinP55ChipCardData = null;
                isoP60NewPin = null;
                twinP61PosData = null;
                isoP57_P63InsInd = null;
                twinP63AdditionData = null;
                isoS95ReplaceAmt = (decimal)0.00;
                isoS127CellPhone = null;
                isoS127Dob = null;
                isoS127EdcFunction = null;
                isoS127Pws = null;
                isoS127Id = null;
                isoS127PayPws = null;
                isoS127EcFlag = null;
                isoS127MpFlag = null;
                isoS127MerchVfy = null;
                arqcCheck = 'N';
                arqcChkResult = 'N';
                cvvCheck = 'N';
                cvvChkResult = 'N';
                cvv2Check = 'N';
                cvv2ChkResult = 'N';
                cavvCheck = 'N';
                cavvChkResult = 'N';
                pvvCheck = 'N';
                pvvChkResult = null;
                pvvDobCheck = 'N';
                pvvDobChkResult = null;
                c5InstallRebateFlag = 'N';
                token04Data = null;
                token17Data = null;
                token20Data = null;
                tokenB0Data = null;
                tokenB2Data = null;
                tokenB3Data = null;
                tokenB4Data = null;
                tokenB5Data = null;
                tokenC0Data = null;
                tokenC4Data = null;
                tokenC5Data = null;
                tokenCHData = null;
                tokenF1Data = null;
                tokenQRData = null;
                tokenS8Data = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestTwinIso8583.Header().Encode();
                isoMessageType = requestTwinIso8583.Mti();
                var fieldPos = FIELD.F_2;
                isoP2CardNbr = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP2CardNbr = requestTwinIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    fieldPos = FIELD.F_35;
                    if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                    {
                        string track2Data = requestTwinIso8583.RawVal(fieldPos).ToString();
                        //int dataStartPos = 2;//include 2 variable length
                        int dataStartPos = 0;
                        int delimiterPos = 0;
                        string cardNbr = string.Empty;
                        delimiterPos = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                        if (delimiterPos < 0)
                        {
                            cardNbr = track2Data;
                        }
                        else
                        {
                            cardNbr = track2Data.Substring(dataStartPos, delimiterPos - dataStartPos);
                        }

                        isoP2CardNbr = cardNbr;
                    }
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_49;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestTwinIso8583.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP4Amt = Convert.ToDecimal(requestTwinIso8583.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_6;
                isoP6Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_51;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestTwinIso8583.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP6Amt = Convert.ToDecimal(requestTwinIso8583.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestTwinIso8583.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_11;
                isoP11TraceNo = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_18;
                isoP18MccCode = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP18MccCode = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_19;
                isoP19AcqCountry = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP19AcqCountry = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_22;
                isoP22Em = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP22Em = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_25;
                isoP25CondCode = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP25CondCode = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_35;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    string track2Data = requestTwinIso8583.RawVal(fieldPos).ToString();
                    int track2DataLength = track2Data.Length;
                    int startIndex = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                    if (startIndex > 0)
                    {
                        startIndex++;
                        int dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35ExpirDte = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Svc = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 1;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Pvki = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Pvv = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35Cvv = track2Data.Substring(startIndex, dataLength);
                    }
                }
                else
                {
                    isoP35ExpirDte = null;

                    isoP35Svc = null;

                    isoP35Pvki = null;

                    isoP35Pvv = null;

                    isoP35Cvv = null;
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestTwinIso8583.RawVal(fieldPos).ToString();
                }
                else
                {
                    if ((isoMessageType.Substring(1, 1).Equals("1")) ||
                        (isoMessageType.Substring(1, 1).Equals("2")))
                    {
                        isoP38AuthCode = ColaCommUtility.GetApproveCode();
                    }
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_43;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    if (requestTwinIso8583.RawVal(fieldPos).ToString().Length == 40)
                    {
                        string merchInfo = requestTwinIso8583.RawVal(fieldPos).ToString();
                        int startIndex = 0;
                        int dataLength = 22;
                        isoP43MerName = merchInfo.Substring(startIndex, dataLength);

                        startIndex = 22;
                        dataLength = 13;
                        isoP43MerCity = merchInfo.Substring(startIndex, dataLength);

                        startIndex = 38;
                        dataLength = 2;
                        isoP43LocCountry = merchInfo.Substring(startIndex, dataLength);
                    }
                }
                else
                {
                    isoP43MerName = null;

                    isoP43MerCity = null;

                    isoP43LocCountry = null;
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_48;
                de48Data = null;
                twinP48SubAcquirerId = null;
                twinP48NetworkRefId = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    if (requestTwinIso8583.RawVal(fieldPos).ToString().Length == 21)
                    {
                        string networkInfo = requestTwinIso8583.RawVal(fieldPos).ToString();
                        int startIndex = 0;
                        int dataLength = 6;
                        twinP48SubAcquirerId = networkInfo.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 15;
                        twinP48NetworkRefId = networkInfo.Substring(startIndex, dataLength);
                    }
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_49;
                isoP49TxCode = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP49TxCode = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_52;
                isoP52PinBlock = null;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoP52PinBlock = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_53;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_55;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    twinP55ChipCardData = (TWINFIELD_55)requestTwinIso8583.Core().GetOthersValByKey(fieldPos.ToString());
                    if (twinP55ChipCardData != null)
                    {

                    }
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_61;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    twinP61PosData = requestTwinIso8583.RawVal(fieldPos).ToString();
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_63;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    twinP63AdditionData = (TWINTLV)requestTwinIso8583.Core().GetOthersValByKey(fieldPos.ToString());
                    if (twinP63AdditionData != null)
                    {

                    }
                }

                //update for TWIN Card use
                fieldPos = FIELD.F_90;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    string originalDataElement = requestTwinIso8583.RawVal(fieldPos).ToString();
                    int startIndex = 0;
                    int dataLength = 4;
                    isoS90MessType = originalDataElement.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 12;
                    isoS90RefNbr = originalDataElement.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 4;
                    isoS90TxDte = originalDataElement.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 8;
                    isoS90TxTime = originalDataElement.Substring(startIndex, dataLength);
                }

                fieldPos = FIELD.F_95;
                isoS95ReplaceAmt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestTwinIso8583.RawVal(fieldPos)))
                {
                    isoS95ReplaceAmt = Convert.ToDecimal(requestTwinIso8583.RawVal(fieldPos).ToString().Substring(0, 12)) / (decimal)100.00;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccAuthInfoStruct-3 Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }
    }
}

using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.FISC.DE48;
using Com.Cybersoft4u.TM.FISC.DE58;
using Com.Cybersoft4u.TM.ISO8583;
using NLog;
using System;
using System.Diagnostics;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public struct FiscAuthInfoStruct
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        public string tranLogKey;            //Transaction Log Unique Key
        public string isoMessageType;        //messageType
        public string isoP2CardNbr;          //P2
        public string isoP3ProcessCde;       //P3
        public decimal isoP4Amt;             //p4
        public decimal isoP6Amt;             //p6
        public string isoP7TxDte;            //p7   mmdd
        public string isoP7TxTime;           //p7   hhmmss
        public string isoP10ConvertRte;      //p10  convert rate
        public string isoP11TraceNo;         //p11  trace no
        public string isoP14ExpirDte;        //p14  trace no(YYMM)
        public string isoP18MccCode;         //P18
        public string isoP19AcqCountry;      //P19  Country Code
        public string isoP22Em;              //p22
        public string isoP32BinIca;          //p32  收單行代碼
        public string isoP35OrP45ExpirDte;   //p35  Or P45 Expired Date
        public string isoP35OrP45Svc;        //p35  Or P45 SVC Code
        public string isoP35OrP45Pki;        //p35  Or P45 PVKI
        public string isoP35OrP45Pvv;        //p35  Or P45 PVV
        public string isoP35OrP45Cvv;        //p35  Or P45 CVV
        public string isoP37RefNbr;          //p37  Reference Number
        public string isoP38AuthCode;        //p38
        public string isoP39Rsp;             //p39
        public string isoP41TermNbr;         //p41  終端機編號
        public string isoP42Merchant;        //p42  特店號碼
        public string isoP43MerInfo;         //p43  特店資料
        public string isoP48T42;             //p48,Tag42 ECI
        public string isoP48T43;             //p48,Tag43 CAVV DATA
        public string isoP48T45;             //p48,Tag45 CAVV Result
        public string isoP49CurrencyCode;    //p49 Acquirer Currency Code
        public string isoP51BillingCurrency; //p51 Billing Currency Code
        public string isoP52PinBlock;        //p52 Pin Block
        //public string isoP55
        public string isoP55TVR;             //p55 TVR
        public string isoP55ATC;             //p55 ATC
        public string isoP55CVR;             //p55 CVR
        public string isoP55MDK;             //p55 NDK
        public string isoP55VerNum;          //p55 Version Number
        public string isoP55ROC;             //p55 ROC
        public string isoP55ARQC;            //p55 ARQC
        public string isoP55Un;              //p55 Un
        public string isoP55RSP;             //p55 RSP
        //public string isoP58
        public string isoP58T21LoyaltyNbr;  //p58 紅利折抵編號
        public string isoP58T21LoyaltyInd;  //p58 紅利註記
        public string isoP58T21TxResp;      //p58 紅利回覆碼
        public string isoP58T21SignPoint;   //p58 <紅利, 餘點正負項>
        public string isoP58T21PointBalance;    //p58  <紅利, 餘點>
        public string isoP58T21PointRedemption; //p58  <紅利,折扺點數>
        public string isoP58T21PaidCAmt;         //p58 <紅利,實際支付金額>
        public string isoP58T32Tpan;             //p58  HCE 卡號
        public string isoP58T32ExpDat;           //p58  HCE 效期
        public string isoP58T32AcVeryRslt;       //p58  HCE AC RESULT 
        public string isoP58T32FraudChkRslt;     //p58  HCE Fraud Check Result 
        public string isoP58T67;                 //身份証號
        public string isoP58T68;                 //01:NCCC ON-US Payment Platform 02:E-Invoice 03:電支平台
        public string isoP58T69MPflag;           //Tag 69 MP flag 小額支付
        public string isoP58T87CellPhone;        //身份驗証:手機號碼
        public string isoP58T87Dob;              //身份驗証:生日
        public string isoP61PosChPresInd;        //定期性繳款標識(DE61 Position4=’4’)
        public string isoP61TermEntryCap;        //FIELD 61 Position 11
        public string isoP61VisaAcctVfy;         //#61  Visa Acctount Verify
        public decimal isoS95ReplaceAmt;         //#95  Replement Amt
        public string isoS112InstTxInd;          //#112 分期註記
        public string isoS112InstPeriod;         //#112 分期期數
        public string isoS112DownPayment;        //#112 首期期金
        public string isoS112InstPayment;        //#112 每期期金
        public string isoS112InstFee;            //#112 手續費
        public string isoS112InstTxResp;         //#112 分期回應碼
        //Des Function
        public char arqcCheck;                   //ARQC驗証 Y:有驗証 N:未驗証
        public char arqcChkResult;               //Y : ARQC驗証通過  N:ARQC驗証失敗
        public char cvvCheck;                    //CVV 驗証 Y:有驗証 N:未驗証
        public char cvvChkResult;                //Y : CVV 驗証通過  N:CVV 驗証失敗
        public char cvv2Check;                   //CVV2 驗証 Y:有驗証 N:未驗証
        public char cvv2ChkResult;               //Y : CVV2 驗証通過  N:CVV2 驗証失敗
        public char cavvCheck;                   //CAVV 驗証 Y:有驗証 N:未驗証
        public char cavvChkResult;               //Y : CAVV  驗証通過  N:CAVV 驗証失敗
        public char pvvCheck;                    //PVV 驗証 Y:有驗証 N:未驗証
        public string pvvChkResult;              //Field 52 create PVV
        public char pvvDobCheck;                 //Y: 算回PVV值 N:不用算回PVV值
        public string pvvDobChkResult;           //僅@isoP18MccCode=4011時, 呼叫執Sp:Cola_SelectCardholderDob-->取得CLEAR PIN 呼叫hsm(EncryptBankPin) 算回PVV值放回

        public FiscAuthInfoStruct(FISC requestFiscMessage) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                isoMessageType = null;
                isoP2CardNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.000; //decimal point p49
                isoP6Amt = (decimal)0.000; //decimal point p51
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP10ConvertRte = null;
                isoP11TraceNo = null;
                isoP14ExpirDte = null;
                isoP18MccCode = null;
                isoP19AcqCountry = null;
                isoP22Em = null;
                isoP32BinIca = null;
                isoP35OrP45ExpirDte = null;
                isoP35OrP45Svc = null;
                isoP35OrP45Pki = null;
                isoP35OrP45Pvv = null;
                isoP35OrP45Cvv = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP43MerInfo = null;
                isoP48T42 = null;
                isoP48T43 = null;
                isoP48T45 = null;
                isoP49CurrencyCode = null;
                isoP51BillingCurrency = null;
                isoP52PinBlock = null;
                isoP55TVR = null;
                isoP55ATC = null;
                isoP55CVR = null;
                isoP55MDK = null;
                isoP55VerNum = null;
                isoP55ROC = null;
                isoP55ARQC = null;
                isoP55Un = null;
                isoP55RSP = null;
                isoP58T21LoyaltyNbr = null;
                isoP58T21LoyaltyInd = null;
                isoP58T21TxResp = null;
                isoP58T21SignPoint = null;
                isoP58T21PointBalance = null;
                isoP58T21PointRedemption = null;
                isoP58T21PaidCAmt = null;
                isoP58T67 = null;
                isoP58T68 = null;
                isoP58T69MPflag = null;
                isoP58T87CellPhone = null;
                isoP58T87Dob = null;
                isoP61PosChPresInd = null;
                isoP61TermEntryCap = null;
                isoP61VisaAcctVfy = null;
                isoS95ReplaceAmt = (decimal)0.00;
                isoS112InstTxInd = null;
                isoS112InstPeriod = null;
                isoS112DownPayment = null;
                isoS112InstPayment = null;
                isoS112InstFee = null;
                isoS112InstTxResp = null;
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

                // Add the input parameter and set its properties.
                isoMessageType = requestFiscMessage.Mti();
                var fieldPos = FIELD.F_2;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP2CardNbr = requestFiscMessage.RawVal(fieldPos).ToString();
                }
                else
                {
                    fieldPos = FIELD.F_35;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    {
                        string track2Data = requestFiscMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_49;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestFiscMessage.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP4Amt = Convert.ToDecimal(requestFiscMessage.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_6;
                isoP6Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_51;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestFiscMessage.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP6Amt = Convert.ToDecimal(requestFiscMessage.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestFiscMessage.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_10; ;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP10ConvertRte = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_11;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_14;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP14ExpirDte = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_18;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP18MccCode = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_19;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP19AcqCountry = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_22;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP22Em = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                //Check Track1 Or Track 2
                fieldPos = FIELD.F_35;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string track2Data = requestFiscMessage.RawVal(fieldPos).ToString();
                    int track2DataLength = track2Data.Length;
                    int startIndex = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                    if (startIndex > 0)
                    {
                        startIndex++;
                        int dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45ExpirDte = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Svc = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 1;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Pki = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Pvv = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Cvv = track2Data.Substring(startIndex, dataLength);
                    }
                }
                else
                {
                    //Check TrackI
                    var track1FieldPos = FIELD.F_45;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    {
                        string track1Data = requestFiscMessage.RawVal(track1FieldPos).ToString();
                        int track1DataLength = track1Data.Length;
                        //find second delimiter
                        var firstDelimiterIndex = track1Data.IndexOf(SystemConstantData.CommonData.P45CardDelimiterChar, 0);
                        if (firstDelimiterIndex > 0)
                        {
                            var secondDelimiterIndex = track1Data.IndexOf(SystemConstantData.CommonData.P45CardDelimiterChar, firstDelimiterIndex + 1);
                            if (secondDelimiterIndex > 0)
                            {
                                var startIndex = secondDelimiterIndex;

                                startIndex++;
                                int dataLength = 4;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45ExpirDte = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 3;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Svc = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 1;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Pki = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 4;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Pvv = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 3;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Cvv = track1Data.Substring(startIndex, dataLength);
                            }
                        }
                    }
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestFiscMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_43;
                isoP43MerInfo = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP43MerInfo = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_48;
                isoP48T42 = null;
                isoP48T43 = null;
                isoP48T45 = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var de48TagData = requestFiscMessage.RawVal(fieldPos).ToString();
                    var de48TagField = new DE48TagField(de48TagData);
                    var de48TagIdString = "42";
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                    {
                        // Last Character For tempString
                        string tempString = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                        isoP48T42 = tempString.Substring(tempString.Length - 1, 1);
                    }

                    de48TagIdString = "43";
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                    {
                        isoP48T43 = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                    }

                    de48TagIdString = "45";
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                    {
                        isoP48T45 = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                    }
                }

                fieldPos = FIELD.F_49;
                isoP49CurrencyCode = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP49CurrencyCode = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_51;
                isoP51BillingCurrency = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP51BillingCurrency = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_52;
                isoP52PinBlock = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP52PinBlock = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_55;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var emvData = requestFiscMessage.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;

                    string emvTagIdString = "95";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55TVR = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "9F36";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55ATC = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    CreditCardType creditCardType = CreditCardType.Unknown;
                    if (!string.IsNullOrEmpty(isoP2CardNbr))
                    {
                        creditCardType = CreditCardUtility.FindCardType(isoP2CardNbr);
                        if (creditCardType == CreditCardType.Unknown)
                            creditCardType = CreditCardType.MasterCard;
                    }

                    emvTagIdString = "9F10";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            string emv9F10Data = CommonUtils.HexBinary2HexString(tagData.Value);
                            string[] data9F10 = ColaCommUtility.FiscGet9F10Data(emv9F10Data.Length, emv9F10Data, creditCardType).Split(';');
                            if (data9F10.Length == 3)
                            {
                                isoP55MDK = data9F10[0];
                                isoP55VerNum = data9F10[1];
                                isoP55CVR = data9F10[2];
                            }
                        }
                    }

                    emvTagIdString = "DFEF";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55ROC = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "9F26";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55ARQC = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "9F37";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55Un = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "91";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "8A";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55RSP = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }
                }

                fieldPos = FIELD.F_58;
                isoP58T21LoyaltyNbr = null;
                isoP58T21LoyaltyInd = null;
                isoP58T21TxResp = null;
                isoP58T21SignPoint = null;
                isoP58T21PointBalance = null;
                isoP58T21PointRedemption = null;
                isoP58T21PaidCAmt = null;
                isoP58T32Tpan = null;
                isoP58T32ExpDat = null;
                isoP58T32AcVeryRslt = null;
                isoP58T32FraudChkRslt = null;

                isoP58T67 = null;
                isoP58T68 = null;
                isoP58T87CellPhone = null;
                isoP58T87Dob = null;

                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var de58TagData = requestFiscMessage.RawVal(fieldPos).ToString();
                    var de58TagField = new DE58TagField(de58TagData);
                    var de58TagIdString = "21";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        var isoP58T21Data = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                        if ((!string.IsNullOrEmpty(isoP58T21Data)) && (isoP58T21Data.Length == 42))
                        {
                            int offSet = 0;
                            int dataLength = 0;

                            dataLength = 6;
                            isoP58T21LoyaltyNbr = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 1;
                            isoP58T21LoyaltyInd = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 2;
                            isoP58T21TxResp = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 1;
                            isoP58T21SignPoint = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 10;
                            isoP58T21PointBalance = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 10;
                            isoP58T21PointRedemption = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 12;
                            isoP58T21PaidCAmt = isoP58T21Data.Substring(offSet, dataLength);
                        }
                    }

                    de58TagIdString = "32";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        var isoP58T32Data = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                        if ((!string.IsNullOrEmpty(isoP58T32Data)) && (isoP58T32Data.Length == 56))
                        {
                            //TRACKII(37) + TSP ID (11) + AC Validation Result (2) + Fraud check Result (2) + Reason code (2)
                            int offSet = 0;
                            int dataLength = 2;
                            var isoP58trackIILength = Convert.ToInt32(isoP58T32Data.Substring(offSet, dataLength));
                            offSet += dataLength;

                            dataLength = isoP58trackIILength;
                            var isoP58trackIIData = isoP58T32Data.Substring(offSet, dataLength);
                            var delimiterPos = isoP58trackIIData.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                            if (delimiterPos < 0)
                            {
                                isoP58T32Tpan = isoP58trackIIData;
                            }
                            else
                            {
                                isoP58T32Tpan = isoP58trackIIData[..delimiterPos];
                                int expDataLength = 4;
                                int delimiterLength = 1;
                                if ((delimiterPos + expDataLength) <= isoP58trackIILength)
                                    isoP58T32ExpDat = isoP58trackIIData.Substring(delimiterPos + delimiterLength, expDataLength);
                            }
                            offSet += dataLength;

                            //TSP ID(11) 
                            dataLength = 11;
                            offSet += dataLength;

                            //AC Validation Result(2) 
                            dataLength = 2;
                            isoP58T32AcVeryRslt = isoP58T32Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            //Fraud check Result(2)
                            dataLength = 2;
                            isoP58T32FraudChkRslt = isoP58T32Data.Substring(offSet, dataLength);
                            offSet += dataLength;
                        }
                    }

                    de58TagIdString = "67";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        isoP58T67 = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                    }

                    de58TagIdString = "68";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        isoP58T68 = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                    }

                    de58TagIdString = "69";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        isoP58T69MPflag = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                    }

                    de58TagIdString = "87";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        var isoP58T87Data = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                        var tlv = ColaCommUtility.CreateTLV(2, 2, isoP58T87Data);
                        if (tlv != null)
                        {
                            string id = "01";
                            if (tlv.ContainsKey(id))
                            {
                                isoP58T87Dob = tlv[id];
                            }
                            id = "02";
                            if (tlv.ContainsKey(id))
                            {
                                isoP58T87CellPhone = tlv[id];
                            }
                        }
                    }
                }

                fieldPos = FIELD.F_61; ;
                isoP61VisaAcctVfy = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    int posDataTotalLength = requestFiscMessage.RawVal(fieldPos).ToString().Length;
                    if (posDataTotalLength >= 14)
                    {
                        isoP61VisaAcctVfy = requestFiscMessage.RawVal(fieldPos).ToString().Substring(9, 1);
                        isoP61TermEntryCap = requestFiscMessage.RawVal(fieldPos).ToString().Substring(13, 1);
                    }
                    else if (posDataTotalLength >= 10)
                        isoP61VisaAcctVfy = requestFiscMessage.RawVal(fieldPos).ToString().Substring(9, 1);

                }

                fieldPos = FIELD.F_95;
                isoS95ReplaceAmt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoS95ReplaceAmt = Convert.ToDecimal(requestFiscMessage.RawVal(fieldPos).ToString().Substring(0, 12)) /
                                       (decimal)100.00;
                }

                fieldPos = FIELD.F_112;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string installmentData = requestFiscMessage.RawVal(fieldPos).ToString();
                    int installmentDataLength = installmentData.Length;
                    int startIndex = 0;
                    int dataLength = 1;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstTxInd = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstPeriod = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 8;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112DownPayment = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 8;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstPayment = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 6;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstFee = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstTxResp = installmentData.Substring(startIndex, dataLength);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscAuthInfoStruct-1 Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        public FiscAuthInfoStruct(FISC requestFiscMessage, string txKeySeq) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = txKeySeq;
                isoMessageType = null;
                isoP2CardNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.000; //decimal point p49
                isoP6Amt = (decimal)0.000; //decimal point p51
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP10ConvertRte = null;
                isoP11TraceNo = null;
                isoP14ExpirDte = null;
                isoP18MccCode = null;
                isoP19AcqCountry = null;
                isoP22Em = null;
                isoP32BinIca = null;
                isoP35OrP45ExpirDte = null;
                isoP35OrP45Svc = null;
                isoP35OrP45Pki = null;
                isoP35OrP45Pvv = null;
                isoP35OrP45Cvv = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP43MerInfo = null;
                isoP48T42 = null;
                isoP48T43 = null;
                isoP48T45 = null;
                isoP49CurrencyCode = null;
                isoP51BillingCurrency = null;
                isoP52PinBlock = null;
                isoP55TVR = null;
                isoP55ATC = null;
                isoP55CVR = null;
                isoP55MDK = null;
                isoP55VerNum = null;
                isoP55ROC = null;
                isoP55ARQC = null;
                isoP55Un = null;
                isoP55RSP = null;
                isoP58T21LoyaltyNbr = null;
                isoP58T21LoyaltyInd = null;
                isoP58T21TxResp = null;
                isoP58T21SignPoint = null;
                isoP58T21PointBalance = null;
                isoP58T21PointRedemption = null;
                isoP58T21PaidCAmt = null;
                isoP58T32Tpan = null;
                isoP58T32ExpDat = null;
                isoP58T32AcVeryRslt = null;
                isoP58T32FraudChkRslt = null;
                isoP58T67 = null;
                isoP58T68 = null;
                isoP58T69MPflag = null;
                isoP58T87CellPhone = null;
                isoP58T87Dob = null;
                isoP61PosChPresInd = null;
                isoP61TermEntryCap = null;
                isoP61VisaAcctVfy = null;
                isoS95ReplaceAmt = (decimal)0.00;
                isoS112InstTxInd = null;
                isoS112InstPeriod = null;
                isoS112DownPayment = null;
                isoS112InstPayment = null;
                isoS112InstFee = null;
                isoS112InstTxResp = null;
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

                // Add the input parameter and set its properties.
                isoMessageType = requestFiscMessage.Mti();
                var fieldPos = FIELD.F_2;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP2CardNbr = requestFiscMessage.RawVal(fieldPos).ToString();
                }
                else
                {
                    fieldPos = FIELD.F_35;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    {
                        string track2Data = requestFiscMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_49;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestFiscMessage.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP4Amt = Convert.ToDecimal(requestFiscMessage.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_6;
                isoP6Amt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var currencyCodeFieldPos = FIELD.F_51;
                    var defaultDecimalPoint = 2;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(currencyCodeFieldPos)))
                    {
                        defaultDecimalPoint = ColaCommUtility.GetDecimalPointByCurrencyCode(requestFiscMessage.RawVal(currencyCodeFieldPos).ToString());
                    }

                    isoP6Amt = Convert.ToDecimal(requestFiscMessage.RawVal(fieldPos).ToString()) / (decimal)Math.Pow(10, defaultDecimalPoint);
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestFiscMessage.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_10; ;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP10ConvertRte = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_11;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_14;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP14ExpirDte = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_18;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP18MccCode = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_19;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP19AcqCountry = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_22;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP22Em = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                //Check Track1 Or Track 2
                fieldPos = FIELD.F_35;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string track2Data = requestFiscMessage.RawVal(fieldPos).ToString();
                    int track2DataLength = track2Data.Length;
                    int startIndex = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                    if (startIndex > 0)
                    {
                        startIndex++;
                        int dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45ExpirDte = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Svc = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 1;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Pki = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 4;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Pvv = track2Data.Substring(startIndex, dataLength);

                        startIndex += dataLength;
                        dataLength = 3;
                        if ((startIndex + dataLength) <= track2DataLength)
                            isoP35OrP45Cvv = track2Data.Substring(startIndex, dataLength);
                    }
                }
                else
                {
                    //Check TrackI
                    var track1FieldPos = FIELD.F_45;
                    if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                    {
                        string track1Data = requestFiscMessage.RawVal(track1FieldPos).ToString();
                        int track1DataLength = track1Data.Length;
                        //find second delimiter
                        var firstDelimiterIndex = track1Data.IndexOf(SystemConstantData.CommonData.P45CardDelimiterChar, 0);
                        if (firstDelimiterIndex > 0)
                        {
                            var secondDelimiterIndex = track1Data.IndexOf(SystemConstantData.CommonData.P45CardDelimiterChar, firstDelimiterIndex + 1);
                            if (secondDelimiterIndex > 0)
                            {
                                var startIndex = secondDelimiterIndex;

                                startIndex++;
                                int dataLength = 4;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45ExpirDte = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 3;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Svc = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 1;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Pki = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 4;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Pvv = track1Data.Substring(startIndex, dataLength);

                                startIndex += dataLength;
                                dataLength = 3;
                                if ((startIndex + dataLength) <= track1DataLength)
                                    isoP35OrP45Cvv = track1Data.Substring(startIndex, dataLength);
                            }
                        }
                    }
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestFiscMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_43;
                isoP43MerInfo = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP43MerInfo = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_48;
                isoP48T42 = null;
                isoP48T43 = null;
                isoP48T45 = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var de48TagData = requestFiscMessage.RawVal(fieldPos).ToString();
                    var de48TagField = new DE48TagField(de48TagData);
                    var de48TagIdString = "42";
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                    {
                        // Last Character For tempString
                        string tempString = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                        isoP48T42 = tempString.Substring(tempString.Length - 1, 1);
                    }

                    de48TagIdString = "43";
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                    {
                        isoP48T43 = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                    }

                    de48TagIdString = "45";
                    if (de48TagField.FindDE48TagIdExist(de48TagIdString))
                    {
                        isoP48T45 = de48TagField.GetDE48TagDataByTokenId(de48TagIdString);
                    }
                }

                fieldPos = FIELD.F_49;
                isoP49CurrencyCode = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP49CurrencyCode = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_51;
                isoP51BillingCurrency = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP51BillingCurrency = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_52;
                isoP52PinBlock = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoP52PinBlock = requestFiscMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_55;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var emvData = requestFiscMessage.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;

                    string emvTagIdString = "95";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55TVR = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "9F36";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55ATC = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    CreditCardType creditCardType = CreditCardType.Unknown;
                    if (!string.IsNullOrEmpty(isoP2CardNbr))
                    {
                        creditCardType = CreditCardUtility.FindCardType(isoP2CardNbr);
                        if (creditCardType == CreditCardType.Unknown)
                            creditCardType = CreditCardType.MasterCard;
                    }

                    emvTagIdString = "9F10";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            string emv9F10Data = CommonUtils.HexBinary2HexString(tagData.Value);
                            string[] data9F10 = ColaCommUtility.FiscGet9F10Data(emv9F10Data.Length, emv9F10Data, creditCardType).Split(';');
                            if (data9F10.Length == 3)
                            {
                                isoP55MDK = data9F10[0];
                                isoP55VerNum = data9F10[1];
                                isoP55CVR = data9F10[2];
                            }
                        }
                    }

                    emvTagIdString = "DFEF";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55ROC = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "9F26";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55ARQC = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "9F37";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55Un = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "91";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }

                    emvTagIdString = "8A";
                    if (emvData.ContainsKey(emvTagIdString))
                    {
                        var tagData = emvData[emvTagIdString];
                        if (tagData != null)
                        {
                            isoP55RSP = CommonUtils.HexBinary2HexString(tagData.Value);
                        }
                    }
                }

                fieldPos = FIELD.F_58;
                isoP58T21LoyaltyNbr = null;
                isoP58T21LoyaltyInd = null;
                isoP58T21TxResp = null;
                isoP58T21SignPoint = null;
                isoP58T21PointBalance = null;
                isoP58T21PointRedemption = null;
                isoP58T21PaidCAmt = null;
                isoP58T32Tpan = null;
                isoP58T32ExpDat = null;
                isoP58T32AcVeryRslt = null;
                isoP58T32FraudChkRslt = null;
                isoP58T67 = null;
                isoP58T68 = null;
                isoP58T87CellPhone = null;
                isoP58T87Dob = null;

                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    var de58TagData = requestFiscMessage.RawVal(fieldPos).ToString();
                    var de58TagField = new DE58TagField(de58TagData);
                    var de58TagIdString = "21";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        var isoP58T21Data = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                        if ((!string.IsNullOrEmpty(isoP58T21Data)) && (isoP58T21Data.Length == 42))
                        {
                            int offSet = 0;
                            int dataLength = 0;

                            dataLength = 6;
                            isoP58T21LoyaltyNbr = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 1;
                            isoP58T21LoyaltyInd = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 2;
                            isoP58T21TxResp = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 1;
                            isoP58T21SignPoint = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 10;
                            isoP58T21PointBalance = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 10;
                            isoP58T21PointRedemption = isoP58T21Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            dataLength = 12;
                            isoP58T21PaidCAmt = isoP58T21Data.Substring(offSet, dataLength);
                        }
                    }

                    de58TagIdString = "32";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        var isoP58T32Data = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                        if ((!string.IsNullOrEmpty(isoP58T32Data)) && (isoP58T32Data.Length == 56))
                        {
                            //TRACKII(37) + TSP ID (11) + AC Validation Result (2) + Fraud check Result (2) + Reason code (2)
                            int offSet = 0;
                            int dataLength = 2;
                            var isoP58trackIILength = Convert.ToInt32(isoP58T32Data.Substring(offSet, dataLength));
                            offSet += dataLength;

                            dataLength = isoP58trackIILength;
                            var isoP58trackIIData = isoP58T32Data.Substring(offSet, dataLength);
                            var delimiterPos = isoP58trackIIData.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                            if (delimiterPos < 0)
                            {
                                isoP58T32Tpan = isoP58trackIIData;
                            }
                            else
                            {
                                isoP58T32Tpan = isoP58trackIIData[..delimiterPos];
                                int expDataLength = 4;
                                int delimiterLength = 1;
                                if ((delimiterPos + expDataLength) <= isoP58trackIILength)
                                    isoP58T32ExpDat = isoP58trackIIData.Substring(delimiterPos + delimiterLength, expDataLength);
                            }
                            offSet += dataLength;

                            //TSP ID(11) 
                            dataLength = 11;
                            offSet += dataLength;

                            //AC Validation Result(2) 
                            dataLength = 2;
                            isoP58T32AcVeryRslt = isoP58T32Data.Substring(offSet, dataLength);
                            offSet += dataLength;

                            //Fraud check Result(2)
                            dataLength = 2;
                            isoP58T32FraudChkRslt = isoP58T32Data.Substring(offSet, dataLength);
                            offSet += dataLength;
                        }
                    }

                    de58TagIdString = "67";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        isoP58T67 = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                    }

                    de58TagIdString = "68";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        isoP58T68 = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                    }

                    de58TagIdString = "69";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        isoP58T69MPflag = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                    }

                    de58TagIdString = "87";
                    if (de58TagField.FindDE58TagIdExist(de58TagIdString))
                    {
                        var isoP58T87Data = de58TagField.GetDE58TagDataByTokenId(de58TagIdString);
                        var tlv = ColaCommUtility.CreateTLV(2, 2, isoP58T87Data);
                        if (tlv != null)
                        {
                            string id = "01";
                            if (tlv.ContainsKey(id))
                            {
                                isoP58T87Dob = null;
                            }
                            id = "02";
                            if (tlv.ContainsKey(id))
                            {
                                isoP58T87CellPhone = tlv[id];
                            }
                        }
                    }
                }

                fieldPos = FIELD.F_61; ;
                isoP61VisaAcctVfy = null;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    int posDataTotalLength = requestFiscMessage.RawVal(fieldPos).ToString().Length;
                    if (posDataTotalLength >= 14)
                    {
                        isoP61VisaAcctVfy = requestFiscMessage.RawVal(fieldPos).ToString().Substring(9, 1);
                        isoP61TermEntryCap = requestFiscMessage.RawVal(fieldPos).ToString().Substring(13, 1);
                    }
                    else if (posDataTotalLength >= 10)
                        isoP61VisaAcctVfy = requestFiscMessage.RawVal(fieldPos).ToString().Substring(9, 1);

                }

                fieldPos = FIELD.F_95;
                isoS95ReplaceAmt = (decimal)0.000;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    isoS95ReplaceAmt = Convert.ToDecimal(requestFiscMessage.RawVal(fieldPos).ToString().Substring(0, 12)) /
                                       (decimal)100.00;
                }

                fieldPos = FIELD.F_112;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string installmentData = requestFiscMessage.RawVal(fieldPos).ToString();
                    int installmentDataLength = installmentData.Length;
                    int startIndex = 0;
                    int dataLength = 1;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstTxInd = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstPeriod = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 8;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112DownPayment = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 8;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstPayment = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 6;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstFee = installmentData.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 2;
                    if ((startIndex + dataLength) <= installmentDataLength)
                        isoS112InstTxResp = installmentData.Substring(startIndex, dataLength);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscAuthInfoStruct-2 Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }
    }
}

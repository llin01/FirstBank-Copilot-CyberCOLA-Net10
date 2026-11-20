using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using NLog;
using System;
using System.Diagnostics;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public struct EsvcAuthInfoStruct
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        public string tranLogKey;            //Transaction Log Unique Key
        public string isoMessageHeader;      //MessageHeader
        public string isoMessageType;        //MessageType
        public string isoP2PidNbr;           //P2 Pid
        public string isoP3ProcessCde;       //P3 Process Code
        public decimal isoP4Amt;             //P4 Transaction Amount
        public string isoP7TxDte;            //P7 TxDte(mmdd)
        public string isoP7TxTime;           //P7 TxTime(hhmmss)
        public string isoP11TraceNo;         //P11 TraceNbr
        public string isoP12LocalTime;       //P12 Transaction Local Time(hhmmss)
        public string isoP13LocalDte;        //P13 Transaction Local Date(mmdd)
        public string isoP13TransYear;       //P13 Transmission Year (YYYY)(IPASS)
        public string isoP14ExpirDte;        //P14 Card ExpirDte
        public string isoP15StleDte;         //P15 Settlement Date
        public string isoP32BinIca;          //P32 Acquire Identification Code
        public string isoP36NTID;            //P36 NTID
        public string isoP37RefNbr;          //P37 Retrieval Reference Number
        public string isoP38AuthCode;        //P38 AuthCode Code
        public string isoP39Rsp;             //P39 Response Code
        public string isoP41TermNbr;         //P41 Terminal ID
        public string isoP42Merchant;        //P42 Merchant ID
        public string isoP45SAMId;           //P45 SAM ID(IPASS)
        public string isoP58T05; 	         //58 Tag05 Host TX DateTime CCYYMMDDhhmmss
        public string isoP58T06; 	         //58 Tag06 Terminal TX DateTime CCYYMMDDhhmmss
        public string isoP58T07;	         //P58 Tag07 Terminal Trace No
        public string isoP58T10;	         //P58 Tag10 Cardholder's BirthDate
        public string isoP61T03;             //P61 Tag03 門市代號(ICASH)
        public string isoP61T04;             //P61 Tag04 收銀機代號(ICASH)
        public string isoP61T09;             //P61 Tag09 卡機回傳狀態(ICASH)
        public string isoP63SystemId;        //P63 System Id(IPASS)
        public string isoS90MessageType;     //S90 Origin Tx MessageType
        public string isoS90TxDte;           //S90 Origin Tx Date
        public string isoS90TxTime;          //S90 Origin Tx Time
        public string isoS90Trace;           //S90 Origin Tx TraceNbr
        public string isoS90BinIca;          //S90 Origin Tx Bin
        public string isoS90RefNbr;	         //S90 Origin Tx Retrieval Reference Number

        public EsvcAuthInfoStruct(SVCS requestSvcsMessage) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2PidNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP12LocalTime = null;
                isoP13LocalDte = null;
                isoP13TransYear = null;
                isoP14ExpirDte = null;
                isoP15StleDte = null;
                isoP32BinIca = null;
                isoP36NTID = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP45SAMId = null;
                isoP58T05 = null;
                isoP58T06 = null;
                isoP58T07 = null;
                isoP58T10 = null;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                isoP63SystemId = null;
                isoS90MessageType = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS90Trace = null;
                isoS90BinIca = null;
                isoS90RefNbr = null;

                isoMessageHeader = requestSvcsMessage.Header().Encode();
                isoMessageType = requestSvcsMessage.Mti();
                var fieldPos = FIELD.F_2;
                isoP2PidNbr = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP2PidNbr = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP4Amt = Convert.ToDecimal(requestSvcsMessage.RawVal(fieldPos).ToString()) / (decimal)100.00;
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestSvcsMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_12;
                isoP12LocalTime = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP12LocalTime = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_13;
                isoP13LocalDte = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP13LocalDte = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_14;
                isoP14ExpirDte = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP14ExpirDte = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_15;
                isoP15StleDte = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP15StleDte = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_58;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    var svcsChip = requestSvcsMessage.F58TokenEncode();
                    if (svcsChip != null)
                    {
                        var svcsChipField = FIELD.F_5;
                        isoP58T05 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T05 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                        svcsChipField = FIELD.F_6;
                        isoP58T06 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T06 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                        svcsChipField = FIELD.F_7;
                        isoP58T07 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T07 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                        svcsChipField = FIELD.F_10;
                        isoP58T10 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T10 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                    }
                }

                fieldPos = FIELD.F_90;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    var svcs90Data = requestSvcsMessage.F90OriginalDataElementsEncode();
                    isoS90MessageType = svcs90Data.Substring(0, 4);
                    isoS90TxDte = svcs90Data.Substring(4, 4);
                    isoS90TxTime = svcs90Data.Substring(8, 6);
                    isoS90Trace = svcs90Data.Substring(14, 6);
                    isoS90BinIca = svcs90Data.Substring(20, 8);
                    isoS90RefNbr = svcs90Data.Substring(28, 12);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EsvcAuthInfoStruct Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        public EsvcAuthInfoStruct(SVCS requestSvcsMessage, string txKeySeq) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = txKeySeq;
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2PidNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP12LocalTime = null;
                isoP13LocalDte = null;
                isoP13TransYear = null;
                isoP14ExpirDte = null;
                isoP15StleDte = null;
                isoP32BinIca = null;
                isoP36NTID = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP45SAMId = null;
                isoP58T05 = null;
                isoP58T06 = null;
                isoP58T07 = null;
                isoP58T10 = null;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                isoP63SystemId = null;
                isoS90MessageType = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS90Trace = null;
                isoS90BinIca = null;
                isoS90RefNbr = null;

                isoMessageHeader = requestSvcsMessage.Header().Encode();
                isoMessageType = requestSvcsMessage.Mti();
                var fieldPos = FIELD.F_2;
                isoP2PidNbr = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP2PidNbr = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP4Amt = Convert.ToDecimal(requestSvcsMessage.RawVal(fieldPos).ToString()) / (decimal)100.00;
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestSvcsMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_12;
                isoP12LocalTime = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP12LocalTime = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_13;
                isoP13LocalDte = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP13LocalDte = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_14;
                isoP14ExpirDte = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP14ExpirDte = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_15;
                isoP15StleDte = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP15StleDte = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestSvcsMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_58;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    var svcsChip = requestSvcsMessage.F58TokenEncode();
                    if (svcsChip != null)
                    {
                        var svcsChipField = FIELD.F_5;
                        isoP58T05 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T05 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                        svcsChipField = FIELD.F_6;
                        isoP58T06 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T06 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                        svcsChipField = FIELD.F_7;
                        isoP58T07 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T07 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                        svcsChipField = FIELD.F_10;
                        isoP58T10 = null;
                        if (!ObjectUtils.IsNull(svcsChip.RawVal(svcsChipField)))
                        {
                            isoP58T10 = svcsChip.RawVal(svcsChipField).ToString();
                        }
                    }
                }

                fieldPos = FIELD.F_90;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)))
                {
                    var svcs90Data = requestSvcsMessage.F90OriginalDataElementsEncode();
                    isoS90TxDte = svcs90Data.Substring(0, 4);
                    isoS90TxTime = svcs90Data.Substring(4, 10);
                    isoS90Trace = svcs90Data.Substring(14, 6);
                    isoS90BinIca = svcs90Data.Substring(20, 8);
                    isoS90RefNbr = svcs90Data.Substring(28, 12);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EsvcAuthInfoStruct Constructor StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        public EsvcAuthInfoStruct(ICASH requestIcashMessage) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2PidNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP12LocalTime = null;
                isoP13LocalDte = null;
                isoP13TransYear = null;
                isoP14ExpirDte = null;
                isoP15StleDte = null;
                isoP32BinIca = null;
                isoP36NTID = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP45SAMId = null;
                isoP58T05 = null;
                isoP58T06 = null;
                isoP58T07 = null;
                isoP58T10 = null;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                isoP63SystemId = null;
                isoS90MessageType = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS90Trace = null;
                isoS90BinIca = null;
                isoS90RefNbr = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestIcashMessage.Header().Encode();
                isoMessageType = requestIcashMessage.Mti();
                var fieldPos = FIELD.F_2;
                isoP2PidNbr = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP2PidNbr = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    //isoP4Amt = Convert.ToDecimal(requestIcashMessage.RawVal(fieldPos).ToString()) / (decimal) 100.00;
                    isoP4Amt = Convert.ToDecimal(requestIcashMessage.RawVal(fieldPos).ToString());
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestIcashMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_14;
                isoP14ExpirDte = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP14ExpirDte = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_61;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    var field61Data = (ICASHChip)requestIcashMessage.Core().GetOthersValByKey(fieldPos.ToString());
                    if (!ObjectUtils.IsNull(field61Data))
                    {
                        var subFieldPos = FIELD.F_3;
                        if (!ObjectUtils.IsNull(field61Data.RawVal(subFieldPos)))
                        {
                            isoP61T03 = field61Data.RawVal(subFieldPos).ToString();
                        }
                        subFieldPos = FIELD.F_4;
                        if (!ObjectUtils.IsNull(field61Data.RawVal(subFieldPos)))
                        {
                            isoP61T04 = field61Data.RawVal(subFieldPos).ToString();
                        }
                        subFieldPos = FIELD.F_9;
                        if (!ObjectUtils.IsNull(field61Data.RawVal(subFieldPos)))
                        {
                            isoP61T09 = field61Data.RawVal(subFieldPos).ToString();
                        }
                    }
                }

                fieldPos = FIELD.F_90;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    var isoS90Data = requestIcashMessage.F90OriginalDataElementsEncode();
                    if (!string.IsNullOrEmpty(isoS90Data) && isoS90Data.Length == 42)
                    {
                        isoS90MessageType = isoS90Data.Substring(0, 4);
                        isoS90TxDte = isoS90Data.Substring(4, 4);
                        isoS90TxTime = isoS90Data.Substring(8, 6);
                        isoS90Trace = isoS90Data.Substring(14, 6);
                        isoS90BinIca = isoS90Data.Substring(20, 8);
                        isoS90RefNbr = isoS90Data.Substring(28, 12);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EsvcAuthInfoStruct Constructor-3 StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        public EsvcAuthInfoStruct(ICASH requestIcashMessage, string txKeySeq) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = txKeySeq;
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2PidNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP12LocalTime = null;
                isoP13LocalDte = null;
                isoP13TransYear = null;
                isoP14ExpirDte = null;
                isoP15StleDte = null;
                isoP32BinIca = null;
                isoP36NTID = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP45SAMId = null;
                isoP58T05 = null;
                isoP58T06 = null;
                isoP58T07 = null;
                isoP58T10 = null;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                isoP63SystemId = null;
                isoS90MessageType = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS90Trace = null;
                isoS90BinIca = null;
                isoS90RefNbr = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestIcashMessage.Header().Encode();
                isoMessageType = requestIcashMessage.Mti();
                var fieldPos = FIELD.F_2;
                isoP2PidNbr = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP2PidNbr = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    //isoP4Amt = Convert.ToDecimal(requestIcashMessage.RawVal(fieldPos).ToString()) / (decimal) 100.00;
                    isoP4Amt = Convert.ToDecimal(requestIcashMessage.RawVal(fieldPos).ToString());
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestIcashMessage.RawVal(fieldPos).ToString();
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
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP11TraceNo = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_14;
                isoP14ExpirDte = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP14ExpirDte = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_32;
                isoP32BinIca = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP32BinIca = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_37;
                isoP37RefNbr = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP37RefNbr = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_38;
                isoP38AuthCode = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP38AuthCode = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_42;
                isoP42Merchant = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    isoP42Merchant = requestIcashMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_61;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    var field61Data = (ICASHChip)requestIcashMessage.Core().GetOthersValByKey(fieldPos.ToString());
                    if (!ObjectUtils.IsNull(field61Data))
                    {
                        var subFieldPos = FIELD.F_3;
                        if (!ObjectUtils.IsNull(field61Data.RawVal(subFieldPos)))
                        {
                            isoP61T03 = field61Data.RawVal(fieldPos).ToString();
                        }
                        subFieldPos = FIELD.F_4;
                        if (!ObjectUtils.IsNull(field61Data.RawVal(subFieldPos)))
                        {
                            isoP61T04 = field61Data.RawVal(fieldPos).ToString();
                        }
                        subFieldPos = FIELD.F_9;
                        if (!ObjectUtils.IsNull(field61Data.RawVal(subFieldPos)))
                        {
                            isoP61T09 = field61Data.RawVal(fieldPos).ToString();
                        }
                    }
                }

                fieldPos = FIELD.F_90;
                if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)))
                {
                    var isoS90Data = requestIcashMessage.F90OriginalDataElementsEncode();
                    if (!string.IsNullOrEmpty(isoS90Data) && isoS90Data.Length == 42)
                    {
                        isoS90MessageType = isoS90Data.Substring(0, 4);
                        isoS90TxDte = isoS90Data.Substring(4, 4);
                        isoS90TxTime = isoS90Data.Substring(8, 6);
                        isoS90Trace = isoS90Data.Substring(14, 6);
                        isoS90BinIca = isoS90Data.Substring(20, 8);
                        isoS90RefNbr = isoS90Data.Substring(28, 12);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EsvcAuthInfoStruct Constructor-4 StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        public EsvcAuthInfoStruct(IPASS requestIpassMessage) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2PidNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP12LocalTime = null;
                isoP13LocalDte = null;
                isoP13TransYear = null;
                isoP14ExpirDte = null;
                isoP15StleDte = null;
                isoP32BinIca = null;
                isoP36NTID = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP45SAMId = null;
                isoP58T05 = null;
                isoP58T06 = null;
                isoP58T07 = null;
                isoP58T10 = null;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                isoP63SystemId = null;
                isoS90MessageType = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS90Trace = null;
                isoS90BinIca = null;
                isoS90RefNbr = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestIpassMessage.Header().Encode();
                isoMessageType = requestIpassMessage.Mti();
                var fieldPos = FIELD.F_2;
                isoP2PidNbr = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP2PidNbr = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP4Amt = Convert.ToDecimal(requestIpassMessage.RawVal(fieldPos).ToString()) / (decimal)100.00;
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestIpassMessage.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_13;
                isoP13TransYear = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP13TransYear = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_36;
                isoP36NTID = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP36NTID = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_45;
                isoP45SAMId = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP45SAMId = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_63;
                isoP63SystemId = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP63SystemId = requestIpassMessage.RawVal(fieldPos).ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EsvcAuthInfoStruct Constructor-5 StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }

        public EsvcAuthInfoStruct(IPASS requestIpassMessage, string txKeySeq) : this()
        {
            try
            {
                //Initial Structure member
                tranLogKey = txKeySeq;
                isoMessageHeader = null;
                isoMessageType = null;
                isoP2PidNbr = null;
                isoP3ProcessCde = null;
                isoP4Amt = (decimal)0.00;
                isoP7TxDte = null;
                isoP7TxTime = null;
                isoP11TraceNo = null;
                isoP12LocalTime = null;
                isoP13LocalDte = null;
                isoP13TransYear = null;
                isoP14ExpirDte = null;
                isoP15StleDte = null;
                isoP32BinIca = null;
                isoP36NTID = null;
                isoP37RefNbr = null;
                isoP38AuthCode = null;
                isoP39Rsp = null;
                isoP41TermNbr = null;
                isoP42Merchant = null;
                isoP45SAMId = null;
                isoP58T05 = null;
                isoP58T06 = null;
                isoP58T07 = null;
                isoP58T10 = null;
                isoP61T03 = null;
                isoP61T04 = null;
                isoP61T09 = null;
                isoP63SystemId = null;
                isoS90MessageType = null;
                isoS90TxDte = null;
                isoS90TxTime = null;
                isoS90Trace = null;
                isoS90BinIca = null;
                isoS90RefNbr = null;
                // Add the input parameter and set its properties.
                isoMessageHeader = requestIpassMessage.Header().Encode();
                isoMessageType = requestIpassMessage.Mti();
                var fieldPos = FIELD.F_2;
                isoP2PidNbr = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP2PidNbr = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_3;
                isoP3ProcessCde = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP3ProcessCde = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_4;
                isoP4Amt = (decimal)0.00;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP4Amt = Convert.ToDecimal(requestIpassMessage.RawVal(fieldPos).ToString()) / (decimal)100.00;
                }

                fieldPos = FIELD.F_7;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    string transmissionDateTime = requestIpassMessage.RawVal(fieldPos).ToString();
                    isoP7TxDte = transmissionDateTime.Substring(0, 4);
                    isoP7TxTime = transmissionDateTime.Substring(4, 6);
                }
                else
                {
                    isoP7TxDte = null;
                    isoP7TxTime = null;
                }

                fieldPos = FIELD.F_13;
                isoP13TransYear = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP13TransYear = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_36;
                isoP36NTID = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP36NTID = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_39;
                isoP39Rsp = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP39Rsp = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_41;
                isoP41TermNbr = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP41TermNbr = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_45;
                isoP45SAMId = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP45SAMId = requestIpassMessage.RawVal(fieldPos).ToString();
                }

                fieldPos = FIELD.F_63;
                isoP63SystemId = null;
                if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)))
                {
                    isoP63SystemId = requestIpassMessage.RawVal(fieldPos).ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"EsvcAuthInfoStruct Constructor-6 StackTrace\n {new StackTrace()}\nException:{ex.Message}");
            }
        }
    }
}

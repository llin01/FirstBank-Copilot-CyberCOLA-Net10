using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.FirstHSMUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class NcccAuthFlowUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        // Reusable XmlSerializer for VD response parsing (per-transaction hot path).
        // XmlSerializer instances from the plain XmlSerializer(Type) ctor are thread-safe for Serialize/Deserialize.
        private static readonly XmlSerializer s_vdResponseSerializer = new XmlSerializer(typeof(VDMessage.ResponseXML));

        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;
        private static readonly SingletonOfMQServerInfo SingletonOfMQServerInfo = SingletonOfMQServerInfo.Instance;

        private static readonly string HmsCheckFlags = SystemParameterInfo.HSMCheckFlags.NcccHSM.IsARQCCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.NcccHSM.IsCVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.NcccHSM.IsCVV2Check.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.NcccHSM.IsCAVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.NcccHSM.IsPVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.NcccHSM.IsPVVDobCheck.Trim();


        /// <summary>
        /// Process Transaction Flow
        /// </summary>
        /// <param name="requestNcccMessage">BIC Message Data</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        /// SessionInfoStruct _sessionInfo
        public static byte[] ProcessTransactionFlow(NcccIso8583 requestNcccMessage,
                                                     string localEndPoint,
                                                     string txKeySeq)
        {
            try
            {
                byte[] respBytes;
                var ncccTransactionType = CreditCardUtility.FindNCCCTransactionType(requestNcccMessage);
                switch (requestNcccMessage.Mti())
                {
                    case "0100":
                        switch (ncccTransactionType)
                        {
                            case NCCCTransactionType.NCCC0200TknActive:
                                respBytes = ProcessNCCC0200TknActive(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            case NCCCTransactionType.NCCC0200TknOtp:
                                respBytes = ProcessNCCC0200TknOtp(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            case NCCCTransactionType.NCCC0200TknEvent:
                                respBytes = ProcessNCCC0200TknEvent(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0200TknAuth:
                                respBytes = ProcessNCCC0200TknAuth(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            case NCCCTransactionType.NCCC0200HCEAuth:
                                respBytes = ProcessNCCC0200HCE(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            default:
                                respBytes = ProcessNCCC0200(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                        }
                        break;
                    case "0120":
                    case "0121":
                        switch (ncccTransactionType)
                        {
                            case NCCCTransactionType.NCCC0220TknAuth:
                                respBytes = ProcessNCCC0220TknAuth(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0220HCEAuth:
                                respBytes = ProcessNCCC0220TknAuth(requestNcccMessage, localEndPoint);
                                break;
                            default:
                                respBytes = ProcessNCCC0220(requestNcccMessage, localEndPoint);
                                break;
                        }
                        break;
                    case "0200":
                        switch (ncccTransactionType)
                        {
                            case NCCCTransactionType.NCCC0200TknActive:
                                respBytes = ProcessNCCC0200TknActive(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            case NCCCTransactionType.NCCC0200TknOtp:
                                respBytes = ProcessNCCC0200TknOtp(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            case NCCCTransactionType.NCCC0200TknEvent:
                                respBytes = ProcessNCCC0200TknEvent(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0200TknAuth:
                                respBytes = ProcessNCCC0200TknAuth(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            case NCCCTransactionType.NCCC0200HCEAuth:
                                respBytes = ProcessNCCC0200HCE(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            default:
                                respBytes = ProcessNCCC0200(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                        }
                        break;
                    case "0220":
                    case "0221":
                        switch (ncccTransactionType)
                        {
                            case NCCCTransactionType.NCCC0220TknAuth:
                                respBytes = ProcessNCCC0220TknAuth(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0220HCEAuth:
                                respBytes = ProcessNCCC0220HCE(requestNcccMessage, localEndPoint);
                                break;
                            default:
                                respBytes = ProcessNCCC0220(requestNcccMessage, localEndPoint);
                                break;
                        }
                        break;
                    case "0310":
                        ProcessNCCC0310(requestNcccMessage, localEndPoint, txKeySeq);
                        return null;
                    case "0420":
                    case "0421":
                        switch (ncccTransactionType)
                        {
                            case NCCCTransactionType.NCCC0420TknActive:
                                respBytes = ProcessNCCC0420TknActive(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0420TknAuth:
                                respBytes = ProcessNCCC0420TknAuth(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0420HCEAuth:
                                respBytes = ProcessNCCC0420HCE(requestNcccMessage, localEndPoint);
                                break;
                            default:
                                respBytes = ProcessNCCC0420(requestNcccMessage, localEndPoint);
                                break;
                        }
                        break;
                    case "0500":
                    case "0502":
                    case "0520":
                    case "0521":
                    case "0522":
                    case "0523":
                        respBytes = ProcessNCCC0500(requestNcccMessage, localEndPoint);
                        break;
                    case "0620":
                        switch (ncccTransactionType)
                        {
                            case NCCCTransactionType.NCCC0620TknEvent:
                                respBytes = ProcessNCCC0620TknEvent(requestNcccMessage, localEndPoint);
                                break;
                            case NCCCTransactionType.NCCC0200TknOtp:
                                respBytes = ProcessNCCC0200TknOtp(requestNcccMessage, localEndPoint, HmsCheckFlags);
                                break;
                            default:
                                // Only Visa, or MasterCard/JCB with PCode 93 and Q8 tag 27, is a token event/OTP; anything
                                // else is a business case (answered with a system error by the caller), not a failure.
                                Log.Warn($"NcccAuthFlowUtility.ProcessTransactionFlow 0620 not classified: CardType[{CreditCardUtility.FindCardType(ColaCommUtility.GetCardNumber(requestNcccMessage))}], PCode[{requestNcccMessage.Core().Field(FIELD.F_3)}], no response data");
                                respBytes = null;
                                break;
                        }
                        break;
                    case "0800":
                    case "0820":
                    case "0821":
                        respBytes = ProcessNCCC0800(requestNcccMessage, localEndPoint);
                        break;
                    case "0810":
                        ProcessNCCC0810(requestNcccMessage, localEndPoint, txKeySeq);
                        return null;
                    default:
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                                  new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                                  "Message Type No Match : " + requestNcccMessage.Mti(),
                                  "Message Type No Match");
                        return null;
                }

                if (respBytes == null)
                {
                    // An unclassified 0620 was already logged as a warning above.
                    if (!(requestNcccMessage.Mti() == "0620" && ncccTransactionType == NCCCTransactionType.Unknown))
                    {
                        Log.Fatal($"NcccAuthFlowUtility.ProcessTransactionFlow Message Type {requestNcccMessage.Mti()}, Return Data Is Null");
                    }
                }
                else
                {
                    Log.Info($"NcccAuthFlowUtility.ProcessTransactionFlow Message Type {requestNcccMessage.Mti()}");
                }
                if (respBytes != null)
                {
                    return (ColaCommUtility.ContactBytesArray(ColaCommUtility.IntegerToBinary(respBytes.Length), respBytes));
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccAuthFlowUtility.ProcessTransactionFlow Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ProcessNCCC0200 Process 0200 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0200(NcccIso8583 requestNcccMessage,
                                              string localEndPoint,
                                              string hsmCheckFlags)
        {
            const int returnArrayCount = 23;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0200 Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            var hsmStart = AuthTiming.Start();
            FirstHSMUtility.HSMCheckFlow(requestNcccMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            AuthTiming.StopHsm(hsmStart);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Set Initial data9F10Array Value
            // <summary>
            //  data9F10Array[0] : @tB2DeriveKeyIndex
            //  data9F10Array[1] : @tB2CryptoVerNum
            //  data9F10Array[2] : @tB2CVR
            // </summary>s
            var data9F10Array = new string[3];


            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0200AuthProcess(authInfoData, data9F10Array);

            // Return String Arrays Information
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
            // @t04ErrorFlg	                varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
            // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
            // @tC0CavvResult               varchar(01) 	Output   Field 57 or 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
            // @tC5InstTxInd        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
            // @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
            // @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
            // @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
            // @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
            // @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
            // @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
            // @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
            // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
            // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
            // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
            // @icashCardNbr                varchar(20)    	OUTPUT   ICASH 刪除STIP 檔案
            // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
            // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
            // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
            // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額     


            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0200 Return String Is Null");
                responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0200 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200(requestNcccMessage, authInfoData, authReturnString, data9F10Array).GetNcccMessage();
                    if (!string.IsNullOrEmpty(authReturnString[03]) && authReturnString[03].Trim().Equals("D"))
                    {
                        var serverName = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.PrimaryServerName.Trim();
                        var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                        var portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.ListenerPortNumber.Trim());
                        ColaCommUtility.SendNegFileMaintToNccc(authInfoData.isoP2CardNbr, localEndPoint, $"{serverIPAddr}:{portNumber}").ConfigureAwait(false);
                        Log.Info($"SendFileMaintToNccc process completely at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

                        //SEND TO ICASH
                        if (!string.IsNullOrEmpty(authReturnString[18]) && authReturnString[18].Trim().Length == 16)
                        {
                            serverName = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.PrimaryServerName.Trim();
                            serverIPAddr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                            portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.ListenerPortNumber.Trim());
                            ColaCommUtility.SendDelStipFileMaintToIcash(authReturnString[18].Trim(), localEndPoint, $"{serverIPAddr}:{portNumber}").ConfigureAwait(false);
                            Log.Info($"SendDelStipFileMaintToIcash process completely at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                        }
                    }
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0200",
                          "ProcessNCCC0200 Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                authReturnString = null;  //for check condition
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());     //for process error
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[19])) &&
                (!string.IsNullOrEmpty(authReturnString[20])) &&
                (!string.IsNullOrEmpty(authReturnString[21])) &&
                (!string.IsNullOrEmpty(authReturnString[22])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessNCCC0200VDLoading(authInfoData, authReturnString, localEndPoint, requestNcccMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                           SystemConstantData.CommonData.MessageSendDirection,
                                                           localEndPoint,
                                                           responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessNCCC0200TknActive Process 0200 Tokenization Token Active Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0200TknActive(NcccIso8583 requestNcccMessage,
                                                       string localEndPoint,
                                                       string hsmCheckFlags)
        {
            const int returnArrayCount = 13;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0200TknActive Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            //hsmCheckFlags: SUIP Check Flag For Input String 0:ARQC 1:CVV 2:CVV2 3:CAVV 4:PVV 5:PVVDob-p0->001000
            Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            var hsmStart = AuthTiming.Start();
            FirstHSMUtility.HSMCheckFlow(requestNcccMessage, "NNYNNN", SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            AuthTiming.StopHsm(hsmStart);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0200TknActiveProcess(authInfoData);

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
            // @t04ErrorFlg	                varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
            // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
            // @tQ8Tag16EMail               varchar(64) 	Output   Field 57 or 63 Token Q8 SubToken Id 16 Acct E-Mail
            // @tQ8Tag29IssConfigId         varchar(30) 	Output	 Field 57 or 63 Token Q8 SubToken Id 29 Issuer Production Configuration Id
            // @tQ8Tag36PanSeq       	    varchar(10) 	Output	 Field 57 or 63 Token Q8 SubToken Id 36 Card Sequence Number
            // @tQ8Tag51ChVfyMethod      	varchar(186) 	Output	 Field 57 or 63 Token Q8 SubToken Id 51 CardHolder Verification Method
            // @tQ8Tag56CvmErrCde           varchar(08) 	Output	 Field 57 or 63 Token Q8 SubToken Id 56 Check Eligibility error code
            // @tQ8Tag58CvmRsp              varchar(02) 	Output	 Field 57 or 63 Token Q8 SubToken Id 58 Contactless use
            // @tQ8TagQEPhone      	        varchar(15) 	Output	 Field 57 or 63 Token Q8 SubToken Id QE Mobile Phone number
            // @tQ8TagQGRiskLvlResult       varchar(01) 	Output	 Field 57 or 63 Token Q8 SubToken Id QG Risk Level Result
            // @tknTxType  	                varchar(02) 	Output

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0200TknActive Return String Is Null");
                responseNcccMessage = new NcccMessage0200TknActive(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0200TknActive Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200TknActive(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200TknActive(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0200TknActive",
                          "ProcessNCCC0200TknActive Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200TknActive(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0200TknOtp Process 0200 Tokenization OTP Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0200TknOtp(NcccIso8583 requestNcccMessage,
                                                    string localEndPoint,
                                                    string hsmCheckFlags)
        {
            const int returnArrayCount = 8;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0200TknOtp Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            //FirstHSMUtility.HSMCheckFlow(requestNcccMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                               SystemConstantData.CommonData.MessageReceiveDirection,
                                                               localEndPoint,
                                                               requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0200TknOtpProcess(authInfoData);

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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0200TknOtp Return String Is Null");
                responseNcccMessage = new NcccMessage0200TknOtp(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0200TknOtp Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200TknOtp(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200TknOtp(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0200TknOtp",
                          "ProcessNCCC0200TknOtp Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200TknOtp(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Check Send Message SMS
            if (authReturnString != null)
            {
                try
                {
                    if (authReturnString.Length == returnArrayCount && authReturnString[3].ToUpper().Equals("Y"))
                    {
                        //var senBytes = SMSCommUtility.Create0200TknOtpSMSOutgoingMessage(requestNcccMessage, authInfoData, authReturnString);
                        //var hostName = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ServerName.Trim();
                        //var portNumber = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ListenerPort;
                        //var connectionTimeout  = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ConnectionTimeoutMilliseconds;
                        //var sendReceiveTimeout = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].SendReceiveTimeoutMilliseconds;
                        ////Add this for test
                        //Task.Run(() => ColaCommUtility.SendSMSMessageToSMSServer(senBytes, SingletonOfSMSServerInfo.GetSMSServerInfo().SMSServers.SMSServer[0]));
                        var stringContent = SMSCommUtility.Create0200TknOtpSMSOutgoingMessage(authReturnString);
                        if (stringContent == null)
                        {
                            Log.Error("Create StringContent error, no send message to SMSWebAPI");
                        }
                        else
                        {
                            Task.Run(() => ColaCommUtility.SendSMSMessageToWebAPINoCert(stringContent));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"SendSMSMessageToWebAPI Exception:{ex.Message}");
                }
            }
            return (responseNcccMessage.Encode().Val());
        }


        /// <summary>
        /// ProcessNCCC0200TknEvent Process 0200 Tokenization Event Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0200TknEvent(NcccIso8583 requestNcccMessage, string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0200TknEvent Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0200TknEventProcess(authInfoData);

            // Nccc0200TknEventProcess Return String Arrays Information
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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0200TknEvent Return String Is Null");
                responseNcccMessage = new NcccMessage0200TknEvent(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0200TknEvent Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200TknEvent(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200TknEvent(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0200TknEvent",
                          "ProcessNCCC0200TknEvent Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200TknEvent(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Check Send Message SMS
            if (authReturnString != null)
            {
                try
                {
                    if (authReturnString.Length == returnArrayCount && authReturnString[2].ToUpper().Equals("Y"))
                    {
                        //var senBytes = SMSCommUtility.Create0620TknEventSMSOutgoingMessage(requestNcccMessage, authInfoData, authReturnString, "0200EVENT");
                        //var hostName = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ServerName.Trim();
                        //var portNumber = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ListenerPort;
                        //var connectionTimeout = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ConnectionTimeoutMilliseconds;
                        //var sendReceiveTimeout = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].SendReceiveTimeoutMilliseconds;
                        ////Add this for test
                        //Task.Run(() => ColaCommUtility.SendSMSMessageToSMSServer(senBytes, SingletonOfSMSServerInfo.GetSMSServerInfo().SMSServers.SMSServer[0]));
                        var stringContent = SMSCommUtility.Create0200TknEventSMSOutgoingMessage(authReturnString);
                        if (stringContent == null)
                        {
                            Log.Error("Create StringContent error, no send message to SMSWebAPI");
                        }
                        else
                        {
                            Task.Run(() => ColaCommUtility.SendSMSMessageToWebAPINoCert(stringContent));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"SendSMSMessageToWebAPI Exception:{ex.Message}");
                }
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0200TknAuth Process 0200 Tokenization Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0200TknAuth(NcccIso8583 requestNcccMessage,
                                                     string localEndPoint,
                                                     string hsmCheckFlags)
        {
            const int returnArrayCount = 18;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0200TknAuth Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            //Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            //FirstHSMUtility.HSMCheckFlow(requestNcccMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0200TknAuthProcess(authInfoData);

            // Nccc0200TknAuthProcess Return String Arrays Information
            // Return String Arrays Information
            // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
            // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@t04ErrorFlg
            // authReturnString[04]:@t04CrdVeryFlg       , authReturnString[05]:@tC5InstTxResp
            // authReturnString[06]:@tC5DownPayment      , authReturnString[07]:@tC5InstPayment
            // authReturnString[08]:@tC5InstFee          , authReturnString[09]:@tC5PointRedemption
            // authReturnString[10]:@tC5SignPoint        , authReturnString[11]:@tC5PointBalance
            // authReturnString[12]:@tC5PaidCreditAmt    , authReturnString[13]:@tC5LoyaltyTxInd
            // authReturnString[14]:@vdLinkNbr           , authReturnString[15]:@vdLinkNbrSeq
            // authReturnString[16]:@vdActionCode
            // authReturnString[17]:@vdBlockAmt
            // Description
            // @isoP38AuthCode          	varchar(06) 	Output	 --p38
            // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
            // @isoP39RspAction             varchar(01)     Output	 --對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可)
            // @t04ErrorFlg	                varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
            // @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
            // @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
            // @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
            // @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
            // @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
            // @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
            // @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
            // @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
            // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
            // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
            // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
            // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
            // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
            // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0200TknAuth Return String Is Null");
                responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0200TknAuth Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0200TknAuth",
                          "ProcessNCCC0200TknAuth Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {

                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
            //need to modify 
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[14])) &&
                (!string.IsNullOrEmpty(authReturnString[15])) &&
                (!string.IsNullOrEmpty(authReturnString[16])) &&
                (!string.IsNullOrEmpty(authReturnString[17])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessNCCC0200TknVDLoading(authInfoData, authReturnString, localEndPoint, requestNcccMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                           SystemConstantData.CommonData.MessageSendDirection,
                                                           localEndPoint,
                                                           responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
        }
        /// <summary>
        /// ProcessNCCC0200HCE Process 0200 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0200HCE(NcccIso8583 requestNcccMessage,
                                                  string localEndPoint,
                                                  string hsmCheckFlags)
        {
            const int returnArrayCount = 11;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0200HCE Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            //Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            //FirstHSMUtility.HSMCheckFlow(requestNcccMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Set Initial data9F10Array Value
            // <summary>
            //  data9F10Array[0] : @tB2DeriveKeyIndex
            //  data9F10Array[1] : @tB2CryptoVerNum
            //  data9F10Array[2] : @tB2CVR
            // </summary>s
            var data9F10Array = new string[3];

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0200HceAuthProcess(authInfoData, data9F10Array);

            // Return String Arrays Information
            // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
            // authReturnString[02]:@tC5InstTxResp       , authReturnString[03]:@tC5DownPayment
            // authReturnString[04]:@tC5InstPayment      , authReturnString[05]:@tC5InstFee
            // authReturnString[06]:@tC5PointRedemption  , authReturnString[07]:@tC5SignPoint
            // authReturnString[08]:@tC5PointBalance     , authReturnString[09]:@tC5PaidCreditAmt
            // authReturnString[10]:@tC5LoyaltyTxInd     
            // Description
            // @isoP38AuthCode          	varchar(06) 	Output	 --p38
            // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
            // @tC5InstTxInd        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
            // @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
            // @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
            // @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
            // @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
            // @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
            // @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
            // @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
            // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
            // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0200HCE Return String Is Null");
                responseNcccMessage = new NcccMessage0200Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0200HCE Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200Hce(requestNcccMessage, authInfoData, authReturnString, data9F10Array).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0200HCE",
                          "ProcessNCCC0200HCE Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0220 Process 0220 Authorization Flow For NCCC
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0220(NcccIso8583 requestNcccMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0220 Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0220AuthProcess(authInfoData);

            // ProcessNCCC022 Return String Arrays Information
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
            // @vdBlockAmt                  varchar(15)  	Output   VD 圈存解圈金額

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0220 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0220(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0220",
                          "ProcessNCCC0220 Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                authReturnString = null;  //for check condition
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());     //for process error
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])) &&
                (!string.IsNullOrEmpty(authReturnString[06])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessNCCC0220VDLoading(authInfoData, authReturnString, localEndPoint, requestNcccMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessNCCC0220TknAuth Process 0220 Tokenization Authorization Flow For NCCC
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0220TknAuth(NcccIso8583 requestNcccMessage,
                                                     string localEndPoint)
        {
            const int returnArrayCount = 16;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0220TknAuth Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0220TknAuthProcess(authInfoData);

            // ProcessNCCC0220TknAuth Return String Arrays Information
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
            // @isoP39RspAction		        varchar(01)     Output 	 --對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可)
            // @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
            // @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
            // @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
            // @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
            // @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
            // @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
            // @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
            // @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
            // @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
            // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
            // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
            // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
            // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0220TknAuth Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0220TknAuth",
                          "ProcessNCCC0220TknAuth Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }

            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[12])) &&
                (!string.IsNullOrEmpty(authReturnString[13])) &&
                (!string.IsNullOrEmpty(authReturnString[14])) &&
                (!string.IsNullOrEmpty(authReturnString[15])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessNCCC0220TknVDLoading(authInfoData, authReturnString, localEndPoint, requestNcccMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessNCCC0220HCE Process 0220 Authorization Flow For NCCC
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0220HCE(NcccIso8583 requestNcccMessage,
                                                  string localEndPoint)
        {
            const int returnArrayCount = 11;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0220HCE Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0220HceAuthProcess(authInfoData);

            // ProcessNCCC0220HCE Return String Arrays Information
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


            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0220Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0220HCE Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0220Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0220Hce(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0220HCE",
                          "ProcessNCCC0220HCE Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0220Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0310 Process File Maintenance Management(0310 Process)
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessNCCC0310(NcccIso8583 requestNcccMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Fatal("ProcessNCCC0310 txKeySeq Invalid No Write Log");
                return;
            }
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0310 Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessNCCC0420 Process 0420 Authorization Flow
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0420(NcccIso8583 requestNcccMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 6;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0420 Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0420AuthProcess(authInfoData);

            // Return String Arrays Information
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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0420(requestNcccMessage, authReturnString[0]).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0420",
                          "ProcessNCCC0420 Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                authReturnString = null;  //for check condition
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());     //for process error
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[01].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[02])) &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessNCCC0420VDLoading(authInfoData, authReturnString, localEndPoint, requestNcccMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessNCCC0420TknActive Process 0420 Tokenization Active Authorization Flow
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0420TknActive(NcccIso8583 requestNcccMessage,
                                                       string localEndPoint)
        {
            const int returnArrayCount = 1;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0420TknActive Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0420TknActiveProcess(authInfoData);

            // ProcessNCCC0420TknActive Return String Arrays Information
            // authReturnString[00]:@isoP39Rsp
            // Description
            // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0420TknActive(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0420TknActive Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0420TknActive(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0420TknActive(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0420TknActive",
                          "ProcessNCCC0420TknActive Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0420TknActive(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0420TknAuth Process 0420 Tokenization Authorization Flow
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0420TknAuth(NcccIso8583 requestNcccMessage,
                                                     string localEndPoint)
        {
            const int returnArrayCount = 6;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0420TknAuth Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0420TknAuthProcess(authInfoData);

            // ProcessNCCC0420TknAuth Return String Arrays Information
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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0420TknAuth Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0420TknAuth",
                          "ProcessNCCC0420TknAuth Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[01].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[02])) &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessNCCC0420VDTknLoading(authInfoData, authReturnString, localEndPoint, requestNcccMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseNcccMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessNCCC0420HCE Process 0420 Authorization Flow
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0420HCE(NcccIso8583 requestNcccMessage,
                                                  string localEndPoint)
        {
            const int returnArrayCount = 1;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0420HCE Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0420HceAuthProcess(authInfoData);

            // Return String Arrays Information
            // authReturnString[00]:@isoP39Rsp
            // Description
            // @isoP39Rsp                   varchar(02)     Output   --p39 initial 時請放null即可

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseNcccMessage = new NcccMessage0420Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0420HCE Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0420Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0420Hce(requestNcccMessage, authReturnString[0]).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessNCCC0420HCE",
                          "ProcessNCCC0420HCE Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0420Hce(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0500 Process Reconciliation Process(05XX Process)
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0500(NcccIso8583 requestNcccMessage,
                                              string localEndPoint)
        {
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0500 Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}]");
            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            NcccIso8583 responseNcccMessage = new NcccMessage0500(requestNcccMessage).GetNcccMessage();
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal("Insert Cola_Tran_Log Error,Return Code = [{0}]", spRetnCode);
            }
            return (responseNcccMessage.Encode().Val());
        }


        /// <summary>
        /// rocessNCCC0620TknEvent Process 0620 Tokenizationcess Event Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestNcccMessage">Request MCOLA Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0620TknEvent(NcccIso8583 requestNcccMessage,
                                                      string localEndPoint)
        {
            const int returnArrayCount = 6;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldPos)) ? requestNcccMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0620TknEvent Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            NcccIso8583 responseNcccMessage;
            var authInfoData = new NcccAuthInfoStruct(requestNcccMessage);

            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Nccc0620TknEventProcess(authInfoData);

            // ProcessNCCC0620TknEvent Return String Arrays Information
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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessNCCC0620TknEvent Return String Is Null");
                responseNcccMessage = new NcccMessage0620(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessNCCC0620TknEvent Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0620(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0620(requestNcccMessage, authInfoData, authReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          (new System.Diagnostics.StackTrace(true)).GetFrame(0).GetMethod().Name,
                          "ProcessNCCC0620TknEvent",
                          "ProcessNCCC0620TknEvent Response NCCC Message Return Is Null");

                responseNcccMessage = new NcccMessage0620(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            //Add Send Transaction Log
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal("Insert Cola_Tran_Log Error,Return Code = [{0}]", spRetnCode);
            }
            //Check Send Message SMS
            if (authReturnString != null)
            {
                try
                {
                    if (authReturnString.Length == returnArrayCount && authReturnString[1].ToUpper().Equals("Y"))
                    {
                        //var senBytes = SMSCommUtility.Create0620TknEventSMSOutgoingMessage(requestNcccMessage, authInfoData, authReturnString, "0620EVENT");
                        //var hostName = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ServerName.Trim();
                        //var portNumber = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ListenerPort;
                        //var connectionTimeout  = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].ConnectionTimeoutMilliseconds;
                        //var sendReceiveTimeout = SingletonOfSMSServerInfo.Instance.GetSMSServerInfo().SMSServers.SMSServer[0].SendReceiveTimeoutMilliseconds;
                        ////Add this for test
                        //Task.Run(() => ColaCommUtility.SendSMSMessageToSMSServer(senBytes, SingletonOfSMSServerInfo.GetSMSServerInfo().SMSServers.SMSServer[0]));
                        var stringContent = SMSCommUtility.Create0620TknEventSMSOutgoingMessage(authReturnString);
                        if (stringContent == null)
                        {
                            Log.Error("Create StringContent error, no send message to SMSWebAPI");
                        }
                        else
                        {
                            Task.Run(() => ColaCommUtility.SendSMSMessageToWebAPINoCert(stringContent));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"SendSMSMessageToWebAPI Exception[{ex.Message}]");
                }
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessNCCC0800(NcccIso8583 requestNcccMessage,
                                              string localEndPoint)
        {
            string informationCode;
            FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
            {
                informationCode = (string)requestNcccMessage.Core().Field(field);
                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOff))
                    SingletonOfCommonProperty.Instance.SetColaSignOff(SystemConstantData.CommonData.PasystfNCCCInfo);
                else if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn))
                    SingletonOfCommonProperty.Instance.SetColaSignOn(SystemConstantData.CommonData.PasystfNCCCInfo);
            }
            else
                informationCode = string.Empty;

            // 01 --Outbound key(Visa:IWK)  NCCC -> Bank
            // 02 --Inbound key (Visa:AWK)
            // 03 --both incoming and outgoing key
            int processOption = 2;
            if (informationCode.Equals("162"))
            {
                field = FIELD.F_53;
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    var keyDirection = ((string)requestNcccMessage.Core().Field(field)).Substring(2, 2);
                    processOption = keyDirection.Equals("01") ? 1 : 2;
                }
                else
                {
                    processOption = 2;
                }
            }
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0800 Start,Header[{requestNcccMessage.Header().Encode()}],Information Code[{informationCode}],Process Option[{processOption}](1:Change Key)");
            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Add Send Transaction Log
            NcccIso8583 responseNcccMessage;
            switch (processOption)
            {
                case 1:
                    responseNcccMessage = new NcccMessage0800NewKey(requestNcccMessage).GetNcccMessage();
                    break;
                default:
                    responseNcccMessage = new NcccMessage0800(requestNcccMessage).GetNcccMessage();
                    break;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessNCCC0810 Process Network Management(0810 Process)
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessNCCC0810(NcccIso8583 requestNcccMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0810 txKeySeq inValid]");
                return;
            }
            string informationCode;
            const FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
            {
                informationCode = (string)requestNcccMessage.Core().Field(field);
            }
            else
                informationCode = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0810 Start,Header[{requestNcccMessage.Header().Encode()}],Information Code[{informationCode}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessNCCCErrorProcess Error Flow
        /// </summary>
        /// <param name="localEndPoint">Local End Point</param>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessNCCCError(string localEndPoint,
                                                NcccIso8583 requestNcccMessage)
        {
            string pCodeString;
            const FIELD field = FIELD.F_3;
            if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
            {
                pCodeString = (string)requestNcccMessage.Core().Field(field);
            }
            else
                pCodeString = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCCError Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            Log.Warn($"ProcessNCCCError Response Code = {SystemConstantData.CommonData.SystemProcessErrorCode}");
            var responseNcccMessage = new MessageBase(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            if (responseNcccMessage == null)
            {
                Log.Fatal("Create Response BIC Message Is Null");
                return null;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the NCCC0200 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessNCCC0200VDLoading(NcccAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, NcccIso8583 requestNcccMessage)
        {
            NcccIso8583 responseNcccMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                return (responseNcccMessage.Encode().Val());
            }
            VDMessage.ResponseXML responseXML;
            //send Message To MQ
            var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdRequestXML, true);
            var vdResponseString = MQ.MQUtils.MQMessageSendReceive(SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0], vdRequestString);
            if (vdResponseString == null)
            {
                Log.Warn($"MQMessageSendReceive Process Error");
                responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDTimeOutReturnCode);
            }
            else
            {
                try
                {
                    XmlSerializer serializer = s_vdResponseSerializer;
                    StringReader stringReader = new StringReader(vdResponseString);
                    responseXML = (VDMessage.ResponseXML)serializer.Deserialize(stringReader);
                    if (responseXML.TxRs == null)
                    {
                        Log.Info($"VDMessage.ResponseXML Data : {vdResponseString}");
                        Log.Info($"VDMessage.ResponseXML Process Error Status code : {responseXML.ResponseHeader.StatusCode}");
                    }
                    var MatchMsgSeqNo = authInfoData.tranLogKey.Substring(authInfoData.tranLogKey.Length - 6, 6).ToUpper();
                    if (responseXML.ResponseHeader.MsgSeqNo != MatchMsgSeqNo)
                        Log.Fatal($"VDMessage.ResponseXML Data Match Error origin [{0}] response [{1}] : {MatchMsgSeqNo}{responseXML.ResponseHeader.MsgSeqNo}");
                    else
                        Log.Info($"VDMessage.ResponseXML Data is Match");
                }
                catch (Exception e)
                {
                    Log.Fatal($"Convert XML To Class(VDMessage.ResponseXML) Exception : {e.Message}");
                    responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDParsingErrorReturnCode);
                }
            }
            //run second store procedure
            const int returnArrayCount = 23;
            //Set Initial data9F10Array Value
            // <summary>
            //  data9F10Array[0] : @tB2DeriveKeyIndex
            //  data9F10Array[1] : @tB2CryptoVerNum
            //  data9F10Array[2] : @tB2CVR
            // </summary>s
            var data9F10Array = new string[3];

            //Call Authorization Store Procedure
            string[] secondAuthReturnString = StoreProcUtility.Nccc0200AuthProcess(authInfoData, data9F10Array, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0200 Return String Is Null");
                responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0200 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200(requestNcccMessage, authInfoData, secondAuthReturnString, data9F10Array).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0200",
                          "Second ProcessNCCC0200 Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the NCCC0200 VD Token loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessNCCC0200TknVDLoading(NcccAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, NcccIso8583 requestNcccMessage)
        {
            NcccIso8583 responseNcccMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage For VD Token process error");
                responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                return (responseNcccMessage.Encode().Val());
            }
            VDMessage.ResponseXML responseXML;
            //send Message To MQ
            var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdRequestXML, true);
            var vdResponseString = MQ.MQUtils.MQMessageSendReceive(SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0], vdRequestString);
            if (vdResponseString == null)
            {
                Log.Warn($"MQMessageSendReceive Process For VD Token Error");
                responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDTimeOutReturnCode);
            }
            else
            {
                try
                {
                    XmlSerializer serializer = s_vdResponseSerializer;
                    StringReader stringReader = new StringReader(vdResponseString);
                    responseXML = (VDMessage.ResponseXML)serializer.Deserialize(stringReader);
                    if (responseXML.TxRs == null)
                    {
                        Log.Info($"VDMessage.ResponseXML For VD Token Data : {vdResponseString}");
                        Log.Info($"VDMessage.ResponseXML For VD Token Process Error Status code : {responseXML.ResponseHeader.StatusCode}");
                    }
                    var MatchMsgSeqNo = authInfoData.tranLogKey.Substring(authInfoData.tranLogKey.Length - 6, 6).ToUpper();
                    if (responseXML.ResponseHeader.MsgSeqNo != MatchMsgSeqNo)
                        Log.Fatal($"VDMessage.ResponseXML For VD Token Data Match Error origin [{0}] response [{1}] : {MatchMsgSeqNo}{responseXML.ResponseHeader.MsgSeqNo}");
                    else
                        Log.Info($"VDMessage.ResponseXML For VD Token Data is Match");
                }
                catch (Exception e)
                {
                    Log.Fatal($"Convert XML To Class(VDMessage.ResponseXML) For VD Token Exception : {e.Message}");
                    responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDParsingErrorReturnCode);
                }
            }
            //run second store procedure
            const int returnArrayCount = 18;

            //Call Authorization Store Procedure
            string[] secondAuthReturnString = StoreProcUtility.Nccc0200TknAuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0200 For VD Token Return String Is Null");
                responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0200 For VD Token Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, authInfoData, secondAuthReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0200 For VD",
                          "Second ProcessNCCC0200 For VD Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0200TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the NCCC0220 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessNCCC0220VDLoading(NcccAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, NcccIso8583 requestNcccMessage)
        {
            NcccIso8583 responseNcccMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                return (responseNcccMessage.Encode().Val());
            }
            VDMessage.ResponseXML responseXML;
            //send Message To MQ
            var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdRequestXML, true);
            var vdResponseString = MQ.MQUtils.MQMessageSendReceive(SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0], vdRequestString);
            if (vdResponseString == null)
            {
                Log.Warn($"MQMessageSendReceive Process Error");
                responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDTimeOutReturnCode);
            }
            else
            {
                try
                {
                    XmlSerializer serializer = s_vdResponseSerializer;
                    StringReader stringReader = new StringReader(vdResponseString);
                    responseXML = (VDMessage.ResponseXML)serializer.Deserialize(stringReader);
                    if (responseXML.TxRs == null)
                    {
                        Log.Info($"VDMessage.ResponseXML Data : {vdResponseString}");
                        Log.Info($"VDMessage.ResponseXML Process Error Status code : {responseXML.ResponseHeader.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal($"Convert XML To Class(VDMessage.ResponseXML) Exception : {e.Message}");
                    responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDParsingErrorReturnCode);
                }
            }
            //run second store procedure
            const int returnArrayCount = 7;
            //Call Authorization Store Procedure
            var secondAuthReturnString = StoreProcUtility.Nccc0220AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0220 Return String Is Null");
                responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0220 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0220(requestNcccMessage, authInfoData, secondAuthReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0220",
                          "Second ProcessNCCC0220 Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0220(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the NCCC0220 VD Token loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessNCCC0220TknVDLoading(NcccAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, NcccIso8583 requestNcccMessage)
        {
            NcccIso8583 responseNcccMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage For VD Token process error");
                responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                return (responseNcccMessage.Encode().Val());
            }
            VDMessage.ResponseXML responseXML;
            //send Message To MQ
            var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdRequestXML, true);
            var vdResponseString = MQ.MQUtils.MQMessageSendReceive(SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0], vdRequestString);
            if (vdResponseString == null)
            {
                Log.Warn($"MQMessageSendReceive For VD Token Process Error");
                responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDTimeOutReturnCode);
            }
            else
            {
                try
                {
                    XmlSerializer serializer = s_vdResponseSerializer;
                    StringReader stringReader = new StringReader(vdResponseString);
                    responseXML = (VDMessage.ResponseXML)serializer.Deserialize(stringReader);
                    if (responseXML.TxRs == null)
                    {
                        Log.Info($"VDMessage.ResponseXML For VD Token Data : {vdResponseString}");
                        Log.Info($"VDMessage.ResponseXML For VD Token Process Error Status code : {responseXML.ResponseHeader.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal($"Convert XML To Class(VDMessage.ResponseXML) For VD Token Exception : {e.Message}");
                    responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDParsingErrorReturnCode);
                }
            }
            //run second store procedure
            const int returnArrayCount = 16;
            //Call Authorization Store Procedure
            var secondAuthReturnString = StoreProcUtility.Nccc0220TknAuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0220 For VD Token Return String Is Null");
                responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0220 For VD Token Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, authInfoData, secondAuthReturnString).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0220 For VD Token ",
                          "Second ProcessNCCC0220 For VD Token Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0220TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error For VD Token,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the NCCC0420 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessNCCC0420VDLoading(NcccAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, NcccIso8583 requestNcccMessage)
        {
            NcccIso8583 responseNcccMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                return (responseNcccMessage.Encode().Val());
            }
            VDMessage.ResponseXML responseXML;
            //send Message To MQ
            var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdRequestXML, true);
            var vdResponseString = MQ.MQUtils.MQMessageSendReceive(SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0], vdRequestString);
            if (vdResponseString == null)
            {
                Log.Warn($"MQMessageSendReceive Process Error");
                responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDTimeOutReturnCode);
            }
            else
            {
                try
                {
                    XmlSerializer serializer = s_vdResponseSerializer;
                    StringReader stringReader = new StringReader(vdResponseString);
                    responseXML = (VDMessage.ResponseXML)serializer.Deserialize(stringReader);
                    if (responseXML.TxRs == null)
                    {
                        Log.Info($"VDMessage.ResponseXML Data : {vdResponseString}");
                        Log.Info($"VDMessage.ResponseXML Process Error Status code : {responseXML.ResponseHeader.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal($"Convert XML To Class(VDMessage.ResponseXML) Exception : {e.Message}");
                    responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDParsingErrorReturnCode);
                }
            }
            //run second store procedure
            const int returnArrayCount = 6;
            //Call Authorization Store Procedure
            var secondAuthReturnString = StoreProcUtility.Nccc0420AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0420 Return String Is Null");
                responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0420(requestNcccMessage, secondAuthReturnString[0]).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0420",
                          "Second ProcessNCCC0420 Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0420(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the NCCC0420 VD Token loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessNCCC0420VDTknLoading(NcccAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, NcccIso8583 requestNcccMessage)
        {
            NcccIso8583 responseNcccMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage For VD Token process error");
                responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                return (responseNcccMessage.Encode().Val());
            }
            VDMessage.ResponseXML responseXML;
            //send Message To MQ
            var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdRequestXML, true);
            var vdResponseString = MQ.MQUtils.MQMessageSendReceive(SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0], vdRequestString);
            if (vdResponseString == null)
            {
                Log.Warn($"MQMessageSendReceive Process For VD Token Error");
                responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDTimeOutReturnCode);
            }
            else
            {
                try
                {
                    XmlSerializer serializer = s_vdResponseSerializer;
                    StringReader stringReader = new StringReader(vdResponseString);
                    responseXML = (VDMessage.ResponseXML)serializer.Deserialize(stringReader);
                    if (responseXML.TxRs == null)
                    {
                        Log.Info($"VDMessage.ResponseXML For VD Token Data : {vdResponseString}");
                        Log.Info($"VDMessage.ResponseXML For VD Token Process Error Status code : {responseXML.ResponseHeader.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal($"Convert XML To Class(VDMessage.ResponseXML) For VD Token Exception : {e.Message}");
                    responseXML = ColaCommUtility.CreteVDMQResponseXML(vdRequestXML, firstAuthReturnString, SystemConstantData.CommonData.VDParsingErrorReturnCode);
                }
            }
            //run second store procedure
            const int returnArrayCount = 6;
            //Call Authorization Store Procedure
            var secondAuthReturnString = StoreProcUtility.Nccc0420TknAuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0420 For VD Token Return String Is Null");
                responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0420 For VD Token Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                }
                else
                {
                    responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, secondAuthReturnString[0]).GetNcccMessage();
                }
            }
            if (responseNcccMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0420 For VD Token ",
                          "Second ProcessNCCC0420 For VD Token Response BIC Message Return Is Null");

                responseNcccMessage = new NcccMessage0420TknAuth(requestNcccMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log For VD Token Error,Return Code = [{spRetnCode}]");
            }
            return (responseNcccMessage.Encode().Val());
        }

    }
}

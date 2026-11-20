using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.FirstHSMUtils;
using Com.Cybersoft4u.CommonLib.TwinMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SMS;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Provides utility methods for processing TWIN (Taiwan Interbank Network) authorization transaction flows.
    /// </summary>
    public static class TwinAuthFlowUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;

        private static readonly string HmsCheckFlags = SystemParameterInfo.HSMCheckFlags.TwinHSM.IsARQCCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.TwinHSM.IsCVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.TwinHSM.IsCVV2Check.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.TwinHSM.IsCAVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.TwinHSM.IsPVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.TwinHSM.IsPVVDobCheck.Trim();


        /// <summary>
        /// Process Transaction Flow
        /// </summary>
        /// <param name="requestTwinMessage">BIC Message Data</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        /// SessionInfoStruct _sessionInfo
        public static byte[] ProcessTransactionFlow(TWINIso8583 requestTwinMessage,
                                                     string localEndPoint,
                                                     string txKeySeq)
        {
            try
            {
                byte[] respBytes;
                switch (requestTwinMessage.Mti())
                {
                    case "0100":
                        respBytes = ProcessTWIN0100(requestTwinMessage, localEndPoint, HmsCheckFlags);
                        break;
                    case "0120":
                    case "0121":
                        respBytes = ProcessTWIN0120(requestTwinMessage, localEndPoint);
                        break;
                    case "0200":
                        respBytes = ProcessTWIN0200(requestTwinMessage, localEndPoint, HmsCheckFlags);
                        break;
                    case "0220":
                    case "0221":
                        respBytes = ProcessTWIN0220(requestTwinMessage, localEndPoint);
                        break;
                    case "0310":
                        ProcessTWIN0310(requestTwinMessage, localEndPoint, txKeySeq);
                        return null;
                    case "0420":
                    case "0421":
                        respBytes = ProcessTWIN0420(requestTwinMessage, localEndPoint);
                        break;
                    case "0800":
                    case "0820":
                    case "0821":
                        respBytes = ProcessTWIN0800(requestTwinMessage, localEndPoint);
                        break;
                    case "0810":
                        ProcessTWIN0810(requestTwinMessage, localEndPoint, txKeySeq);
                        return null;
                    default:
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                                  new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                                  "Message Type No Match : " + requestTwinMessage.Mti(),
                                  "Message Type No Match");
                        return null;
                }

                if (respBytes == null)
                {
                    Log.Fatal($"TwinAuthFlowUtility.ProcessTransactionFlow Message Type {requestTwinMessage.Mti()}, Return Data Is Null");
                }
                else
                {
                    Log.Info($"TwinAuthFlowUtility.ProcessTransactionFlow Message Type {requestTwinMessage.Mti()}");
                }
                if (respBytes != null)
                {
                    return (ColaCommUtility.ContactBytesArray(ColaCommUtility.IntegerToBinary(respBytes.Length), respBytes));
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinAuthFlowUtility.ProcessTransactionFlow Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ProcessTWIN0100 Process 0100 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessTWIN0100(TWINIso8583 requestTwinMessage,
                                              string localEndPoint,
                                              string hsmCheckFlags)
        {
            const int returnArrayCount = 22;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldPos)) ? requestTwinMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0100 Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            TWINIso8583 responseTwinMessage;
            var authInfoData = new NcccAuthInfoStruct(requestTwinMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            FirstHSMUtility.HSMCheckFlow(requestTwinMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                               SystemConstantData.CommonData.MessageReceiveDirection,
                                                               localEndPoint,
                                                               requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Set Initial data9F10Array Value
            var data9F10Array = new string[3];


            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Twin0100AuthProcess(authInfoData, data9F10Array);

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
            // authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
            // authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
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
            // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
            // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
            // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
            // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessTWIN0100 Return String Is Null");
                responseTwinMessage = new TwinMessage0100(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessTWIN0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseTwinMessage = new TwinMessage0100(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
                }
                else
                {
                    responseTwinMessage = new TwinMessage0100(requestTwinMessage, authInfoData, authReturnString, data9F10Array).GetTwinMessage();
                    if (!string.IsNullOrEmpty(authReturnString[03]) && authReturnString[03].Trim().Equals("D"))
                    {
                        var serverName = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.TWINClientAuthRouterService.PrimaryServerName.Trim();
                        var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                        var portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.TWINClientAuthRouterService.ListenerPortNumber.Trim());
                        ColaCommUtility.SendNegFileMaintToNccc(authInfoData.isoP2CardNbr, localEndPoint, $"{serverIPAddr}:{portNumber}").ConfigureAwait(false);
                        Log.Info($"SendFileMaintToNccc process completely at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    }
                }
            }
            if (responseTwinMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessTWIN0100",
                          "ProcessTWIN0100 Response BIC Message Return Is Null");

                responseTwinMessage = new TwinMessage0100(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessTWIN0120 Process 0120 Authorization Flow For TWIN
        /// </summary>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessTWIN0120(TWINIso8583 requestTwinMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 2;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldPos)) ? requestTwinMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0120 Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            TWINIso8583 responseTwinMessage;
            var authInfoData = new NcccAuthInfoStruct(requestTwinMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Twin0120AuthProcess(authInfoData);

            // ProcessTWIN022 Return String Arrays Information
            // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
            // Description
            // @isoP38AuthCode          	varchar(06) 	Output	 --p38
            // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseTwinMessage = new TwinMessage0120(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessTWIN0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseTwinMessage = new TwinMessage0120(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
                }
                else
                {
                    responseTwinMessage = new TwinMessage0120(requestTwinMessage, authInfoData, authReturnString).GetTwinMessage();
                }
            }
            if (responseTwinMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessTWIN0120",
                          "ProcessTWIN0120 Response BIC Message Return Is Null");

                responseTwinMessage = new TwinMessage0120(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }



        /// <summary>
        /// ProcessTWIN0200 Process 0200 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessTWIN0200(TWINIso8583 requestTwinMessage,
                                              string localEndPoint,
                                              string hsmCheckFlags)
        {
            const int returnArrayCount = 22;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldPos)) ? requestTwinMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0200 Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            TWINIso8583 responseTwinMessage;
            var authInfoData = new NcccAuthInfoStruct(requestTwinMessage);

            //Check CardBin & Card Attribute(Credit/Debit)
            Log.Info($"LocalEndPoint[{localEndPoint}],HSM Process start here CardNbr is :[{authInfoData.isoP2CardNbr}]");
            FirstHSMUtility.HSMCheckFlow(requestTwinMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                               SystemConstantData.CommonData.MessageReceiveDirection,
                                                               localEndPoint,
                                                               requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Set Initial data9F10Array Value
            var data9F10Array = new string[3];


            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Twin0200AuthProcess(authInfoData, data9F10Array);

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
            // authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
            // authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
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
            // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
            // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
            // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
            // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額  

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("ProcessTWIN0200 Return String Is Null");
                responseTwinMessage = new TwinMessage0200(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessTWIN0200 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseTwinMessage = new TwinMessage0200(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
                }
                else
                {
                    responseTwinMessage = new TwinMessage0200(requestTwinMessage, authInfoData, authReturnString, data9F10Array).GetTwinMessage();
                    if (!string.IsNullOrEmpty(authReturnString[03]) && authReturnString[03].Trim().Equals("D"))
                    {
                        var serverName = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.TWINClientAuthRouterService.PrimaryServerName.Trim();
                        var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                        var portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.TWINClientAuthRouterService.ListenerPortNumber.Trim());
                        ColaCommUtility.SendNegFileMaintToNccc(authInfoData.isoP2CardNbr, localEndPoint, $"{serverIPAddr}:{portNumber}").ConfigureAwait(false);
                        Log.Info($"SendFileMaintToNccc process completely at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    }
                }
            }
            if (responseTwinMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessTWIN0200",
                          "ProcessTWIN0200 Response BIC Message Return Is Null");

                responseTwinMessage = new TwinMessage0200(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessTWIN0220 Process 0220 Authorization Flow For TWIN
        /// </summary>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessTWIN0220(TWINIso8583 requestTwinMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldPos)) ? requestTwinMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0220 Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            TWINIso8583 responseTwinMessage;
            var authInfoData = new NcccAuthInfoStruct(requestTwinMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Twin0220AuthProcess(authInfoData);

            // ProcessTWIN022 Return String Arrays Information
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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseTwinMessage = new TwinMessage0220(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessTWIN0220 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseTwinMessage = new TwinMessage0220(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
                }
                else
                {
                    responseTwinMessage = new TwinMessage0220(requestTwinMessage, authInfoData, authReturnString).GetTwinMessage();
                }
            }
            if (responseTwinMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessTWIN0220",
                          "ProcessTWIN0220 Response BIC Message Return Is Null");

                responseTwinMessage = new TwinMessage0220(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessTWIN0310 Process File Maintenance Management(0310 Process)
        /// </summary>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessTWIN0310(TWINIso8583 requestTwinMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Fatal("ProcessTWIN0310 txKeySeq Invalid No Write Log");
                return;
            }
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldPos)) ? requestTwinMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0310 Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessTWIN0420 Process 0420 Authorization Flow
        /// </summary>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessTWIN0420(TWINIso8583 requestTwinMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 6;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldPos)) ? requestTwinMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0420 Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            TWINIso8583 responseTwinMessage;
            var authInfoData = new NcccAuthInfoStruct(requestTwinMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Twin0420AuthProcess(authInfoData);

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
                responseTwinMessage = new TwinMessage0420(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessTWIN0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseTwinMessage = new TwinMessage0420(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
                }
                else
                {
                    responseTwinMessage = new TwinMessage0420(requestTwinMessage, authReturnString[0]).GetTwinMessage();
                }
            }
            if (responseTwinMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessTWIN0420",
                          "ProcessTWIN0420 Response BIC Message Return Is Null");

                responseTwinMessage = new TwinMessage0420(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessTWIN0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessTWIN0800(TWINIso8583 requestTwinMessage,
                                              string localEndPoint)
        {
            string informationCode;
            FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
            {
                informationCode = (string)requestTwinMessage.Core().Field(field);
                if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOff))
                    SingletonOfCommonProperty.Instance.SetColaSignOff(SystemConstantData.CommonData.PasystfTWINInfo);
                else if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOn))
                    SingletonOfCommonProperty.Instance.SetColaSignOn(SystemConstantData.CommonData.PasystfTWINInfo);
            }
            else
                informationCode = string.Empty;

            // 01 --Outbound key(Visa:IWK)  TWIN -> Bank
            // 02 --Inbound key (Visa:AWK)
            // 03 --both incoming and outgoing key
            int processOption = 2;
            if (informationCode.Equals("162"))
            {
                field = FIELD.F_53;
                if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                {
                    var keyDirection = ((string)requestTwinMessage.Core().Field(field)).Substring(2, 2);
                    processOption = keyDirection.Equals("01") ? 1 : 2;
                }
                else
                {
                    processOption = 2;
                }
            }
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0800 Start,Header[{requestTwinMessage.Header().Encode()}],Information Code[{informationCode}],Process Option[{processOption}](1:Change Key)");
            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Add Send Transaction Log
            TWINIso8583 responseTwinMessage;
            switch (processOption)
            {
                case 1:
                    responseTwinMessage = new TwinMessage0800NewKey(requestTwinMessage).GetTwinMessage();
                    break;
                default:
                    responseTwinMessage = new TwinMessage0800(requestTwinMessage).GetTwinMessage();
                    break;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessTWIN0810 Process Network Management(0810 Process)
        /// </summary>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessTWIN0810(TWINIso8583 requestTwinMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0810 txKeySeq inValid]");
                return;
            }
            string informationCode;
            const FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
            {
                informationCode = (string)requestTwinMessage.Core().Field(field);
            }
            else
                informationCode = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWIN0810 Start,Header[{requestTwinMessage.Header().Encode()}],Information Code[{informationCode}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessTWINErrorProcess Error Flow
        /// </summary>
        /// <param name="localEndPoint">Local End Point</param>
        /// <param name="requestTwinMessage">Request BIC Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessTWINError(string localEndPoint,
                                               TWINIso8583 requestTwinMessage)
        {
            string pCodeString;
            const FIELD field = FIELD.F_3;
            if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
            {
                pCodeString = (string)requestTwinMessage.Core().Field(field);
            }
            else
                pCodeString = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessTWINError Start,Header[{requestTwinMessage.Header().Encode()}],Type[{requestTwinMessage.Mti()}],PCode[{pCodeString}]");
            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            Log.Warn($"ProcessTWINError Response Code = {SystemConstantData.CommonData.SystemProcessErrorCode}");
            var responseTwinMessage = new TwinMessageBase(requestTwinMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetTwinMessage();
            if (responseTwinMessage == null)
            {
                Log.Fatal("Create Response BIC Message Is Null");
                return null;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseTwinMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseTwinMessage.Encode().Val());
        }
    }
}

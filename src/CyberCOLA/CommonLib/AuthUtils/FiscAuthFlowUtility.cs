using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.FirstHSMUtils;
using Com.Cybersoft4u.CommonLib.FiscMessage;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using NLog;
using System;
using System.IO;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class FiscAuthFlowUtility
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
        private static readonly string HmsCheckFlags = SystemParameterInfo.HSMCheckFlags.FiscHSM.IsARQCCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.FiscHSM.IsCVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.FiscHSM.IsCVV2Check.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.FiscHSM.IsCAVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.FiscHSM.IsPVVCheck.Trim() +
                                                       SystemParameterInfo.HSMCheckFlags.FiscHSM.IsPVVDobCheck.Trim();


        /// <summary>
        /// Process Transaction Flow
        /// </summary>
        /// <param name="requestFiscMessage">BIC Message Data</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        /// SessionInfoStruct _sessionInfo
        public static byte[] ProcessTransactionFlow(FISC requestFiscMessage,
                                                     string localEndPoint,
                                                     string txKeySeq)
        {
            try
            {
                byte[] respBytes;
                switch (requestFiscMessage.Mti())
                {
                    case "0100":
                        if (CreditCardUtility.fiscHceTransaction(requestFiscMessage))
                            respBytes = ProcessFISC0100HCE(requestFiscMessage, localEndPoint, HmsCheckFlags);
                        else
                            respBytes = ProcessFISC0100(requestFiscMessage, localEndPoint, HmsCheckFlags);
                        break;
                    case "0120":
                        if (CreditCardUtility.fiscHceTransaction(requestFiscMessage))
                            respBytes = ProcessFISC0120HCE(requestFiscMessage, localEndPoint);
                        else
                            respBytes = ProcessFISC0120(requestFiscMessage, localEndPoint);
                        break;
                    case "0310":
                        ProcessNCCC0310(requestFiscMessage, localEndPoint, txKeySeq);
                        return null;
                    case "0420":
                        if (CreditCardUtility.fiscHceTransaction(requestFiscMessage))
                            respBytes = ProcessFISC0420HCE(requestFiscMessage, localEndPoint);
                        else
                            respBytes = ProcessFISC0420(requestFiscMessage, localEndPoint);
                        break;
                    case "0800":
                    case "0820":
                    case "0821":
                        respBytes = ProcessFISC0800(requestFiscMessage, localEndPoint);
                        break;
                    case "0810":
                        ProcessFISC0810(requestFiscMessage, localEndPoint);
                        return (null);
                    default:
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                            new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                            "Message Type No Match : " + requestFiscMessage.Mti(),
                            "Message Type No Match");
                        return (null);
                }

                if (respBytes == null)
                {
                    Log.Fatal($"FiscAuthFlowUtility.ProcessTransactionFlow Message Type {requestFiscMessage.Mti()}, Return Data Is Null");
                }
                else
                {
                    Log.Info($"FiscAuthFlowUtility.ProcessTransactionFlow Message Type {requestFiscMessage.Mti()}");
                }

                if (respBytes != null)
                {
                    return (ColaCommUtility.ContactBytesArray(ColaCommUtility.IntegerToBinary(respBytes.Length), respBytes));
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscAuthFlowUtility.ProcessTransactionFlow Exception:{ex.Message}");
                return (null);

            }
        }

        /// <summary>
        /// ProcessFISC0100 Authorization Flow
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessFISC0100(FISC requestFiscMessage,
                                               string localEndPoint,
                                               string hsmCheckFlags)
        {
            const int returnArrayCount = 19;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessFISC0100 Start, Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");
            FISC responseFiscMessage;
            FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(requestFiscMessage);

            //Process HSM For Check Data
            var hsmStart = AuthTiming.Start();
            FirstHSMUtility.HSMCheckFlow(requestFiscMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);
            AuthTiming.StopHsm(hsmStart);

            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
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
            if (authInfoData.isoP55MDK != null)
            {
                data9F10Array[0] = authInfoData.isoP55MDK;
            }
            if (authInfoData.isoP55VerNum != null)
            {
                data9F10Array[1] = authInfoData.isoP55VerNum;
            }
            if (authInfoData.isoP55CVR != null)
            {
                data9F10Array[2] = authInfoData.isoP55CVR;
            }

            //Call Authorization Store Procedure
            string[] authReturnString = null;
            authReturnString = StoreProcUtility.Fisc0100AuthProcess(authInfoData);

            // Return String Arrays Information
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
            // @isoP58T21PaidCAmt      	    varchar(12) 	Output	 Field 58 <紅利,實際支付金額>
            // @isoS112DownPayment      	varchar(08) 	Output	 Field 112 首期期金
            // @isoS112InstPayment          varchar(08) 	Output	 Field 112 每期期金
            // @isoS112InstFee  	        varchar(06) 	Output	 Field 112 手續費
            // @isoS112InstTxResp        	varchar(02) 	Output	 Field 112 分期回應碼
            // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
            // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
            // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
            // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
            // @vdBlockAmt                  varchar(15)   	Output   VD 圈存解圈金額

            if (authReturnString == null) //The Store Procedure Process Error
            {
                Log.Fatal("ProcessFISC0100 Return String Is Null");
                responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessFISC0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0100(requestFiscMessage, authInfoData, authReturnString, data9F10Array).GetFiscMessage();
                }
            }

            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                         new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                         "ProcessFISC0100",
                         "ProcessFISC0100 Response FISC Message Return Is Null");
                responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                authReturnString = null;  //for check condition
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseFiscMessage.Encode().Val());     //for process error
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[15])) &&
                (!string.IsNullOrEmpty(authReturnString[16])) &&
                (!string.IsNullOrEmpty(authReturnString[17])) &&
                (!string.IsNullOrEmpty(authReturnString[18])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessFISC0100VDLoading(authInfoData, authReturnString, localEndPoint, requestFiscMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseFiscMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessFISC0100HCE Authorization Flow
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="hsmCheckFlags">HSM SUIP Check Flag</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessFISC0100HCE(FISC requestFiscMessage,
                                               string localEndPoint,
                                               string hsmCheckFlags)
        {
            const int returnArrayCount = 14;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessFISC0100HCE Start, Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");
            FISC responseFiscMessage;
            FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(requestFiscMessage);

            //Process HSM For Check Data(NO PROCESS)
            //FirstHSMUtility.HSMCheckFlow(requestFiscMessage, hsmCheckFlags, SystemParameterInfo.BankInfo.BankCode, ref authInfoData);

            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = null;
            authReturnString = StoreProcUtility.Fisc0100HceAuthProcess(authInfoData);

            // Return String Arrays Information
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
            // @isoP48T45                	varchar(01)	    Output	 --CAVV Result
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

            if (authReturnString == null) //The Store Procedure Process Error
            {
                Log.Fatal("ProcessFISC0100HCE Return String Is Null");
                responseFiscMessage = new FiscMessage0100Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessFISC0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0100Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0100Hce(requestFiscMessage, authInfoData, authReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                         new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                         "ProcessFISC0100HCE",
                         "ProcessFISC0100HCE Response FISC Message Return Is Null");
                responseFiscMessage = new FiscMessage0100Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                authReturnString = null;  //for check condition
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseFiscMessage.Encode().Val());     //for process error
        }

        /// <summary>
        /// ProcessFISC0120 Process 0120 Authorization Flow
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessFISC0120(FISC requestFiscMessage, string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;

            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessFISC0120 Start, Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");

            FISC responseFiscMessage = null;
            FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(requestFiscMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = null;
            authReturnString = StoreProcUtility.Fisc0120AuthProcess(authInfoData);

            // Return String Arrays Information
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
            // @vdBlockAmt                  varchar(15)  	Output   VD 圈存解圈金額

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessFISC0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0120(requestFiscMessage, authInfoData, authReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                         new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                         "ProcessFISC0120",
                         "ProcessFISC0120 Response FISC Message Return Is Null");
                responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                authReturnString = null;  //for check condition
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseFiscMessage.Encode().Val());     //for process error
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])) &&
                (!string.IsNullOrEmpty(authReturnString[06])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessFISC0120VDLoading(authInfoData, authReturnString, localEndPoint, requestFiscMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseFiscMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessFISC0120HCE Process 0120 Authorization Flow
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessFISC0120HCE(FISC requestFiscMessage, string localEndPoint)
        {
            const int returnArrayCount = 3;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;

            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessFISC0120HCE Start, Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");

            FISC responseFiscMessage = null;
            FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(requestFiscMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = null;
            authReturnString = StoreProcUtility.Fisc0120HceAuthProcess(authInfoData);

            // Return String Arrays Information
            // authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
            // authReturnString[02]:@isoP39RspAction       
            // Description
            // @isoP38AuthCode          	varchar(06) 	Output	 --p38
            // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseFiscMessage = new FiscMessage0120Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessFISC0120HCE Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0120Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0120Hce(requestFiscMessage, authInfoData, authReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                         new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                         "ProcessFISC0120HCE",
                         "ProcessFISC0120HCE Response FISC Message Return Is Null");
                responseFiscMessage = new FiscMessage0120Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                authReturnString = null;  //for check condition
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseFiscMessage.Encode().Val());     //for process error

        }

        /// <summary>
        /// ProcessFISC0310 Process File Maintenance Management(0310 Process)
        /// </summary>
        /// <param name="requestFiscMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessNCCC0310(FISC requestFiscMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Fatal("ProcessFISC0310 txKeySeq Invalid No Write Log");
                return;
            }
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessNCCC0310 Start,Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessFISC0420 Process 0420 Authorization Flow
        /// </summary>
        /// <param name="localEndPoint">Session Name</param>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessFISC0420(FISC requestFiscMessage, string localEndPoint)
        {
            const int returnArrayCount = 12;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessFISC0420 Start, Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");

            FISC responseFiscMessage = null;
            FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(requestFiscMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = null;
            authReturnString = StoreProcUtility.Fisc0420AuthProcess(authInfoData);
            // Return String Arrays Information
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
            // @vdBlockAmt                  varchar(15)     Output   VD 圈存解圈金額

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessFISC0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0420(requestFiscMessage, authInfoData, authReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                        new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                        "ProcessFISC0420",
                        "ProcessFISC0420 Response FISC Message Return Is Null");
                responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                authReturnString = null;  //for check condition
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseFiscMessage.Encode().Val());     //for process error
            }
            if (ColaCommUtility.IsVDCard(authInfoData.isoP2CardNbr) &&
                authReturnString[02].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[08])) &&
                (!string.IsNullOrEmpty(authReturnString[09])) &&
                (!string.IsNullOrEmpty(authReturnString[10])) &&
                (!string.IsNullOrEmpty(authReturnString[11])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessFISC0420VDLoading(authInfoData, authReturnString, localEndPoint, requestFiscMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseFiscMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessFISC0420HCE Process 0420 Authorization Flow
        /// </summary>
        /// <param name="localEndPoint">Session Name</param>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessFISC0420HCE(FISC requestFiscMessage, string localEndPoint)
        {
            const int returnArrayCount = 8;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)) ? requestFiscMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessFISC0420HCE Start, Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");

            FISC responseFiscMessage = null;
            FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(requestFiscMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = null;
            authReturnString = StoreProcUtility.Fisc0420HceAuthProcess(authInfoData);
            // Return String Arrays Information
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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseFiscMessage = new FiscMessage0420Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessFISC0420HCE Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0420Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0420Hce(requestFiscMessage, authInfoData, authReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                        new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                        "ProcessFISC0420HCE",
                        "ProcessFISC0420HCE Response FISC Message Return Is Null");
                responseFiscMessage = new FiscMessage0420Hce(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                authReturnString = null;  //for check condition
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseFiscMessage.Encode().Val());     //for process error
        }

        /// <summary>
        /// ProcessFISC0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint">Session Name</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessFISC0800(FISC requestFiscMessage, string localEndPoint)
        {
            string informationCode;
            int processOption;
            var fieldPos = FIELD.F_70;

            if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                informationCode = requestFiscMessage.Core().Field(FIELD.F_70).ToString();
            else
                informationCode = string.Empty;

            processOption = 2;
            if (informationCode is "162")
            {
                fieldPos = FIELD.F_53;
                if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                {
                    string keyDirection = requestFiscMessage.Core().Field(FIELD.F_70).ToString()?.Substring(2, 2);
                    if (keyDirection.Equals("01"))
                        processOption = 1;
                    else
                        processOption = 2;
                }
                else
                {
                    processOption = 2;
                }
            }
            Log.Info($"ProcessFISC0800 Start,Information Code[{informationCode}],Process Option[{processOption}](1:Change Key)");

            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Add Send Transaction Log
            FISC responseFiscMessage = null;
            switch (processOption)
            {
                case 1:
                    responseFiscMessage = new FiscMessage0800NewKey(requestFiscMessage).GetFiscMessage();
                    break;
                default:
                    responseFiscMessage = new FiscMessage0800(requestFiscMessage).GetFiscMessage();
                    break;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return responseFiscMessage.Encode().Val();
        }

        /// <summary>
        ///  ProcessFISC0810 Process Network Management(0810 Process)
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static void ProcessFISC0810(FISC requestFiscMessage, string localEndPoint)
        {
            string informationCode;
            var fieldPos = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                informationCode = requestFiscMessage.Core().Field(FIELD.F_70).ToString();
            else
                informationCode = string.Empty;

            Log.Info($"ProcessFISC0810 Start,Information Code[{informationCode}]");

            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return;
        }

        /// <summary>
        /// ProcessFISCError Process Error Flow
        /// </summary>
        /// <param name="requestFiscMessage">Request FISC Message</param>
        /// <param name="localEndPoint">Session Name</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessFISCError(string localEndPoint, FISC requestFiscMessage)
        {
            string pCodeString;
            var fieldPos = FIELD.F_3;
            if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldPos)))
                pCodeString = requestFiscMessage.Core().Field(FIELD.F_70).ToString();
            else
                pCodeString = string.Empty;
            Log.Info($"ProcessFISCError Start,Type[{requestFiscMessage.Mti()}],PCode[{pCodeString}]");

            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            var responseFiscMessage = new FiscMessage.MessageBase(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            if (responseFiscMessage == null)
            {
                Log.Fatal("Create Response FISC Message Is Null");
                return (null);
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return responseFiscMessage.Encode().Val();
        }


        /// <summary>
        /// Processes the FISC0100 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestFiscMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessFISC0100VDLoading(FiscAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, FISC requestFiscMessage)
        {
            FISC responseFiscMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                return (responseFiscMessage.Encode().Val());
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
            const int returnArrayCount = 19;
            //Set Initial data9F10Array Value
            // <summary>
            //  data9F10Array[0] : @tB2DeriveKeyIndex
            //  data9F10Array[1] : @tB2CryptoVerNum
            //  data9F10Array[2] : @tB2CVR
            // </summary>s
            var data9F10Array = new string[3];

            if (authInfoData.isoP55MDK != null)
            {
                data9F10Array[0] = authInfoData.isoP55MDK;
            }
            if (authInfoData.isoP55VerNum != null)
            {
                data9F10Array[1] = authInfoData.isoP55VerNum;
            }
            if (authInfoData.isoP55CVR != null)
            {
                data9F10Array[2] = authInfoData.isoP55CVR;
            }

            //Call Authorization Store Procedure
            string[] secondAuthReturnString = StoreProcUtility.Fisc0100AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessFISC0100 Return String Is Null");
                responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessFISC0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0100(requestFiscMessage, authInfoData, secondAuthReturnString, data9F10Array).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessFISC0100",
                          "Second ProcessFISC0100 Response FISC Message Return Is Null");

                responseFiscMessage = new FiscMessage0100(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseFiscMessage.Encode().Val());
        }


        /// <summary>
        /// Processes the FISC0120 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestFiscMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessFISC0120VDLoading(FiscAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, FISC requestFiscMessage)
        {
            FISC responseFiscMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                return (responseFiscMessage.Encode().Val());
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
            string[] secondAuthReturnString = StoreProcUtility.Fisc0120AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessFISC0120 Return String Is Null");
                responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessFISC0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0120(requestFiscMessage, authInfoData, secondAuthReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessFISC0120",
                          "Second ProcessFISC0120 Response FISC Message Return Is Null");

                responseFiscMessage = new FiscMessage0120(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseFiscMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the FISC0420 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestFiscMessage">The request NCCC message.</param>
        /// <returns></returns>
        private static byte[] ProcessFISC0420VDLoading(FiscAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, FISC requestFiscMessage)
        {
            FISC responseFiscMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                return (responseFiscMessage.Encode().Val());
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
            const int returnArrayCount = 12;
            //Call Authorization Store Procedure
            string[] secondAuthReturnString = StoreProcUtility.Fisc0420AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessFISC0420 Return String Is Null");
                responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessFISC0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
                }
                else
                {
                    responseFiscMessage = new FiscMessage0420(requestFiscMessage, authInfoData, secondAuthReturnString).GetFiscMessage();
                }
            }
            if (responseFiscMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessFISC0420",
                          "Second ProcessFISC0420 Response FISC Message Return Is Null");

                responseFiscMessage = new FiscMessage0420(requestFiscMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetFiscMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseFiscMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseFiscMessage.Encode().Val());
        }
    }
}

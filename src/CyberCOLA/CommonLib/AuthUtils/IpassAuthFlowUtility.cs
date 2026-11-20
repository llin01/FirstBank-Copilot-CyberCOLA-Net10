using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.IpassMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using NLog;
using System;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class IpassAuthFlowUtility
    {
        #region NLog

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion //NLog

        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();

        /// <summary>
        /// Process Transaction Flow
        /// </summary>
        /// <param name="requestIpassMessage">IPASS Message Data</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        /// SessionInfoStruct _sessionInfo
        public static byte[] ProcessTransactionFlow(IPASS requestIpassMessage,
                                                    string localEndPoint,
                                                    string txKeySeq)
        {
            try
            {
                byte[] respBytes;
                switch (requestIpassMessage.Mti())
                {
                    case "0100":
                        respBytes = ProcessIPASS0100(requestIpassMessage, localEndPoint);
                        break;
                    case "0120":
                    case "0121":
                        respBytes = ProcessIPASS0120(requestIpassMessage, localEndPoint);
                        break;
                    case "0200":
                        respBytes = ProcessIPASS0100(requestIpassMessage, localEndPoint);
                        break;
                    case "0220":
                    case "0221":
                        respBytes = ProcessIPASS0120(requestIpassMessage, localEndPoint);
                        break;
                    case "0312":
                        ProcessIPASS0312(requestIpassMessage, localEndPoint, txKeySeq);
                        return null;
                    case "0420":
                    case "0421":
                        respBytes = ProcessIPASS0420(requestIpassMessage, localEndPoint);
                        break;
                    case "0800":
                    case "0820":
                    case "0821":
                        respBytes = ProcessIPASS0800(requestIpassMessage, localEndPoint);
                        break;
                    case "0810":
                        ProcessIPASS0810(requestIpassMessage, localEndPoint, txKeySeq);
                        return null;
                    default:
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                            new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                            "Message Type No Match : " + requestIpassMessage.Mti(),
                            "Message Type No Match");
                        return null;
                }

                if (respBytes == null)
                {
                    Log.Fatal(
                        $"IpassAuthFlowUtility.ProcessTransactionFlow Message Type {requestIpassMessage.Mti()}, Return Data Is Null");
                }
                else
                {
                    Log.Info($"IpassAuthFlowUtility.ProcessTransactionFlow Message Type {requestIpassMessage.Mti()}");
                }

                if (respBytes != null)
                {
                    return (ColaCommUtility.ContactBytesArray(ColaCommUtility.IntegerToBinary(respBytes.Length),
                        respBytes));
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IpassAuthFlowUtility.ProcessTransactionFlow Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ProcessIPASS0200 Process 0200 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessIPASS0100(IPASS requestIpassMessage,
                                               string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos))
                              ? requestIpassMessage.RawVal(fieldPos).ToString()
                              : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0100 Start,Header[{requestIpassMessage.Header().Encode()}],Type[{requestIpassMessage.Mti()}],PCode[{pCodeString}]");
            IPASS responseIpassMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestIpassMessage);

            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Esvc0100AuthProcess(authInfoData);

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

            if (authReturnString == null) //The Store Procedure Process Error
            {
                Log.Fatal("ProcessIPASS0100 Return String Is Null");
                responseIpassMessage = new IpassMessage0100(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessIPASS0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseIpassMessage = new IpassMessage0100(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
                }
                else
                {
                    responseIpassMessage = new IpassMessage0100(requestIpassMessage, authReturnString).GetIpassMessage();
                }
            }

            if (responseIpassMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessIPASS0100",
                          "ProcessIPASS0100 Response IPASS Message Return Is Null");

                responseIpassMessage = new IpassMessage0100(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            return (responseIpassMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessIPASS0120 Process 0220 Authorization Flow For IPASS
        /// </summary>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessIPASS0120(IPASS requestIpassMessage,
                                                string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos))
                ? requestIpassMessage.RawVal(fieldPos).ToString()
                : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0120 Start,Header[{requestIpassMessage.Header().Encode()}],Type[{requestIpassMessage.Mti()}],PCode[{pCodeString}]");
            IPASS responseIpassMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestIpassMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Esvc0120AuthProcess(authInfoData);

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

            if (authReturnString == null) //The Store Procedure Process Error
            {
                responseIpassMessage = new IpassMessage0120(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessIPASS0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseIpassMessage = new IpassMessage0120(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
                }
                else
                {
                    responseIpassMessage = new IpassMessage0120(requestIpassMessage, authReturnString).GetIpassMessage();
                }
            }

            if (responseIpassMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessIPASS0120",
                          "ProcessIPASS0120 Response IPASS Message Return Is Null");

                responseIpassMessage = new IpassMessage0120(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            return (responseIpassMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessIPASS0312 Process File Maintenance Management(0310 Process)
        /// </summary>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessIPASS0312(IPASS requestIpassMessage,
                                              string localEndPoint,
                                              string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Fatal("ProcessIPASS0312 txKeySeq Invalid No Write Log");
                return;
            }
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)) ? requestIpassMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0312 Start,Header[{requestIpassMessage.Header().Encode()}],Type[{requestIpassMessage.Mti()}],PCode[{pCodeString}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessIPASS0420 Process 0420 Authorization Flow
        /// </summary>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessIPASS0420(IPASS requestIpassMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIpassMessage.RawVal(fieldPos)) ? requestIpassMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0420 Start,Header[{requestIpassMessage.Header().Encode()}],Type[{requestIpassMessage.Mti()}],PCode[{pCodeString}]");
            IPASS responseIpassMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestIpassMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.Esvc0420AuthProcess(authInfoData);

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

            if (authReturnString == null)  //The Store Procedure Process Error
            {
                responseIpassMessage = new IpassMessage0420(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessIPASS0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseIpassMessage = new IpassMessage0420(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
                }
                else
                {
                    responseIpassMessage = new IpassMessage0420(requestIpassMessage, authReturnString[0]).GetIpassMessage();
                }
            }
            if (responseIpassMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessIPASS0420",
                          "ProcessIPASS0420 Response IPASS Message Return Is Null");

                responseIpassMessage = new IpassMessage0420(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseIpassMessage.Encode().Val());
        }


        /// <summary>
        /// ProcessIPASS0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessIPASS0800(IPASS requestIpassMessage,
                                                string localEndPoint)
        {
            string informationCode;
            FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(field)))
            {
                informationCode = (string)requestIpassMessage.Core().Field(field);
                if (informationCode.Equals(IpassTransactionConst.NetworkManagementInformationCode.SignOff))
                    SingletonOfCommonProperty.Instance.SetColaSignOff(SystemConstantData.CommonData.PasystfIPASSInfo);
                else if (informationCode.Equals(IpassTransactionConst.NetworkManagementInformationCode.SignOn))
                    SingletonOfCommonProperty.Instance.SetColaSignOn(SystemConstantData.CommonData.PasystfIPASSInfo);
            }
            else
                informationCode = string.Empty;

            // 01 --Outbound key(Visa:IWK)  NCCC -> Bank
            // 02 --Inbound key (Visa:AWK)
            // 03 --both incoming and outgoing key
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0800 Start,Header[{requestIpassMessage.Header().Encode()}],Information Code[{informationCode}]");
            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Add Send Transaction Log
            var responseIpassMessage = new IpassMessage0800(requestIpassMessage).GetIpassMessage();
            responseIpassMessage.Header().Decode($"{requestIpassMessage.Header().SrcID()}{requestIpassMessage.Header().DestID()}");
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseIpassMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessIPASS0810 Process Network Management(0810 Process)
        /// </summary>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessIPASS0810(IPASS requestIpassMessage,
                                              string localEndPoint,
                                              string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0810 txKeySeq inValid]");
                return;
            }
            string informationCode;
            const FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(field)))
            {
                informationCode = (string)requestIpassMessage.Core().Field(field);
            }
            else
                informationCode = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASS0810 Start,Header[{requestIpassMessage.Header().Encode()}],Information Code[{informationCode}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessIPASSErrorProcess Error Flow
        /// </summary>
        /// <param name="localEndPoint">Local End Point</param>
        /// <param name="requestIpassMessage">Request IPASS Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessIPASSError(string localEndPoint,
                                                IPASS requestIpassMessage)
        {
            string pCodeString;
            const FIELD field = FIELD.F_3;
            if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(field)))
            {
                pCodeString = (string)requestIpassMessage.Core().Field(field);
            }
            else
                pCodeString = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessIPASSError Start,Header[{requestIpassMessage.Header().Encode()}],Type[{requestIpassMessage.Mti()}],PCode[{pCodeString}]");
            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            Log.Warn($"ProcessIPASSError Response Code = {SystemConstantData.CommonData.SystemProcessErrorCode}");
            var responseIpassMessage = new IpassMessageBase(requestIpassMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIpassMessage();
            if (responseIpassMessage == null)
            {
                Log.Fatal("Create Response IPASS Message Is Null");
                return null;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIpassMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseIpassMessage.Encode().Val());
        }
    }
}

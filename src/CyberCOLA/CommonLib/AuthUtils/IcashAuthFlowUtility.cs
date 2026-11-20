using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.IcashMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using NLog;
using System;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class IcashAuthFlowUtility
    {
        #region NLog

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion //NLog

        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();

        /// <summary>
        /// Process Transaction Flow
        /// </summary>
        /// <param name="requestIcashMessage">ICASH Message Data</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        /// SessionInfoStruct _sessionInfo
        public static byte[] ProcessTransactionFlow(ICASH requestIcashMessage,
                                                     string localEndPoint,
                                                     string txKeySeq)
        {
            try
            {
                byte[] respBytes;
                switch (requestIcashMessage.Mti())
                {
                    case "0100":
                        respBytes = ProcessICASH0100(requestIcashMessage, localEndPoint);
                        break;
                    case "0120":
                    case "0121":
                        respBytes = ProcessICASH0120(requestIcashMessage, localEndPoint);
                        break;
                    //case "0140":
                    //    respBytes = ProcessICASH0140(requestIcashMessage, localEndPoint);
                    //    break;
                    case "0200":
                        respBytes = ProcessICASH0100(requestIcashMessage, localEndPoint);
                        break;
                    case "0312":
                        ProcessICASH0312(requestIcashMessage, localEndPoint, txKeySeq);
                        return null;
                    case "0420":
                    case "0421":
                        respBytes = ProcessICASH0420(requestIcashMessage, localEndPoint);
                        break;
                    case "0800":
                    case "0820":
                    case "0821":
                        respBytes = ProcessICASH0800(requestIcashMessage, localEndPoint);
                        break;
                    case "0810":
                        ProcessICASH0810(requestIcashMessage, localEndPoint, txKeySeq);
                        return null;
                    default:
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                            new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                            "Message Type No Match : " + requestIcashMessage.Mti(),
                            "Message Type No Match");
                        return null;
                }

                if (respBytes == null)
                {
                    Log.Fatal(
                        $"IcashAuthFlowUtility.ProcessTransactionFlow Message Type {requestIcashMessage.Mti()}, Return Data Is Null");
                }
                else
                {
                    Log.Info($"IcashAuthFlowUtility.ProcessTransactionFlow Message Type {requestIcashMessage.Mti()}");
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
                Log.Fatal(ex, $"IcashAuthFlowUtility.ProcessTransactionFlow Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ProcessICASH0200 Process 0200 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessICASH0100(ICASH requestIcashMessage,
                                               string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos))
                              ? requestIcashMessage.RawVal(fieldPos).ToString()
                              : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0100 Start,Header[{requestIcashMessage.Header().Encode()}],Type[{requestIcashMessage.Mti()}],PCode[{pCodeString}]");
            ICASH responseIcashMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestIcashMessage);

            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                requestIcashMessage);
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
                Log.Fatal("ProcessICASH0100 Return String Is Null");
                responseIcashMessage = new IcashMessage0100(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessICASH0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseIcashMessage = new IcashMessage0100(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
                }
                else
                {
                    responseIcashMessage = new IcashMessage0100(requestIcashMessage, authReturnString).GetIcashMessage();
                }
            }

            if (responseIcashMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessICASH0100",
                          "ProcessICASH0100 Response ICASH Message Return Is Null");

                responseIcashMessage = new IcashMessage0100(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            return (responseIcashMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessICASH0120 Process 0120 Authorization Flow For ICASH
        /// </summary>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessICASH0120(ICASH requestIcashMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos))
                ? requestIcashMessage.RawVal(fieldPos).ToString()
                : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0120 Start,Header[{requestIcashMessage.Header().Encode()}],Type[{requestIcashMessage.Mti()}],PCode[{pCodeString}]");
            ICASH responseIcashMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestIcashMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIcashMessage);
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
                responseIcashMessage = new IcashMessage0120(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessICASH0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseIcashMessage = new IcashMessage0120(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
                }
                else
                {
                    responseIcashMessage = new IcashMessage0120(requestIcashMessage, authReturnString).GetIcashMessage();
                }
            }

            if (responseIcashMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessICASH0120",
                          "ProcessICASH0120 Response ICASH Message Return Is Null");

                responseIcashMessage = new IcashMessage0120(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            }

            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            return (responseIcashMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessICASH0312 Process File Maintenance Management(0310 Process)
        /// </summary>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessICASH0312(ICASH requestIcashMessage,
                                              string localEndPoint,
                                              string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Fatal("ProcessICASH0312 txKeySeq Invalid No Write Log");
                return;
            }
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)) ? requestIcashMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0312 Start,Header[{requestIcashMessage.Header().Encode()}],Type[{requestIcashMessage.Mti()}],PCode[{pCodeString}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessICASH0420 Process 0420 Authorization Flow
        /// </summary>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessICASH0420(ICASH requestIcashMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestIcashMessage.RawVal(fieldPos)) ? requestIcashMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0420 Start,Header[{requestIcashMessage.Header().Encode()}],Type[{requestIcashMessage.Mti()}],PCode[{pCodeString}]");
            ICASH responseIcashMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestIcashMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIcashMessage);
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
                responseIcashMessage = new IcashMessage0420(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessICASH0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseIcashMessage = new IcashMessage0420(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
                }
                else
                {
                    responseIcashMessage = new IcashMessage0420(requestIcashMessage, authReturnString[0]).GetIcashMessage();
                }
            }
            if (responseIcashMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessICASH0420",
                          "ProcessICASH0420 Response ICASH Message Return Is Null");

                responseIcashMessage = new IcashMessage0420(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            }
            spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseIcashMessage.Encode().Val());
        }


        /// <summary>
        /// ProcessICASH0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessICASH0800(ICASH requestIcashMessage,
                                                string localEndPoint)
        {
            string informationCode;
            FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(field)))
            {
                informationCode = (string)requestIcashMessage.Core().Field(field);
                if (informationCode.Equals(IcashTransactionConst.NetworkManagementInformationCode.SignOff))
                    SingletonOfCommonProperty.Instance.SetColaSignOff(SystemConstantData.CommonData.PasystfICASHInfo);
                else if (informationCode.Equals(IcashTransactionConst.NetworkManagementInformationCode.SignOn))
                    SingletonOfCommonProperty.Instance.SetColaSignOn(SystemConstantData.CommonData.PasystfICASHInfo);
            }
            else
                informationCode = string.Empty;

            // 01 --Outbound key(Visa:IWK)  NCCC -> Bank
            // 02 --Inbound key (Visa:AWK)
            // 03 --both incoming and outgoing key
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0800 Start,Header[{requestIcashMessage.Header().Encode()}],Information Code[{informationCode}]");
            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                requestIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Add Send Transaction Log
            var responseIcashMessage = new IcashMessage0800(requestIcashMessage).GetIcashMessage();
            responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseIcashMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessICASH0810 Process Network Management(0810 Process)
        /// </summary>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessICASH0810(ICASH requestIcashMessage,
                                              string localEndPoint,
                                              string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0810 txKeySeq inValid]");
                return;
            }
            string informationCode;
            const FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(field)))
            {
                informationCode = (string)requestIcashMessage.Core().Field(field);
            }
            else
                informationCode = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASH0810 Start,Header[{requestIcashMessage.Header().Encode()}],Information Code[{informationCode}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessICASHErrorProcess Error Flow
        /// </summary>
        /// <param name="localEndPoint">Local End Point</param>
        /// <param name="requestIcashMessage">Request ICASH Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessICASHError(string localEndPoint,
                                                ICASH requestIcashMessage)
        {
            string pCodeString;
            const FIELD field = FIELD.F_3;
            if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(field)))
            {
                pCodeString = (string)requestIcashMessage.Core().Field(field);
            }
            else
                pCodeString = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessICASHError Start,Header[{requestIcashMessage.Header().Encode()}],Type[{requestIcashMessage.Mti()}],PCode[{pCodeString}]");
            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            Log.Warn($"ProcessICASHError Response Code = {SystemConstantData.CommonData.SystemProcessErrorCode}");
            var responseIcashMessage = new IcashMessageBase(requestIcashMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetIcashMessage();
            if (responseIcashMessage == null)
            {
                Log.Fatal("Create Response ICASH Message Is Null");
                return null;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseIcashMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseIcashMessage.Encode().Val());
        }
    }
}

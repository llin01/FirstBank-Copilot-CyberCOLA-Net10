using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.SvcsMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using NLog;
using System;
using System.IO;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class SvcsAuthFlowUtility
    {
        #region NLog

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion //NLog

        // Reusable XmlSerializer for VD response parsing (per-transaction hot path).
        // XmlSerializer instances from the plain XmlSerializer(Type) ctor are thread-safe for Serialize/Deserialize.
        private static readonly XmlSerializer s_vdResponseSerializer = new XmlSerializer(typeof(VDMessage.ResponseXML));

        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private static readonly SingletonOfMQServerInfo SingletonOfMQServerInfo = SingletonOfMQServerInfo.Instance;

        /// <summary>
        /// Process Transaction Flow
        /// </summary>
        /// <param name="requestSvcsMessage">SVCS Message Data</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        /// SessionInfoStruct _sessionInfo
        public static byte[] ProcessTransactionFlow(SVCS requestSvcsMessage,
                                                    string localEndPoint,
                                                    string txKeySeq)
        {
            try
            {
                byte[] respBytes;
                switch (requestSvcsMessage.Mti())
                {
                    case "0100":
                        respBytes = ProcessSVCS0100(requestSvcsMessage, localEndPoint);
                        break;
                    case "0120":
                    case "0121":
                        respBytes = ProcessSVCS0120(requestSvcsMessage, localEndPoint);
                        break;
                    case "0200":
                        respBytes = ProcessSVCS0100(requestSvcsMessage, localEndPoint);
                        break;
                    case "0220":
                    case "0221":
                        respBytes = ProcessSVCS0120(requestSvcsMessage, localEndPoint);
                        break;
                    case "0312":
                        ProcessSVCS0312(requestSvcsMessage, localEndPoint, txKeySeq);
                        return null;
                    case "0420":
                    case "0421":
                        respBytes = ProcessSVCS0420(requestSvcsMessage, localEndPoint);
                        break;
                    case "0800":
                    case "0820":
                    case "0821":
                        respBytes = ProcessSVCS0800(requestSvcsMessage, localEndPoint);
                        break;
                    case "0810":
                        ProcessSVCS0810(requestSvcsMessage, localEndPoint, txKeySeq);
                        return null;
                    default:
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                            new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                            "Message Type No Match : " + requestSvcsMessage.Mti(),
                            "Message Type No Match");
                        return null;
                }

                if (respBytes == null)
                {
                    Log.Fatal(
                        $"SvcsAuthFlowUtility.ProcessTransactionFlow Message Type {requestSvcsMessage.Mti()}, Return Data Is Null");
                }
                else
                {
                    Log.Info($"SvcsAuthFlowUtility.ProcessTransactionFlow Message Type {requestSvcsMessage.Mti()}");
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
                Log.Fatal(ex, $"SvcsAuthFlowUtility.ProcessTransactionFlow Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ProcessSVCS0200 Process 0200 Authorization Flow
        /// </summary>
        /// <param>Session Name</param>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessSVCS0100(SVCS requestSvcsMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos))
                              ? requestSvcsMessage.RawVal(fieldPos).ToString()
                              : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0100 Start,Header[{requestSvcsMessage.Header().Encode()}],Type[{requestSvcsMessage.Mti()}],PCode[{pCodeString}]");
            SVCS responseSvcsMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestSvcsMessage);

            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestSvcsMessage);
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
                Log.Fatal("ProcessSVCS0100 Return String Is Null");
                responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessSVCS0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                }
                else
                {
                    responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, authReturnString).GetSvcsMessage();
                }
            }

            if (responseSvcsMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessSVCS0100",
                          "ProcessSVCS0100 Response SVCS Message Return Is Null");

                responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                authReturnString = null;
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseSvcsMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }

                return (responseSvcsMessage.Encode().Val());
            }
            if (ColaCommUtility.IsVDCard(authReturnString[02]) &&
                authReturnString[01].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])) &&
                (!string.IsNullOrEmpty(authReturnString[06])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessSVCS0100VDLoading(authInfoData, authReturnString, localEndPoint, requestSvcsMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                           SystemConstantData.CommonData.MessageSendDirection,
                                                           localEndPoint,
                                                           responseSvcsMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseSvcsMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessSVCS0120 Process 0220 Authorization Flow For SVCS
        /// </summary>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessSVCS0120(SVCS requestSvcsMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos))
                ? requestSvcsMessage.RawVal(fieldPos).ToString()
                : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0120 Start,Header[{requestSvcsMessage.Header().Encode()}],Type[{requestSvcsMessage.Mti()}],PCode[{pCodeString}]");
            SVCS responseSvcsMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestSvcsMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestSvcsMessage);
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
                responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessSVCS0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                }
                else
                {
                    responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, authReturnString).GetSvcsMessage();
                }
            }

            if (responseSvcsMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessSVCS0120",
                          "ProcessSVCS0120 Response SVCS Message Return Is Null");

                responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                authReturnString = null;
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseSvcsMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }

                return (responseSvcsMessage.Encode().Val());
            }
            if (ColaCommUtility.IsVDCard(authReturnString[02]) &&
                authReturnString[01].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])) &&
                (!string.IsNullOrEmpty(authReturnString[06])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessSVCS0120VDLoading(authInfoData, authReturnString, localEndPoint, requestSvcsMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                           SystemConstantData.CommonData.MessageSendDirection,
                                                           localEndPoint,
                                                           responseSvcsMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseSvcsMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessSVCS0312 Process File Maintenance Management(0310 Process)
        /// </summary>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessSVCS0312(SVCS requestSvcsMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Fatal("ProcessSVCS0312 txKeySeq Invalid No Write Log");
                return;
            }
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)) ? requestSvcsMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0312 Start,Header[{requestSvcsMessage.Header().Encode()}],Type[{requestSvcsMessage.Mti()}],PCode[{pCodeString}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessSVCS0420 Process 0420 Authorization Flow
        /// </summary>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessSVCS0420(SVCS requestSvcsMessage,
                                              string localEndPoint)
        {
            const int returnArrayCount = 7;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestSvcsMessage.RawVal(fieldPos)) ? requestSvcsMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0420 Start,Header[{requestSvcsMessage.Header().Encode()}],Type[{requestSvcsMessage.Mti()}],PCode[{pCodeString}]");
            SVCS responseSvcsMessage;
            var authInfoData = new EsvcAuthInfoStruct(requestSvcsMessage);
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestSvcsMessage);
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
                responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            else
            {
                if (authReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"ProcessSVCS0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{authReturnString.Length}]");
                    responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                }
                else
                {
                    responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, authReturnString[0]).GetSvcsMessage();
                }
            }
            if (responseSvcsMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessSVCS0420",
                          "ProcessSVCS0420 Response SVCS Message Return Is Null");

                responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                authReturnString = null;
            }
            //update flow for VD
            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseSvcsMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseSvcsMessage.Encode().Val());
            }
            if (ColaCommUtility.IsVDCard(authReturnString[02]) &&
                authReturnString[01].Trim().Equals("A") &&
                (!string.IsNullOrEmpty(authReturnString[03])) &&
                (!string.IsNullOrEmpty(authReturnString[04])) &&
                (!string.IsNullOrEmpty(authReturnString[05])) &&
                (!string.IsNullOrEmpty(authReturnString[06])))
            {
                //process VD , add data to transaction database
                var returnBytes = ProcessSVCS0420VDLoading(authInfoData, authReturnString, localEndPoint, requestSvcsMessage);
                return returnBytes;
            }
            else
            {
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                           SystemConstantData.CommonData.MessageSendDirection,
                                                           localEndPoint,
                                                           responseSvcsMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
                return (responseSvcsMessage.Encode().Val());
            }
        }

        /// <summary>
        /// ProcessSVCS0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static byte[] ProcessSVCS0800(SVCS requestSvcsMessage,
                                              string localEndPoint)
        {
            string informationCode;
            FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(field)))
            {
                informationCode = (string)requestSvcsMessage.Core().Field(field);
                if (informationCode.Equals(SvcsTransactionConst.NetworkManagementInformationCode.SignOff))
                    SingletonOfCommonProperty.Instance.SetColaSignOff(SystemConstantData.CommonData.PasystfSVCSInfo);
                else if (informationCode.Equals(SvcsTransactionConst.NetworkManagementInformationCode.SignOn))
                    SingletonOfCommonProperty.Instance.SetColaSignOn(SystemConstantData.CommonData.PasystfSVCSInfo);
            }
            else
                informationCode = string.Empty;

            // 01 --Outbound key(Visa:IWK)  NCCC -> Bank
            // 02 --Inbound key (Visa:AWK)
            // 03 --both incoming and outgoing key
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0800 Start,Header[{requestSvcsMessage.Header().Encode()}],Information Code[{informationCode}]");
            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                requestSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }

            //Add Send Transaction Log
            var responseSvcsMessage = new SvcsMessage0800(requestSvcsMessage).GetSvcsMessage();
            responseSvcsMessage.Header().Decode($"{requestSvcsMessage.Header().SrcID()}{requestSvcsMessage.Header().DestID()}");
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseSvcsMessage.Encode().Val());
        }

        /// <summary>
        /// ProcessSVCS0810 Process Network Management(0810 Process)
        /// </summary>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        private static void ProcessSVCS0810(SVCS requestSvcsMessage,
                                            string localEndPoint,
                                            string txKeySeq)
        {
            if (string.IsNullOrEmpty(txKeySeq))
            {
                Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0810 txKeySeq inValid]");
                return;
            }
            string informationCode;
            const FIELD field = FIELD.F_70;
            if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(field)))
            {
                informationCode = (string)requestSvcsMessage.Core().Field(field);
            }
            else
                informationCode = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCS0810 Start,Header[{requestSvcsMessage.Header().Encode()}],Information Code[{informationCode}]");
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
        }

        /// <summary>
        /// ProcessSVCSErrorProcess Error Flow
        /// </summary>
        /// <param name="localEndPoint">Local End Point</param>
        /// <param name="requestSvcsMessage">Request SVCS Message</param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessSVCSError(string localEndPoint,
                                                SVCS requestSvcsMessage)
        {
            string pCodeString;
            const FIELD field = FIELD.F_3;
            if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(field)))
            {
                pCodeString = (string)requestSvcsMessage.Core().Field(field);
            }
            else
                pCodeString = string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessSVCSError Start,Header[{requestSvcsMessage.Header().Encode()}],Type[{requestSvcsMessage.Mti()}],PCode[{pCodeString}]");
            string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            //Add Receive Transaction Log
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            //Add Send Transaction Log
            Log.Warn($"ProcessSVCSError Response Code = {SystemConstantData.CommonData.SystemProcessErrorCode}");
            var responseSvcsMessage = new SvcsMessageBase(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            if (responseSvcsMessage == null)
            {
                Log.Fatal("Create Response SVCS Message Is Null");
                return null;
            }
            spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                        SystemConstantData.CommonData.MessageSendDirection,
                                                        localEndPoint,
                                                        responseSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseSvcsMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the SVCS0100 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestSvcsMessage">The request SVCS message.</param>
        /// <returns></returns>
        private static byte[] ProcessSVCS0100VDLoading(EsvcAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, SVCS requestSvcsMessage)
        {
            SVCS responseSvcsMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();

                return (responseSvcsMessage.Encode().Val());
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
            string[] secondAuthReturnString = StoreProcUtility.Esvc0100AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessSVCS0100 Return String Is Null");
                responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessSVCS0100 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                }
                else
                {
                    responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, secondAuthReturnString).GetSvcsMessage();
                }
            }
            if (responseSvcsMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessSVCS0100",
                          "Second ProcessSVCS0100 Response BIC Message Return Is Null");

                responseSvcsMessage = new SvcsMessage0100(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseSvcsMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the SVCS0120 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestSvcsMessage">The request SVCS message.</param>
        /// <returns></returns>
        private static byte[] ProcessSVCS0120VDLoading(EsvcAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, SVCS requestSvcsMessage)
        {
            SVCS responseSvcsMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                return (responseSvcsMessage.Encode().Val());
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
            var secondAuthReturnString = StoreProcUtility.Esvc0120AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessSVCS0120 Return String Is Null");
                responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessSVCS0120 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                }
                else
                {
                    responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, secondAuthReturnString).GetSvcsMessage();
                }
            }
            if (responseSvcsMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessSVCS0120",
                          "Second ProcessSVCS0120 Response BIC Message Return Is Null");

                responseSvcsMessage = new SvcsMessage0120(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseSvcsMessage.Encode().Val());
        }

        /// <summary>
        /// Processes the SVCS0420 VD loading.
        /// </summary>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="firstAuthReturnString">The first authentication return string.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="requestSvcsMessage">The request SVCS message.</param>
        /// <returns></returns>
        private static byte[] ProcessSVCS0420VDLoading(EsvcAuthInfoStruct authInfoData, string[] firstAuthReturnString, string localEndPoint, SVCS requestSvcsMessage)
        {
            SVCS responseSvcsMessage;
            VDMessage.RequestXML vdRequestXML = ColaCommUtility.CreteVDMQRequestMessage(authInfoData, firstAuthReturnString);
            if (vdRequestXML == null)
            {
                Log.Warn($"CreteVDMQRequestMessage process error");
                responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                return (responseSvcsMessage.Encode().Val());
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
            var secondAuthReturnString = StoreProcUtility.Esvc0420AuthProcess(authInfoData, firstAuthReturnString, responseXML);

            if (secondAuthReturnString == null)  //The Store Procedure Process Error
            {
                Log.Fatal("Second ProcessNCCC0420 Return String Is Null");
                responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            else
            {
                if (secondAuthReturnString.Length != returnArrayCount)
                {
                    Log.Fatal($"Second ProcessNCCC0420 Return String Array Count Error,Define[{returnArrayCount}], Return[{secondAuthReturnString.Length}]");
                    responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
                }
                else
                {
                    responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, secondAuthReturnString[0]).GetSvcsMessage();
                }
            }
            if (responseSvcsMessage == null)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Second ProcessNCCC0420",
                          "Second ProcessNCCC0420 Response BIC Message Return Is Null");

                responseSvcsMessage = new SvcsMessage0420(requestSvcsMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetSvcsMessage();
            }
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseSvcsMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
            }
            return (responseSvcsMessage.Encode().Val());
        }
    }
}

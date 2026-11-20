using System;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class QRCodeAuthFlowUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();

        public static string[] ProcessQRCode0200(NcccIso8583 requestMessage,
                                                  FCB9GQ.FCB9GQ_REQ reqJSONObject,
                                                  string localEndPoint,
                                                  string txKeySeq)





        {
            const int returnArrayCount = 4;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestMessage.RawVal(fieldPos)) ? requestMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessQRCode0200 Start,Header[{requestMessage.Header().Encode()}],Type[{requestMessage.Mti()}],PCode[{pCodeString}]");

            var authInfoData = new NcccAuthInfoStruct(requestMessage, reqJSONObject, txKeySeq);
            //Add Receive Transaction Log
            var spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            requestMessage);
            if (spRetnCode != 0)
            {
                Log.Fatal("Insert Cola_Tran_Log Error,Return Code = [{0}]", spRetnCode);
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.QR0200AuthProcess(authInfoData);

            // Return String Arrays Information
            // authReturnString[00]:@isoP39Rsp         , authReturnString[01]:@isoP39RspAction
            // authReturnString[02]:@disPatchSwitch    
            // Description
            // @isoP35ExpirDte              varchar(04)     --P35 有效期限
            // @isoP39Rsp                   varchar(02)     --p39
            // @isoP39RspAction             varchar(01)     --對映於P39的授權回覆動作 A/D/R/P
            // @disPatchSwitch              varchar(01)     --依回覆值傳送對映單位

            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                Log.Warn("ProcessQRCode0200 Return Is Null Or Return Array Count Error");
                var responseMessage = new NcccMessage0200(requestMessage, SystemConstantData.CommonData.SystemProcessErrorCode).GetNcccMessage();
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            responseMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal("Insert Cola_Tran_Log Error,Return Code = [{0}]", spRetnCode);
                }

            }
            else
            {
                //update expired date data (F35 and Token S8)
                Log.Debug($"Before EAICommUtility.UpdateExpiredDate[{requestMessage.RawVal(FIELD.F_35).ToString()}]");
                EAICommUtility.UpdateExpiredDate(ref requestMessage, reqJSONObject, authReturnString);
                Log.Debug($"After EAICommUtility.UpdateExpiredDate[{requestMessage.RawVal(FIELD.F_35).ToString()}]");

                if (!authReturnString[2].Equals("A") || !authReturnString[1].Equals("00"))
                {
                    Log.Info($"ProcessQRCode0200 No Process Push Payment, @isoP35ExpirDte[{authReturnString[0]}], @isoP39RspAction[{authReturnString[2]}");
                    var responseMessage = new NcccMessage0200(requestMessage, authReturnString[1]).GetNcccMessage();
                    spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                responseMessage);
                    if (spRetnCode != 0)
                    {
                        Log.Fatal("Insert Cola_Tran_Log Error,Return Code = [{0}]", spRetnCode);
                    }
                }
            }
            return authReturnString;
        }


        /// <summary>
        /// ProcessQRCodeTxUpdate Process QRCode Update Transaction
        /// </summary>
        /// <param name="requestMessage">Request BIC Message</param>
        /// <param name="responseMessage">Response BIC Message</param>
        /// <param name="reqJSONObject"></param>
        /// <param name="localEndPoint"></param>
        /// <param name="txKeySeq"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static string[] ProcessQRCodeTxUpdate(NcccIso8583 requestMessage,
                                                     NcccIso8583 responseMessage,
                                                     FCB9GQ.FCB9GQ_REQ reqJSONObject,
                                                     string localEndPoint,
                                                     string txKeySeq)
        {
            const int returnArrayCount = 3;
            var fieldPos = FIELD.F_3;
            var pCodeString = !ObjectUtils.IsNull(requestMessage.RawVal(fieldPos)) ? requestMessage.RawVal(fieldPos).ToString() : string.Empty;
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessQRCodeTxUpdate Start,Header[{requestMessage.Header().Encode()}],Type[{requestMessage.Mti()}],PCode[{pCodeString}]");
            NcccAuthInfoStruct authInfoData;
            int spRetnCode;
            if (responseMessage != null)
            {
                authInfoData = new NcccAuthInfoStruct(responseMessage, reqJSONObject, txKeySeq);
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            responseMessage);
            }
            else
            {
                var response = new NcccMessage0200(requestMessage, SystemConstantData.CommonData.SystemProcessTimeout).GetNcccMessage();
                authInfoData = new NcccAuthInfoStruct(response, reqJSONObject, txKeySeq);
                spRetnCode = StoreProcUtility.InsertTranLog(authInfoData.tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            response);
            }
            if (spRetnCode != 0)
            {
                Log.Fatal("Insert Cola_Tran_Log Error,Return Code = [{0}]", spRetnCode);
            }

            //Call Authorization Store Procedure
            string[] authReturnString = StoreProcUtility.QR0200UpdTxAuthProcess(authInfoData);

            // Return String Arrays Information
            // authReturnString[00]:@isoP38AuthCode    , authReturnString[01]:@isoP39Rsp
            // authReturnString[02]:@TokenProvider
            // Description
            // @isoP38AuthCode              varchar(06)     --p38
            // @isoP39Rsp                   varchar(02)     --p39
            // @isoP39RspAction             varchar(01)     --對映於P39的授權回覆動作 A/ D / R / P

            if ((authReturnString == null) || (authReturnString.Length != returnArrayCount))
            {
                Log.Warn("ProcessQRCodeTxUpdate Return Is Null Or Return Array Count Error");
            }
            return authReturnString;
        }

        /// <summary>
        /// ProcessNCCC0500 Process Reconciliation Process(05XX Process)
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessNCCC0500(NcccIso8583 requestNcccMessage,
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
        /// ProcessNCCC0800 Process Network Management(08XX Process)
        /// </summary>
        /// <param name="requestNcccMessage">Request BIC Message</param>
        /// <param name="localEndPoint"></param>
        /// <returns>
        /// return Response Byte Array
        /// </returns>
        public static byte[] ProcessNCCC0800(NcccIso8583 requestNcccMessage,
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
            int spRetnCode;
            if (!informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.EchoTest))
            {
                spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageReceiveDirection,
                                                            localEndPoint,
                                                            requestNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
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

            if (!informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.EchoTest))
            {

                spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            localEndPoint,
                                                            responseNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
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
        public static void ProcessNCCC0810(NcccIso8583 requestNcccMessage,
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
            if (!informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.EchoTest))
            {
                int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                                SystemConstantData.CommonData.MessageReceiveDirection,
                                                                localEndPoint,
                                                                requestNcccMessage);
                if (spRetnCode != 0)
                {
                    Log.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }
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
            Log.Info($"LocalEndPoint[{localEndPoint}],ProcessError Start,Header[{requestNcccMessage.Header().Encode()}],Type[{requestNcccMessage.Mti()}],PCode[{pCodeString}]");
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
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.FiscMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class FiscMessage0120
    {

        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> FiscMessageTypeMapInfo = SingletonOfFiscMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly FISC responseFiscMessage;
        private readonly MessageType fiscMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public FiscMessage0120()
        {
            responseFiscMessage = null;
            FiscMessageTypeMapInfo.TryGetValue("0120", out fiscMessageType);
        }
        public FiscMessage0120(FISC requestFiscMessage, string responseCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120 MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = null;
                    return;
                }

                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);

                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                                default:
                                    responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0120 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        public FiscMessage0120(FISC requestFiscMessage, string responseCode, string approveCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120 MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = null;
                    return;
                }

                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);

                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {

                            switch (intFieldPos)
                            {
                                case 38:
                                    if (responseCode.Equals("00"))
                                    {
                                        responseFiscMessage.Core().Field(field, approveCode);
                                    }
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                                default:
                                    responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {

                            switch (intFieldPos)
                            {
                                case 15:
                                    responseFiscMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfFISCInfo));
                                    break;
                                case 38:
                                    responseFiscMessage.Core().Field(field, approveCode);
                                    if (responseCode.Equals("00"))
                                    {
                                        responseFiscMessage.Core().Field(field, approveCode);
                                    }
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                    Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0120 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        /// <summary>
        /// FiscMessage0120 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction       , authReturnString[03]:@vdLinkNbr
        /// authReturnString[04]:@vdLinkNbrSeq          , authReturnString[05]:@vdActionCode
        /// authReturnString[06]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	 varchar(06) 	 Output	 --p38
        /// @isoP39Rsp               	 varchar(02) 	 Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	         varchar(01)	 Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
        /// @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                  varchar(15)	 Output   VD 圈存解圈金額
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0120"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public FiscMessage0120(FISC requestFiscMessage, string[] authReturnString)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120 MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = null;
                    return;
                }

                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);

                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {

                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                default:
                                    responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {

                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }

                    Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0120 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        /// <summary>
        /// FiscMessage0120 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction       , authReturnString[03]:@vdLinkNbr
        /// authReturnString[04]:@vdLinkNbrSeq          , authReturnString[05]:@vdActionCode
        /// authReturnString[06]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	 varchar(06) 	 Output	 --p38
        /// @isoP39Rsp               	 varchar(02) 	 Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	         varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
        /// @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                  varchar(15)	 Output   VD 圈存解圈金額
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0120"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="authInfoData">The request FiscAuthInfoStruct layout.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public FiscMessage0120(FISC requestFiscMessage, FiscAuthInfoStruct authInfoData, string[] authReturnString)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120 MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = null;
                    return;
                }

                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);

                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {

                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                default:
                                    responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {

                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }

                    Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0120 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        public FISC GetFiscMessage()
        {
            return (responseFiscMessage);
        }
    }
}

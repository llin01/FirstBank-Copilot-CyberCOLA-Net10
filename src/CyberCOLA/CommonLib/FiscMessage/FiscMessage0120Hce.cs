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
    public class FiscMessage0120Hce
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
        public FiscMessage0120Hce()
        {
            responseFiscMessage = null;
            FiscMessageTypeMapInfo.TryGetValue("0120", out fiscMessageType);
        }
        public FiscMessage0120Hce(FISC requestFiscMessage, string responseCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120Hce MessageType Error:{requestFiscMessage.Mti()}");
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
                Log.Fatal(ex, $"FiscMessage0120Hce Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        public FiscMessage0120Hce(FISC requestFiscMessage, string responseCode, string approveCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120Hce MessageType Error:{requestFiscMessage.Mti()}");
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
                Log.Fatal(ex, $"FiscMessage0120Hce Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        /// <summary>
        /// FiscMessage0120Hce Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction       
        /// Description
        /// @isoP38AuthCode          	 varchar(06) 	 Output	 --p38
        /// @isoP39Rsp               	 varchar(02) 	 Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	         varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0120Hce"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public FiscMessage0120Hce(FISC requestFiscMessage, string[] authReturnString)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120Hce MessageType Error:{requestFiscMessage.Mti()}");
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
                Log.Fatal(ex, $"FiscMessage0120Hce Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        /// <summary>
        /// FiscMessage0120Hce Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode        , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction       
        /// Description
        /// @isoP38AuthCode          	 varchar(06) 	 Output	 --p38
        /// @isoP39Rsp               	 varchar(02) 	 Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	         varchar(01)	 Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0120Hce"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="authInfoData">The request FiscAuthInfoStruct layout.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public FiscMessage0120Hce(FISC requestFiscMessage, FiscAuthInfoStruct authInfoData, string[] authReturnString)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0120Hce MessageType Error:{requestFiscMessage.Mti()}");
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
                Log.Fatal(ex, $"FiscMessage0120Hce Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        public FISC GetFiscMessage()
        {
            return (responseFiscMessage);
        }
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class NcccMessage0200TknEvent
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private NcccIso8583 responseNcccMessage;
        private readonly MessageType ncccMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public NcccMessage0200TknEvent()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0200", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknOtp"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0200TknEvent(NcccIso8583 requestNcccMessage,
                                        string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknEvent MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestNcccMessage.Header().Encode()}],Response Header[{responseNcccMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Message0200 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknEvent"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="approveCode">The approve code.</param>
        public NcccMessage0200TknEvent(NcccIso8583 requestNcccMessage,
                                        string responseCode,
                                        string approveCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknEvent MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 38:
                                    if (responseCode.Equals("00"))
                                    {
                                        responseNcccMessage.Core().Field(field, approveCode);
                                    }
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0200TknEvent Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// NcccMessage0200TknEvent Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@otpSend             , authReturnString[03]:@tokenMsg
        /// authReturnString[04]:@custName            , authReturnString[05]:@cellPhone
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @otpSend                    varchar(01) 	Output	 簡訊使用
        /// @tokenMsg       	        varchar(100) 	Output	 簡訊使用
        /// @custName      	            varchar(20) 	Output	 簡訊使用
        /// @cellPhone      	        varchar(20) 	Output	 簡訊使用
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknEvent"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public NcccMessage0200TknEvent(NcccIso8583 requestNcccMessage,
                                        string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknEvent MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 38:
                                    if (authReturnString[1].Equals("00"))
                                    {
                                        responseNcccMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestNcccMessage.Header().Encode()}],Response Header[{responseNcccMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0200TknEvent Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// NcccMessage0200TknEvent Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@otpSend             , authReturnString[03]:@tokenMsg
        /// authReturnString[04]:@custName            , authReturnString[05]:@cellPhone
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @otpSend                    varchar(01) 	Output	 簡訊使用
        /// @tokenMsg       	        varchar(100) 	Output	 簡訊使用
        /// @custName      	            varchar(20) 	Output	 簡訊使用
        /// @cellPhone      	        varchar(20) 	Output	 簡訊使用
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public NcccMessage0200TknEvent(NcccIso8583 requestNcccMessage,
                                        NcccAuthInfoStruct authInfoData,
                                        string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknEvent MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                default:
                                    responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 38:
                                    responseNcccMessage.Core().Field(field, authReturnString[0]);
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestNcccMessage.Header().Encode()}],Response Header[{responseNcccMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0200TknEvent Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        public NcccIso8583 GetNcccMessage()
        {
            return responseNcccMessage;
        }
    }
}

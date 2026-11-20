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
    public class MessageBase
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly NcccIso8583 responseNcccMessage;
        private readonly MessageType ncccMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public MessageBase()
        {
            responseNcccMessage = null;
            ncccMessageType = null;
        }
        public MessageBase(NcccIso8583 requestNcccMessage, string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get MessageBase MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = new NcccIso8583(requestNcccMessage.Encode().Val());
                    var requestMTI = requestNcccMessage.Mti();
                    responseNcccMessage.Mti($"{requestMTI.Substring(0, 2)}{Convert.ToInt16(requestMTI.Substring(2, 1)) + 1}{requestMTI.Substring(3, 1)}");
                    responseNcccMessage.Core().Field(FIELD.F_39, SystemConstantData.CommonData.SystemProcessErrorCode);
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
                            if (intFieldPos == 39)
                            {
                                responseNcccMessage.Core().Field(field, responseCode);
                            }
                            else
                            {
                                responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 15)
                            {
                                responseNcccMessage.Core().Field(field, DateTime.Now.ToString("MMdd"));
                            }
                        }
                    }
                }
                FIELD fieldData = FIELD.F_39;
                if (ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldData)))
                {
                    responseNcccMessage.Core().Field(fieldData, responseCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"MessageBase Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public MessageBase(NcccIso8583 requestNcccMessage, string responseCode, string approveCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get MessageBase MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = new NcccIso8583(requestNcccMessage.Encode().Val());
                    var requestMTI = requestNcccMessage.Mti();
                    responseNcccMessage.Mti($"{requestMTI.Substring(0, 2)}{Convert.ToInt16(requestMTI.Substring(2, 1)) + 1}{requestMTI.Substring(3, 1)}");
                    responseNcccMessage.Core().Field(FIELD.F_39, SystemConstantData.CommonData.SystemProcessErrorCode);
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
                                case 38:
                                    responseNcccMessage.Core().Field(field, approveCode);
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, responseCode);
                                    break;
                                default:
                                    responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    break;
                            }
                        }
                    }
                }
                FIELD fieldData = FIELD.F_38;
                if (ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldData)))
                {
                    responseNcccMessage.Core().Field(fieldData, approveCode);
                }
                fieldData = FIELD.F_39;
                if (ObjectUtils.IsNull(requestNcccMessage.RawVal(fieldData)))
                {
                    responseNcccMessage.Core().Field(fieldData, responseCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"MessageBase Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public NcccIso8583 GetNcccMessage()
        {
            return (responseNcccMessage);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.TM.ISO8583;
using NLog;

namespace Com.Cybersoft4u.CommonLib.TwinMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class TwinMessageBase
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, TwinMessage.MessageType> TwinMessageTypeMapInfo = SingletonOfTwinMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly TWINIso8583 responseTwinMessage;
        private readonly TwinMessage.MessageType twinMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public TwinMessageBase()
        {
            responseTwinMessage = null;
            twinMessageType = null;
        }
        public TwinMessageBase(TWINIso8583 requestTwinMessage, string responseCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessageBase MessageType Error:{requestTwinMessage.Mti()}");
                    responseTwinMessage = new TWINIso8583(requestTwinMessage.Encode().Val());
                    var requestMTI = requestTwinMessage.Mti();
                    responseTwinMessage.Mti($"{requestMTI.Substring(0, 2)}{Convert.ToInt16(requestMTI.Substring(2, 1)) + 1}{requestMTI.Substring(3, 1)}");
                    responseTwinMessage.Core().Field(FIELD.F_39, SystemConstantData.CommonData.SystemProcessErrorCode);
                    return;
                }
                responseTwinMessage = new TWINIso8583();
                responseTwinMessage.Header().Decode(requestTwinMessage.Header().Encode());
                responseTwinMessage.Mti(twinMessageType.ResponseMessage);
                foreach (var message in twinMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                        {
                            if (intFieldPos == 39)
                            {
                                responseTwinMessage.Core().Field(field, responseCode);
                            }
                            else
                            {
                                responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 15)
                            {
                                responseTwinMessage.Core().Field(field, DateTime.Now.ToString("MMdd"));
                            }
                        }
                    }
                }
                FIELD fieldData = FIELD.F_39;
                if (ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldData)))
                {
                    responseTwinMessage.Core().Field(fieldData, responseCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessageBase Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TwinMessageBase(TWINIso8583 requestTwinMessage, string responseCode, string approveCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessageBase MessageType Error:{requestTwinMessage.Mti()}");
                    responseTwinMessage = new TWINIso8583(requestTwinMessage.Encode().Val());
                    var requestMTI = requestTwinMessage.Mti();
                    responseTwinMessage.Mti($"{requestMTI.Substring(0, 2)}{Convert.ToInt16(requestMTI.Substring(2, 1)) + 1}{requestMTI.Substring(3, 1)}");
                    responseTwinMessage.Core().Field(FIELD.F_39, SystemConstantData.CommonData.SystemProcessErrorCode);
                    return;
                }
                responseTwinMessage = new TWINIso8583();
                responseTwinMessage.Header().Decode(requestTwinMessage.Header().Encode());
                responseTwinMessage.Mti(twinMessageType.ResponseMessage);

                foreach (var message in twinMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                        {
                            switch (intFieldPos)
                            {
                                case 38:
                                    responseTwinMessage.Core().Field(field, approveCode);
                                    break;
                                case 39:
                                    responseTwinMessage.Core().Field(field, responseCode);
                                    break;
                                default:
                                    responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    break;
                            }
                        }
                    }
                }
                FIELD fieldData = FIELD.F_38;
                if (ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldData)))
                {
                    responseTwinMessage.Core().Field(fieldData, approveCode);
                }
                fieldData = FIELD.F_39;
                if (ObjectUtils.IsNull(requestTwinMessage.RawVal(fieldData)))
                {
                    responseTwinMessage.Core().Field(fieldData, responseCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessageBase Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TWINIso8583 GetTwinMessage()
        {
            return (responseTwinMessage);
        }
    }
}

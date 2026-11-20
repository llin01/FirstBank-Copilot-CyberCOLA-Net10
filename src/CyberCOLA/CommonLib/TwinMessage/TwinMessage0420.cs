using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.TwinMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class TwinMessage0420
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog


        private static readonly ConcurrentDictionary<string, MessageType> TwinMessageTypeMapInfo = SingletonOfTwinMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private TWINIso8583 responseTwinMessage;
        private readonly MessageType twinMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public TwinMessage0420()
        {
            responseTwinMessage = null;
            TwinMessageTypeMapInfo.TryGetValue("0420", out twinMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0420"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="responseCode">The response code.</param>
        public TwinMessage0420(TWINIso8583 requestTwinMessage,
                                string responseCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0420 MessageType Error:{requestTwinMessage.Mti()}");
                    responseTwinMessage = null;
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
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0420 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0420"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWMP message.</param>
        /// <param name="authInfoData"></param>
        /// <param name="authReturnString"></param>
        public TwinMessage0420(TWINIso8583 requestTwinMessage,
                               NcccAuthInfoStruct authInfoData,
                               string[] authReturnString)
        {
            const int fieldS127Index = 1;
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0420 MessageType Error:{requestTwinMessage.Mti()}");
                    responseTwinMessage = null;
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
                                case 39:
                                    responseTwinMessage.Core().Field(field, authReturnString[0]);
                                    break;
                                case 127:
                                    if (authReturnString.Length == (fieldS127Index + 1))  //for TWMP
                                    {
                                        if (!string.IsNullOrEmpty(authReturnString[fieldS127Index]))
                                        {
                                            responseTwinMessage.Core().Field(field, $"{authInfoData.isoP2CardNbr}{authReturnString[fieldS127Index]}");
                                        }
                                        else
                                        {
                                            responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                        }
                                    }
                                    else
                                    {
                                        responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    }
                                    break;
                                default:
                                    responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 127:
                                    if (authReturnString.Length == (fieldS127Index + 1))  //for TWMP
                                    {
                                        if (!string.IsNullOrEmpty(authReturnString[fieldS127Index]))
                                        {
                                            responseTwinMessage.Core().Field(field, $"{authInfoData.isoP2CardNbr}{authReturnString[fieldS127Index]}");
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0420 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TWINIso8583 GetTwinMessage()
        {
            return (responseTwinMessage);
        }
    }
}

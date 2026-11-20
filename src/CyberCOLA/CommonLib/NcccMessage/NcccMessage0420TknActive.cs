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
    public class NcccMessage0420TknActive
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
        public NcccMessage0420TknActive()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0420", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0420TknActive"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0420TknActive(NcccIso8583 requestNcccMessage,
                                        string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0420TknActive MessageType Error:{requestNcccMessage.Mti()}");
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
                                    responseNcccMessage.Core().Field(field, responseCode);
                                    break;
                                default:
                                    responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0420TknActive Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0420TknActive"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request TWMP message.</param>
        /// <param name="authInfoData"></param>
        /// <param name="authReturnString"></param>
        public NcccMessage0420TknActive(NcccIso8583 requestNcccMessage,
                                        NcccAuthInfoStruct authInfoData,
                                        string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0420TknActive MessageType Error:{requestNcccMessage.Mti()}");
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
                                    responseNcccMessage.Core().Field(field, authReturnString[0]);
                                    break;
                                default:
                                    responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0420TknActive Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public NcccIso8583 GetNcccMessage()
        {
            return (responseNcccMessage);
        }
    }
}

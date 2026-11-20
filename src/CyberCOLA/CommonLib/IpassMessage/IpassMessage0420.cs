using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.IpassMessage
{
    /// <remarks>
    /// This IPASS 0420 Message Class
    /// </remarks>
    public class IpassMessage0420
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> IpassMessageTypeMapInfo = SingletonOfIpassMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly IPASS responseIpassMessage;
        private readonly MessageType ipassMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public IpassMessage0420()
        {
            responseIpassMessage = null;
            IpassMessageTypeMapInfo.TryGetValue("0420", out ipassMessageType);
        }
        public IpassMessage0420(IPASS requestIpassMessage, string responseCode)
        {
            try
            {
                IpassMessageTypeMapInfo.TryGetValue(requestIpassMessage.Mti(), out ipassMessageType);
                if (ipassMessageType == null)
                {
                    Log.Error($"Get IpassMessage0420 MessageType Error:{requestIpassMessage.Mti()}");
                    responseIpassMessage = null;
                    return;
                }

                responseIpassMessage = new IPASS();
                responseIpassMessage.Header().Decode($"{requestIpassMessage.Header().SrcID()}{requestIpassMessage.Header().DestID()}");
                responseIpassMessage.Mti(ipassMessageType.ResponseMessage);
                foreach (var message in ipassMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(field)))
                        {
                            responseIpassMessage.Core().Field(field, requestIpassMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseIpassMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                        Log.Debug($"Request Header[{requestIpassMessage.Header().Encode()}],Response Header[{responseIpassMessage.Header().Encode()}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IpassMessage0420 Exception:{ex.Message}");
                responseIpassMessage = null;
            }
        }
        public IPASS GetIpassMessage()
        {
            return (responseIpassMessage);
        }
    }
}

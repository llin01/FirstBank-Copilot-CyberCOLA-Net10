using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.IcashMessage
{
    /// <remarks>
    /// This ICASH Base Message Class For All ICASH Message
    /// </remarks>
    public class IcashMessageBase
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> IcashMessageTypeMapInfo = SingletonOfIcashMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly ICASH responseIcashMessage;
        private readonly MessageType icashMessageType;
        /// <remarks>
        /// This Message For ICASH Base Message Class
        /// </remarks>
        public IcashMessageBase()
        {
            responseIcashMessage = null;
            icashMessageType = null;
        }
        public IcashMessageBase(ICASH requestIcashMessage, string responseCode)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0100 MessageType Error:{requestIcashMessage.Mti()}");
                    responseIcashMessage = null;
                    return;
                }

                responseIcashMessage = new ICASH();
                responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
                responseIcashMessage.Mti(icashMessageType.ResponseMessage);
                foreach (var message in icashMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(field)))
                        {
                            responseIcashMessage.Core().Field(field, requestIcashMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseIcashMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestIcashMessage.Header().Encode()}],Response Header[{responseIcashMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IcashMessageBase Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        public IcashMessageBase(ICASH requestIcashMessage, string responseCode, string approveCode)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0100 MessageType Error:{requestIcashMessage.Mti()}");
                    responseIcashMessage = null;
                    return;
                }

                responseIcashMessage = new ICASH();
                responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
                responseIcashMessage.Mti(icashMessageType.ResponseMessage);
                foreach (var message in icashMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(field)))
                        {
                            responseIcashMessage.Core().Field(field, requestIcashMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 38:
                                    responseIcashMessage.Core().Field(field, approveCode);
                                    break;
                                case 39:
                                    responseIcashMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestIcashMessage.Header().Encode()}],Response Header[{responseIcashMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IcashMessageBase Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        public ICASH GetIcashMessage()
        {
            return (responseIcashMessage);
        }
    }
}

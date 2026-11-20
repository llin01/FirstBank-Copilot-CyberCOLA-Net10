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
    /// This Base Message Class For All Message
    /// </remarks>
    public class IcashMessage0140
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
        public IcashMessage0140()
        {
            responseIcashMessage = null;
            IcashMessageTypeMapInfo.TryGetValue("0140", out icashMessageType);
        }
        public IcashMessage0140(ICASH requestIcashMessage, string responseCode)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0140 MessageType Error:{requestIcashMessage.Mti()}");
                    responseIcashMessage = null;
                    return;
                }

                responseIcashMessage = new ICASH();
                responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
                responseIcashMessage.Mti(icashMessageType.ResponseMessage);
                requestIcashMessage.Core().Field(FIELD.F_3).ToString();
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
                Log.Fatal(ex, $"IcashMessage0140 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        public IcashMessage0140(ICASH requestIcashMessage, string responseCode, string approveCode)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0140 MessageType Error:{requestIcashMessage.Mti()}");
                    responseIcashMessage = null;
                    return;
                }

                responseIcashMessage = new ICASH();
                responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
                responseIcashMessage.Mti(icashMessageType.ResponseMessage);
                string processCode;
                processCode = requestIcashMessage.Core().Field(FIELD.F_3).ToString();
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
                            if (intFieldPos == 61)
                            {
                                continue;
                            }
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
                Log.Fatal(ex, $"IcashMessage0140 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }


        // authReturnString[00]:@isoP39Rsp
        public IcashMessage0140(ICASH requestIcashMessage, string[] authReturnString)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0140 MessageType Error:{requestIcashMessage.Mti()}");
                    responseIcashMessage = null;
                    return;
                }

                responseIcashMessage = new ICASH();
                responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
                responseIcashMessage.Mti(icashMessageType.ResponseMessage);
                string processCode;
                processCode = requestIcashMessage.Core().Field(FIELD.F_3).ToString();
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
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseIcashMessage.Core().Field(field, authReturnString[0]);
                                    break;
                                default:
                                    responseIcashMessage.Core().Field(field, requestIcashMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseIcashMessage.Core().Field(field, authReturnString[0]);
                                    break;
                            }
                        }
                        Log.Debug($"Request Header[{requestIcashMessage.Header().Encode()}],Response Header[{responseIcashMessage.Header().Encode()}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IcashMessage0140 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        public ICASH GetIcashMessage()
        {
            return (responseIcashMessage);
        }
    }
}

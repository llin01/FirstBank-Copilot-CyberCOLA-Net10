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
    /// <summary>
    /// Base message class for ICASH 0100 (authorization request) message type processing and response generation.
    /// </summary>
    public class IcashMessage0100
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> IcashMessageTypeMapInfo = SingletonOfIcashMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly ICASH responseIcashMessage;
        private readonly MessageType icashMessageType;

        /// <summary>
        /// Initializes a new instance of the IcashMessage0100 class with default message type.
        /// </summary>
        public IcashMessage0100()
        {
            responseIcashMessage = null;
            IcashMessageTypeMapInfo.TryGetValue("0100", out icashMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the IcashMessage0100 class and creates a response message from the request.
        /// </summary>
        /// <param name="requestIcashMessage">The incoming ICASH request message.</param>
        /// <param name="responseCode">The response code to set in field 39.</param>
        public IcashMessage0100(ICASH requestIcashMessage, string responseCode)
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
                var processCode = requestIcashMessage.Core().Field(FIELD.F_3).ToString();
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
                            if (intFieldPos == 14)
                            {
                                if (processCode != null && processCode.Equals("990172"))
                                    continue;
                            }
                            if (intFieldPos == 61)
                            {
                                if (processCode != null && processCode.Equals("990174"))
                                    continue;
                            }
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
                Log.Fatal(ex, $"IcashMessage0100 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the IcashMessage0100 class and creates a response message with approval code.
        /// </summary>
        /// <param name="requestIcashMessage">The incoming ICASH request message.</param>
        /// <param name="responseCode">The response code to set in field 39.</param>
        /// <param name="approveCode">The approval code to set in field 38.</param>
        public IcashMessage0100(ICASH requestIcashMessage, string responseCode, string approveCode)
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
                            if (intFieldPos == 14)
                            {
                                if (processCode != null && processCode.Equals("990172"))
                                    continue;
                            }
                            if (intFieldPos == 61)
                            {
                                if (processCode != null && processCode.Equals("990174"))
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
                Log.Fatal(ex, $"IcashMessage0100 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the IcashMessage0100 class and creates a response message from authorization return data.
        /// </summary>
        /// <param name="requestIcashMessage">The incoming ICASH request message.</param>
        /// <param name="authReturnString">Array of authorization return strings, where index 0 contains the response code (field 39).</param>
        public IcashMessage0100(ICASH requestIcashMessage, string[] authReturnString)
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
                            if (intFieldPos == 14)
                            {
                                if (processCode != null && processCode.Equals("990172"))
                                    continue;
                            }
                            if (intFieldPos == 61)
                            {
                                if (processCode != null && processCode.Equals("990174"))
                                    continue;
                            }

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
                    }
                }
                Log.Debug($"Request Header[{requestIcashMessage.Header().Encode()}],Response Header[{responseIcashMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IcashMessage0100 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }
        /// <summary>
        /// Gets the constructed ICASH response message.
        /// </summary>
        /// <returns>The ICASH response message, or null if construction failed.</returns>
        public ICASH GetIcashMessage()
        {
            return (responseIcashMessage);
        }
    }
}

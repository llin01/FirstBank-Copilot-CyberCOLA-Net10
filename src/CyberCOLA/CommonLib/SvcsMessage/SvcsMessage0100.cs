using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;


namespace Com.Cybersoft4u.CommonLib.SvcsMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class SvcsMessage0100
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> SvcsMessageTypeMapInfo = SingletonOfSvcsMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly SVCS responseSvcsMessage;
        private readonly MessageType svcsMessageType;

        /// <remarks>
        /// This Message For SVCS Base Message Class
        /// </remarks>
        public SvcsMessage0100()
        {
            responseSvcsMessage = null;
            SvcsMessageTypeMapInfo.TryGetValue("0100", out svcsMessageType);
        }
        public SvcsMessage0100(SVCS requestSvcsMessage, string responseCode)
        {
            try
            {
                SvcsMessageTypeMapInfo.TryGetValue(requestSvcsMessage.Mti(), out svcsMessageType);
                if (svcsMessageType == null)
                {
                    Log.Error($"Get SvcsMessage0100 MessageType Error:{requestSvcsMessage.Mti()}");
                    responseSvcsMessage = null;
                    return;
                }

                responseSvcsMessage = new SVCS();
                responseSvcsMessage.Header().Decode($"{requestSvcsMessage.Header().SrcID()}{requestSvcsMessage.Header().DestID()}");
                responseSvcsMessage.Mti(svcsMessageType.ResponseMessage);
                foreach (var message in svcsMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(field)))
                        {
                            responseSvcsMessage.Core().Field(field, requestSvcsMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseSvcsMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestSvcsMessage.Header().Encode()}],Response Header[{responseSvcsMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SvcsMessage0100 Exception:{ex.Message}");
                responseSvcsMessage = null;
            }
        }

        // authReturnString[00]:isoP39Rsp
        public SvcsMessage0100(SVCS requestSvcsMessage, string[] authReturnString)
        {
            try
            {
                SvcsMessageTypeMapInfo.TryGetValue(requestSvcsMessage.Mti(), out svcsMessageType);
                if (svcsMessageType == null)
                {
                    Log.Error($"Get SvcsMessage0100 MessageType Error:{requestSvcsMessage.Mti()}");
                    responseSvcsMessage = null;
                    return;
                }

                responseSvcsMessage = new SVCS();
                responseSvcsMessage.Header().Decode($"{requestSvcsMessage.Header().SrcID()}{requestSvcsMessage.Header().DestID()}");
                responseSvcsMessage.Mti(svcsMessageType.ResponseMessage);
                foreach (var message in svcsMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(field)))
                        {
                            responseSvcsMessage.Core().Field(field, requestSvcsMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseSvcsMessage.Core().Field(field, authReturnString[0]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestSvcsMessage.Header().Encode()}],Response Header[{responseSvcsMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SvcsMessage0100 Exception:{ex.Message}");
                responseSvcsMessage = null;
            }
        }
        public SVCS GetSvcsMessage()
        {
            return (responseSvcsMessage);
        }
    }
}

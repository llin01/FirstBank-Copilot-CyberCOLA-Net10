using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
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
    public class IcashMessage0302
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> IcashMessageTypeMapInfo = SingletonOfIcashMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly ICASH responseIcashMessage;
        private readonly MessageType icashMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public IcashMessage0302()
        {
            responseIcashMessage = null;
            IcashMessageTypeMapInfo.TryGetValue("0302", out icashMessageType);
        }
        public IcashMessage0302(ICASH requestIcashMessage, string responseCode)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0302 MessageType Error:{requestIcashMessage.Mti()}");
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
                Log.Fatal(ex, $"IcashMessage0302 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        public IcashMessage0302(FileMaintData fileMaintData)
        {
            //This Message0302 Create MTI 0302
            try
            {
                IcashMessageTypeMapInfo.TryGetValue("0302", out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error("Get IcashMessage0302 MessageType Error:0302");
                    responseIcashMessage = null;
                    return;
                }
                responseIcashMessage = new ICASH();
                //change ICASH Header
                responseIcashMessage.Header().Decode($"{IcashTransactionConst.MessageHeader.IcashIssuerID}{IcashTransactionConst.MessageHeader.IcashHostID}");
                responseIcashMessage.Mti(icashMessageType.RequestMessage);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP2CardNbr))
                {
                    responseIcashMessage.RawVal(FIELD.F_2, fileMaintData.IsoP2CardNbr);
                }
                responseIcashMessage.RawVal(FIELD.F_3, fileMaintData.IsoP3ProcessCde);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP7TxDteTime))
                {
                    responseIcashMessage.RawVal(FIELD.F_7, fileMaintData.IsoP7TxDteTime);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP11TraceNo))
                {
                    responseIcashMessage.RawVal(FIELD.F_11, fileMaintData.IsoP11TraceNo);
                }

                if (!string.IsNullOrEmpty(fileMaintData.IsoP14ExpirDte))
                {
                    responseIcashMessage.RawVal(FIELD.F_14, fileMaintData.IsoP14ExpirDte);
                }

                if (!string.IsNullOrEmpty(fileMaintData.IsoP37RefNbr))
                {
                    responseIcashMessage.RawVal(FIELD.F_37, fileMaintData.IsoP37RefNbr);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS91FileUpdCode))
                {
                    responseIcashMessage.RawVal(FIELD.F_91, fileMaintData.IsoS91FileUpdCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IcashMessage0302 Exception:{ex.Message}");
                responseIcashMessage = null;
            }
        }

        public ICASH GetIcashMessage()
        {
            return (responseIcashMessage);
        }
    }
}

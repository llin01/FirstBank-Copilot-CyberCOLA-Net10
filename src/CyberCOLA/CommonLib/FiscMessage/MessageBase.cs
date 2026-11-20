using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.FiscMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class MessageBase
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> FiscMessageTypeMapInfo = SingletonOfFiscMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly FISC responseFiscMessage;
        private readonly MessageType fiscMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public MessageBase()
        {
            responseFiscMessage = null;
            fiscMessageType = null;
        }
        public MessageBase(FISC requestFiscMessage, string responseCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get MessageBase MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = new FISC(requestFiscMessage.Encode().Val());
                    var requestMTI = requestFiscMessage.Mti();
                    responseFiscMessage.Mti($"{requestMTI.Substring(0, 2)}{Convert.ToInt16(requestMTI.Substring(2, 1)) + 1}{requestMTI.Substring(3, 1)}");
                    responseFiscMessage.Core().Field(FIELD.F_39, SystemConstantData.CommonData.SystemProcessErrorCode);
                    return;
                }
                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);
                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {
                            if (intFieldPos == 39)
                            {
                                responseFiscMessage.Core().Field(field, responseCode);
                            }
                            else
                            {
                                responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 15)
                            {
                                responseFiscMessage.Core().Field(field, DateTime.Now.ToString("MMdd"));
                            }
                        }
                    }
                }
                FIELD fieldData = FIELD.F_39;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldData)))
                {
                    responseFiscMessage.Core().Field(fieldData, responseCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"MessageBase Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        public MessageBase(FISC requestFiscMessage, string responseCode, string approveCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get MessageBase MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = new FISC(requestFiscMessage.Encode().Val());
                    var requestMTI = requestFiscMessage.Mti();
                    responseFiscMessage.Mti($"{requestMTI.Substring(0, 2)}{Convert.ToInt16(requestMTI.Substring(2, 1)) + 1}{requestMTI.Substring(3, 1)}");
                    responseFiscMessage.Core().Field(FIELD.F_39, SystemConstantData.CommonData.SystemProcessErrorCode);
                    return;
                }
                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);

                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {
                            switch (intFieldPos)
                            {
                                case 38:
                                    responseFiscMessage.Core().Field(field, approveCode);
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                                default:
                                    responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                                    break;
                            }
                        }
                    }
                }
                FIELD fieldData = FIELD.F_38;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldData)))
                {
                    responseFiscMessage.Core().Field(fieldData, approveCode);
                }
                fieldData = FIELD.F_39;
                if (ObjectUtils.IsNull(requestFiscMessage.RawVal(fieldData)))
                {
                    responseFiscMessage.Core().Field(fieldData, responseCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"MessageBase Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        public FISC GetFiscMessage()
        {
            return (responseFiscMessage);
        }
    }
}

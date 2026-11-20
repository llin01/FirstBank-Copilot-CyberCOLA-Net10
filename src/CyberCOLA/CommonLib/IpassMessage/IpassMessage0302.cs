using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.IpassMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class IpassMessage0302
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
        public IpassMessage0302()
        {
            responseIpassMessage = null;
            IpassMessageTypeMapInfo.TryGetValue("0302", out ipassMessageType);
        }
        public IpassMessage0302(IPASS requestIpassMessage, string responseCode)
        {
            try
            {
                IpassMessageTypeMapInfo.TryGetValue(requestIpassMessage.Mti(), out ipassMessageType);
                if (ipassMessageType == null)
                {
                    Log.Error($"Get IpassMessage0302 MessageType Error:{requestIpassMessage.Mti()}");
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
                    }
                }
                Log.Debug($"Request Header[{requestIpassMessage.Header().Encode()}],Response Header[{responseIpassMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IpassMessage0302 Exception:{ex.Message}");
                responseIpassMessage = null;
            }
        }

        public IpassMessage0302(FileMaintData fileMaintData)
        {
            //This Message0302 Create MTI 0302
            try
            {
                IpassMessageTypeMapInfo.TryGetValue("0302", out ipassMessageType);
                if (ipassMessageType == null)
                {
                    Log.Error("Get IpassMessage0302 MessageType Error:0302");
                    responseIpassMessage = null;
                    return;
                }
                responseIpassMessage = new IPASS();
                //change IPASS Header
                responseIpassMessage.Header().Decode($"{IpassTransactionConst.MessageHeader.IpassHostID}{IpassTransactionConst.MessageHeader.IpassIssuerID}");
                responseIpassMessage.Mti(ipassMessageType.RequestMessage);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP2CardNbr))
                {
                    responseIpassMessage.RawVal(FIELD.F_2, fileMaintData.IsoP2CardNbr);
                }
                responseIpassMessage.RawVal(FIELD.F_3, fileMaintData.IsoP3ProcessCde);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP7TxDteTime))
                {
                    responseIpassMessage.RawVal(FIELD.F_7, fileMaintData.IsoP7TxDteTime);
                }
                //IPASS P13 date is Transmission Year
                if (!string.IsNullOrEmpty(fileMaintData.IsoP13TransYear))
                {
                    responseIpassMessage.RawVal(FIELD.F_13, fileMaintData.IsoP13TransYear);
                }
                else
                {
                    var localDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                    responseIpassMessage.RawVal(FIELD.F_13, localDateTime.Substring(0, 4));
                }

                if (!string.IsNullOrEmpty(fileMaintData.IsoP14ExpirDte))
                {
                    responseIpassMessage.RawVal(FIELD.F_14, fileMaintData.IsoP14ExpirDte);
                }

                //update IPASS for new version
                if (!string.IsNullOrEmpty(fileMaintData.IsoP36NTID))
                {
                    responseIpassMessage.RawVal(FIELD.F_36, fileMaintData.IsoP36NTID);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP63SystemID))
                {
                    responseIpassMessage.RawVal(FIELD.F_63, fileMaintData.IsoP63SystemID);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS91FileUpdCode))
                {
                    responseIpassMessage.RawVal(FIELD.F_91, fileMaintData.IsoS91FileUpdCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"IpassMessage0302 Exception:{ex.Message}");
                responseIpassMessage = null;
            }
        }

        public IPASS GetIpassMessage()
        {
            return (responseIpassMessage);
        }
    }
}

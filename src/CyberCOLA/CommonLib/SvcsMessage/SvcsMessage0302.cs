using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
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
    public class SvcsMessage0302
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> SvcsMessageTypeMapInfo = SingletonOfSvcsMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly SVCS responseSvcsMessage;
        private readonly MessageType svcsMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public SvcsMessage0302()
        {
            responseSvcsMessage = null;
            SvcsMessageTypeMapInfo.TryGetValue("0302", out svcsMessageType);
        }
        public SvcsMessage0302(SVCS requestSvcsMessage, string responseCode)
        {
            try
            {
                SvcsMessageTypeMapInfo.TryGetValue(requestSvcsMessage.Mti(), out svcsMessageType);
                if (svcsMessageType == null)
                {
                    Log.Error($"Get SvcsMessage0302 MessageType Error:{requestSvcsMessage.Mti()}");
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
                Log.Fatal(ex, $"SvcsMessage0302 Exception:{ex.Message}");
                responseSvcsMessage = null;
            }
        }

        public SvcsMessage0302(FileMaintData fileMaintData)
        {
            //This Message0302 Create MTI 0302
            try
            {
                SvcsMessageTypeMapInfo.TryGetValue("0302", out svcsMessageType);
                if (svcsMessageType == null)
                {
                    Log.Error("Get SvcsMessage0302 MessageType Error:0302");
                    responseSvcsMessage = null;
                    return;
                }
                responseSvcsMessage = new SVCS();
                //change SVCS Header
                responseSvcsMessage.Header().Decode($"{SvcsTransactionConst.MessageHeader.SvcsHostID}{SvcsTransactionConst.MessageHeader.SvcsIssuerID}");
                responseSvcsMessage.Mti(svcsMessageType.RequestMessage);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP2CardNbr))
                {
                    responseSvcsMessage.RawVal(FIELD.F_2, fileMaintData.IsoP2CardNbr);
                }
                responseSvcsMessage.RawVal(FIELD.F_3, fileMaintData.IsoP3ProcessCde);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP7TxDteTime))
                {
                    responseSvcsMessage.RawVal(FIELD.F_7, fileMaintData.IsoP7TxDteTime);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP11TraceNo))
                {
                    responseSvcsMessage.RawVal(FIELD.F_11, fileMaintData.IsoP11TraceNo);
                }

                var localDateTime = DateTime.Now.ToString("MMddHHmmss");
                responseSvcsMessage.RawVal(FIELD.F_12, localDateTime.Substring(4, 6));
                responseSvcsMessage.RawVal(FIELD.F_13, localDateTime.Substring(0, 4));
                responseSvcsMessage.RawVal(FIELD.F_14, fileMaintData.IsoP14ExpirDte);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP37RefNbr))
                {
                    responseSvcsMessage.RawVal(FIELD.F_37, fileMaintData.IsoP37RefNbr);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP58T05TxDte))
                {
                    responseSvcsMessage.RawVal(FIELD.F_58, $"0800000000000000{fileMaintData.IsoP58T05TxDte}");
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP63SystemID))
                {
                    responseSvcsMessage.RawVal(FIELD.F_63, fileMaintData.IsoP63SystemID);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS91FileUpdCode))
                {
                    responseSvcsMessage.RawVal(FIELD.F_91, fileMaintData.IsoS91FileUpdCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS92FileSecurityCode))
                {
                    responseSvcsMessage.RawVal(FIELD.F_92, fileMaintData.IsoS92FileSecurityCode);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SvcsMessage0302 Exception:{ex.Message}");
                responseSvcsMessage = null;
            }
        }

        public SVCS GetSvcsMessage()
        {
            return (responseSvcsMessage);
        }
    }
}

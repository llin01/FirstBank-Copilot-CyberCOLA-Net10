using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.TwinMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class TwinMessage0300
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> TwinMessageTypeMapInfo = SingletonOfTwinMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly TWINIso8583 responseTwinMessage;
        private readonly MessageType twinMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public TwinMessage0300()
        {
            responseTwinMessage = null;
            TwinMessageTypeMapInfo.TryGetValue("0300", out twinMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0300"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="responseCode">The response code.</param>
        public TwinMessage0300(TWINIso8583 requestTwinMessage,
                               string responseCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0300 MessageType Error:{requestTwinMessage.Mti()}");
                    responseTwinMessage = null;
                    return;
                }
                responseTwinMessage = new TWINIso8583();
                responseTwinMessage.Header().Decode(requestTwinMessage.Header().Encode());
                responseTwinMessage.Mti(twinMessageType.ResponseMessage);
                foreach (var message in twinMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                        {
                            responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseTwinMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0300 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TwinMessage0300(FileMaintData fileMaintData)
        {
            //This Message0300 Create MTI 0300
            try
            {
                TwinMessageTypeMapInfo.TryGetValue("0300", out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error("Get TwinMessage0300 MessageType Error:0300");
                    responseTwinMessage = null;
                    return;
                }
                responseTwinMessage = new TWINIso8583();
                //change ISO Header
                responseTwinMessage.Header().Decode($"NCM{08:00}{01:00}{000:000}{0:0}{0:0}");
                responseTwinMessage.Mti(twinMessageType.RequestMessage);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP2CardNbr))
                {
                    responseTwinMessage.RawVal(FIELD.F_2, fileMaintData.IsoP2CardNbr);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP7TxDteTime))
                {
                    responseTwinMessage.RawVal(FIELD.F_7, fileMaintData.IsoP7TxDteTime);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP11TraceNo))
                {
                    responseTwinMessage.RawVal(FIELD.F_11, fileMaintData.IsoP11TraceNo);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP39Rsp))
                {
                    responseTwinMessage.RawVal(FIELD.F_39, fileMaintData.IsoP39Rsp);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP48AdditionalData))
                {
                    responseTwinMessage.RawVal(FIELD.F_48, fileMaintData.IsoP48AdditionalData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP49TranCurCode))
                {
                    responseTwinMessage.RawVal(FIELD.F_49, fileMaintData.IsoP49TranCurCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP60FromHostMaintData))
                {
                    responseTwinMessage.RawVal(FIELD.F_60, fileMaintData.IsoP60FromHostMaintData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS73ActDte))
                {
                    responseTwinMessage.RawVal(FIELD.F_73, fileMaintData.IsoS73ActDte);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS91FileUpdCode))
                {
                    responseTwinMessage.RawVal(FIELD.F_91, fileMaintData.IsoS91FileUpdCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS101FileName))
                {
                    responseTwinMessage.RawVal(FIELD.F_101, fileMaintData.IsoS101FileName);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS115AdditionalTraceData))
                {
                    responseTwinMessage.RawVal(FIELD.F_115, fileMaintData.IsoS115AdditionalTraceData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS120FileUpd))
                {
                    responseTwinMessage.RawVal(FIELD.F_120, fileMaintData.IsoS120FileUpd);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS122HostMaint))
                {
                    responseTwinMessage.RawVal(FIELD.F_122, fileMaintData.IsoS122HostMaint);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0300 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TWINIso8583 GetTwinMessage()
        {
            return responseTwinMessage;
        }
    }
}

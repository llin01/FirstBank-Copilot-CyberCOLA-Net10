using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class NcccMessage0300
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly NcccIso8583 responseNcccMessage;
        private readonly MessageType ncccMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public NcccMessage0300()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0300", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0300"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0300(NcccIso8583 requestNcccMessage,
                               string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0300 MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseNcccMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0300 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public NcccMessage0300(FileMaintData fileMaintData)
        {
            //This Message0300 Create MTI 0300
            try
            {
                NcccMessageTypeMapInfo.TryGetValue("0300", out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error("Get NcccMessage0300 MessageType Error:0300");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                //change ISO Header
                responseNcccMessage.Header().Decode($"ISO{08:00}{60:00}{000:000}{5:0}{1:0}");
                responseNcccMessage.Mti(ncccMessageType.RequestMessage);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP2CardNbr))
                {
                    responseNcccMessage.RawVal(FIELD.F_2, fileMaintData.IsoP2CardNbr);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP7TxDteTime))
                {
                    responseNcccMessage.RawVal(FIELD.F_7, fileMaintData.IsoP7TxDteTime);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP11TraceNo))
                {
                    responseNcccMessage.RawVal(FIELD.F_11, fileMaintData.IsoP11TraceNo);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP32BinIca))
                {
                    responseNcccMessage.RawVal(FIELD.F_32, fileMaintData.IsoP32BinIca);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP39Rsp))
                {
                    responseNcccMessage.RawVal(FIELD.F_39, fileMaintData.IsoP39Rsp);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP48AdditionalData))
                {
                    responseNcccMessage.RawVal(FIELD.F_48, fileMaintData.IsoP48AdditionalData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP49TranCurCode))
                {
                    responseNcccMessage.RawVal(FIELD.F_49, fileMaintData.IsoP49TranCurCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP60FromHostMaintData))
                {
                    responseNcccMessage.RawVal(FIELD.F_60, fileMaintData.IsoP60FromHostMaintData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS73ActDte))
                {
                    responseNcccMessage.RawVal(FIELD.F_73, fileMaintData.IsoS73ActDte);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS91FileUpdCode))
                {
                    responseNcccMessage.RawVal(FIELD.F_91, fileMaintData.IsoS91FileUpdCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS101FileName))
                {
                    responseNcccMessage.RawVal(FIELD.F_101, fileMaintData.IsoS101FileName);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS115AdditionalTraceData))
                {
                    responseNcccMessage.RawVal(FIELD.F_115, fileMaintData.IsoS115AdditionalTraceData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS120FileUpd))
                {
                    responseNcccMessage.RawVal(FIELD.F_120, fileMaintData.IsoS120FileUpd);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS122HostMaint))
                {
                    responseNcccMessage.RawVal(FIELD.F_122, fileMaintData.IsoS122HostMaint);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0300 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public NcccIso8583 GetNcccMessage()
        {
            return responseNcccMessage;
        }
    }
}

using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
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
    public class FiscMessage0302
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, FiscMessage.MessageType> FiscMessageTypeMapInfo = SingletonOfFiscMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly FISC responseFiscMessage;
        private readonly FiscMessage.MessageType fiscMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public FiscMessage0302()
        {
            responseFiscMessage = null;
            FiscMessageTypeMapInfo.TryGetValue("0302", out fiscMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0302"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="responseCode">The response code.</param>
        public FiscMessage0302(FISC requestFiscMessage,
                               string responseCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0302 MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = null;
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
                            responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0302 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        public FiscMessage0302(FileMaintData fileMaintData)
        {
            //This Message0302 Create MTI 0302
            try
            {
                FiscMessageTypeMapInfo.TryGetValue("0302", out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error("Get FiscMessage0302 MessageType Error:0302");
                    responseFiscMessage = null;
                    return;
                }
                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.RequestMessage);
                if (!string.IsNullOrEmpty(fileMaintData.IsoP2CardNbr))
                {
                    responseFiscMessage.RawVal(FIELD.F_2, fileMaintData.IsoP2CardNbr);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP7TxDteTime))
                {
                    responseFiscMessage.RawVal(FIELD.F_7, fileMaintData.IsoP7TxDteTime);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP11TraceNo))
                {
                    responseFiscMessage.RawVal(FIELD.F_11, fileMaintData.IsoP11TraceNo);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP39Rsp))
                {
                    responseFiscMessage.RawVal(FIELD.F_39, fileMaintData.IsoP39Rsp);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP48AdditionalData))
                {
                    responseFiscMessage.RawVal(FIELD.F_48, fileMaintData.IsoP48AdditionalData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP49TranCurCode))
                {
                    responseFiscMessage.RawVal(FIELD.F_49, fileMaintData.IsoP49TranCurCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoP60FromHostMaintData))
                {
                    responseFiscMessage.RawVal(FIELD.F_60, fileMaintData.IsoP60FromHostMaintData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS73ActDte))
                {
                    responseFiscMessage.RawVal(FIELD.F_73, fileMaintData.IsoS73ActDte);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS91FileUpdCode))
                {
                    responseFiscMessage.RawVal(FIELD.F_91, fileMaintData.IsoS91FileUpdCode);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS101FileName))
                {
                    responseFiscMessage.RawVal(FIELD.F_101, fileMaintData.IsoS101FileName);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS115AdditionalTraceData))
                {
                    responseFiscMessage.RawVal(FIELD.F_115, fileMaintData.IsoS115AdditionalTraceData);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS120FileUpd))
                {
                    responseFiscMessage.RawVal(FIELD.F_120, fileMaintData.IsoS120FileUpd);
                }
                if (!string.IsNullOrEmpty(fileMaintData.IsoS122HostMaint))
                {
                    responseFiscMessage.RawVal(FIELD.F_122, fileMaintData.IsoS122HostMaint);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0302 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }
        public FISC GetFiscMessage()
        {
            return responseFiscMessage;
        }
    }
}

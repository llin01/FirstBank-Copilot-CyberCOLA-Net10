using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
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
    public class TwinMessage0120
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> TwinMessageTypeMapInfo = SingletonOfTwinMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private TWINIso8583 responseTwinMessage;
        private readonly MessageType twinMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public TwinMessage0120()
        {
            responseTwinMessage = null;
            TwinMessageTypeMapInfo.TryGetValue("0120", out twinMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0120"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="responseCode">The response code.</param>
        public TwinMessage0120(TWINIso8583 requestTwinMessage,
                               string responseCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0120 MessageType Error:{requestTwinMessage.Mti()}");
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
                            if (intFieldPos == 39)
                            {
                                responseTwinMessage.Core().Field(field, responseCode);
                            }
                            else
                            {
                                responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 39)
                            {
                                responseTwinMessage.Core().Field(field, responseCode);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0120 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }


        // TwinMessage0120 Return String Arrays Information
        // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp

        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0120"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public TwinMessage0120(TWINIso8583 requestTwinMessage,
                               string[] authReturnString)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0120 MessageType Error:{requestTwinMessage.Mti()}");
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
                            if (intFieldPos == 39)
                            {
                                responseTwinMessage.Core().Field(field, authReturnString[1]);
                            }
                            else
                            {
                                responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 39)
                            {
                                responseTwinMessage.Core().Field(field, authReturnString[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0120 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0120"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public TwinMessage0120(TWINIso8583 requestTwinMessage,
                               NcccAuthInfoStruct authInfoData,
                               string[] authReturnString)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0120 MessageType Error:{requestTwinMessage.Mti()}");
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
                            //if (intFieldPos == FIELDMethods.Val(requestTwinMessage.GetTokenFieldId()))
                            //{
                            //    dynamic valueTokenC5 = ((TokenGroup)requestTwinMessage.Core().GetOthersValByKey(requestTwinMessage.GetTokenFieldId().ToString()))?.GetToken(TwinTransactionConst.PosAdditionalData.TokenC5.Id);
                            //    if (valueTokenC5 != null)
                            //    {
                            //        string tokenData = ColaCommUtility.CreateTwin0220TokenData(requestTwinMessage,
                            //                                                                   authInfoData,
                            //                                                                   authReturnString);
                            //        if (tokenData == null)
                            //            throw new System.ArgumentException("Create Token Data,Return Data Is Null");
                            //        if (tokenData.Length > 0)
                            //        {
                            //            responseTwinMessage.Core().Field(field, tokenData);
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseTwinMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                default:
                                    responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    break;
                            }
                            //}
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseTwinMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0120 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TWINIso8583 GetTwinMessage()
        {
            return (responseTwinMessage);
        }
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
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
    public class NcccMessage0220
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private NcccIso8583 responseNcccMessage;
        private readonly MessageType ncccMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public NcccMessage0220()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0220", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0220"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0220(NcccIso8583 requestNcccMessage,
                               string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0220 MessageType Error:{requestNcccMessage.Mti()}");
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
                            if (intFieldPos == 39)
                            {
                                responseNcccMessage.Core().Field(field, responseCode);
                            }
                            else
                            {
                                responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 39)
                            {
                                responseNcccMessage.Core().Field(field, responseCode);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0220 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }


        // NcccMessage0220 Return String Arrays Information
        // authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        // authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@vdLinkNbr
        // authReturnString[04]:@vdLinkNbrSeq        , authReturnString[05]:@vdActionCode
        // authReturnString[06]:@vdBlockAmt

        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0220"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public NcccMessage0220(NcccIso8583 requestNcccMessage,
                               string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0220 MessageType Error:{requestNcccMessage.Mti()}");
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
                            if (intFieldPos == 39)
                            {
                                responseNcccMessage.Core().Field(field, authReturnString[1]);
                            }
                            else
                            {
                                responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                            }
                        }
                        else
                        {
                            if (intFieldPos == 39)
                            {
                                responseNcccMessage.Core().Field(field, authReturnString[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0220 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0220"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public NcccMessage0220(NcccIso8583 requestNcccMessage,
                               NcccAuthInfoStruct authInfoData,
                               string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0220 MessageType Error:{requestNcccMessage.Mti()}");
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
                            //if (intFieldPos == FIELDMethods.Val(requestNcccMessage.GetTokenFieldId()))
                            //{
                            //    dynamic valueTokenC5 = ((TokenGroup)requestNcccMessage.Core().GetOthersValByKey(requestNcccMessage.GetTokenFieldId().ToString()))?.GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                            //    if (valueTokenC5 != null)
                            //    {
                            //        string tokenData = ColaCommUtility.CreateNccc0220TokenData(requestNcccMessage,
                            //                                                                   authInfoData,
                            //                                                                   authReturnString);
                            //        if (tokenData == null)
                            //            throw new System.ArgumentException("Create Token Data,Return Data Is Null");
                            //        if (tokenData.Length > 0)
                            //        {
                            //            responseNcccMessage.Core().Field(field, tokenData);
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                default:
                                    responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    break;
                            }
                            //}
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0220 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public NcccIso8583 GetNcccMessage()
        {
            return (responseNcccMessage);
        }
    }
}

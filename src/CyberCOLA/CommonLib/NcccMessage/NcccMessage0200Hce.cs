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
    public class NcccMessage0200Hce
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
        public NcccMessage0200Hce()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0200", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0200Hce(NcccIso8583 requestNcccMessage,
                                   string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200Hce MessageType Error:{requestNcccMessage.Mti()}");
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
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestNcccMessage.Header().Encode()}],Response Header[{responseNcccMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0200Hce Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200Hce"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="approveCode">The approve code.</param>
        public NcccMessage0200Hce(NcccIso8583 requestNcccMessage,
                                   string responseCode,
                                   string approveCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200Hce MessageType Error:{requestNcccMessage.Mti()}");
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
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 38:
                                    if (responseCode.Equals("00"))
                                    {
                                        responseNcccMessage.Core().Field(field, approveCode);
                                    }
                                    break;
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
                Log.Fatal(ex, $"NcccMessage0200Hce Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// NcccMessage0200Hce Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@tC5InstTxResp       , authReturnString[03]:@tC5DownPayment
        /// authReturnString[04]:@tC5InstPayment      , authReturnString[05]:@tC5InstFee
        /// authReturnString[06]:@tC5PointRedemption  , authReturnString[07]:@tC5SignPoint
        /// authReturnString[08]:@tC5PointBalance     , authReturnString[09]:@tC5PaidCreditAmt
        /// authReturnString[10]:@tC5LoyaltyTxInd 
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
        /// @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
        /// @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
        /// @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
        /// @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
        /// @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
        /// @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
        /// @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
        /// @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200Hce"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public NcccMessage0200Hce(NcccIso8583 requestNcccMessage,
                                   string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200Hce MessageType Error:{requestNcccMessage.Mti()}");
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
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 38:
                                    if (authReturnString[1].Equals("00"))
                                    {
                                        responseNcccMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestNcccMessage.Header().Encode()}],Response Header[{responseNcccMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0200Hce Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="NcccMessage0200Hce"/> class.
        ///  data9F10Array[0] : @tB2DeriveKeyIndex
        ///  data9F10Array[1] : @tB2CryptoVerNum
        ///  data9F10Array[2] : @tB2CVR
        ///
        /// NcccMessage0200Hce Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@tC5InstTxResp       , authReturnString[03]:@tC5DownPayment
        /// authReturnString[04]:@tC5InstPayment      , authReturnString[05]:@tC5InstFee
        /// authReturnString[06]:@tC5PointRedemption  , authReturnString[07]:@tC5SignPoint
        /// authReturnString[08]:@tC5PointBalance     , authReturnString[09]:@tC5PaidCreditAmt
        /// authReturnString[10]:@tC5LoyaltyTxInd 
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
        /// @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
        /// @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
        /// @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
        /// @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
        /// @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
        /// @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
        /// @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
        /// @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="data9F10Array"></param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public NcccMessage0200Hce(NcccIso8583 requestNcccMessage,
                                   NcccAuthInfoStruct authInfoData,
                                   string[] authReturnString,
                                   string[] data9F10Array)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200Hce MessageType Error:{requestNcccMessage.Mti()}");
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
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                case 57:
                                case 63:
                                    if (intFieldPos != FIELDMethods.Val(requestNcccMessage.GetTokenFieldId()))
                                    {
                                        responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    }
                                    else
                                    {
                                        var tokenData = ColaCommUtility.CreateNcccHceC5TokenData(requestNcccMessage,
                                                                                              authInfoData,
                                                                                              authReturnString);
                                        if (tokenData == null)
                                            throw new System.ArgumentException("Create Token Data, Return Data Is Null");
                                        if (tokenData.Length > 0)
                                        {
                                            responseNcccMessage.Core().Field(field, tokenData);
                                        }
                                    }
                                    break;
                                default:
                                    responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseNcccMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    break;
                                case 38:
                                    responseNcccMessage.Core().Field(field, authReturnString[0]);
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestNcccMessage.Header().Encode()}],Response Header[{responseNcccMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0200Hce Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        public NcccIso8583 GetNcccMessage()
        {
            return responseNcccMessage;
        }
    }
}

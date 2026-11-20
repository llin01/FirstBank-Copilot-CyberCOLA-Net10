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
    public class NcccMessage0200TknActive
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
        public NcccMessage0200TknActive()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0200", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknActive"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0200TknActive(NcccIso8583 requestNcccMessage,
                                         string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknActive MessageType Error:{requestNcccMessage.Mti()}");
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
                Log.Fatal(ex, $"NcccMessage0200TknActive Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknActive"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="approveCode">The approve code.</param>
        public NcccMessage0200TknActive(NcccIso8583 requestNcccMessage,
                                         string responseCode,
                                         string approveCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknActive MessageType Error:{requestNcccMessage.Mti()}");
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
                Log.Fatal(ex, $"NcccMessage0200TknActive Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// Nccc0200TknActiveProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@t04ErrorFlg         , authReturnString[03]:@t04CrdVeryFlg
        /// authReturnString[04]:@tQ8Tag16EMail       , authReturnString[05]:@tQ8Tag29IssConfigId
        /// authReturnString[06]:@tQ8Tag36PanSeq      , authReturnString[07]:@tQ8Tag51ChVfyMethod
        /// authReturnString[08]:@tQ8Tag56CvmErrCde   , authReturnString[09]:@tQ8Tag58CvmRsp
        /// authReturnString[10]:@tQ8TagQEPhone       , authReturnString[11]:@tQ8TagQGRiskLvlResult
        /// authReturnString[12]:@tknTxType
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @t04ErrorFlg	            varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
        /// @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
        /// @tQ8Tag16EMail              varchar(64) 	Output   Field 57 or 63 Token Q8 SubToken Id 16 Acct E-Mail
        /// @tQ8Tag29IssConfigId        varchar(30) 	Output	 Field 57 or 63 Token Q8 SubToken Id 29 Issuer Production Configuration Id
        /// @tQ8Tag36PanSeq       	    varchar(10) 	Output	 Field 57 or 63 Token Q8 SubToken Id 36 Card Sequence Number
        /// @tQ8Tag51ChVfyMethod      	varchar(186) 	Output	 Field 57 or 63 Token Q8 SubToken Id 51 CardHolder Verification Method
        /// @tQ8Tag56CvmErrCde          varchar(08) 	Output	 Field 57 or 63 Token Q8 SubToken Id 56 Check Eligibility error code
        /// @tQ8Tag58CvmRsp             varchar(02) 	Output	 Field 57 or 63 Token Q8 SubToken Id 58 Contactless use
        /// @tQ8TagQEPhone      	    varchar(15) 	Output	 Field 57 or 63 Token Q8 SubToken Id QE Mobile Phone number
        /// @tQ8TagQGRiskLvlResult      varchar(01) 	Output	 Field 57 or 63 Token Q8 SubToken Id QG Risk Level Result
        /// @tknTxType  	            varchar(02) 	Output
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknActive"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public NcccMessage0200TknActive(NcccIso8583 requestNcccMessage,
                                         string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknActive MessageType Error:{requestNcccMessage.Mti()}");
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
                Log.Fatal(ex, $"NcccMessage0200TknActive Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200TknActive"/> class.
        /// Nccc0200TknActiveProcess Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@t04ErrorFlg         , authReturnString[03]:@t04CrdVeryFlg
        /// authReturnString[04]:@tQ8Tag16EMail       , authReturnString[05]:@tQ8Tag29IssConfigId
        /// authReturnString[06]:@tQ8Tag36PanSeq      , authReturnString[07]:@tQ8Tag51ChVfyMethod
        /// authReturnString[08]:@tQ8Tag56CvmErrCde   , authReturnString[09]:@tQ8Tag58CvmRsp
        /// authReturnString[10]:@tQ8TagQEPhone       , authReturnString[11]:@tQ8TagQGRiskLvlResult
        /// authReturnString[12]:@tknTxType
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @t04ErrorFlg	            varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
        /// @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
        /// @tQ8Tag16EMail              varchar(64) 	Output   Field 57 or 63 Token Q8 SubToken Id 16 Acct E-Mail
        /// @tQ8Tag29IssConfigId        varchar(30) 	Output	 Field 57 or 63 Token Q8 SubToken Id 29 Issuer Production Configuration Id
        /// @tQ8Tag36PanSeq       	    varchar(10) 	Output	 Field 57 or 63 Token Q8 SubToken Id 36 Card Sequence Number
        /// @tQ8Tag51ChVfyMethod      	varchar(186) 	Output	 Field 57 or 63 Token Q8 SubToken Id 51 CardHolder Verification Method
        /// @tQ8Tag56CvmErrCde          varchar(08) 	Output	 Field 57 or 63 Token Q8 SubToken Id 56 Check Eligibility error code
        /// @tQ8Tag58CvmRsp             varchar(02) 	Output	 Field 57 or 63 Token Q8 SubToken Id 58 Contactless use
        /// @tQ8TagQEPhone      	    varchar(15) 	Output	 Field 57 or 63 Token Q8 SubToken Id QE Mobile Phone number
        /// @tQ8TagQGRiskLvlResult      varchar(01) 	Output	 Field 57 or 63 Token Q8 SubToken Id QG Risk Level Result
        /// @tknTxType  	            varchar(02) 	Output
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="data9F10Array"></param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public NcccMessage0200TknActive(NcccIso8583 requestNcccMessage,
                                         NcccAuthInfoStruct authInfoData,
                                         string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200TknActive MessageType Error:{requestNcccMessage.Mti()}");
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
                                        var tokenData = ColaCommUtility.CreateNccc0200TknActiveTokenData(requestNcccMessage,
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
                                    if (authReturnString[1].Equals("00"))
                                    {
                                        responseNcccMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                //case 38:
                                //    responseNcccMessage.Core().Field(field, authReturnString[0]);
                                //   break;
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
                Log.Fatal(ex, $"NcccMessage0200TknActive Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        public NcccIso8583 GetNcccMessage()
        {
            return responseNcccMessage;
        }
    }
}

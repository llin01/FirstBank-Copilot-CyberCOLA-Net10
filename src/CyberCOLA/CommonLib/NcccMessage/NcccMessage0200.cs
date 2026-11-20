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
    public class NcccMessage0200
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
        public NcccMessage0200()
        {
            responseNcccMessage = null;
            NcccMessageTypeMapInfo.TryGetValue("0200", out ncccMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        public NcccMessage0200(NcccIso8583 requestNcccMessage,
                               string responseCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200 MessageType Error:{requestNcccMessage.Mti()}");
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
                Log.Fatal(ex, $"NcccMessage0200 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="approveCode">The approve code.</param>
        public NcccMessage0200(NcccIso8583 requestNcccMessage,
                               string responseCode,
                               string approveCode)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200 MessageType Error:{requestNcccMessage.Mti()}");
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
                Log.Fatal(ex, $"NcccMessage0200 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        /// NcccMessage0200 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
        /// authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
        /// authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
        /// authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
        /// authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
        /// authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
        /// authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
        /// authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
        /// authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
        /// authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	        varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @isoS127NegDelFlag       	varchar(01)	    Output	 --NegFile刪除註記 initial 時請放null即可註記為D時, 傳送一筆MessageType :0300 Detele Neg File
        /// @t04ErrorFlg	            varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
        /// @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
        /// @tC0CavvResult              varchar(01) 	Output   Field 57 or 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
        /// @tC5InstTxInd        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
        /// @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
        /// @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
        /// @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
        /// @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
        /// @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
        /// @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
        /// @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
        /// @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
        /// @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
        /// @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
        /// @vdLinkNbr                  varchar(20)     Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq               varchar(02)     Output   VD 圈存帳戶序號
        /// @vdActionCode               varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                 varchar(15) 	Output   VD 圈存解圈金額
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="NcccMessage0200"/> class.
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public NcccMessage0200(NcccIso8583 requestNcccMessage,
                               string[] authReturnString)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200 MessageType Error:{requestNcccMessage.Mti()}");
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
                Log.Fatal(ex, $"NcccMessage0200 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="NcccMessage0200"/> class.
        ///  data9F10Array[0] : @tB2DeriveKeyIndex
        ///  data9F10Array[1] : @tB2CryptoVerNum
        ///  data9F10Array[2] : @tB2CVR
        ///
        /// NcccMessage0200 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode      , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction     , authReturnString[03]:@isoS127NegDelFlag
        /// authReturnString[04]:@t04ErrorFlg         , authReturnString[05]:@t04CrdVeryFlg
        /// authReturnString[06]:@tC0CavvResult       , authReturnString[07]:@tC5InstTxInd
        /// authReturnString[08]:@tC5InstTxResp       , authReturnString[09]:@tC5DownPayment
        /// authReturnString[10]:@tC5InstPayment      , authReturnString[11]:@tC5InstFee
        /// authReturnString[12]:@tC5PointRedemption  , authReturnString[13]:@tC5SignPoint
        /// authReturnString[14]:@tC5PointBalance     , authReturnString[15]:@tC5PaidCreditAmt
        /// authReturnString[16]:@tC5LoyaltyTxInd     , authReturnString[17]:@idVfyOutFlag
        /// authReturnString[18]:@vdLinkNbr           , authReturnString[19]:@vdLinkNbrSeq
        /// authReturnString[20]:@vdActionCode        , authReturnString[21]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	        varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @isoS127NegDelFlag       	varchar(01)	    Output	 --NegFile刪除註記 initial 時請放null即可註記為D時, 傳送一筆MessageType :0300 Detele Neg File
        /// @t04ErrorFlg	            varchar(01) 	Output	 Field 57 or 63 Token04:Error-flag Token04.ErrorFlg
        /// @t04CrdVeryFlg           	varchar(01) 	Output	 Field 57 or 63 Token04:Crd-very-flag Token04.CrdVeryFlg
        /// @tC0CavvResult              varchar(01) 	Output   Field 57 or 63 TokenC0 TokenC0.CavvAavResults(請以本欄位押回Token C0 的Results (Token C0的第26位))
        /// @tC5InstTxInd        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Installment-Tx-Ind<E/I> / Loyalty-Tx-Ind<' ','1'…>
        /// @tC5InstTxResp       	    varchar(02) 	Output	 Field 57 or 63 Token C5 分期紅利註記 Installment txn resp-cde/Loyalty-tx-Resp-code
        /// @tC5DownPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Down Payment<分期,首期>
        /// @tC5InstPayment      	    varchar(08) 	Output	 Field 57 or 63 Token C5 Installment Payment<分期, 每期>
        /// @tC5InstFee          	    varchar(06) 	Output	 Field 57 or 63 Token C5 Formality-Fee<分期, 手續費/利息>
        /// @tC5PointRedemption  	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Redemption<紅利,折扺點數>
        /// @tC5SignPoint        	    varchar(01) 	Output	 Field 57 or 63 Token C5 Sign Point Of Balance<紅利, 餘點正負項>
        /// @tC5PointBalance     	    varchar(08) 	Output	 Field 57 or 63 Token C5 Point of Balance<紅利, 餘點>
        /// @tC5PaidCreditAmt    	    varchar(10) 	Output	 Field 57 or 63 Token C5 Paid-Credit-Amount<紅利,實際支付金額>
        /// @tC5LoyaltyTxInd     	    varchar(01) 	Output	 Field 57 or 63 Token C5 <紅利註記>
        /// @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
        /// @vdLinkNbr                  varchar(20)     Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq               varchar(02)     Output   VD 圈存帳戶序號
        /// @vdActionCode               varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                 varchar(15) 	Output   VD 圈存解圈金額
        /// </summary>
        /// <param name="requestNcccMessage">The request NCCC message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="data9F10Array"></param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public NcccMessage0200(NcccIso8583 requestNcccMessage,
                               NcccAuthInfoStruct authInfoData,
                               string[] authReturnString,
                               string[] data9F10Array)
        {
            const int idVfyOutFlagIndex = 17;
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0200 MessageType Error:{requestNcccMessage.Mti()}");
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
                                        var tokenData = ColaCommUtility.CreateNccc0200TokenData(requestNcccMessage,
                                                                                                authInfoData,
                                                                                                authReturnString,
                                                                                                data9F10Array);
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
                                    if (string.IsNullOrEmpty(authReturnString[idVfyOutFlagIndex]) || !authReturnString[idVfyOutFlagIndex].Equals("Y"))
                                    {
                                        if (authReturnString[1].Equals("00") || authReturnString[1].Equals("85"))
                                        {
                                            responseNcccMessage.Core().Field(field, authReturnString[0]);
                                        }
                                        else
                                        {
                                            const FIELD merchPos = FIELD.F_48;
                                            if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(merchPos)))
                                            {
                                                if (((string)requestNcccMessage.Core().Field(merchPos)).Substring(0, 10).Equals(SystemConstantData.CommonData.NcccOpenCardMerchantNbr))
                                                {
                                                    responseNcccMessage.Core().Field(field, authReturnString[0]);
                                                }
                                            }
                                        }
                                    }
                                    else
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
                Log.Fatal(ex, $"NcccMessage0200 Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }

        public NcccIso8583 GetNcccMessage()
        {
            return responseNcccMessage;
        }
    }
}

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
    public class TwinMessage0200
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
        public TwinMessage0200()
        {
            responseTwinMessage = null;
            TwinMessageTypeMapInfo.TryGetValue("0200", out twinMessageType);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0200"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="responseCode">The response code.</param>
        public TwinMessage0200(TWINIso8583 requestTwinMessage,
                               string responseCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0200 MessageType Error:{requestTwinMessage.Mti()}");
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
                                case 15:
                                    responseTwinMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfTWINInfo));
                                    break;
                                case 39:
                                    responseTwinMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestTwinMessage.Header().Encode()}],Response Header[{responseTwinMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0200 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="TwinMessage0200"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="approveCode">The approve code.</param>
        public TwinMessage0200(TWINIso8583 requestTwinMessage,
                               string responseCode,
                               string approveCode)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0200 MessageType Error:{requestTwinMessage.Mti()}");
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
                                case 15:
                                    responseTwinMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfTWINInfo));
                                    break;
                                case 38:
                                    if (responseCode.Equals("00"))
                                    {
                                        responseTwinMessage.Core().Field(field, approveCode);
                                    }
                                    break;
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
                Log.Fatal(ex, $"TwinMessage0200 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }

        /// <summary>
        /// TwinMessage0200 Return String Arrays Information
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
        /// Initializes a new instance of the <see cref="TwinMessage0200"/> class.
        /// </summary>
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public TwinMessage0200(TWINIso8583 requestTwinMessage,
                               string[] authReturnString)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0200 MessageType Error:{requestTwinMessage.Mti()}");
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
                                case 15:
                                    responseTwinMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfTWINInfo));
                                    break;
                                case 38:
                                    if (authReturnString[1].Equals("00"))
                                    {
                                        responseTwinMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    responseTwinMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestTwinMessage.Header().Encode()}],Response Header[{responseTwinMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0200 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="TwinMessage0200"/> class.
        ///  data9F10Array[0] : @tB2DeriveKeyIndex
        ///  data9F10Array[1] : @tB2CryptoVerNum
        ///  data9F10Array[2] : @tB2CVR
        ///
        /// TwinMessage0200 Return String Arrays Information
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
        /// <param name="requestTwinMessage">The request TWIN Card message.</param>
        /// <param name="authInfoData">The authentication information data.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        /// <param name="data9F10Array"></param>
        /// <exception cref="System.ArgumentException">Create Token Data, Return Data Is Null</exception>
        public TwinMessage0200(TWINIso8583 requestTwinMessage,
                               NcccAuthInfoStruct authInfoData,
                               string[] authReturnString,
                               string[] data9F10Array)
        {
            const int idVfyOutFlagIndex = 17;
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0200 MessageType Error:{requestTwinMessage.Mti()}");
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
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseTwinMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                case 55:
                                    responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    break;
                                case 63:
                                    responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    break;
                                default:
                                    responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseTwinMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfTWINInfo));
                                    break;
                                case 38:
                                    if (string.IsNullOrEmpty(authReturnString[idVfyOutFlagIndex]) || !authReturnString[idVfyOutFlagIndex].Equals("Y"))
                                    {
                                        if (authReturnString[1].Equals("00") || authReturnString[1].Equals("85"))
                                        {
                                            responseTwinMessage.Core().Field(field, authReturnString[0]);
                                        }
                                        else
                                        {
                                            const FIELD merchPos = FIELD.F_48;
                                            if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(merchPos)))
                                            {
                                                if (((string)requestTwinMessage.Core().Field(merchPos)).Substring(0, 10).Equals(SystemConstantData.CommonData.NcccOpenCardMerchantNbr))
                                                {
                                                    responseTwinMessage.Core().Field(field, authReturnString[0]);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        responseTwinMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    responseTwinMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Debug($"Request Header[{requestTwinMessage.Header().Encode()}],Response Header[{responseTwinMessage.Header().Encode()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0200 Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }

        public TWINIso8583 GetTwinMessage()
        {
            return responseTwinMessage;
        }
    }
}

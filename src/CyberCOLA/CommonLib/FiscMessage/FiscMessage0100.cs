using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.FiscMessage
{
    public class FiscMessage0100
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> FiscMessageTypeMapInfo = SingletonOfFiscMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly FISC responseFiscMessage;
        private readonly MessageType fiscMessageType;
        /// <remarks>
        /// This Message For Base Message Class
        /// </remarks>
        public FiscMessage0100()
        {
            responseFiscMessage = null;
            FiscMessageTypeMapInfo.TryGetValue("0100", out fiscMessageType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0100"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="responseCode">The response code.</param>
        public FiscMessage0100(FISC requestFiscMessage,
                                string responseCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0100 MessageType Error:{requestFiscMessage.Mti()}");
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
                                case 15:
                                    responseFiscMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfFISCInfo));
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0100 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0100"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="responseCode">The response code.</param>
        /// <param name="approveCode">The approve code.</param>
        public FiscMessage0100(FISC requestFiscMessage,
                                string responseCode,
                                string approveCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0100 MessageType Error:{requestFiscMessage.Mti()}");
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
                                case 15:
                                    responseFiscMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfFISCInfo));
                                    break;
                                case 38:
                                    if (responseCode.Equals("00"))
                                    {
                                        responseFiscMessage.Core().Field(field, approveCode);
                                    }
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, responseCode);
                                    break;
                            }
                        }
                    }
                }
                Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0100 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        // Return String Arrays Information
        // authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        // authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        // authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        // authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        // authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        // authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        // authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        // authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        // authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        // authReturnString[18]:@vdBlockAmt
        // Description
        // @isoP38AuthCode          	varchar(06) 	Output	 --p38
        // @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        // @isoP39RspAction	            varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        // @isoP48T45               	varchar(01)	    Output	 --CAVV Result
        // @isoP58T21LoyaltyInd	        varchar(01) 	Output	 Field 58 紅利註記
        // @isoP58T21TxResp           	varchar(02) 	Output	 Field 58 紅利回覆碼
        // @isoP58T21SignPoint          varchar(01) 	Output   Field 58 <紅利, 餘點正負項>
        // @isoP58T21PointBalance       varchar(10) 	Output	 Field 58 <紅利, 餘點>
        // @isoP58T21PointRedemption    varchar(10) 	Output	 Field 58 <紅利,折扺點數>
        // @isoP58T21PaidCAmt      	    varchar(12) 	Output	 Field 58  <紅利,實際支付金額>
        // @isoS112DownPayment      	varchar(08) 	Output	 Field 112 首期期金
        // @isoS112InstPayment          varchar(08) 	Output	 Field 112 每期期金
        // @isoS112InstFee  	        varchar(06) 	Output	 Field 112 手續費
        // @isoS112InstTxResp        	varchar(02) 	Output	 Field 112 分期回應碼
        // @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
        // @vdLinkNbr                   varchar(20)     Output   VD 圈存銀行帳戶
        // @vdLinkNbrSeq                varchar(02)     Output   VD 圈存帳戶序號
        // @vdActionCode                varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        // @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額

        public FiscMessage0100(FISC requestFiscMessage,
                                string[] authReturnString)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0100 MessageType Error:{requestFiscMessage.Mti()}");
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
                                case 15:
                                    responseFiscMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfFISCInfo));
                                    break;
                                case 38:
                                    if (authReturnString[1].Equals("00"))
                                    {
                                        responseFiscMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0100 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="FiscMessage0100"/> class.
        ///  data9F10Array[0] : @tB2DeriveKeyIndex
        ///  data9F10Array[1] : @tB2CryptoVerNum
        ///  data9F10Array[2] : @tB2CVR
        ///
        /// FiscMessage0100 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode             , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction            , authReturnString[03]:@isoP48T45
        /// authReturnString[04]:@isoP58T21LoyaltyInd        , authReturnString[05]:@isoP58T21TxResp
        /// authReturnString[06]:@isoP58T21SignPoint         , authReturnString[07]:@isoP58T21PointBalance
        /// authReturnString[08]:@isoP58T21PointRedemption   , authReturnString[09]:@isoP58T21PaidCAmt
        /// authReturnString[10]:@isoS112DownPayment         , authReturnString[11]:@isoS112InstPayment
        /// authReturnString[12]:@isoS112InstFee             , authReturnString[13]:@isoS112InstTxResp
        /// authReturnString[14]:@idVfyOutFlag               , authReturnString[15]:@vdLinkNbr
        /// authReturnString[16]:@vdLinkNbrSeq               , authReturnString[17]:@vdActionCode
        /// authReturnString[18]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode          	varchar(06) 	Output	 --p38
        /// @isoP39Rsp               	varchar(02) 	Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	        varchar(01)	    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @isoP48T45              	varchar(01)	    Output	 --CAVV Result
        /// @isoP58T21LoyaltyInd	    varchar(01) 	Output	 Field 58 紅利註記
        /// @isoP58T21TxResp           	varchar(02) 	Output	 Field 58 紅利回覆碼
        /// @isoP58T21SignPoint         varchar(01) 	Output   Field 58 <紅利, 餘點正負項>
        /// @isoP58T21PointBalance      varchar(10) 	Output	 Field 58 <紅利, 餘點>
        /// @isoP58T21PointRedemption   varchar(10) 	Output	 Field 58 <紅利,折扺點數>
        /// @isoP58T21PaidCAmt      	varchar(12) 	Output	 Field 58  <紅利,實際支付金額>
        /// @isoS112DownPayment      	varchar(08) 	Output	 Field 112 首期期金
        /// @isoS112InstPayment         varchar(08) 	Output	 Field 112 每期期金
        /// @isoS112InstFee  	        varchar(06) 	Output	 Field 112 手續費
        /// @isoS112InstTxResp        	varchar(02) 	Output	 Field 112 分期回應碼
        /// @idVfyOutFlag	            varchar(01)	    Output	 判斷本欄位為Y時強制將授權碼帶出
        /// @vdLinkNbr                  varchar(20)     Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq               varchar(02)     Output   VD 圈存帳戶序號
        /// @vdActionCode               varchar(01)     Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                 varchar(15)	    Output   VD 圈存解圈金額
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0100"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param></param>
        /// <param name="authInfoData"></param>
        /// <param name="authReturnString">The authentication return string.</param>
        public FiscMessage0100(FISC requestFiscMessage,
                                FiscAuthInfoStruct authInfoData,
                                string[] authReturnString,
                                string[] data9F10Array)
        {
            const int idVfyOutFlagIndex = 14;
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0100 MessageType Error:{requestFiscMessage.Mti()}");
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
                            switch (intFieldPos)
                            {
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                                case 48:
                                    var tagIdData = ColaCommUtility.CreateFisc0100P48Data(requestFiscMessage,
                                                                                          authInfoData,
                                                                                          authReturnString);
                                    if (tagIdData != null)
                                    {
                                        responseFiscMessage.Core().Field(field, tagIdData);
                                    }
                                    break;
                                case 55:
                                    var emvTagData = ColaCommUtility.CreateFisc0100P55Data(requestFiscMessage,
                                                                                                 authInfoData,
                                                                                                 authReturnString,
                                                                                                 data9F10Array
                                                                                                 );
                                    if (emvTagData == null)
                                        throw new System.ArgumentException("Create EMV Tag Data,Return Data Is Null");
                                    if (emvTagData.Length > 0)
                                    {
                                        responseFiscMessage.Core().Field(field, emvTagData);
                                    }
                                    break;
                                case 58:
                                    var tagId58Data = ColaCommUtility.CreateFisc0100P58Data(requestFiscMessage,
                                                                                                  authInfoData,
                                                                                                  authReturnString);
                                    if (tagId58Data != null)
                                    {
                                        responseFiscMessage.Core().Field(field, tagId58Data);
                                    }
                                    break;
                                case 112:
                                    var S112InstallmentData = ColaCommUtility.CreateFisc0100S112Data(requestFiscMessage,
                                                                                                  authInfoData,
                                                                                                  authReturnString);
                                    if (S112InstallmentData != null)
                                    {
                                        responseFiscMessage.Core().Field(field, S112InstallmentData);
                                    }
                                    break;
                                default:
                                    responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                                    break;
                            }
                        }
                        else
                        {
                            switch (intFieldPos)
                            {
                                case 15:
                                    responseFiscMessage.Core().Field(field, ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfFISCInfo));
                                    break;
                                case 38:
                                    if (string.IsNullOrEmpty(authReturnString[idVfyOutFlagIndex]) || !authReturnString[idVfyOutFlagIndex].Equals("Y"))
                                    {
                                        if (authReturnString[1].Equals("00") || authReturnString[1].Equals("85"))
                                        {
                                            responseFiscMessage.Core().Field(field, authReturnString[0]);
                                        }
                                    }
                                    else
                                    {
                                        responseFiscMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    break;
                            }
                        }
                    }
                }
                Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0100 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        public FISC GetFiscMessage()
        {
            return (responseFiscMessage);
        }
    }
}


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
    /// <remarks>
    /// This Base Message Class For All Message
    /// </remarks>
    public class FiscMessage0420
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
        public FiscMessage0420()
        {
            responseFiscMessage = null;
            FiscMessageTypeMapInfo.TryGetValue("0420", out fiscMessageType);
        }
        public FiscMessage0420(FISC requestFiscMessage, string responseCode)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0420 MessageType Error:{requestFiscMessage.Mti()}");
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
                                    responseFiscMessage.Core().Field(field, responseCode);
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
                Log.Fatal(ex, $"FiscMessage0420 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        /// <summary>
        /// FiscMessage0420 Return String Arrays Information
        /// authReturnString[00]:@isoP38AuthCode           , authReturnString[01]:@isoP39Rsp
        /// authReturnString[02]:@isoP39RspAction          , authReturnString[03]:@isoP58T21TxResp
        /// authReturnString[04]:@isoP58T21SignPoint       , authReturnString[05]:@isoP58T21PointBalance
        /// authReturnString[06]:@isoP58T21PointRedemption , authReturnString[07]:@isoP58T21PaidCAmt
        /// authReturnString[08]:@vdLinkNbr                , authReturnString[09]:@vdLinkNbrSeq
        /// authReturnString[10]:@vdActionCode             , authReturnString[11]:@vdBlockAmt
        /// Description
        /// @isoP38AuthCode              varchar(06)    Output	 --p38
        /// @isoP39Rsp                   varchar(02)    Output	 --p39 initial 時請放null即可
        /// @isoP39RspAction	         varchar(01)    Output	 --對映於P39的授權回覆動作 A/D/R/P	initial 時請放null即可
        /// @isoP58T21TxResp             varchar(02)    Output,  --紅利回覆碼
        /// @isoP58T21SignPoint          varchar(01)    Output,  --< 紅利, 餘點正負項 >
        /// @isoP58T21PointBalance       varchar(10)    Output,  --< 紅利, 餘點 >
        /// @isoP58T21PointRedemption    varchar(10)    Output,  --< 紅利,折扺點數 >
        /// @isoP58T21PaidCAmt           varchar(12)    Output,  --< 紅利,實際支付金額 >
        /// @vdLinkNbr                   varchar(20)    Output   VD 圈存銀行帳戶
        /// @vdLinkNbrSeq                varchar(02)    Output   VD 圈存帳戶序號
        /// @vdActionCode                varchar(01)    Output   VD 圈存代碼: 1.圈存 3.查詢餘額 5.解圈(差額解圈) 9.解圈(足額解圈)
        /// @vdBlockAmt                  varchar(15) 	Output   VD 圈存解圈金額
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="FiscMessage0420"/> class.
        /// </summary>
        /// <param name="requestFiscMessage">The request FISC message.</param>
        /// <param name="authReturnString">The authentication return string.</param>
        public FiscMessage0420(FISC requestFiscMessage, FiscAuthInfoStruct authInfoData, string[] authReturnString)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0420 MessageType Error:{requestFiscMessage.Mti()}");
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
                                case 38:
                                    if ((!string.IsNullOrEmpty(authReturnString[0])) && (authReturnString[0].Length == 6))
                                    {
                                        responseFiscMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    if ((!string.IsNullOrEmpty(authReturnString[1])) && (authReturnString[1].Length == 2))
                                    {
                                        responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    }
                                    break;
                                case 58:
                                    var tagId58Data = ColaCommUtility.CreateFisc0420P58Data(requestFiscMessage,
                                                                                            authInfoData,
                                                                                            authReturnString);
                                    if (tagId58Data != null)
                                    {
                                        responseFiscMessage.Core().Field(field, tagId58Data);
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
                                case 38:
                                    if ((!string.IsNullOrEmpty(authReturnString[0])) && (authReturnString[0].Length == 6))
                                    {
                                        responseFiscMessage.Core().Field(field, authReturnString[0]);
                                    }
                                    break;
                                case 39:
                                    if ((!string.IsNullOrEmpty(authReturnString[1])) && (authReturnString[1].Length == 2))
                                    {
                                        responseFiscMessage.Core().Field(field, authReturnString[1]);
                                    }
                                    break;
                            }
                        }
                    }
                }
                Log.Info($"Request MTI[{requestFiscMessage.Mti()}],Response MTI[{responseFiscMessage.Mti()}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0420 Exception:{ex.Message}");
                responseFiscMessage = null;
            }
        }

        public FISC GetFiscMessage()
        {
            return (responseFiscMessage);
        }
    }
}

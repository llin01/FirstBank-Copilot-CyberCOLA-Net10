using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;

namespace Com.Cybersoft4u.CommonLib.TwinMessage
{
    public class TwinMessage0800
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> TwinMessageTypeMapInfo = SingletonOfTwinMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private TWINIso8583 responseTwinMessage;
        private readonly MessageType twinMessageType;
        private const string NetWorkMessageType = "0800";

        /// <remarks>
        /// This Active Request Message(Example. Auto SignOn Message Network Management Information Code Bust Be 001)
        /// </remarks>
        public TwinMessage0800(string informationCode)
        {
            try
            {
                if (informationCode.Length != 3)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              (new System.Diagnostics.StackTrace(true)).GetFrame(0)?.GetMethod()?.Name,
                              "TwinMessage0800:" + informationCode,
                              "Create 0800 Message Error For InformationCode Error");

                    throw new ArgumentException("Create 0800 Message Error For InformationCode Error");
                }
                if (!(informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOn) ||
                      informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOff) ||
                      informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.ChangeKey) ||
                      informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.NewKey) ||
                      informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.VerifyKey) ||
                      informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              (new System.Diagnostics.StackTrace(true)).GetFrame(0)?.GetMethod()?.Name,
                              "TwinMessage0800:" + informationCode,
                              "0800 Message InformationCode Invalid");

                    throw new ArgumentException("0800 Message InformationCode Invalid");
                }
                TwinMessageTypeMapInfo.TryGetValue("0800", out twinMessageType);
                responseTwinMessage = new TWINIso8583();
                responseTwinMessage.Header().Decode($"NCM{00:00}{01:00}{000:000}{0:0}{0:0}");
                responseTwinMessage.Mti($"{Convert.ToInt32(NetWorkMessageType):0000}");

                FIELD field = FIELD.F_7;
                responseTwinMessage.Core().Field(field, DateTime.Now.AddHours(-8).ToString("MMddHHmmss"));
                field = FIELD.F_11;
                responseTwinMessage.Core().Field(field, ColaCommUtility.GetSystemTraceNumber());

                //Position 1-2(鍵值類別)
                //00：PIN key
                //Position 3-4(鍵值使用方向)
                //01 : Outbound key only
                //02 : Inbound key only
                //03 : both incoming and outgoing keys
                //Position 5-6(鍵值加密方式)
                //02:Double Length Triple DES
                //Position 7-16
                //固定值全部補0
                if ((informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.ChangeKey)) ||
                    (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.NewKey)) ||
                    (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.VerifyKey)))
                {
                    field = FIELD.F_53;
                    responseTwinMessage.Core().Field(field, "0002000000000000");
                }
                field = FIELD.F_70;
                responseTwinMessage.Core().Field(field, informationCode);

                //Position 1-10(機構代碼)
                //TWIN Card 賦予 TWIN Card 發卡機構之代碼
                //Position 11-16(鍵值檢查碼)
                //Position 17-48(以連線機構於 TWIN Card 系統建置之 ZMK 加密之 ZPK 值)
                //對應欄位 53 鍵值加密方式，資料長度為 32 hexadecimal characters
                if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.ChangeKey))
                {

                    field = FIELD.F_123;
                    //use field 53 encrypt key method create key value
                    var twinField123StringFormat = "{0:-10}{1,-6}{2,-32}";
                    var tempString = string.Format(twinField123StringFormat, "0", "0", "0");
                    responseTwinMessage.Core().Field(field, tempString);
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "TwinMessage0800 Exception");
                responseTwinMessage = null;
            }
        }

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public TwinMessage0800(TWINIso8583 requestTwinMessage)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0800 MessageType Error:{requestTwinMessage.Mti()}");
                    responseTwinMessage = null;
                    return;
                }
                responseTwinMessage = new TWINIso8583();
                responseTwinMessage.Header().Decode(requestTwinMessage.Header().Encode());
                responseTwinMessage.Mti(twinMessageType.ResponseMessage);
                string informationCode = string.Empty;
                string responseCode = "00";
                string settlementMMDD = string.Empty;
                string systemNextDateString = string.Empty;

                FIELD field = FIELD.F_70;
                if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                {
                    informationCode = (string)requestTwinMessage.Core().Field(field);
                    if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.Cutover))
                    {
                        field = FIELD.F_15;
                        if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                        {
                            settlementMMDD = (string)requestTwinMessage.Core().Field(field);
                            systemNextDateString = ColaCommUtility.GetNextSettlementDateForCutover(SystemConstantData.CommonData.PasystfTWINInfo, settlementMMDD);
                            if (string.IsNullOrEmpty(systemNextDateString))
                            {
                                Log.Error($"GetNextSettlementDateForCutover Error[{SystemConstantData.CommonData.PasystfTWINInfo}]");
                                responseTwinMessage = null;
                                return;
                            }
                            int spRetnCode = StoreProcUtility.UpdateColaPasystf(SystemConstantData.CommonData.ColaBu, SystemConstantData.CommonData.PasystfTWINSystemDteUpdate, systemNextDateString);
                            if (spRetnCode != 0)
                            {
                                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                                    (new System.Diagnostics.StackTrace(true)).GetFrame(0)?.GetMethod()?.Name,
                                    "TwinMessage0800",
                                    "StoreProcedure UpdateColaPasystf Error :" + spRetnCode.ToString());
                                responseTwinMessage = null;
                                return;
                            }
                            SingletonOfCommonProperty.Instance.SetColaPasystfInfoSystemDate(systemNextDateString, SystemConstantData.CommonData.PasystfTWINInfo);
                        }
                    }
                }

                foreach (var message in twinMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                        {
                            responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                        }
                    }
                }
                //Set Capture Date P17 = P15 + 1Day
                //if (informationCode.Equals(BicConstData.BicNetworkCutover))
                //{
                //    fieldPos = 17;
                //    _responseBicMessage.SetMessageField(fieldPos,systemNextDateString.Substring(5, 2) +
                //                                         systemNextDateString.Substring(8, 2));
                //    if (fieldPos >= 65)
                //        secondArray[fieldPos - 1 - 64] = 0x31;
                //    else
                //        firstArray[fieldPos - 1] = 0x31;
                //}
                if (informationCode.Equals(TwinTransactionConst.NetworkManagementInformationCode.Cutover))
                {
                    field = FIELD.F_17;
                    responseTwinMessage.Core().Field(field, systemNextDateString.Substring(5, 2) + systemNextDateString.Substring(8, 2));
                }

                field = FIELD.F_39;
                responseTwinMessage.Core().Field(field, responseCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0800 Exception:{ex.Message}");
                responseTwinMessage = null;
            }


        }
        public TWINIso8583 GetTwinMessage()
        {
            return (responseTwinMessage);
        }
    }
}

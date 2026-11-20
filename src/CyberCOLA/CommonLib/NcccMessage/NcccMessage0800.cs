using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
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
    public class NcccMessage0800
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private NcccIso8583 responseNcccMessage;
        private readonly MessageType ncccMessageType;
        private const string NetWorkMessageType = "0800";

        /// <remarks>
        /// This Active Request Message(Example. Auto SignOn Message Network Management Information Code Bust Be 001)
        /// </remarks>
        public NcccMessage0800(string informationCode)
        {
            try
            {
                if (informationCode.Length != 3)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "NcccMessage0800:" + informationCode,
                              "Create 0800 Message Error For InformationCode Error");

                    throw new ArgumentException("Create 0800 Message Error For InformationCode Error");
                }
                if (!(informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn) ||
                      informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOff) ||
                      informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.ChangeKey) ||
                      informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.RepeatKey) ||
                      informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.VerifyKey) ||
                      informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "NcccMessage0800:" + informationCode,
                              "0800 Message InformationCode Invalid");

                    throw new ArgumentException("0800 SignOn Message InformationCode Invalid");
                }
                NcccMessageTypeMapInfo.TryGetValue("0800", out ncccMessageType);
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode($"ISO{00:00}{60:00}{000:000}{6:0}{0:0}");
                responseNcccMessage.Mti($"{Convert.ToInt32(NetWorkMessageType):0000}");

                FIELD field = FIELD.F_7;
                responseNcccMessage.Core().Field(field, DateTime.Now.AddHours(-8).ToString("MMddHHmmss"));
                field = FIELD.F_11;
                responseNcccMessage.Core().Field(field, ColaCommUtility.GetSystemTraceNumber());

                if ((informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn)) ||
                    (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.RepeatKey)) ||
                    (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.ChangeKey)) ||
                    (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.VerifyKey)))
                {
                    field = FIELD.F_48;
                    responseNcccMessage.Core().Field(field, NcccTransactionConst.FieldConstant.F48.NetworkConstData);
                }
                //01 = Outbound key only
                //02 = Inbound key only
                //03 = both incoming and outgoing keys
                if ((informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.ChangeKey)) ||
                    (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.RepeatKey)) ||
                    (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.VerifyKey)))
                {
                    field = FIELD.F_53;
                    responseNcccMessage.Core().Field(field, "0002000000000000");
                }
                field = FIELD.F_70;
                responseNcccMessage.Core().Field(field, informationCode);

                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.VerifyKey))
                {
                    field = FIELD.F_120; //For IWK Check Digit
                    string testString = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCIwkCheckDigits;
                    responseNcccMessage.Core().Field(field, SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCIwkCheckDigits);
                }

                //if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.ChangeKey))
                //{

                //    field = FIELD.F_123;
                //    string temp = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID;
                //    string tempString = String.Format(SystemConstantData.CommonData.NcccRsiStringFormat, SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID);
                //    responseNcccMessage.Core().Field(field, tempString);
                //}

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "NcccMessage0800 Exception");
                responseNcccMessage = null;
            }
        }

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public NcccMessage0800(NcccIso8583 requestNcccMessage)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get NcccMessage0800 MessageType Error:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                string informationCode = string.Empty;
                string responseCode = "00";
                string settlementMMDD = string.Empty;
                string systemNextDateString = string.Empty;

                FIELD field = FIELD.F_70;
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    informationCode = (string)requestNcccMessage.Core().Field(field);
                    if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.Cutover))
                    {
                        field = FIELD.F_15;
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            settlementMMDD = (string)requestNcccMessage.Core().Field(field);
                            systemNextDateString = ColaCommUtility.GetNextSettlementDateForCutover(SystemConstantData.CommonData.PasystfNCCCInfo, settlementMMDD);
                            if (string.IsNullOrEmpty(systemNextDateString))
                            {
                                Log.Error($"GetNextSettlementDateForCutover Error[{SystemConstantData.CommonData.PasystfNCCCInfo}]");
                                responseNcccMessage = null;
                                return;
                            }

                            int spRetnCode = StoreProcUtility.UpdateColaPasystf(SystemConstantData.CommonData.ColaBu, SystemConstantData.CommonData.PasystfNCCCSystemDteUpdate, systemNextDateString);
                            if (spRetnCode != 0)
                            {
                                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                                    new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                                    "NcccMessage0800",
                                    "StoreProcedure UpdateColaPasystf Error :" + spRetnCode.ToString());
                                responseNcccMessage = null;
                                return;
                            }
                            SingletonOfCommonProperty.Instance.SetColaPasystfInfoSystemDate(systemNextDateString, SystemConstantData.CommonData.PasystfNCCCInfo);
                        }
                    }
                }

                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, requestNcccMessage.Core().Field(field));
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
                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.Cutover))
                {
                    field = FIELD.F_17;
                    responseNcccMessage.Core().Field(field, systemNextDateString.Substring(5, 2) + systemNextDateString.Substring(8, 2));
                }

                field = FIELD.F_39;
                responseNcccMessage.Core().Field(field, responseCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0800 Exception:{ex.Message}");
                responseNcccMessage = null;
            }


        }
        public NcccIso8583 GetNcccMessage()
        {
            return (responseNcccMessage);
        }
    }
}

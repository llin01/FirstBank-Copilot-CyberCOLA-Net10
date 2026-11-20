using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.FirstHSMUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    public class NcccMessage0800NewKey
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private static readonly ConcurrentDictionary<string, MessageType> NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private NcccIso8583 responseNcccMessage;
        private string rtnCode;
        private string rsnCode;

        /// <remarks>
        /// This Response Network Message For Request Message(TR31 NewKey)
        /// </remarks>
        public NcccMessage0800NewKey(NcccIso8583 requestNcccMessage)
        {
            try
            {
                NcccMessageTypeMapInfo.TryGetValue(requestNcccMessage.Mti(), out var ncccMessageType);
                if (ncccMessageType == null)
                {
                    Log.Error($"Get  NcccMessage0800TR31NewKey MessageType Erro:{requestNcccMessage.Mti()}");
                    responseNcccMessage = null;
                    return;
                }
                responseNcccMessage = new NcccIso8583();
                responseNcccMessage.Header().Decode(requestNcccMessage.Header().Encode());
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                string responseCode;
                string settlementMMDD = string.Empty;
                FIELD field;
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;          //Skip Secondary Bit Map
                                                             //if (intFieldPos == 110) continue;      //Skip TR31 Pin Block
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
                //Call Des Check Value Is OK
                field = FIELD.F_110;
                string s120FieldData;
                string _checkDigit = string.Empty;
                var _s110PkBlock = !ObjectUtils.IsNull(requestNcccMessage.RawVal(field)) ? requestNcccMessage.RawVal(field).ToString() : string.Empty;
                field = FIELD.F_120;
                if (!ObjectUtils.IsNull(requestNcccMessage.RawVal(field)))
                {
                    s120FieldData = (string)requestNcccMessage.Core().Field(field);
                    _checkDigit = s120FieldData.Substring(0, 4);
                }
                if ((_checkDigit.Equals(string.Empty)) || (_s110PkBlock.Trim().Equals(string.Empty)))
                {
                    responseCode = SystemConstantData.CommonData.SystemProcessErrorCode;
                }
                else
                {
                    var _s110FieldLength = _s110PkBlock.ToString().Length;
                    var hsmInputData = FirstHSMUtility.HSMCreateChangeReceiveIWKAWK(_s110PkBlock, "NCCC", SystemParameterInfo.BankInfo.BankCode, _checkDigit);
                    var stringData = Encoding.ASCII.GetString(hsmInputData);
                    var returnString = FirstHSMUtility.ChangeReceivingKeyKB(stringData.Substring(0, 8),
                                                                              stringData.Substring(8, 8),
                                                                              stringData.Substring(16, 3),
                                                                              stringData.Substring(19, 3),
                                                                              stringData.Substring(28, 3),
                                                                              stringData.Substring(31, _s110FieldLength),
                                                                              stringData.Substring(22, 4),
                                                                              stringData.Substring(26, 1),
                                                                              stringData.Substring(27, 1),
                                                                              out rtnCode,
                                                                              out rsnCode);
                    Log.Info($"CallChangeReceivingKey, rtnCode[{rtnCode}], rsnCode[{rsnCode}], returnString[{returnString}]");
                    if (rtnCode.Equals("00"))
                    {
                        hsmInputData = FirstHSMUtility.HSMNewKeyReplaceIWKAWK("NCCC", SystemParameterInfo.BankInfo.BankCode);
                        stringData = Encoding.ASCII.GetString(hsmInputData);
                        returnString = FirstHSMUtility.CallNewKeyReplaceOldKey(stringData.Substring(0, 8),
                                                                             stringData.Substring(8, 3),
                                                                             stringData.Substring(11, 3),
                                                                             stringData.Substring(14, 1),
                                                                             stringData.Substring(15, 1),
                                                                             stringData.Substring(16, 1),
                                                                             stringData.Substring(17, 1),
                                                                             out rtnCode,
                                                                             out rsnCode);
                        Log.Info($"CallNewKeyReplaceOldKey, rtnCode[{rtnCode}], rsnCode[{rsnCode}], returnString[{returnString}]");
                        if (rtnCode.Equals("00"))
                            responseCode = "00";
                        else
                        {
                            responseCode = SystemConstantData.CommonData.SystemProcessErrorCode;
                            //errorESM = String.Format(SystemConstantData.CommonData.NcccEsmStringFormat, SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID, "F");
                        }

                    }
                    else
                    {
                        responseCode = SystemConstantData.CommonData.SystemProcessErrorCode;
                        //errorESM = string.Format(SystemConstantData.CommonData.NcccEsmStringFormat, SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID, "F");
                    }
                }

                FIELD fieldPos = FIELD.F_39;
                responseNcccMessage.Core().Field(fieldPos, responseCode);

                if (responseCode.Equals("00"))
                {
                    int spRetnCode = StoreProcUtility.UpdateColaPasystf(SystemConstantData.CommonData.ColaBu, 2, _checkDigit + "00");
                    if (spRetnCode != 0)
                    {
                        Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                                  new System.Diagnostics.StackTrace(true).GetFrame(0).GetMethod().Name,
                                  "NcccMessage0800TR31NewKey",
                                  "StoreProcedure UpdateColaPasystf Check Digits Error :" + spRetnCode.ToString());
                        throw new ArgumentException("StoreProcedure UpdateColaPasystf Check Digits Error");
                    }
                    SingletonOfCommonProperty.Instance.SetColaPasystfInfoIwkCheckDigits(_checkDigit + "00");
                }

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"NcccMessage0800TR31NewKey Exception:{ex.Message}");
                responseNcccMessage = null;
            }
        }
        public NcccIso8583 GetNcccMessage()
        {
            return (responseNcccMessage);
        }
    }
}

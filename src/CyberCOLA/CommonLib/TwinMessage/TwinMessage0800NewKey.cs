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
    public class TwinMessage0800NewKey
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> TwinMessageTypeMapInfo = SingletonOfTwinMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private TWINIso8583 responseTwinMessage;

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public TwinMessage0800NewKey(TWINIso8583 requestTwinMessage)
        {
            try
            {
                TwinMessageTypeMapInfo.TryGetValue(requestTwinMessage.Mti(), out var twinMessageType);
                if (twinMessageType == null)
                {
                    Log.Error($"Get TwinMessage0800NewKey MessageType Erro:{requestTwinMessage.Mti()}");
                    responseTwinMessage = null;
                    return;
                }
                responseTwinMessage = new TWINIso8583();
                responseTwinMessage.Header().Decode(requestTwinMessage.Header().Encode());
                responseTwinMessage.Mti(twinMessageType.ResponseMessage);
                string responseCode = "96";
                string settlementMMDD = string.Empty;
                foreach (var message in twinMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;          //Skip Secondary Bit Map
                    if (intFieldPos == 123) continue;        //Skip Cryptographic Service Message
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestTwinMessage.RawVal(field)))
                        {
                            responseTwinMessage.Core().Field(field, requestTwinMessage.Core().Field(field));
                        }
                    }
                }
                //Call Des Check Value Is OK
                //modified by TWIN Card spec.
                //BicCsmType bicCsmData = new BicCsmType(requestTwinMessage);
                //string errorESM = string.Empty;
                //if ((bicCsmData.CheckDigit.Equals(string.Empty)) || (bicCsmData.NewKeyValue.Equals(string.Empty)))
                //{
                //	responseCode = SystemConstantData.CommonData.SystemProcessErrorCode;
                //	errorESM = string.Format(SystemConstantData.CommonData.NcccEsmStringFormat, SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID, "F");
                //}
                //else
                //{
                //	var hsmInputData = FirstHSMUtility.HSMCreateChangeReceiveIWKAWK(bicCsmData);
                //	var stringData = Encoding.ASCII.GetString(hsmInputData);
                //	var returnString = FirstHSMUtility.CallChangeReceivingKey(stringData.Substring(0, 2),
                //															 stringData.Substring(2, 2),
                //															 stringData.Substring(4, 32),
                //															 out rtnCode,
                //															 out rsnCode,
                //															 out var keySync);
                //
                //	Log.Info($"CallChangeReceivingKey, rtnCode[{rtnCode}], rsnCode[{rsnCode}], returnString[{returnString}]");
                //	if (rtnCode.Equals("00") && keySync.Substring(0,4).Equals(bicCsmData.CheckDigit))
                //	{
                //		hsmInputData = FirstHSMUtility.HSMNewKeyReplaceIWKAWK();
                //		stringData = Encoding.ASCII.GetString(hsmInputData);
                //		returnString = FirstHSMUtility.CallNewKeyReplaceOldKey(stringData.Substring(0, 2),
                //															 stringData.Substring(2, 2),
                //															 out rtnCode,
                //															 out rsnCode);
                //		Log.Info($"CallNewKeyReplaceOldKey, rtnCode[{rtnCode}], rsnCode[{rsnCode}], returnString[{returnString}]");
                //		if (rtnCode.Equals("00"))
                //			responseCode = "00";
                //		else
                //		{
                //			//if ((retnCodeString.Equals("27")) ||
                //			//    (retnCodeString.Equals("40")) ||
                //			//    (retnCodeString.Equals("41")) ||
                //			//    (retnCodeString.Equals("42")) ||
                //			//    (retnCodeString.Equals("43")) ||
                //			//    (retnCodeString.Equals("44")) ||
                //			//    (retnCodeString.Equals("45")) ||
                //			//    (retnCodeString.Equals("46")) ||
                //			//    (retnCodeString.Equals("47")))
                //			//    responseCode = "12";
                //			//else
                //			responseCode = SystemConstantData.CommonData.SystemProcessErrorCode;
                //			errorESM = String.Format(SystemConstantData.CommonData.NcccEsmStringFormat, SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID, "F");
                //		}
                //
                //	}
                //	else
                //	{
                //		//if ((retnCodeString.Equals("27")) ||
                //		//    (retnCodeString.Equals("40")) ||
                //		//    (retnCodeString.Equals("41")) ||
                //		//    (retnCodeString.Equals("42")) ||
                //		//    (retnCodeString.Equals("43")) ||
                //		//    (retnCodeString.Equals("44")) ||
                //		//    (retnCodeString.Equals("45")) ||
                //		//    (retnCodeString.Equals("46")) ||
                //		//    (retnCodeString.Equals("47")))
                //		//    responseCode = "12";
                //		//else
                //		responseCode = SystemConstantData.CommonData.SystemProcessErrorCode;
                //		errorESM = string.Format(SystemConstantData.CommonData.NcccEsmStringFormat, SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankID, "F");
                //	}
                //}

                FIELD fieldPos = FIELD.F_39;
                responseTwinMessage.Core().Field(fieldPos, responseCode);

                fieldPos = FIELD.F_123;
                //if (!responseCode.Equals("00"))
                //{
                //	responseTwinMessage.Core().Field(fieldPos, errorESM);
                //}
                //else
                //{
                //	string tempString = (string)requestTwinMessage.Core().Field(fieldPos);
                //	//string replaceString = tempString.Replace(@"RCV/BC35 ORG/NCCC", @"RCV/NCCC ORG/BC35");
                //	string replaceString = tempString.Replace($"RCV/BC{SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankCode} ORG/NCCC", $"RCV/NCCC ORG/BC{SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().BankInfo.BankCode}");
                //
                //	responseTwinMessage.Core().Field(fieldPos,replaceString);
                //}
                //if (responseCode.Equals("00"))
                //{
                //	int spRetnCode = StoreProcUtility.UpdateColaPasystf(SystemConstantData.CommonData.ColaBu, SystemConstantData.CommonData.PasystfNCCCIwkUpdate, bicCsmData.CheckDigit + "00");
                //	if (spRetnCode != 0)
                //	{
                //		Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                //				  new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                //				  "NcccMessage0800NewKey",
                //				  "StoreProcedure UpdateColaPasystf Check Digits Error :" + spRetnCode.ToString());
                //		throw new ArgumentException("StoreProcedure UpdateColaPasystf Check Digits Error");
                //	}
                //	SingletonOfCommonProperty.Instance.SetColaPasystfInfoIwkCheckDigits(bicCsmData.CheckDigit + "00");
                //}

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"TwinMessage0800NewKey Exception:{ex.Message}");
                responseTwinMessage = null;
            }
        }
        public TWINIso8583 GetTwinMessage()
        {
            return (responseTwinMessage);
        }
    }
}

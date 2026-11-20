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
    public class FiscMessage0800
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> FiscMessageTypeMapInfo = SingletonOfFiscMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private FISC responseFiscMessage;
        private readonly MessageType fiscMessageType;
        private const string NetWorkMessageType = "0800";

        /// <remarks>
        /// This Active Request Message(Example. Auto SignOn Message Network Management Information Code Bust Be 001)
        /// </remarks>
        public FiscMessage0800(string informationCode)
        {
            try
            {
                if (informationCode.Length != 3)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "FiscMessage0800:" + informationCode,
                              "Create 0800 Message Error For InformationCode Error");

                    throw new ArgumentException("Create 0800 Message Error For InformationCode Error");
                }
                if (!(informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.SignOn) ||
                      informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.SignOff) ||
                      informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeOnDemandRequest) ||
                      informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest) ||
                      informationCode.Equals(FISCTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "FiscMessage0800:" + informationCode,
                              "0800 Message InformationCode Invalid");

                    throw new ArgumentException("0800 SignOn Message InformationCode Invalid");
                }
                FiscMessageTypeMapInfo.TryGetValue("0800", out fiscMessageType);
                responseFiscMessage = new FISC();
                responseFiscMessage.Mti($"{Convert.ToInt32(NetWorkMessageType):0000}");
                FIELD field = FIELD.F_2;
                responseFiscMessage.Core().Field(field, FISCTransactionConst.MemberDefinedData.FISCField2Data.FISCNetworkF2NCID);
                field = FIELD.F_7;
                responseFiscMessage.Core().Field(field, TimeUtils.CurrentIso8583UtcDatetime());
                field = FIELD.F_11;
                responseFiscMessage.Core().Field(field, SerialGenerator.Stan());
                field = FIELD.F_33;
                responseFiscMessage.Core().Field(field, FISCTransactionConst.MemberDefinedData.FISCField2Data.FISCNetworkF2NCID);
                field = FIELD.F_63;
                responseFiscMessage.Core().Field(field, FISCTransactionConst.MemberDefinedData.FISCField2Data.FISCNetworkData);
                field = FIELD.F_70;
                responseFiscMessage.Core().Field(field, informationCode);
                field = FIELD.F_94;
                var serviceIndicator = (FISCTransactionConst.ServiceIndicator.ReservedForFutureUse.Default
                                           + FISCTransactionConst.ServiceIndicator.AcquirerIssuerIndicator.AcquirerOnly
                                           + FISCTransactionConst.ServiceIndicator.AddressDataIndicator.AvsNotSupport)
                                           .PadRight(7, ' ');
                responseFiscMessage.Core().Field(field, serviceIndicator);
                field = FIELD.F_96;
                responseFiscMessage.Core().Field(field, "0000000000000000");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "FiscMessage0800 Exception");
                responseFiscMessage = null;
            }
        }

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public FiscMessage0800(FISC requestFiscMessage)
        {
            try
            {
                FiscMessageTypeMapInfo.TryGetValue(requestFiscMessage.Mti(), out fiscMessageType);
                if (fiscMessageType == null)
                {
                    Log.Error($"Get FiscMessage0800 MessageType Error:{requestFiscMessage.Mti()}");
                    responseFiscMessage = null;
                    return;
                }
                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);

                string informationCode = string.Empty;
                string responseCode = "00";
                string settlementMMDD = string.Empty;
                string systemNextDateString = string.Empty;

                FIELD field;
                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(field)))
                        {
                            responseFiscMessage.Core().Field(field, requestFiscMessage.Core().Field(field));
                        }
                    }
                }
                field = FIELD.F_39;
                responseFiscMessage.Core().Field(field, responseCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FiscMessage0800 Exception:{ex.Message}");
                responseFiscMessage = null;
            }


        }
        public FISC GetFiscMessage()
        {
            return (responseFiscMessage);
        }
    }
}

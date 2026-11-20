using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.IcashMessage
{
    /// <remarks>
    /// This ICASH 0800 Message Class
    /// </remarks>
    public class IcashMessage0800
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> IcashMessageTypeMapInfo = SingletonOfIcashMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly ICASH responseIcashMessage;
        private readonly MessageType icashMessageType;

        private const string NetWorkMessageType = "0800";

        private readonly Regex _returnPattern = new Regex("[mMCc]");
        /// <remarks>
        /// This Active Request Message(Exp. Auto SignOn Message Network Management Information Code Bust Be 001)
        /// </remarks>
        public IcashMessage0800(string informationCode)
        {
            try
            {
                if (informationCode.Length != 3)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "IcashMessage0800:" + informationCode,
                              "Create ICASH 0800 Message Error For InformationCode Error");

                    throw new ArgumentException("Create ICASH 0800 Message Error For InformationCode Error");
                }
                if (!(informationCode.Equals(IcashTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     informationCode.Equals(IcashTransactionConst.NetworkManagementInformationCode.SignOff) ||
                     informationCode.Equals(IcashTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "IcashMessage0800:" + informationCode,
                              "ICASH 0800 Message InformationCode Invalid");

                    throw new ArgumentException("ICASH 0800 Message InformationCode Invalid");
                }

                IcashMessageTypeMapInfo.TryGetValue("0800", out icashMessageType);
                responseIcashMessage = new ICASH();
                //SrcID + DestID
                responseIcashMessage.Header().Decode($"{IcashTransactionConst.MessageHeader.IcashIssuerID}{IcashTransactionConst.MessageHeader.IcashHostID}");
                responseIcashMessage.Mti($"{Convert.ToInt32(NetWorkMessageType):0000}");

                FIELD field = FIELD.F_7;
                responseIcashMessage.Core().Field(field, DateTime.Now.ToString("MMddHHmmss"));

                field = FIELD.F_11;
                responseIcashMessage.Core().Field(field, ColaCommUtility.GetSystemTraceNumber());

                field = FIELD.F_70;
                responseIcashMessage.Core().Field(field, informationCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "IcashMessage0800 Exception");
                responseIcashMessage = null;
            }
        }

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public IcashMessage0800(ICASH requestIcashMessage)
        {
            try
            {
                IcashMessageTypeMapInfo.TryGetValue(requestIcashMessage.Mti(), out icashMessageType);
                if (icashMessageType == null)
                {
                    Log.Error($"Get IcashMessage0800 MessageType Error:{requestIcashMessage.Mti()}");
                    responseIcashMessage = null;
                    return;
                }


                string responseCode = "00";
                FIELD field;
                responseIcashMessage = new ICASH();
                responseIcashMessage.Header().Decode($"{requestIcashMessage.Header().DestID()}{requestIcashMessage.Header().SrcID()}");
                responseIcashMessage.Mti(icashMessageType.ResponseMessage);

                foreach (var message in icashMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestIcashMessage.RawVal(field)))
                        {
                            responseIcashMessage.Core().Field(field, requestIcashMessage.Core().Field(field));
                        }
                    }
                }
                field = FIELD.F_39;
                responseIcashMessage.Core().Field(field, responseCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "IcashMessage0800 Exception");
                responseIcashMessage = null;
            }
        }
        public ICASH GetIcashMessage()
        {
            return (responseIcashMessage);
        }
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.IpassMessage
{
    /// <remarks>
    /// This IPASS 0800 Message Class
    /// </remarks>
    public class IpassMessage0800
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> IpassMessageTypeMapInfo = SingletonOfIpassMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly IPASS responseIpassMessage;
        private readonly MessageType ipassMessageType;

        private const string NetWorkMessageType = "0800";

        private readonly Regex _returnPattern = new Regex("[mMCc]");
        /// <remarks>
        /// This Active Request Message(Exp. Auto SignOn Message Network Management Information Code Bust Be 001)
        /// </remarks>
        public IpassMessage0800(string informationCode)
        {
            try
            {
                if (informationCode.Length != 3)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "IpassMessage0800:" + informationCode,
                              "Create IPASS 0800 Message Error For InformationCode Error");

                    throw new ArgumentException("Create IPASS 0800 Message Error For InformationCode Error");
                }
                if (!(informationCode.Equals(IpassTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     informationCode.Equals(IpassTransactionConst.NetworkManagementInformationCode.SignOff) ||
                     informationCode.Equals(IpassTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "IpassMessage0800:" + informationCode,
                              "IPASS 0800 Message InformationCode Invalid");

                    throw new ArgumentException("IPASS 0800 Message InformationCode Invalid");
                }

                IpassMessageTypeMapInfo.TryGetValue("0800", out ipassMessageType);
                responseIpassMessage = new IPASS();
                //DestID + SrcID
                responseIpassMessage.Header().Decode($"{IpassTransactionConst.MessageHeader.IpassHostID}{IpassTransactionConst.MessageHeader.IpassIssuerID}");
                responseIpassMessage.Mti($"{Convert.ToInt32(NetWorkMessageType):0000}");

                FIELD field = FIELD.F_7;
                responseIpassMessage.Core().Field(field, DateTime.Now.ToString("MMddHHmmss"));

                field = FIELD.F_11;
                responseIpassMessage.Core().Field(field, ColaCommUtility.GetSystemTraceNumber());

                field = FIELD.F_70;
                responseIpassMessage.Core().Field(field, informationCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "IpassMessage0800 Exception");
                responseIpassMessage = null;
            }
        }

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public IpassMessage0800(IPASS requestIpassMessage)
        {
            try
            {
                IpassMessageTypeMapInfo.TryGetValue(requestIpassMessage.Mti(), out ipassMessageType);
                if (ipassMessageType == null)
                {
                    Log.Error($"Get IpassMessage0800 MessageType Error:{requestIpassMessage.Mti()}");
                    responseIpassMessage = null;
                    return;
                }


                string responseCode = "00";
                FIELD field;
                responseIpassMessage = new IPASS();
                responseIpassMessage.Header().Decode($"{requestIpassMessage.Header().SrcID()}{requestIpassMessage.Header().DestID()}");
                responseIpassMessage.Mti(ipassMessageType.ResponseMessage);

                foreach (var message in ipassMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestIpassMessage.RawVal(field)))
                        {
                            responseIpassMessage.Core().Field(field, requestIpassMessage.Core().Field(field));
                        }
                    }
                }
                field = FIELD.F_39;
                responseIpassMessage.Core().Field(field, responseCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "IpassMessage0800 Exception");
                responseIpassMessage = null;
            }
        }
        public IPASS GetIpassMessage()
        {
            return (responseIpassMessage);
        }
    }
}

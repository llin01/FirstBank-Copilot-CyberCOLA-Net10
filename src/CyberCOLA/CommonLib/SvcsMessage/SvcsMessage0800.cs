using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.SvcsMessage
{
    /// <remarks>
    /// This SVCS 0800 Message Class
    /// </remarks>
    public class SvcsMessage0800
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ConcurrentDictionary<string, MessageType> SvcsMessageTypeMapInfo = SingletonOfSvcsMessageTypeMap.Instance.GetMessageTypeMap();
        protected static Regex FindPattern = new Regex("[cCmM]");
        private readonly SVCS responseSvcsMessage;
        private readonly MessageType svcsMessageType;

        private const string NetWorkMessageType = "0800";

        private readonly Regex _returnPattern = new Regex("[mMCc]");
        /// <remarks>
        /// This Active Request Message(Exp. Auto SignOn Message Network Management Information Code Bust Be 001)
        /// </remarks>
        public SvcsMessage0800(string informationCode)
        {
            try
            {
                if (informationCode.Length != 3)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "SvcsMessage0800:" + informationCode,
                              "Create SVCS 0800 Message Error For InformationCode Error");

                    throw new ArgumentException("Create SVCS 0800 Message Error For InformationCode Error");
                }
                if (!(informationCode.Equals(SvcsTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     informationCode.Equals(SvcsTransactionConst.NetworkManagementInformationCode.SignOff) ||
                     informationCode.Equals(SvcsTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                              new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                              "SvcsMessage0800:" + informationCode,
                              "SVCS 0800 Message InformationCode Invalid");

                    throw new ArgumentException("SVCS 0800 Message InformationCode Invalid");
                }

                SvcsMessageTypeMapInfo.TryGetValue("0800", out svcsMessageType);
                responseSvcsMessage = new SVCS();
                //DestID + SrcID
                responseSvcsMessage.Header().Decode($"{SvcsTransactionConst.MessageHeader.SvcsHostID}{SvcsTransactionConst.MessageHeader.SvcsIssuerID}");
                responseSvcsMessage.Mti($"{Convert.ToInt32(NetWorkMessageType):0000}");

                FIELD field = FIELD.F_7;
                responseSvcsMessage.Core().Field(field, DateTime.Now.ToString("MMddHHmmss"));

                field = FIELD.F_11;
                responseSvcsMessage.Core().Field(field, ColaCommUtility.GetSystemTraceNumber());

                field = FIELD.F_70;
                responseSvcsMessage.Core().Field(field, informationCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SvcsMessage0800 Exception");
                responseSvcsMessage = null;
            }
        }

        /// <remarks>
        /// This Response Network Message For Request Message
        /// </remarks>
        public SvcsMessage0800(SVCS requestSvcsMessage)
        {
            try
            {
                SvcsMessageTypeMapInfo.TryGetValue(requestSvcsMessage.Mti(), out svcsMessageType);
                if (svcsMessageType == null)
                {
                    Log.Error($"Get SvcsMessage0800 MessageType Error:{requestSvcsMessage.Mti()}");
                    responseSvcsMessage = null;
                    return;
                }


                string responseCode = "00";
                FIELD field;
                responseSvcsMessage = new SVCS();
                responseSvcsMessage.Header().Decode($"{requestSvcsMessage.Header().SrcID()}{requestSvcsMessage.Header().DestID()}");
                responseSvcsMessage.Mti(svcsMessageType.ResponseMessage);

                foreach (var message in svcsMessageType.ResponseField.Field)
                {
                    int intFieldPos = Convert.ToInt32(message.Number);
                    if (intFieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (FindPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(requestSvcsMessage.RawVal(field)))
                        {
                            responseSvcsMessage.Core().Field(field, requestSvcsMessage.Core().Field(field));
                        }
                    }
                }
                field = FIELD.F_39;
                responseSvcsMessage.Core().Field(field, responseCode);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SvcsMessage0800 Exception");
                responseSvcsMessage = null;
            }
        }
        public SVCS GetSvcsMessage()
        {
            return (responseSvcsMessage);
        }
    }
}

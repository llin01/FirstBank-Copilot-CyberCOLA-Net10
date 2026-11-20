// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscColaInboundMessageTypeConvertHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscColaInboundMessageTypeConvertHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.FISC;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.FISC.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.TM.FISC.Transaction;

    #endregion

    public class FiscColaInboundMessageTypeConvertHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            switch (fisc.Mti())
            {
                case "0800":
                    //if (FISCTransactionConst.NetworkManagementInformationCode.SignOn.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscSignOnRequest().Core(fisc.Core());
                    //}
                    //else if (FISCTransactionConst.NetworkManagementInformationCode.SignOff.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscSignOffRequest().Core(fisc.Core());
                    //}
                    //else if (FISCTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscEchoTestRequest().Core(fisc.Core());
                    //}
                    //else if (FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscNewKeyRequest().Core(fisc.Core());
                    //}
                    //else
                    //{
                    //    fisc = new FiscRequest().Core(fisc.Core());
                    //    payload.Val = fisc;
                    //}
                    fisc = new FiscRequest().Core(fisc.Core());
                    payload.Val = fisc;
                    break;
                case "0810":
                    //if (FISCTransactionConst.NetworkManagementInformationCode.SignOn.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscSignOnResponse().Core(fisc.Core());
                    //}
                    //else if (FISCTransactionConst.NetworkManagementInformationCode.SignOff.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscSignOffResponse().Core(fisc.Core());
                    //}
                    //else if (FISCTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscEchoTestResponse().Core(fisc.Core());
                    //}
                    //else if (FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest.Equals(fisc.Val(FIELD.F_70)))
                    //{
                    //    payload.Val = new FiscNewKeyResponse().Core(fisc.Core());
                    //}
                    //else
                    //{
                    //    fisc = new FiscResponse().Core(fisc.Core());
                    //    payload.Val = fisc;
                    //}
                    fisc = new FiscRequest().Core(fisc.Core());
                    payload.Val = fisc;
                    break;
                default:
                    if (Convert.ToUInt16(fisc.Mti()[2]) % 2 == 0)
                    {
                        fisc = new FiscRequest().Core(fisc.Core());
                    }
                    else
                    {
                        fisc = new FiscResponse().Core(fisc.Core());
                    }
                    //C.F Add For Test Process Response
                    if (fisc is FiscRequest)
                    {
                        var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{fisc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                       );

                        ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.FISC.FISC>>()[relationId] = fisc;
                    }
                    payload.Val = fisc;
                    break;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

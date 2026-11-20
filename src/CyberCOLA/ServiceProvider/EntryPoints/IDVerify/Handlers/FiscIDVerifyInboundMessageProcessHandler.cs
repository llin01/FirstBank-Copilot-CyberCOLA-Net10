// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyInboundMessageProcessHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyInboundMessageTypeConvertHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IDVerify.Handlers
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.ServiceProvider.Resources;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.FISC;

    #endregion
    public class FiscIDVerifyInboundMessageProcessHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {

            this.Logger.Debug($"Go Hander: {this.GetType().FullName}, Timestamp[{ctx.Channel.Carrier<ReadTransactionTimeKey, DateTime>()}]");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            //Update FiscChannelInfo
            var fiscChannelInfo = ctx.Channel.Carrier<FiscChannelInfoKey, ChannelSessionInfo>();
            fiscChannelInfo?.Update(fisc.Encode().Val());
            ctx.Channel.Carrier<FiscChannelInfoKey>(fiscChannelInfo);
            //
            switch (fisc.Mti())
            {
                case "0800":
                    if (FISCTransactionConst.NetworkManagementInformationCode.SignOn.Equals(fisc.Val(FIELD.F_70)))
                    {
                        payload.Val = FISCTransCreator.FiscSignOnRequest().Core(fisc.Core());
                    }
                    else if (FISCTransactionConst.NetworkManagementInformationCode.SignOff.Equals(fisc.Val(FIELD.F_70)))
                    {
                        payload.Val = FISCTransCreator.FiscSignOffRequest().Core(fisc.Core());
                    }
                    else if (FISCTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(fisc.Val(FIELD.F_70)))
                    {
                        payload.Val = FISCTransCreator.FiscEchoTestRequest().Core(fisc.Core());
                    }

                    break;
                case "0810":
                    if (FISCTransactionConst.NetworkManagementInformationCode.SignOn.Equals(fisc.Val(FIELD.F_70)))
                    {
                        payload.Val = FISCTransCreator.FiscSignOnResponse().Core(fisc.Core());
                    }
                    else if (FISCTransactionConst.NetworkManagementInformationCode.SignOff.Equals(fisc.Val(FIELD.F_70)))
                    {
                        payload.Val = FISCTransCreator.FiscSignOffResponse().Core(fisc.Core());
                    }
                    else if (FISCTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(fisc.Val(FIELD.F_70)))
                    {
                        payload.Val = FISCTransCreator.FiscEchoTestResponse().Core(fisc.Core());
                    }
                    //return : no process this event 20170412(Gateway send message), comment return : process this event
                    //return;
                    break;
                ////Test for Bug
                //case "0310":
                //    return;
                //    break;
                default:
                    //Add For Header Bug
                    if (Convert.ToUInt16(fisc.Mti()[2]) % 2 == 0)
                    {
                        fisc = (TM.FISC.FISC)FISCTransCreator.FiscRequest().Core(fisc.Core());
                    }
                    else
                    {
                        fisc = (TM.FISC.FISC)FISCTransCreator.FiscResponse().Core(fisc.Core());
                    }

                    payload.Val = fisc;
                    break;
            }
            //var cyberGWFISCIDVerify = ctx.Channel.Carrier<ServiceKey, CyberGWFISCIDVerfy>();
            //GlobalResource.FiscIDVerfyColaService.Put(fisc);
            base.ChannelRead(ctx, msg);
        }
    }
}


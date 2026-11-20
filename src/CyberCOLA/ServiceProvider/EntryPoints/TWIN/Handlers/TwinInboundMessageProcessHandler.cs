// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinInboundMessageProcessHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinInboundMessageTypeConvertHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.Resources;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583.TWIN;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion
    public class TwinInboundMessageProcessHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {

            this.Logger.Debug($"Go Hander: {this.GetType().FullName}, Timestamp[{ctx.Channel.Carrier<ReadTransactionTimeKey, DateTime>()}]");
            var payload = (PayloadContext)msg;
            var twin = (TWINIso8583)payload.Val;
            //Update TwinChannelInfo
            var twinChannelInfo = ctx.Channel.Carrier<TwinChannelInfoKey, ChannelSessionInfo>();
            twinChannelInfo?.Update(twin.Encode().Val());
            ctx.Channel.Carrier<TwinChannelInfoKey>(twinChannelInfo);
            //
            switch (twin.Mti())
            {
                case "0800":
                    if (TwinTransactionConst.FieldConstant.F70.SignOn.Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = TwinTransCreator.TwinSignOnRequest().Core(twin.Core());
                    }
                    else if (TwinTransactionConst.FieldConstant.F70.SignOff.Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = TwinTransCreator.TwinSignOffRequest().Core(twin.Core());
                    }
                    else if (TwinTransactionConst.FieldConstant.F70.EchoTest.Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = TwinTransCreator.TwinEchoTestRequest().Core(twin.Core());
                    }

                    break;
                case "0810":
                    if (TwinTransactionConst.FieldConstant.F70.SignOn.Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = TwinTransCreator.TwinSignOnResponse().Core(twin.Core());
                    }
                    else if (TwinTransactionConst.FieldConstant.F70.SignOff.Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = TwinTransCreator.TwinSignOffResponse().Core(twin.Core());
                    }
                    else if (TwinTransactionConst.FieldConstant.F70.EchoTest.Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = TwinTransCreator.TwinEchoTestResponse().Core(twin.Core());
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
                    if (Convert.ToUInt16(twin.Mti()[2]) % 2 == 0)
                    {
                        var header = twin.Header().Encode();
                        twin = (TWINIso8583)TwinTransCreator.TwinRequest().Core(twin.Core());
                        twin.Header().Decode(header);
                    }
                    else
                    {
                        var header = twin.Header().Encode();
                        twin = (TWINIso8583)TwinTransCreator.TwinResponse().Core(twin.Core());
                        twin.Header().Decode(header);
                    }

                    payload.Val = twin;
                    break;
            }
            //var cyberGWTWIN = ctx.Channel.Carrier<ServiceKey, CyberGWTWIN>();
            //GlobalResource.TwinColaService.Put(twin);
            base.ChannelRead(ctx, msg);
        }
    }
}


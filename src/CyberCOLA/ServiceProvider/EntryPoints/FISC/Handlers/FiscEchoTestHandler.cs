// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscEchoHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.Handlers
{
    #region DEPENDENCY
    using System;
    using System.Threading;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class FiscEchoTestHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>())
                            {
                                var processEchoFlag = false;
                                var nowDateTime = DateTime.Now;
                                var tranDateTime = ctx.Channel.Carrier<ReadTransactionTimeKey, DateTime>();
                                {
                                    var diff = (nowDateTime - tranDateTime).Minutes;
                                    if (diff >= 3) processEchoFlag = true;
                                    ctx.Channel.Carrier<ReadTransactionTimeKey>(tranDateTime);
                                }

                                if (processEchoFlag)
                                {
                                    ctx.Channel.Write(new PayloadContext(FISCTransCreator.FiscEchoTestGWRequest()));
                                }

                                //if (ctx.Channel.Carrier<MaintAutoEchoKey, bool>())
                                //{
                                //    ctx.Channel.Write(new PayloadContext(FISCTransCreator.FiscEchoTestRequest()));
                                //}
                            }
                        },
                    null,
                    5000,
                    120000));
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            if (!ObjectUtils.IsNull(ctx.Channel.Carrier<EchoTimerKey, Timer>()))
            {
                ctx.Channel.Carrier<EchoTimerKey, Timer>().Dispose();
            }

            ctx.Channel.RemoveCarrier<EchoTimerKey>();
            base.ChannelInActived(ctx);
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            if (fisc is EchoTest && fisc.Mti().Equals("0800"))
            {
                var response = FISCTransCreator.FiscEchoTestResponse();
                response.RawVal(FIELD.F_7, fisc.RawVal(FIELD.F_7))
                        .RawVal(FIELD.F_11, fisc.RawVal(FIELD.F_11))
                        .RawVal(FIELD.F_37, fisc.RawVal(FIELD.F_37))
                        .RawVal(FIELD.F_39, "00")
                        .RawVal(FIELD.F_48, fisc.RawVal(FIELD.F_48))
                        .RawVal(FIELD.F_63, fisc.RawVal(FIELD.F_63))
                        .RawVal(FIELD.F_70, fisc.RawVal(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                return;
            }
            //if (fisc is SignOn && fisc.Mti().Equals("0800"))
            //{
            //    var response = FISCTransCreator.FiscSignOnResponse();
            //    response.RawVal(FIELD.F_7, fisc.RawVal(FIELD.F_7))
            //            .RawVal(FIELD.F_11, fisc.RawVal(FIELD.F_11))
            //            .RawVal(FIELD.F_37, fisc.RawVal(FIELD.F_37))
            //            .RawVal(FIELD.F_39, "00")
            //            .RawVal(FIELD.F_48, fisc.RawVal(FIELD.F_48))
            //            .RawVal(FIELD.F_70, fisc.RawVal(FIELD.F_70));
            //    ctx.Channel.WriteAndFlush(new PayloadContext(response));
            //    return;
            //}
            //if (fisc is SignOff && fisc.Mti().Equals("0800"))
            //{
            //    var response = FISCTransCreator.FiscSignOffResponse();
            //    response.RawVal(FIELD.F_7, fisc.RawVal(FIELD.F_7))
            //            .RawVal(FIELD.F_11, fisc.RawVal(FIELD.F_11))
            //            .RawVal(FIELD.F_37, fisc.RawVal(FIELD.F_37))
            //            .RawVal(FIELD.F_39, "00")
            //            .RawVal(FIELD.F_48, fisc.RawVal(FIELD.F_48))
            //            .RawVal(FIELD.F_70, fisc.RawVal(FIELD.F_70));
            //    ctx.Channel.WriteAndFlush(new PayloadContext(response));
            //    return;
            //}
            base.ChannelRead(ctx, msg);
        }
    }
}

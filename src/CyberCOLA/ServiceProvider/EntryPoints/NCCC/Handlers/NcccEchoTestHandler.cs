// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="EchoHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC.Handlers
{
    #region DEPENDENCY
    using System;
    using System.Threading;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class NcccEchoTestHandler : ChannelInboundHandler
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
                                    ctx.Channel.Write(new PayloadContext(NcccTransCreator.EchoTestGWRequest()));
                                }

                                //if (ctx.Channel.Carrier<MaintAutoEchoKey, bool>())
                                //{
                                //    ctx.Channel.Write(new PayloadContext(NcccTransCreator.EchoTestRequest()));
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
            var nccc = (NcccIso8583)payload.Val;
            if (nccc is EchoTestRequest)
            {
                var response = NcccTransCreator.EchoTestResponse();
                response.RawVal(FIELD.F_7, nccc.RawVal(FIELD.F_7))
                    .RawVal(FIELD.F_11, nccc.RawVal(FIELD.F_11))
                    .RawVal(FIELD.F_37, nccc.RawVal(FIELD.F_37))
                    .RawVal(FIELD.F_39, "00")
                    .RawVal(FIELD.F_48, nccc.RawVal(FIELD.F_48))
                    .RawVal(FIELD.F_70, nccc.RawVal(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                return;
            }

            if (nccc is EchoTestResponse)
            {
                //pass EchoTestResponse
                //return;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

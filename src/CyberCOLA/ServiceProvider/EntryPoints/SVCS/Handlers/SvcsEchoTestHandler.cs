// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file=" SvcsEchoHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SvcsEchoHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsEchoTestHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>().Default(false))
                            {
                                ctx.Channel.Write(new PayloadContext(SvcsTransCreator.SvcsEchoTestGWRequest()));
                            }
                        },
                    null,
                    5000,
                    120000));

            base.ChannelActived(ctx);
        }

        //public override void ChannelInActived(ChannelHandlerContext ctx)
        //{
        //    if (!ObjectUtils.IsNull(ctx.Channel.Carrier<EchoTimerKey, Timer>()))
        //    {
        //        ctx.Channel.Carrier<EchoTimerKey, Timer>().Dispose();
        //    }
        //
        //    ctx.Channel.RemoveCarrier<EchoTimerKey>();
        //    base.ChannelInActived(ctx);
        //}

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            if (!ObjectUtils.IsNull(ctx.Channel.Carrier<EchoTimerKey, Timer>()))
            {
                ctx.Channel.Carrier<EchoTimerKey, Timer>().Dispose();
            }

            ctx.Channel.RemoveCarrier<EchoTimerKey>();
            base.ChannelUnRegistered(ctx);
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            if (svcs is SvcsEchoTestRequest)
            {
                var response = new SvcsEchoTestResponse();
                response.Header().Decode($"{svcs.Header().SrcID()}{svcs.Header().DestID()}");
                response.Val(FIELD.F_7, svcs.Val(FIELD.F_7))
                        .Val(FIELD.F_11, svcs.Val(FIELD.F_11))
                        .Val(FIELD.F_39, "00")
                        .Val(FIELD.F_70, svcs.Val(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                return;
            }

            if (svcs is SvcsEchoTestResponse)
            {
                //pass EchoTestResponse
                return;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

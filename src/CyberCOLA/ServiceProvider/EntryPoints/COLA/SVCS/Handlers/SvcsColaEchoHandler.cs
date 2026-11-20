// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="EchoTestHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoTestHandler type.
//   Use Channel Carrier Key AutoEchoKey:bool
//   Use Channel Carrier Key EchoTimerKey:Timer
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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaEchoHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            Logger.Info($"ServerColaEchoHandler ChannelActived {ctx.Channel.Carrier<AutoEchoKey, bool>()}");
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>())
                            {
                                if (ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Take(ctx.Channel))
                                {
                                    ctx.Channel.Write(new PayloadContext(SvcsTransCreator.SvcsEchoTestRequest()));
                                }
                            }
                        },
                    null,
                    5000,
                    120000));

            base.ChannelActived(ctx);
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            if (svcs is SvcsEchoTestResponse)
            {
                if (ctx.Channel.Carrier<ServiceKey, SvcsColaService>().IsTake(ctx.Channel))
                {
                    ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Release(ctx.Channel);
                }

                return;
            }

            if (svcs is SvcsEchoTestRequest)
            {
                ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Take(ctx.Channel);
                var response = new SvcsEchoTestResponse();
                response.Header().Decode($"{svcs.Header().SrcID()}{svcs.Header().DestID()}");
                response.Val(FIELD.F_7, svcs.Val(FIELD.F_7))
                        .Val(FIELD.F_11, svcs.Val(FIELD.F_11))
                        .Val(FIELD.F_39, "00")
                        .Val(FIELD.F_70, svcs.Val(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Release(ctx.Channel);
                return;
            }

            base.ChannelRead(ctx, msg);
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            if (!ObjectUtils.IsNull(ctx.Channel.Carrier<EchoTimerKey, Timer>()))
            {
                ctx.Channel.Carrier<EchoTimerKey, Timer>().Dispose();
            }

            ctx.Channel.RemoveCarrier<EchoTimerKey>();
            base.ChannelUnRegistered(ctx);
        }
    }
}

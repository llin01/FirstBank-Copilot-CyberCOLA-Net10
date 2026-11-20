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
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccColaEchoHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            Logger.Info($"NcccColaEchoHandler ChannelActived {ctx.Channel.Carrier<AutoEchoKey, bool>()}");
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>())
                            {
                                if (ctx.Channel.Carrier<ServiceKey, NcccColaService>().Take(ctx.Channel))
                                {
                                    ctx.Channel.Write(new PayloadContext(NcccTransCreator.EchoTestRequest()));
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
            var nccc = (NcccIso8583)payload.Val;
            if (nccc is EchoTestResponse)
            {
                if (ctx.Channel.Carrier<ServiceKey, NcccColaService>().IsTake(ctx.Channel))
                {
                    ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);
                }

                return;
            }

            if (nccc is EchoTestRequest)
            {
                ctx.Channel.Carrier<ServiceKey, NcccColaService>().Take(ctx.Channel);
                var response = new EchoTestResponse();
                response.Val(FIELD.F_7, nccc.Val(FIELD.F_7))
                    .Val(FIELD.F_11, nccc.Val(FIELD.F_11))
                    .Val(FIELD.F_39, "00")
                    .Val(FIELD.F_70, nccc.Val(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);
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

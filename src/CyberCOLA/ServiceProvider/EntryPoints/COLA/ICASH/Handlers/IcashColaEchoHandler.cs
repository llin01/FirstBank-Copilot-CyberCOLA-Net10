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
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaEchoHandler : ChannelInboundHandler
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
                                if (ctx.Channel.Carrier<ServiceKey, IcashColaService>().Take(ctx.Channel))
                                {
                                    ctx.Channel.Write(new PayloadContext(IcashTransCreator.IcashEchoTestRequest()));
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
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            if (icash is IcashEchoTestResponse)
            {
                if (ctx.Channel.Carrier<ServiceKey, IcashColaService>().IsTake(ctx.Channel))
                {
                    ctx.Channel.Carrier<ServiceKey, IcashColaService>().Release(ctx.Channel);
                }

                return;
            }

            if (icash is IcashEchoTestRequest)
            {
                ctx.Channel.Carrier<ServiceKey, IcashColaService>().Take(ctx.Channel);
                var response = new IcashEchoTestResponse();
                response.Header().Decode($"{icash.Header().DestID()}{icash.Header().SrcID()}");
                response.Val(FIELD.F_7, icash.Val(FIELD.F_7))
                        .Val(FIELD.F_11, icash.Val(FIELD.F_11))
                        .Val(FIELD.F_39, "00")
                        .Val(FIELD.F_70, icash.Val(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                ctx.Channel.Carrier<ServiceKey, IcashColaService>().Release(ctx.Channel);
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

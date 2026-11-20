// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyColaEchoHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyColaEchoHandler type.
//   Use Channel Carrier Key AutoEchoKey:bool
//   Use Channel Carrier Key EchoTimerKey:Timer
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IDVerify.Handlers
{
    #region DEPENDENCY

    using System.Threading;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.TM.FISC;

    #endregion

    public class FiscIDVerifyColaEchoHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            Logger.Info($"FiscIDVerifyColaEchoHandler ChannelActived {ctx.Channel.Carrier<AutoEchoKey, bool>()}");
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>())
                            {
                                if (ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Take(ctx.Channel))
                                {
                                    ctx.Channel.Write(new PayloadContext(FISCTransCreator.FiscEchoTestGWRequest()));
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
            var fisc = (TM.FISC.FISC)payload.Val;
            if (fisc is EchoTest && fisc.Mti().Equals("0810"))
            {
                if (ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().IsTake(ctx.Channel))
                {
                    ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Release(ctx.Channel);
                }

                return;
            }

            if (fisc is EchoTest && fisc.Mti().Equals("0800"))
            {
                ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Take(ctx.Channel);
                var response = FISCTransCreator.FiscEchoTestResponse();
                response.RawVal(FIELD.F_7, fisc.RawVal(FIELD.F_7))
                        .RawVal(FIELD.F_11, fisc.RawVal(FIELD.F_11))
                        .RawVal(FIELD.F_37, fisc.RawVal(FIELD.F_37))
                        .RawVal(FIELD.F_39, "00")
                        .RawVal(FIELD.F_48, fisc.RawVal(FIELD.F_48))
                        .RawVal(FIELD.F_70, fisc.RawVal(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Release(ctx.Channel);
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

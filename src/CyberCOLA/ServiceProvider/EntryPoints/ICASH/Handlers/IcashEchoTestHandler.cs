// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file=" IcashEchoHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashEchoHandler type.
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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashEchoTestHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>().Default(false))
                            {
                                ctx.Channel.Write(new PayloadContext(IcashTransCreator.IcashEchoTestGWRequest()));
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
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            if (icash is IcashEchoTestRequest)
            {
                var response = new IcashEchoTestResponse();
                response.Header().Decode($"{icash.Header().DestID()}{icash.Header().SrcID()}");
                response.Val(FIELD.F_7, icash.Val(FIELD.F_7))
                        .Val(FIELD.F_11, icash.Val(FIELD.F_11))
                        .Val(FIELD.F_39, "00")
                        .Val(FIELD.F_70, icash.Val(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                return;
            }

            if (icash is IcashEchoTestResponse)
            {
                //pass EchoTestResponse
                return;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

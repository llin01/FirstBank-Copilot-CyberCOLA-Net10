// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file=" IpassEchoHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassEchoHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassEchoTestHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<EchoTimerKey>(
                new Timer(
                    state =>
                        {
                            if (ctx.Channel.Carrier<AutoEchoKey, bool>().Default(false))
                            {
                                ctx.Channel.Write(new PayloadContext(IpassTransCreator.IpassEchoTestGWRequest()));
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
            var ipass = (TM.ISO8583.IPASS.IPASS)payload.Val;
            if (ipass is IpassEchoTestRequest)
            {
                var response = new IpassEchoTestResponse();
                response.Header().Decode($"{ipass.Header().SrcID()}{ipass.Header().DestID()}");
                response.Val(FIELD.F_7, ipass.Val(FIELD.F_7))
                        .Val(FIELD.F_11, ipass.Val(FIELD.F_11))
                        .Val(FIELD.F_39, "00")
                        .Val(FIELD.F_70, ipass.Val(FIELD.F_70));
                ctx.Channel.WriteAndFlush(new PayloadContext(response));
                return;
            }

            if (ipass is IpassEchoTestResponse)
            {
                //pass EchoTestResponse
                return;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion
    public class IcashColaChannelStatusHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, IcashColaService>().Release(ctx.Channel);
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, IcashColaService>().Remove(ctx.Channel);
            base.ChannelInActived(ctx);
        }
    }
}

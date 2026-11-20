using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion
    public class NcccColaChannelStatusHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, NcccColaService>().Remove(ctx.Channel);
            base.ChannelInActived(ctx);
        }
    }
}

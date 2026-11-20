using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.QRCode.Handlers
{
    #region DEPENDENCY

    #endregion
    public class NcccQRCodeColaChannelStatusHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, NcccQRCodeColaService>().Release(ctx.Channel);
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, NcccQRCodeColaService>().Remove(ctx.Channel);
            base.ChannelInActived(ctx);
        }
    }
}

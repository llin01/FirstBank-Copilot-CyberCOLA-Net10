namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IDVerify.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion
    public class FiscIDVerifyColaChannelStatusHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Release(ctx.Channel);
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Remove(ctx.Channel);
            base.ChannelInActived(ctx);
        }
    }
}

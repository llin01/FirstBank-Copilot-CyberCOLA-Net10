namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using Com.Cybersoft4u.Yetty.Context;

    #endregion

    public interface IChannelInboundHandler : IChannelHandler
    {
        void ChannelActived(ChannelHandlerContext ctx);

        void ChannelInActived(ChannelHandlerContext ctx);

        void ChannelRegistered(ChannelHandlerContext ctx);

        void ChannelUnRegistered(ChannelHandlerContext ctx);

        void ChannelRead(ChannelHandlerContext ctx, object msg);

        void ChannelReadCompleted(ChannelHandlerContext ctx);
    }
}

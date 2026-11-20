namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.Yetty.Context;

    #endregion

    public interface IChannelHandler
    {
        void ChannelAdded(ChannelHandlerContext ctx);

        void ChannelRemoved(ChannelHandlerContext ctx);

        void ExceptionCaught(ChannelHandlerContext ctx, Exception cause);
    }
}

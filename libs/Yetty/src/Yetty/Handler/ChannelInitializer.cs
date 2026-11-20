namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Context;

    #endregion

    public abstract class ChannelInitializer : IChannelInboundHandler
    {
        public void ChannelAdded(ChannelHandlerContext ctx)
        {
        }

        public void ChannelRemoved(ChannelHandlerContext ctx)
        {
        }

        public virtual void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            ctx.FireExceptionCaught(cause);
        }

        public void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.FireChannelActived();
        }

        public void ChannelInActived(ChannelHandlerContext ctx)
        {
            ctx.FireChannelInActived();
        }

        public void ChannelRegistered(ChannelHandlerContext ctx)
        {
            var channel = ctx.Channel;
            var pipe = ctx.Pipe;
            pipe.Remove(this);
            this.InitChannel(channel);
            pipe.FireChannelRegistered();
        }

        public void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            ctx.FireChannelUnRegistered();
        }

        public void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            ctx.FireChannelRead(msg);
        }

        public void ChannelReadCompleted(ChannelHandlerContext ctx)
        {
            ctx.FireChannelReadCompleted();
        }

        public abstract void InitChannel(IChannel channel);
    }
}

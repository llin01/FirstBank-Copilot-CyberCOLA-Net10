namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Utils;

    #endregion

    public class ChannelInboundHandler : IChannelInboundHandler
    {
        private Logger logger;

        protected Logger Logger
        {
            get
            {
                return this.logger ?? (this.logger = LoggerAdapter.GetLogger(this.GetType().FullName));
            }

            set
            {
                this.logger = value;
            }
        }

        public virtual void ChannelAdded(ChannelHandlerContext ctx)
        {
        }

        public virtual void ChannelRemoved(ChannelHandlerContext ctx)
        {
        }

        public virtual void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            ctx.FireExceptionCaught(cause);
        }

        public virtual void ChannelActived(ChannelHandlerContext ctx)
        {
            ctx.FireChannelActived();
        }

        public virtual void ChannelInActived(ChannelHandlerContext ctx)
        {
            ctx.FireChannelInActived();
        }

        public virtual void ChannelRegistered(ChannelHandlerContext ctx)
        {
            ctx.FireChannelRegistered();
        }

        public virtual void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            ctx.FireChannelUnRegistered();
        }

        public virtual void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            ctx.FireChannelRead(msg);
        }

        public virtual void ChannelReadCompleted(ChannelHandlerContext ctx)
        {
            ctx.FireChannelReadCompleted();
        }
    }
}

namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;
    using System.Net;

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Utils;

    #endregion

    public class ChannelOutboundHandler : IChannelOutboundHandler
    {
        private Logger logger;

        protected Logger Logger
        {
            get
            {
                return this.logger ?? (this.logger = LoggerAdapter.GetLogger(this.GetType().FullName));
            }
        }

        public virtual void ChannelAdded(ChannelHandlerContext ctx)
        {
        }

        public virtual void ChannelRemoved(ChannelHandlerContext ctx)
        {
        }

        public void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
        }

        public void Read(ChannelHandlerContext ctx)
        {
            ctx.Read();
        }

        public virtual void Write(ChannelHandlerContext ctx, object msg)
        {
            ctx.Write(msg);
        }

        public virtual void Flush(ChannelHandlerContext ctx)
        {
            ctx.Flush();
        }

        public virtual void Bind(ChannelHandlerContext ctx, EndPoint localAddress)
        {
            ctx.Bind(localAddress);
        }

        public virtual void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint)
        {
            ctx.Connect(remoteEndPoint);
        }

        public virtual void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            ctx.Connect(remoteEndPoint, localEndPoint);
        }

        public virtual void Disconnect(ChannelHandlerContext ctx)
        {
            ctx.Disconnect();
        }

        public virtual void Close(ChannelHandlerContext ctx)
        {
            ctx.Close();
        }
    }
}

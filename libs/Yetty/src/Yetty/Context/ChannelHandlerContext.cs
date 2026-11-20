namespace Com.Cybersoft4u.Yetty.Context
{
    #region DEPENDENCY

    using System;
    using System.Diagnostics;
    using System.Net;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.Yetty.Utils;
    using Com.Cybersoft4u.Utils;

    #endregion

    /// <summary>
    /// Represents the context of a channel handler in the pipeline, providing access to the channel, 
    /// pipeline, and methods to fire events to the next handler.
    /// </summary>
    public class ChannelHandlerContext
    {
        protected internal ChannelHandlerContext Next = null;

        protected internal ChannelHandlerContext Prev = null;

        private Logger logger;
        internal ChannelHandlerContext(IChannelPipe pipe, IChannelHandler handler, string name)
        {
            if (pipe == null)
            {
                throw new ArgumentNullException(nameof(pipe));
            }

            this.Pipe = pipe;
            this.Channel = this.Pipe.Channel();
            this.Handler = handler;
            this.Name = name;
            logger = LoggerAdapter.GetLogger(this.GetType().FullName);

        }

        public IChannelHandler Handler { get; protected set; }

        public IChannel Channel { get; protected internal set; }

        public IChannelPipe Pipe { get; protected internal set; }

        public string Name { get; protected set; }

        public void FireExceptionCaught(Exception cause)
        {
            try
            {
                var next = this.FindNextInbound();
                if (next != null)
                {
                    next.Handler.ExceptionCaught(next, cause);
                }

            }
            catch (Exception ex)
            {
                logger.Error($"FireExceptionCaught Exception : {ex.Message}");
            }
        }

        public void FireChannelActived()
        {
            var next = this.FindNextInbound();
            if (next == null)
            {
                return;
            }

            var handler = next.Handler as IChannelInboundHandler;
            try
            {
                Debug.Assert(handler != null, "handler != null");
                handler?.ChannelActived(next);
            }
            catch (Exception exception)
            {
                handler?.ExceptionCaught(next, exception);
                throw;
            }
        }

        public void FireChannelInActived()
        {
            var next = this.FindNextInbound();
            if (next == null)
            {
                return;
            }

            var handler = next.Handler as IChannelInboundHandler;
            try
            {
                Debug.Assert(handler != null, "handler != null");
                handler?.ChannelInActived(next);
            }
            catch (Exception exception)
            {
                handler.ExceptionCaught(next, exception);
                throw;
            }
        }

        public void FireChannelRegistered()
        {
            var next = this.FindNextInbound();
            if (next == null)
            {
                return;
            }

            var handler = next.Handler as IChannelInboundHandler;
            try
            {
                Debug.Assert(handler != null, "handler != null");
                handler?.ChannelRegistered(next);
            }
            catch (Exception exception)
            {
                handler?.ExceptionCaught(next, exception);
                throw;
            }
        }

        public void FireChannelUnRegistered()
        {
            var next = this.FindNextInbound();
            if (next == null)
            {
                return;
            }

            var handler = next.Handler as IChannelInboundHandler;
            try
            {
                Debug.Assert(handler != null, "handler != null");
                handler?.ChannelUnRegistered(next);
            }
            catch (Exception exception)
            {
                handler?.ExceptionCaught(next, exception);
                throw;
            }
        }

        public void FireChannelRead(object msg)
        {
            var next = this.FindNextInbound();
            if (next == null)
            {
                return;
            }

            var handler = next.Handler as IChannelInboundHandler;
            try
            {
                Debug.Assert(handler != null, "handler != null");
                handler?.ChannelRead(next, msg);
            }
            catch (Exception exception)
            {
                handler?.ExceptionCaught(next, exception);
                throw;
            }
        }

        public void FireChannelReadCompleted()
        {
            var next = this.FindNextInbound();
            if (next == null)
            {
                return;
            }

            var handler = next.Handler as IChannelInboundHandler;
            try
            {
                Debug.Assert(handler != null, "handler != null");
                handler?.ChannelReadCompleted(next);
            }
            catch (Exception exception)
            {
                handler?.ExceptionCaught(next, exception);
                throw;
            }
        }

        public void Read()
        {
            var next = this.FindNextOutbound();
            if (next == null)
            {
                return;
            }

            ((IChannelOutboundHandler)next.Handler).Read(next);
        }

        public void Write(object msg)
        {
            var next = this.FindNextOutbound();
            if (next == null)
            {
                return;
            }

            ((IChannelOutboundHandler)next.Handler).Write(next, msg);
        }

        public void Flush()
        {
            var next = this.FindNextOutbound();
            if (next == null)
            {
                return;
            }

            ((IChannelOutboundHandler)next.Handler).Flush(next);
        }

        public void Bind(EndPoint localEndPoint)
        {
            var next = this.FindNextOutbound();
            if (next == null)
            {
                return;
            }

            ((IChannelOutboundHandler)next.Handler).Bind(next, localEndPoint);
        }

        public void Connect(EndPoint remoteEndPoint)
        {
            var next = this.FindNextOutbound();
            if (next == null)
            {
                return;
            }

            ((IChannelOutboundHandler)next.Handler).Connect(next, remoteEndPoint);
        }

        public void Connect(EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            var next = this.FindNextOutbound();
            if (next == null)
            {
                return;
            }

            ((IChannelOutboundHandler)next.Handler)?.Connect(next, remoteEndPoint, localEndPoint);
        }

        public void Disconnect()
        {
            try
            {
                var next = this.FindNextOutbound();
                if (next == null)
                {
                    return;
                }
                ((IChannelOutboundHandler)next.Handler)?.Disconnect(next);
            }
            catch (Exception ex)
            {
                logger.Error($"ChannelHandlerContext Disconnect Exception : {ex.Message}");
            }
        }

        public void Close()
        {
            try
            {
                var next = this.FindNextOutbound();
                if (next == null)
                {
                    return;
                }
                ((IChannelOutboundHandler)next.Handler)?.Close(next);
            }
            catch (Exception ex)
            {
                logger.Error($"ChannelHandlerContext Close Exception : {ex.Message}");
            }
        }

        protected ChannelHandlerContext FindNextInbound()
        {
            var ctx = this;
            do
            {
                ctx = ctx.Next;
            }
            while (!ctx.IsInbound());
            return ctx;
        }

        protected ChannelHandlerContext FindNextOutbound()
        {
            var ctx = this;
            do
            {
                ctx = ctx.Prev;
            }
            while (!ctx.IsOutbound());
            return ctx;
        }

        private bool IsInbound(IChannelHandler handler = null)
        {
            return handler == null && this.Handler is IChannelInboundHandler;
        }

        private bool IsOutbound(IChannelHandler handler = null)
        {
            return handler == null && this.Handler is IChannelOutboundHandler;
        }

        protected internal class HeadChannelHandlerContext : ChannelHandlerContext
        {
            public HeadChannelHandlerContext(IChannelPipe pipe)
                : base(pipe, new HeadChannelHandler(pipe), "HeadChannelHandlerContext")
            {
            }
        }

        protected internal class TailChannelHandlerContext : ChannelHandlerContext
        {
            public TailChannelHandlerContext(IChannelPipe pipe)
                : base(pipe, new TailChannelHandler(), "TailChannelHandlerContext")
            {
            }
        }

        protected class HeadChannelHandler : IChannelOutboundHandler
        {
            private readonly IChannelPipe pipe;

            public HeadChannelHandler(IChannelPipe pipe)
            {
                this.pipe = pipe;
            }

            public void ChannelAdded(ChannelHandlerContext ctx)
            {
            }

            public void ChannelRemoved(ChannelHandlerContext ctx)
            {
            }

            public void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
            {
                ctx.FireExceptionCaught(cause);
            }

            public void Read(ChannelHandlerContext ctx)
            {
                this.pipe.Channel().Core().StartRead();
            }

            public void Write(ChannelHandlerContext ctx, object msg)
            {
                if (msg == null)
                {
                    throw new Yetty.Exception.InvalidValueException();
                }

                if (msg is ByteBuf)
                {
                    this.pipe.Channel().Core().Write((ByteBuf)msg);
                    return;
                }

                if (msg is byte[])
                {
                    this.pipe.Channel().Core().Write(new ByteBuf((byte[])msg));
                    return;
                }

                throw new Yetty.Exception.InvalidTypeException("msg");
            }

            public void Flush(ChannelHandlerContext ctx)
            {
                this.pipe.Channel().Core().Flush();
            }

            public void Bind(ChannelHandlerContext ctx, EndPoint localAddress)
            {
                this.pipe.Channel().Core().Bind(localAddress);
            }

            public void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint)
            {
                this.pipe.Channel().Core().Connect(remoteEndPoint);
            }

            public void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint, EndPoint localEndPoint)
            {
                this.pipe.Channel().Core().Connect(remoteEndPoint, localEndPoint);
            }

            public void Disconnect(ChannelHandlerContext ctx)
            {
                this.pipe.Channel().Core().Disconnect();
            }

            public void Close(ChannelHandlerContext ctx)
            {
                this.pipe.Channel().Core().Close();
            }
        }

        protected class TailChannelHandler : ChannelInboundHandler
        {
            public override void ChannelAdded(ChannelHandlerContext ctx)
            {
            }

            public override void ChannelRemoved(ChannelHandlerContext ctx)
            {
            }

            public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
            {
                //Console.Write("Reach Tail.\n");
                this.Logger.Info("ExceptionCaught Reach Tail.");
                try
                {
                    if (!(cause is TprsException))
                    {
                        //Console.Write(cause);
                        this.Logger.Info($"ExceptionCaught Exception : {cause.Message}");
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Error($"ExceptionCaught Check Exception : {ex.Message}");
                }
            }

            public override void ChannelActived(ChannelHandlerContext ctx)
            {
            }

            public override void ChannelInActived(ChannelHandlerContext ctx)
            {
            }

            public override void ChannelRegistered(ChannelHandlerContext ctx)
            {
            }

            public override void ChannelUnRegistered(ChannelHandlerContext ctx)
            {
            }

            public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            {
                //Console.Write("Reach Tail.\n");
                this.Logger.Info("ChannelRead Reach Tail.");
            }

            public override void ChannelReadCompleted(ChannelHandlerContext ctx)
            {
            }
        }
    }
}

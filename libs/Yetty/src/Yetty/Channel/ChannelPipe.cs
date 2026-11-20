namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System;
    using System.Net;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class ChannelPipe : IChannelPipe
    {
        private readonly IChannel channel;

        private readonly ChannelHandlerContext.HeadChannelHandlerContext head;

        private readonly ChannelHandlerContext.TailChannelHandlerContext tail;

        public ChannelPipe(IChannel channel)
        {
            this.channel = channel;
            this.head = new ChannelHandlerContext.HeadChannelHandlerContext(this);
            this.tail = new ChannelHandlerContext.TailChannelHandlerContext(this);
            this.head.Next = this.tail;
            this.tail.Prev = this.head;
        }

        public IChannel Channel()
        {
            return this.channel;
        }

        public void FireChannelActived()
        {
            this.head.FireChannelActived();
        }

        public void FireChannelInActived()
        {
            this.head.FireChannelInActived();
        }

        public void FireChannelRegistered()
        {
            this.head.FireChannelRegistered();
        }

        public void FireChannelUnRegistered()
        {
            this.head.FireChannelUnRegistered();
        }

        public void FireChannelRead(object msg)
        {
            this.head.FireChannelRead(msg);
        }

        public void FireChannelReadCompleted()
        {
            this.head.FireChannelReadCompleted();
        }

        public void Read()
        {
            this.tail.Read();
        }

        public void Write(object msg)
        {
            this.tail.Write(msg);
        }

        public void Flush()
        {
            this.tail.Flush();
        }

        public void WriteAndFlush(object msg)
        {
            this.tail.Write(msg);
            this.tail.Flush();
        }

        public void Bind(EndPoint localEndPoint)
        {
            this.tail.Bind(localEndPoint);
        }

        public void Connect(EndPoint remoteEndPoint)
        {
            this.tail.Connect(remoteEndPoint);
        }

        public void Connect(EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            this.tail.Connect(remoteEndPoint, localEndPoint);
        }

        public void Disconnect()
        {
            this.tail.Disconnect();
        }

        public void Close()
        {
            this.tail.Close();
        }

        public IChannelPipe AddFirst(IChannelHandler channelHandler, string name = null)
        {
            this.ValidateHandler(channelHandler, name);
            var ctx = new ChannelHandlerContext(this, channelHandler, name) { Next = this.head.Next, Prev = this.head };
            this.head.Next.Prev = ctx;
            this.head.Next = ctx;
            channelHandler.ChannelAdded(ctx);
            return this;
        }

        public IChannelPipe AddLast(IChannelHandler channelHandler, string name = null)
        {
            this.ValidateHandler(channelHandler, name);
            var ctx = new ChannelHandlerContext(this, channelHandler, name) { Next = this.tail, Prev = this.tail.Prev };
            this.tail.Prev.Next = ctx;
            this.tail.Prev = ctx;
            channelHandler.ChannelAdded(ctx);
            return this;
        }

        public IChannelPipe Remove(IChannelHandler channelHandler)
        {
            for (var ctx = this.head.Next; ctx != this.tail; ctx = ctx.Next)
            {
                if (!ctx.Handler.Equals(channelHandler))
                {
                    continue;
                }

                ctx.Prev.Next = ctx.Next;
                ctx.Next.Prev = ctx.Prev;
                return this;
            }

            throw new NoSuchElementException("channelHandler");
        }

        public IChannelPipe Remove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            for (var ctx = this.head.Next; ctx != this.tail; ctx = ctx.Next)
            {
                if (!ctx.Name.Equals(name))
                {
                    continue;
                }

                ctx.Prev.Next = ctx.Next;
                ctx.Next.Prev = ctx.Prev;
                return this;
            }

            throw new NoSuchElementException(name);
        }

        public IChannelPipe RemoveFirst()
        {
            if (this.head.Next.Equals(this.tail))
            {
                throw new NoSuchElementException("no ChannelHandlerContext");
            }

            var ctx = this.head.Next;
            ctx.Prev.Next = ctx.Next;
            ctx.Next.Prev = ctx.Prev;
            return this;
        }

        public IChannelPipe RemoveLast()
        {
            if (this.tail.Prev.Equals(this.head))
            {
                throw new NoSuchElementException("no ChannelHandlerContext");
            }

            var ctx = this.tail.Prev;
            ctx.Prev.Next = ctx.Next;
            ctx.Next.Prev = ctx.Prev;
            return this;
        }

        private void ValidateHandler(IChannelHandler channelHandler, string name)
        {
            if (channelHandler == null)
            {
                throw new ArgumentNullException(nameof(channelHandler));
            }

            for (var ctx = this.head.Next; ctx != this.tail; ctx = ctx.Next)
            {
                if (ctx.Handler.Equals(channelHandler))
                {
                    throw new DuplicateHandlerException();
                }

                if (name != null && ctx.Name.Equals(name))
                {
                    throw new DuplicateHandlerException();
                }
            }
        }
    }
}

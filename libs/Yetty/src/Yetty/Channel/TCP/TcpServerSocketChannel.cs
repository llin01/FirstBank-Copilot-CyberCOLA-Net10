namespace Com.Cybersoft4u.Yetty.Channel.TCP
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using Com.Cybersoft4u.Yetty.Exception;

    #endregion

    public class TcpServerSocketChannel : ServerSocketChannel
    {
        private const int BufferSize = 8192;

        private readonly TcpChannelCore core;

        protected internal TcpServerSocketChannel()
        {
            this.core = new TcpChannelCore(this, BufferSize);
            this.AsyncFuture = new AsyncFuture(this);
            this.ChildChannels = new ConcurrentDictionary<TcpSocketChannel, TcpSocketChannel>();
            this.Backlog = 10240;
        }

        public int Backlog { get; internal set; }

        public bool IsShutdown { get; private set; }

        public bool IsBind { get; private set; }

        protected internal AsyncFuture AsyncFuture { get; private set; }

        protected internal ConcurrentDictionary<TcpSocketChannel, TcpSocketChannel> ChildChannels { get; private set; }

        public int IOControl(ChannelIOControlCode code, byte[] inValues, byte[] outValues)
        {
            return this.core.IOControl(code, inValues, outValues);
        }

        public override AsyncFuture Bind(EndPoint localEndPoint)
        {
            if (this.IsBind)
            {
                throw new SocketOperationException("Already Bind");
            }

            this.IsBind = true;
            try
            {
                this.AsyncFuture.Reset();
                this.Pipe().Bind(localEndPoint);
                this.AsyncFuture.Set();
            }
            catch (SocketOperationException)
            {
                throw;
            }

            return this.AsyncFuture;
        }

        public override void Close()
        {
            this.IsShutdown = true;
            foreach (var childChannel in this.ChildChannels.Values)
            {
                childChannel.Disconnect();
            }

            this.ChildChannels.Clear();
            this.Pipe().Close();
            this.Pipe().FireChannelInActived();
            this.Pipe().FireChannelUnRegistered();
        }

        public override ChannelCore Core()
        {
            return this.core;
        }

        public sealed override IChannel Read()
        {
            throw new NotSupportedException();
        }

        public sealed override IChannel Write(object data)
        {
            throw new NotSupportedException();
        }

        public sealed override IChannel Flush()
        {
            throw new NotSupportedException();
        }

        public sealed override IChannel WriteAndFlush(object data)
        {
            throw new NotSupportedException();
        }

        public override EndPoint RemoteEndPoint()
        {
            throw new NotSupportedException();
        }

        public override EndPoint LocalEndPoint()
        {
            return this.core.BaseSocket.LocalEndPoint;
        }

        public sealed override AsyncFuture Connect(EndPoint remoteEndPoint)
        {
            throw new NotSupportedException();
        }

        public sealed override AsyncFuture Connect(EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            throw new NotSupportedException();
        }

        public sealed override void Disconnect()
        {
            throw new NotSupportedException();
        }

        protected internal void RemoveChild(TcpSocketChannel socketChannel)
        {
            this.ChildChannels.TryRemove(socketChannel, out _);
        }
    }
}

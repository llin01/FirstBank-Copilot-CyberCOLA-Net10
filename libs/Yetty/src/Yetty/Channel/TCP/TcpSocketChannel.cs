namespace Com.Cybersoft4u.Yetty.Channel.TCP
{
    #region DEPENDENCY

    using System;
    using System.Net;
    using System.Net.Sockets;

    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class TcpSocketChannel : SocketChannel
    {
        protected internal TcpServerSocketChannel ParentSocketChannel;

        private const int BufferSize = 8192;

        // Idle time before the first probe and the gap between probes, as Windows had them; 10 probes is the count
        // Windows fixes, set explicitly so Linux (default 9) matches. A dead peer is detected after about 420 s.
        internal const int KeepAliveIdleSeconds = 120;

        internal const int KeepAliveIntervalSeconds = 30;

        internal const int KeepAliveRetryCount = 10;

        private readonly TcpChannelCore core;

        private int disconnected;

        protected internal TcpSocketChannel()
        {
            this.Closed = false;
            this.core = new TcpChannelCore(this, BufferSize);
            this.core.EnableKeepAlive(KeepAliveIdleSeconds, KeepAliveIntervalSeconds, KeepAliveRetryCount);
            this.AsyncFuture = new AsyncFuture(this);
        }

        // Does not chain to this(): that built a core around a new Socket which was replaced right away and never
        // closed, so every accepted connection left a socket (a file descriptor on Linux) for the finalizer.
        protected internal TcpSocketChannel(
            TcpServerSocketChannel parentSocketChannel,
            Socket baseSocket,
            IChannelHandler channelHandler)
        {
            this.Closed = false;
            this.AsyncFuture = new AsyncFuture(this);

            if (parentSocketChannel == null)
            {
                throw new ArgumentNullException(nameof(parentSocketChannel));
            }

            if (baseSocket == null)
            {
                throw new ArgumentNullException(nameof(baseSocket));
            }

            // Built before the try: the catch below disconnects through this core, which the throwaway core
            // from this() used to stand in for.
            this.core = new TcpChannelCore(this, baseSocket, BufferSize);

            try
            {
                // set parent socket
                this.ParentSocketChannel = parentSocketChannel;

                this.core.EnableKeepAlive(KeepAliveIdleSeconds, KeepAliveIntervalSeconds, KeepAliveRetryCount);
                // add channelhandler
                if (channelHandler != null)
                {
                    this.Handler = channelHandler;
                }

                // after socket got, fire actived event.
                this.Pipe().FireChannelActived();

                // regist socket receive event.
                this.Read();
            }
            catch (Exception exception)
            {
                this.Disconnect();
                throw new SocketInitialFailException(exception);
            }
        }

        protected internal AsyncFuture AsyncFuture { get; private set; }

        public int IOControl(ChannelIOControlCode code, byte[] inValues, byte[] outValues)
        {
            return this.core.IOControl(code, inValues, outValues);
        }

        public override sealed IChannel Read()
        {
            this.Pipe().Read();
            return this;
        }

        public override IChannel Write(object data)
        {
            this.Pipe().Write(data);
            return this;
        }

        public override IChannel Flush()
        {
            this.Pipe().Flush();
            return this;
        }

        public override IChannel WriteAndFlush(object data)
        {
            this.Pipe().WriteAndFlush(data);
            return this;
        }

        public override AsyncFuture Connect(EndPoint remoteEndPoint)
        {
            if (this.Connected)
            {
                throw new SocketOperationException("Already Connected");
            }

            // this.IsConnected = true;
            try
            {
                this.AsyncFuture.Reset();
                this.Pipe().Connect(remoteEndPoint);
            }
            catch (Exception ex)
            {
                throw new SocketOperationException($"Connect RemoteEndPoint[{remoteEndPoint?.ToString()}] Exception : {ex.Message}");
            }

            return this.AsyncFuture;
        }

        public override AsyncFuture Connect(EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            if (this.Connected)
            {
                throw new SocketOperationException("Already Connected");
            }

            // this.IsConnected = true;
            try
            {
                this.AsyncFuture.Reset();
                this.Pipe().Connect(remoteEndPoint, localEndPoint);
            }
            catch (Exception ex)
            {
                //throw;
                throw new SocketOperationException($"Connect RemoteEndPoint[{remoteEndPoint?.ToString()}] Exception : {ex.Message}");
            }

            return this.AsyncFuture;
        }

        public override sealed void Disconnect()
        {
            // Closing the socket completes the pending receive at once, and its callback disconnects the channel too
            // (it checks Closed, which is set only further down). Run once, or every handler's ChannelInActived and
            // ChannelUnRegistered runs twice, concurrently, racing on the channel's carriers.
            if (System.Threading.Interlocked.Exchange(ref this.disconnected, 1) == 1)
            {
                return;
            }

            this.Pipe().Disconnect();
            this.Closed = true;
            this.Pipe().FireChannelInActived();
            this.Pipe().FireChannelUnRegistered();
            if (this.ParentSocketChannel != null)
            {
                this.ParentSocketChannel.RemoveChild(this);
            }
        }

        public override void Close()
        {
            throw new NotSupportedException();
        }

        public override AsyncFuture Bind(EndPoint localEndPoint)
        {
            throw new NotSupportedException();
        }

        public override EndPoint RemoteEndPoint()
        {
            return this.core.RemoteEndPoint;
        }

        public override EndPoint LocalEndPoint()
        {
            return this.core.LocalEndPoint;
        }

        public override ChannelCore Core()
        {
            return this.core;
        }
    }
}

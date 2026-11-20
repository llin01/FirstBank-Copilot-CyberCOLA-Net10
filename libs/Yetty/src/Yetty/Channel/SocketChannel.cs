namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using System.Net;
    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public abstract class SocketChannel : IChannel, ISocket
    {
        protected internal bool Closed;

        protected internal bool Connected;

        private readonly IChannelPipe pipe;

        private readonly Dictionary<object, object> carries = new Dictionary<object, object>();

        private IChannelHandler handler;

        protected SocketChannel()
        {
            this.pipe = new ChannelPipe(this);
        }

        protected internal IChannelHandler Handler
        {
            get
            {
                return this.handler;
            }

            set
            {
                if (this.handler != null)
                {
                    throw new ValueAlreadySetException();
                }

                this.handler = value;
                this.Pipe().AddFirst(this.handler);
                this.Pipe().FireChannelRegistered();
            }
        }

        public bool IsClosed()
        {
            return this.Closed;
        }

        bool IChannel.IsConnected()
        {
            return this.Connected;
        }

        public abstract ChannelCore Core();

        public IChannelPipe Pipe()
        {
            return this.pipe;
        }

        public abstract IChannel Read();

        public abstract IChannel Write(object data);

        public abstract IChannel Flush();

        public abstract IChannel WriteAndFlush(object data);

        public abstract EndPoint RemoteEndPoint();

        public abstract EndPoint LocalEndPoint();

        public abstract void Disconnect();

        public abstract void Close();

        public abstract AsyncFuture Bind(EndPoint localEndPoint);

        public object Carrier(object key)
        {
            object rtn;
            return this.carries.TryGetValue(key, out rtn) ? rtn : null;
        }

        public IChannel Carrier(object key, object value)
        {
            this.carries[key] = value;
            return this;
        }

        public void RemoveCarrier(object key)
        {
            this.carries.Remove(key);
        }

        public TReturnType Carrier<TCarrierKey, TReturnType>()
            where TCarrierKey : CarrierKey
        {
            object rtn;
            return (TReturnType)(this.carries.TryGetValue(typeof(TCarrierKey), out rtn) ? rtn : null);
        }

        public IChannel Carrier<TCarrierKey>(object value) where TCarrierKey : CarrierKey
        {
            this.carries[typeof(TCarrierKey)] = value;
            return this;
        }

        public void RemoveCarrier<TCarrierKey>() where TCarrierKey : CarrierKey
        {
            this.carries.Remove(typeof(TCarrierKey));
        }

        public abstract AsyncFuture Connect(EndPoint remoteEndPoint);

        public abstract AsyncFuture Connect(EndPoint remoteEndPoint, EndPoint localEndPoint);
    }
}

namespace Com.Cybersoft4u.Yetty.Channel.TCP
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    internal class TcpSocketChannelBundle
    {
        protected internal bool InRead = false;

        protected internal bool InWrite = false;

        private int readBufferSize;

        public TcpSocketChannelBundle(int readBufferSize = 0)
        {
            this.WriteBufferList = new ConcurrentQueue<ByteBuf>();
            this.readBufferSize = readBufferSize == 0 ? 2048 : readBufferSize;
            this.ReadBuffer = new byte[this.readBufferSize];
        }

        public ConcurrentQueue<ByteBuf> WriteBufferList { get; private set; }

        public byte[] ReadBuffer { get; protected internal set; }

        // Buffers handed to the current gathered send, unsent part only. Touched only by the
        // InWrite owner, so it needs no lock.
        public List<ArraySegment<byte>> SendSegments { get; } = new List<ArraySegment<byte>>();

        public TcpChannelCore ChannelCore { get; protected internal set; }

        public int ReadBufferSize
        {
            get
            {
                return this.readBufferSize;
            }

            internal set
            {
                this.readBufferSize = value;
                this.ReadBuffer = new byte[this.readBufferSize];
            }
        }

        public SocketChannel Channel { get; internal set; }
    }
}

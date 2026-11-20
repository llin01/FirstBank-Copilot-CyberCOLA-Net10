namespace Com.Cybersoft4u.Yetty.Channel.TCP
{
    #region DEPENDENCY

    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Utils;
    using NLog;

    using System.IO.Pipelines;
    using System.Threading.Tasks;
    #endregion

    internal class TcpChannelCore : ChannelCore
    {
        private Pipe _pipe;
        private CancellationTokenSource _cts;
        private Task _fillPipeTask;
        private Task _readPipeTask;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // Caps one gathered send so a long backlog does not pin a large buffer list at once.
        private const int MaxBuffersPerSend = 64;

        public EndPoint LocalEndPoint;

        public EndPoint RemoteEndPoint;

        private readonly TcpSocketChannelBundle bundle;

        // Flush waits on counters instead of a reset/wait event: a ManualResetEvent that is
        // Reset() after the emptiness check loses a Set() raised in between and hangs forever.
        private readonly object flushLocker = new object();

        private long enqueuedCount;

        private long writtenCount;

        private readonly object readLocker;

        private readonly object writeLocker = new object();

        private readonly SocketChannel socketChannel;

        private volatile Socket baseSocket;

        public TcpChannelCore(SocketChannel socketChannel, int readBufferSize = 0)
        {
            this.readLocker = new object();
            this.socketChannel = socketChannel;
            this.baseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.bundle = new TcpSocketChannelBundle(readBufferSize) { Channel = socketChannel, ChannelCore = this };
        }

        public TcpChannelCore(SocketChannel socketChannel, Socket socket, int readBufferSize = 0)
        {
            this.readLocker = new object();
            this.socketChannel = socketChannel;
            this.baseSocket = socket;
            this.bundle = new TcpSocketChannelBundle(readBufferSize) { Channel = socketChannel, ChannelCore = this };
            this.LocalEndPoint = this.baseSocket.LocalEndPoint;
            this.RemoteEndPoint = this.baseSocket.RemoteEndPoint;
        }

        protected internal Socket BaseSocket
        {
            get
            {
                return this.baseSocket;
            }
        }

        protected internal SocketChannel SocketChannel
        {
            get
            {
                return this.socketChannel;
            }
        }

        protected internal int IOControl(ChannelIOControlCode code, byte[] inValues, byte[] outValues)
        {
            return this.BaseSocket.IOControl(GetIOControlCode(code), inValues, outValues);
        }

        // SIO_KEEPALIVE_VALS (IOControl) is Windows-only, so Linux sockets had no keepalive at all and a silently
        // dropped peer went unnoticed for hours. Windows keeps the IOControl call: the TcpKeepAlive* socket options
        // need Windows 10 1709 / Server 2019 and would throw on older servers, failing every new channel.
        internal void EnableKeepAlive(int idleSeconds, int intervalSeconds, int retryCount)
        {
            if (OperatingSystem.IsWindows())
            {
                // Layout of struct tcp_keepalive: onoff, keepalivetime (ms), keepaliveinterval (ms).
                // Windows fixes the probe count at 10, which is why retryCount is not passed here.
                var values = new byte[sizeof(uint) * 3];
                BitConverter.GetBytes(1u).CopyTo(values, 0);
                BitConverter.GetBytes((uint)idleSeconds * 1000).CopyTo(values, sizeof(uint));
                BitConverter.GetBytes((uint)intervalSeconds * 1000).CopyTo(values, sizeof(uint) * 2);
                this.BaseSocket.IOControl(IOControlCode.KeepAliveValues, values, null);
                return;
            }

            this.BaseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            this.BaseSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, idleSeconds);
            this.BaseSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, intervalSeconds);
            this.BaseSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, retryCount);
        }

        protected internal override void Write(ByteBuf data)
        {
            if (data == null || data.Length() == 0)
            {
                return;
            }

            // Count before enqueueing so writtenCount can never overtake enqueuedCount.
            Interlocked.Increment(ref this.enqueuedCount);
            this.bundle.WriteBufferList.Enqueue(data);
            this.StartWrite();
        }

        protected internal override void Flush()
        {
            // Only wait for buffers enqueued before this call; writes from other threads
            // arriving later must not extend this caller's wait.
            var target = Interlocked.Read(ref this.enqueuedCount);
            lock (this.flushLocker)
            {
                while (Interlocked.Read(ref this.writtenCount) < target && !this.socketChannel.Closed)
                {
                    Monitor.Wait(this.flushLocker);
                }
            }

            // Returning instead of throwing: callers include timer callbacks without try/catch,
            // where an exception would terminate the process. Log so the data loss is visible.
            var unsent = target - Interlocked.Read(ref this.writtenCount);
            if (unsent > 0)
            {
                logger.Warn($"TcpChannelCore Flush returned on closed channel [{this.LocalEndPoint} => {this.RemoteEndPoint}], {unsent} buffer(s) not sent.");
            }
        }

        private void NotifyFlushWaiters()
        {
            lock (this.flushLocker)
            {
                Monitor.PulseAll(this.flushLocker);
            }
        }

        protected internal override void Bind(EndPoint localEndPoint)
        {
            if (this.socketChannel.Closed)
            {
                throw new SocketOperationException("Channel Closed.");
            }

            if (localEndPoint == null)
            {
                throw new ArgumentNullException(nameof(localEndPoint));
            }

            var baseSocket = this.BaseSocket;
            try
            {
                baseSocket.Bind(localEndPoint);
                baseSocket.Listen(((TcpServerSocketChannel)this.socketChannel).Backlog);
                this.bundle.Channel.Pipe().FireChannelActived();
                this.LocalEndPoint = localEndPoint;
                this.bundle.ChannelCore.BaseSocket.BeginAccept(AcceptTask, this.bundle.ChannelCore);
            }
            catch (Exception exception)
            {
                logger.Error("TcpChannelCore Bind Exception");
                throw new SocketOperationException(exception);
            }
        }

        protected internal override void Disconnect()
        {
            try
            {
                if (this.BaseSocket == null || this.socketChannel.Closed)
                {
                    return;
                }

                // Stop pipelines tasks
                StopPipelines();

                // Shutdown sends FIN without waiting. Disconnect(true) made the socket reusable, which on Windows
                // blocked until TIME_WAIT ended (about 120 s), but a disconnected channel is never reused: it is
                // marked Closed and a reconnect builds a new channel. Close() also releases the handle, which a
                // disconnected channel used to keep.
                if (this.BaseSocket.Connected)
                {
                    try
                    {
                        this.BaseSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch (SocketException)
                    {
                        // The peer may have reset the connection already; closing below is still required.
                    }
                }

                this.BaseSocket.Close();
                this.socketChannel.Closed = true;
                this.NotifyFlushWaiters();
            }
            catch (Exception exception)
            {
                logger.Error("TcpChannelCore Disconnect Exception");
                throw new SocketOperationException(exception);
            }
        }

        protected internal override void Connect(EndPoint remoteEndPoint)
        {
            if (this.socketChannel.Closed)
            {
                throw new SocketOperationException("Channel Closed.");
            }

            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            var baseSocket = this.BaseSocket;
            try
            {
                baseSocket.BeginConnect(remoteEndPoint, ConnectTask, this.bundle);
            }
            catch (Exception exception)
            {
                logger.Error("1. TcpChannelCore Connect Exception");
                throw new SocketOperationException(exception);
            }
        }

        protected internal override void Connect(EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            if (this.socketChannel.Closed)
            {
                throw new SocketOperationException("Channel Closed.");
            }

            if (remoteEndPoint == null)

            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            var baseSocket = this.BaseSocket;
            try
            {
                this.baseSocket.Bind(localEndPoint);
                baseSocket.BeginConnect(remoteEndPoint, ConnectTask, this.bundle);
            }
            catch (Exception exception)
            {
                logger.Error("2. TcpChannelCore Connect Exception");
                throw new SocketOperationException(exception);
            }
        }

        protected internal override void Close()
        {
            if (this.BaseSocket == null || this.socketChannel.Closed)
            {
                return;
            }

            // Stop pipelines tasks
            StopPipelines();

            if (this.BaseSocket.IsBound)
            {
                this.BaseSocket.Close();
            }

            this.socketChannel.Closed = true;
            this.NotifyFlushWaiters();
            //            this.baseSocket = null;
        }

        private void StopPipelines()
        {
            try
            {
                if (this._cts != null && !this._cts.IsCancellationRequested)
                {
                    this._cts.Cancel();
                    this._cts.Dispose();
                }
            }
            catch {}
        }

        protected internal override void StartRead()
        {
            if (this.socketChannel.Closed)
            {
                return;
            }

            if (this._pipe == null)
            {
                lock (this.readLocker)
                {
                    if (this._pipe == null)
                    {
                        this._pipe = new Pipe();
                        this._cts = new CancellationTokenSource();
                        this._fillPipeTask = Task.Run(() => FillPipeAsync(this._cts.Token));
                        this._readPipeTask = Task.Run(() => ReadPipeAsync(this._cts.Token));
                    }
                }
            }
        }

        private async Task FillPipeAsync(CancellationToken token)
        {
            var writer = this._pipe.Writer;
            while (!token.IsCancellationRequested && !this.socketChannel.Closed)
            {
                Memory<byte> memory = writer.GetMemory(this.bundle.ReadBufferSize);
                try
                {
                    int bytesRead = await this.BaseSocket.ReceiveAsync(memory, SocketFlags.None, token);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    writer.Advance(bytesRead);
                }
                catch (Exception)
                {
                    break;
                }

                FlushResult result = await writer.FlushAsync(token);
                if (result.IsCompleted)
                {
                    break;
                }
            }

            await writer.CompleteAsync();
            
            if (!this.socketChannel.Closed)
            {
                if (this.socketChannel is TcpServerSocketChannel)
                {
                    this.socketChannel.Pipe().Close();
                }
                else
                {
                    // Use the channel-level Disconnect (as ReadTask does) so ChannelInActived/ChannelUnRegistered
                    // fire on a peer close; Pipe().Disconnect() only closed the socket, so revive handlers never ran.
                    this.socketChannel.Disconnect();
                }
            }
        }

        private async Task ReadPipeAsync(CancellationToken token)
        {
            var reader = this._pipe.Reader;
            while (!token.IsCancellationRequested && !this.socketChannel.Closed)
            {
                ReadResult result;
                try
                {
                    result = await reader.ReadAsync(token);
                }
                catch (Exception)
                {
                    break;
                }

                var buffer = result.Buffer;
                if (buffer.Length > 0)
                {
                    try
                    {
                        // One copy and one pipeline pass per read: firing per pipe segment copied each
                        // segment twice and ran every handler once per segment. Frame decoders downstream
                        // already handle any chunking, as a TCP stream gives no boundaries anyway.
                        this.socketChannel.Pipe().FireChannelRead(new ByteBuf(buffer.ToArray()));
                        this.socketChannel.Pipe().FireChannelReadCompleted();
                    }
                    catch (Exception)
                    {
                        // Ignore handler level exception to keep the receive loop alive
                    }
                }

                reader.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            await reader.CompleteAsync();
        }

        protected internal override void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            int optionValue)
        {
            if (optionName == ChannelOptionName.Backlog)
            {
                try
                {
                    ((TcpServerSocketChannel)this.socketChannel).Backlog = optionValue;
                }
                catch (Exception)
                {
                    // Ignored    
                }

                return;
            }

            this.BaseSocket.SetSocketOption(
                GetSocketOptionLevel(optionLevel),
                GetSocketOptionName(optionName),
                optionValue);
        }

        protected internal override void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            byte[] optionValue)
        {
            this.BaseSocket.SetSocketOption(
                GetSocketOptionLevel(optionLevel),
                GetSocketOptionName(optionName),
                optionValue);
        }

        protected internal override void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            bool optionValue)
        {
            this.BaseSocket.SetSocketOption(
                GetSocketOptionLevel(optionLevel),
                GetSocketOptionName(optionName),
                optionValue);
        }

        protected internal override void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            object optionValue)
        {
            this.BaseSocket.SetSocketOption(
                GetSocketOptionLevel(optionLevel),
                GetSocketOptionName(optionName),
                optionValue);
        }

        private static void ReadTask(IAsyncResult result)
        {
            var bundle = (TcpSocketChannelBundle)result.AsyncState;
            var channel = bundle.Channel;
            var channelCore = bundle.ChannelCore;
            int sizeRead;
            try
            {
                if (channelCore.BaseSocket.Poll(0, SelectMode.SelectRead) && channelCore.BaseSocket.Available == 0)
                {
                    throw new Exception("Client Socket disconnected, jump to disconnect procedure.");
                }

                sizeRead = channelCore.BaseSocket.EndReceive(result);
            }
            catch (Exception)
            {
                if (channel.Closed)
                {
                    return;
                }

                if (channel is IServerSocket)
                {
                    channel.Close();
                }
                else
                {
                    channel.Disconnect();
                }

                return;
            }
            try
            {
                if (sizeRead > 0)
                {
                    var data = ByteBuf.From(bundle.ReadBuffer, 0, sizeRead);
                    channel.Pipe().FireChannelRead(data);
                    channel.Pipe().FireChannelReadCompleted();
                }

                bundle.InRead = false;
                channel.Read();
            }
            catch (Exception)
            {
                if (channel.Closed)
                {
                    return;
                }

                if (channel is IServerSocket)
                {
                    channel.Close();
                }
                else
                {
                    channel.Disconnect();
                }

                return;
            }
        }

        private static void WriteTask(IAsyncResult result)
        {
            var bundle = (TcpSocketChannelBundle)result.AsyncState;
            var channel = bundle.Channel;
            var channelCore = bundle.ChannelCore;
            int sizeWrite;
            try
            {
                if (channelCore.BaseSocket == null || channel.Closed)
                {
                    return;
                }

                sizeWrite = channelCore.BaseSocket.EndSend(result);
            }
            catch (Exception)
            {
                if (channel.Closed)
                {
                    return;
                }

                if (channel is IServerSocket)
                {
                    channel.Close();
                }
                else
                {
                    channel.Disconnect();
                }

                return;
            }

            // Drop the buffers sent in full and trim a partly sent one by offset, instead of
            // shifting the unsent bytes down one at a time.
            var segments = bundle.SendSegments;
            var completed = 0;
            while (completed < segments.Count && sizeWrite >= segments[completed].Count)
            {
                sizeWrite -= segments[completed].Count;
                completed++;
            }

            segments.RemoveRange(0, completed);
            if (sizeWrite > 0)
            {
                segments[0] = segments[0].Slice(sizeWrite);
            }

            if (completed > 0)
            {
                Interlocked.Add(ref channelCore.writtenCount, completed);
                channelCore.NotifyFlushWaiters();
            }

            // A send that completes at once runs this callback inline inside BeginSend, so sending the
            // next batch from here nested one level deeper each time, and a sustained burst of writes
            // overflowed the stack and crashed the process. Continue on the thread pool to unwind it.
            if (result.CompletedSynchronously)
            {
                ThreadPool.UnsafeQueueUserWorkItem(static core => core.ContinueWrite(), channelCore, preferLocal: false);
                return;
            }

            channelCore.ContinueWrite();
        }

        private void ContinueWrite()
        {
            if (this.bundle.SendSegments.Count > 0)
            {
                // Still the InWrite owner: finish this batch before taking more from the queue.
                this.BeginSendSegments();
                return;
            }

            this.ReleaseWrite();
            this.StartWrite();
        }

        private static void ConnectTask(IAsyncResult result)
        {
            var bundle = (TcpSocketChannelBundle)result.AsyncState;
            var channel = (TcpSocketChannel)bundle.Channel;
            try
            {
                bundle.ChannelCore.BaseSocket.EndConnect(result);
                channel.Connected = true;
                bundle.Channel.Pipe().FireChannelActived();
                bundle.ChannelCore.LocalEndPoint = bundle.ChannelCore.BaseSocket.LocalEndPoint;
                bundle.ChannelCore.RemoteEndPoint = bundle.ChannelCore.BaseSocket.RemoteEndPoint;

                channel.AsyncFuture.Set();
                bundle.Channel.Read();
            }
            catch (Exception)
            {
                channel.AsyncFuture.Set();
                bundle.Channel.Pipe().FireChannelUnRegistered();
            }
        }

        private static void AcceptTask(IAsyncResult result)
        {
            var channelCore = (TcpChannelCore)result.AsyncState;
            var socket = channelCore.BaseSocket;
            var serverChannel = (TcpServerSocketChannel)channelCore.SocketChannel;
            if (serverChannel.IsShutdown)
            {
                return;
            }

            var childChannel = new TcpSocketChannel(serverChannel, socket.EndAccept(result), serverChannel.ChildHandler);
            serverChannel.ChildChannels.TryAdd(childChannel, childChannel);
            socket.BeginAccept(AcceptTask, channelCore);
        }

        private static IOControlCode GetIOControlCode(ChannelIOControlCode channelIoControlCode)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("IOControlCode Only support Windows platform");
            }
            IOControlCode code;
            switch (channelIoControlCode)
            {
                case ChannelIOControlCode.AbsorbRouterAlert:
                    code = IOControlCode.AbsorbRouterAlert;
                    break;
                case ChannelIOControlCode.AddMulticastGroupOnInterface:
                    code = IOControlCode.AddMulticastGroupOnInterface;
                    break;
                case ChannelIOControlCode.AddressListChange:
                    code = IOControlCode.AddressListChange;
                    break;
                case ChannelIOControlCode.AddressListQuery:
                    code = IOControlCode.AddressListQuery;
                    break;
                case ChannelIOControlCode.AddressListSort:
                    code = IOControlCode.AddressListSort;
                    break;
                case ChannelIOControlCode.AssociateHandle:
                    code = IOControlCode.AssociateHandle;
                    break;
                case ChannelIOControlCode.AsyncIO:
                    code = IOControlCode.AsyncIO;
                    break;
                case ChannelIOControlCode.BindToInterface:
                    code = IOControlCode.BindToInterface;
                    break;
                case ChannelIOControlCode.DataToRead:
                    code = IOControlCode.DataToRead;
                    break;
                case ChannelIOControlCode.DeleteMulticastGroupFromInterface:
                    code = IOControlCode.DeleteMulticastGroupFromInterface;
                    break;
                case ChannelIOControlCode.EnableCircularQueuing:
                    code = IOControlCode.EnableCircularQueuing;
                    break;
                case ChannelIOControlCode.Flush:
                    code = IOControlCode.Flush;
                    break;
                case ChannelIOControlCode.GetBroadcastAddress:
                    code = IOControlCode.GetBroadcastAddress;
                    break;
                case ChannelIOControlCode.GetExtensionFunctionPointer:
                    code = IOControlCode.GetExtensionFunctionPointer;
                    break;
                case ChannelIOControlCode.GetGroupQos:
                    code = IOControlCode.GetGroupQos;
                    break;
                case ChannelIOControlCode.GetQos:
                    code = IOControlCode.GetQos;
                    break;
                case ChannelIOControlCode.KeepAliveValues:
                    code = IOControlCode.KeepAliveValues;
                    break;
                case ChannelIOControlCode.LimitBroadcasts:
                    code = IOControlCode.LimitBroadcasts;
                    break;
                case ChannelIOControlCode.MulticastInterface:
                    code = IOControlCode.MulticastInterface;
                    break;
                case ChannelIOControlCode.MulticastScope:
                    code = IOControlCode.MulticastScope;
                    break;
                case ChannelIOControlCode.MultipointLoopback:
                    code = IOControlCode.MultipointLoopback;
                    break;
                case ChannelIOControlCode.NamespaceChange:
                    code = IOControlCode.NamespaceChange;
                    break;
                case ChannelIOControlCode.NonBlockingIO:
                    code = IOControlCode.NonBlockingIO;
                    break;
                case ChannelIOControlCode.OobDataRead:
                    code = IOControlCode.OobDataRead;
                    break;
                case ChannelIOControlCode.QueryTargetPnpHandle:
                    code = IOControlCode.QueryTargetPnpHandle;
                    break;
                case ChannelIOControlCode.ReceiveAll:
                    code = IOControlCode.ReceiveAll;
                    break;
                case ChannelIOControlCode.ReceiveAllIgmpMulticast:
                    code = IOControlCode.ReceiveAllIgmpMulticast;
                    break;
                case ChannelIOControlCode.ReceiveAllMulticast:
                    code = IOControlCode.ReceiveAllMulticast;
                    break;
                case ChannelIOControlCode.RoutingInterfaceChange:
                    code = IOControlCode.RoutingInterfaceChange;
                    break;
                case ChannelIOControlCode.RoutingInterfaceQuery:
                    code = IOControlCode.RoutingInterfaceQuery;
                    break;
                case ChannelIOControlCode.SetGroupQos:
                    code = IOControlCode.SetGroupQos;
                    break;
                case ChannelIOControlCode.SetQos:
                    code = IOControlCode.SetQos;
                    break;
                case ChannelIOControlCode.TranslateHandle:
                    code = IOControlCode.TranslateHandle;
                    break;
                case ChannelIOControlCode.UnicastInterface:
                    code = IOControlCode.UnicastInterface;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channelIoControlCode), channelIoControlCode, null);
            }

            return code;
        }

        private static SocketOptionLevel GetSocketOptionLevel(ChannelOptionLevel optionLevel)
        {
            SocketOptionLevel level;
            switch (optionLevel)
            {
                case ChannelOptionLevel.IP:
                    level = SocketOptionLevel.IP;
                    break;
                case ChannelOptionLevel.IPv6:
                    level = SocketOptionLevel.IPv6;
                    break;
                case ChannelOptionLevel.Socket:
                    level = SocketOptionLevel.Socket;
                    break;
                case ChannelOptionLevel.Tcp:
                    level = SocketOptionLevel.Tcp;
                    break;
                case ChannelOptionLevel.Udp:
                    level = SocketOptionLevel.Udp;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(optionLevel), optionLevel, null);
            }

            return level;
        }

        private static SocketOptionName GetSocketOptionName(ChannelOptionName optionName)
        {
            SocketOptionName name;
            switch (optionName)
            {
                case ChannelOptionName.AcceptConnection:
                    name = SocketOptionName.AcceptConnection;
                    break;

                case ChannelOptionName.AddMembership:
                    name = SocketOptionName.AddMembership;
                    break;

                case ChannelOptionName.AddSourceMembership:
                    name = SocketOptionName.AddSourceMembership;
                    break;

                case ChannelOptionName.BlockSource:
                    name = SocketOptionName.BlockSource;
                    break;

                case ChannelOptionName.Broadcast:
                    name = SocketOptionName.Broadcast;
                    break;

                case ChannelOptionName.BsdUrgent:
                    name = SocketOptionName.BsdUrgent;
                    break;

                case ChannelOptionName.ChecksumCoverage:
                    name = SocketOptionName.ChecksumCoverage;
                    break;

                case ChannelOptionName.Debug:
                    name = SocketOptionName.Debug;
                    break;

                case ChannelOptionName.DontFragment:
                    name = SocketOptionName.DontFragment;
                    break;

                case ChannelOptionName.DontLinger:
                    name = SocketOptionName.DontLinger;
                    break;

                case ChannelOptionName.DontRoute:
                    name = SocketOptionName.DontRoute;
                    break;

                case ChannelOptionName.DropMembership:
                    name = SocketOptionName.DropMembership;
                    break;

                case ChannelOptionName.DropSourceMembership:
                    name = SocketOptionName.DropSourceMembership;
                    break;

                case ChannelOptionName.Error:
                    name = SocketOptionName.Error;
                    break;

                case ChannelOptionName.ExclusiveAddressUse:
                    name = SocketOptionName.ExclusiveAddressUse;
                    break;

                case ChannelOptionName.Expedited:
                    name = SocketOptionName.Expedited;
                    break;

                case ChannelOptionName.HeaderIncluded:
                    name = SocketOptionName.HeaderIncluded;
                    break;

                case ChannelOptionName.HopLimit:
                    name = SocketOptionName.HopLimit;
                    break;

                case ChannelOptionName.IPOptions:
                    name = SocketOptionName.IPOptions;
                    break;

                case ChannelOptionName.IPProtectionLevel:
                    name = SocketOptionName.IPProtectionLevel;
                    break;

                case ChannelOptionName.IpTimeToLive:
                    name = SocketOptionName.IpTimeToLive;
                    break;

                case ChannelOptionName.IPv6Only:
                    name = SocketOptionName.IPv6Only;
                    break;

                case ChannelOptionName.KeepAlive:
                    name = SocketOptionName.KeepAlive;
                    break;

                case ChannelOptionName.Linger:
                    name = SocketOptionName.Linger;
                    break;

                case ChannelOptionName.MaxConnections:
                    name = SocketOptionName.MaxConnections;
                    break;

                case ChannelOptionName.MulticastInterface:
                    name = SocketOptionName.MulticastInterface;
                    break;

                case ChannelOptionName.MulticastLoopback:
                    name = SocketOptionName.MulticastLoopback;
                    break;

                case ChannelOptionName.MulticastTimeToLive:
                    name = SocketOptionName.MulticastTimeToLive;
                    break;

                case ChannelOptionName.NoChecksum:
                    name = SocketOptionName.NoChecksum;
                    break;

                case ChannelOptionName.NoDelay:
                    name = SocketOptionName.NoDelay;
                    break;

                case ChannelOptionName.OutOfBandInline:
                    name = SocketOptionName.OutOfBandInline;
                    break;

                case ChannelOptionName.PacketInformation:
                    name = SocketOptionName.PacketInformation;
                    break;

                case ChannelOptionName.ReceiveBuffer:
                    name = SocketOptionName.ReceiveBuffer;
                    break;

                case ChannelOptionName.ReceiveLowWater:
                    name = SocketOptionName.ReceiveLowWater;
                    break;

                case ChannelOptionName.ReceiveTimeout:
                    name = SocketOptionName.ReceiveTimeout;
                    break;

                case ChannelOptionName.ReuseAddress:
                    name = SocketOptionName.ReuseAddress;
                    break;

                case ChannelOptionName.SendBuffer:
                    name = SocketOptionName.SendBuffer;
                    break;

                case ChannelOptionName.SendLowWater:
                    name = SocketOptionName.SendLowWater;
                    break;

                case ChannelOptionName.SendTimeout:
                    name = SocketOptionName.SendTimeout;
                    break;

                case ChannelOptionName.Type:
                    name = SocketOptionName.Type;
                    break;

                case ChannelOptionName.TypeOfService:
                    name = SocketOptionName.TypeOfService;
                    break;

                case ChannelOptionName.UnblockSource:
                    name = SocketOptionName.UnblockSource;
                    break;

                case ChannelOptionName.UpdateAcceptContext:
                    name = SocketOptionName.UpdateAcceptContext;
                    break;

                case ChannelOptionName.UpdateConnectContext:
                    name = SocketOptionName.UpdateConnectContext;
                    break;

                case ChannelOptionName.UseLoopback:
                    name = SocketOptionName.UseLoopback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(optionName), optionName, null);
            }

            return name;
        }

        private void StartWrite()
        {
            var localBundle = this.bundle;
            while (true)
            {
                if (localBundle.InWrite || this.socketChannel.Closed || localBundle.WriteBufferList.IsEmpty)
                {
                    return;
                }

                lock (this.writeLocker)
                {
                    if (localBundle.InWrite || this.socketChannel.Closed)
                    {
                        return;
                    }

                    localBundle.InWrite = true;
                }

                // Gather what is queued into one send: one syscall (and, with NoDelay, often one
                // segment) for a burst instead of one per message.
                // Val() copies on purpose: Write(byte[]) wraps the caller's array, which it may reuse.
                var segments = localBundle.SendSegments;
                while (segments.Count < MaxBuffersPerSend && localBundle.WriteBufferList.TryDequeue(out var buf))
                {
                    segments.Add(new ArraySegment<byte>(buf.Val()));
                }

                if (segments.Count > 0)
                {
                    this.BeginSendSegments();
                    return;
                }

                // The previous owner drained the queue between the emptiness check and taking InWrite.
                // Returning with InWrite still set stalled the channel for good: every later Write was
                // queued and never sent. Release it and look again for a Write enqueued meanwhile.
                this.ReleaseWrite();
            }
        }

        private void ReleaseWrite()
        {
            // Full fence: the caller checks the queue next, and a plain store may be reordered after
            // that load. A Write enqueued meanwhile could then see InWrite still set while this side
            // sees the queue empty, and neither would send it.
            this.bundle.InWrite = false;
            Interlocked.MemoryBarrier();
        }

        private void BeginSendSegments()
        {
            try
            {
                this.BaseSocket.BeginSend(this.bundle.SendSegments, SocketFlags.None, WriteTask, this.bundle);
            }
            catch (Exception)
            {
                if (this.socketChannel.Closed)
                {
                    return;
                }

                if (this.socketChannel is IServerSocket)
                {
                    this.socketChannel.Close();
                }
                else
                {
                    this.socketChannel.Disconnect();
                }
            }
        }
    }
}

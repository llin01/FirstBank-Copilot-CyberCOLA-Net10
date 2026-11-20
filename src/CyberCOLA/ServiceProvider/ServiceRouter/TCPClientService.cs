using System.Linq;
using System.Net;
using System.Threading;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using LogLevel = Com.Cybersoft4u.Utils.LogLevel;

namespace Com.Cybersoft4u.ServiceProvider.ServiceRouter
{
    #region DEPENDENCY

    using InvalidOperationException = Yetty.Exception.InvalidOperationException;

    #endregion

    public class TCPClientService
    {
        public OnInActived OnInActivedTask;

        public Bootstrap Bootstrap;

        protected internal bool IsStop;

        protected LogLevel LogLevel = LogLevel.Error;

        protected string LogName = string.Empty;

        protected OnReceived OnReceivedTask;

        protected volatile IChannel ClientMonitorChannel;

        protected bool Started;

        private readonly EndPoint remoteEndPoint;

        private readonly EndPoint localEndPoint;

        private static readonly Logger Log = LoggerAdapter.GetLogger("LoggerHandler[TCPClientService]");

        /// <summary>
        /// Initializes a new instance of the <see cref="TCPClientService"/> class.
        /// </summary>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="localEndPoint">The local end point.</param>
        public TCPClientService(string clientName, EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.ClientName = clientName;
            this.Bootstrap = new Bootstrap().ChannelType(typeof(TcpSocketChannel));
            this.ReconnectTime = 3000;
            this.KeepAlive = true;
        }

        public delegate void OnInActived();

        public delegate void OnReceived(byte[] receiveBytes);

        public string ClientName { get; private set; }

        public bool KeepAlive { get; set; }

        public int ReconnectTime { get; set; }

        public byte[] GetReturnBytes { get; private set; }

        public TCPClientService EnableLogger(string logName, LogLevel logLevel)
        {
            this.LogName = logName;
            this.LogLevel = logLevel;
            return this;
        }

        public bool IsConnected()
        {
            return this.ClientMonitorChannel != null && this.ClientMonitorChannel.IsConnected();
        }

        public bool IsClosed()
        {
            return this.ClientMonitorChannel != null && this.ClientMonitorChannel.IsClosed();
        }

        public bool IsStarted()
        {
            return this.ClientMonitorChannel != null && this.Started;
        }

        public EndPoint RemoteEndPoint()
        {
            return this.remoteEndPoint;
        }

        public EndPoint LocalEndPoint()
        {
            return this.localEndPoint;
        }

        public void Start(OnReceived receivedTask = null)
        {
            if (this.Started)
            {
                throw new InvalidOperationException();
            }

            this.Started = true;
            this.OnReceivedTask = receivedTask ?? DefaultOnReceivedTask;
            this.Bootstrap.Handler(
                this.LogName.Equals(string.Empty)
                    ? new Initializer(this)
                    : new Initializer(this, this.LogName, this.LogLevel));
            this.CreateConnect();
        }

        public void Stop()
        {
            this.IsStop = true;
            this.Started = false;
            this.ClientMonitorChannel?.Disconnect();
        }

        public bool Send(byte[] sendBytes)
        {
            if (this.IsClosed())
            {
                return false;
            }
            this.GetReturnBytes = null;
            this.ClientMonitorChannel.Write(sendBytes);
            return true;
        }

        public void Flush()
        {
            this.ClientMonitorChannel.Flush();
        }

        private void CreateConnect()
        {
            var channel = this.Bootstrap.SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                              .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                              .Connect(this.remoteEndPoint, this.localEndPoint).Sync().Channel();
            this.ClientMonitorChannel = channel;
        }

        private static void DefaultOnReceivedTask(byte[] receiveBytes)
        {
        }

        public class Initializer : ChannelInitializer
        {
            public static ClientMonitorHandler clientMonitorHandler;

            private readonly string logName;

            private readonly LogLevel logLevel = LogLevel.Debug;

            private readonly TCPClientService tcpClientService;

            public Initializer(TCPClientService tcpClientService)
            {
                this.tcpClientService = tcpClientService;
                clientMonitorHandler = new ClientMonitorHandler(tcpClientService);
                this.logName = null;
            }

            public Initializer(TCPClientService tcpClientService, string logName, LogLevel logLevel)
                    : this(tcpClientService)
            {
                this.logName = logName;
                this.logLevel = logLevel;
            }

            public override void InitChannel(IChannel channel)
            {
                channel.Carrier<ClientMonitorServiceKey>(this.tcpClientService);
                channel.Carrier<EndPointKey>(this.tcpClientService.remoteEndPoint);
                channel.Carrier<LocalEndPointKey>(this.tcpClientService.localEndPoint);
                if (this.logName != null)
                {
                    channel.Pipe().AddLast(new LoggerHandler(this.logName, this.logLevel));
                }

                channel.Pipe()
                    .AddLast(clientMonitorHandler);
            }
        }

        public class ClientMonitorHandler : ChannelDuplexHandler
        {
            private readonly TCPClientService tcpClientService;

            public ClientMonitorHandler(TCPClientService tcpClientService)
            {
                this.tcpClientService = tcpClientService;
            }

            public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            {
                var buf = (ByteBuf)msg;
                var readBytes = buf.Val();
                var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(readBytes);
                if (interchangeMessage == null)
                {
                    Logger.Info("Receive Message Invalid");
                }
                else
                {
                    Log.Info($"--- Start Display Receive InterchangeMessage ---");
                    Log.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                    Log.Info($"--- End   Display Receive InterchangeMessage ---");
                }
                this.tcpClientService.GetReturnBytes = readBytes != null ? readBytes.ToArray() : null;
                base.ChannelRead(ctx, msg);
            }

            public override void ChannelInActived(ChannelHandlerContext ctx)
            {
                if (this.tcpClientService.OnInActivedTask != null && !this.tcpClientService.IsStop)
                {
                    this.tcpClientService.OnInActivedTask();
                }

                ctx.FireChannelInActived();
            }

            public override void ChannelUnRegistered(ChannelHandlerContext ctx)
            {
                if (!this.tcpClientService.KeepAlive || this.tcpClientService.IsStop)
                {
                    return;
                }

                Thread.Sleep(this.tcpClientService.ReconnectTime);
                this.tcpClientService.CreateConnect();
            }
        }
    }
}

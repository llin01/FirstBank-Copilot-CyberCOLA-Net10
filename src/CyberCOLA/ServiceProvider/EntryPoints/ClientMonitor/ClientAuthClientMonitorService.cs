using System;
using System.Net;
using System.Text;
using System.Threading;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using LogLevel = Com.Cybersoft4u.Utils.LogLevel;


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor
{
    #region DEPENDENCY

    using InvalidOperationException = Yetty.Exception.InvalidOperationException;

    #endregion

    /// <summary>
    /// Client authorization monitoring service that manages TCP client connections to remote authorization servers.
    /// </summary>
    public class ClientAuthClientMonitorService
    {
        public OnInActived OnInActivedTask;

        public Bootstrap Bootstrap;

        public string SettlementDate = TimeUtils.CurrentIso8583Date();

        public string CaptureDate = TimeUtils.CurrentIso8583Date();

        protected internal bool IsStop;

        protected LogLevel LogLevel = LogLevel.Error;

        protected string LogName = string.Empty;

        protected OnReceived OnReceivedTask;

        protected volatile IChannel ClientMonitorChannel;

        protected bool Started;

        private readonly EndPoint remoteEndPoint;

        private readonly EndPoint localEndPoint;

        private readonly GatewayClientAuthServiceType gatewayClientAuthServiceType;

        private readonly bool isDummyEcho;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientAuthClientMonitorService"/> class.
        /// </summary>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="gatewayClientAuthServiceTyp"></param>
        /// <param name="isDummyEcho"></param>
        public ClientAuthClientMonitorService(string clientName, EndPoint remoteEndPoint, EndPoint localEndPoint, GatewayClientAuthServiceType gatewayClientAuthServiceTyp, bool isDummyEcho = false)
        {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.ClientName = clientName;
            this.Bootstrap = new Bootstrap().ChannelType(typeof(TcpSocketChannel));
            this.ReconnectTime = 10000;
            this.KeepAlive = true;
            this.gatewayClientAuthServiceType = gatewayClientAuthServiceTyp;
            this.isDummyEcho = isDummyEcho;
        }

        public delegate void OnInActived();

        public delegate void OnReceived(byte[] receiveBytes);

        public string ClientName { get; private set; }

        public bool KeepAlive { get; set; }

        public int ReconnectTime { get; set; }

        public ClientAuthClientMonitorService EnableLogger(string logName, LogLevel logLevel)
        {
            this.LogName = logName;
            this.LogLevel = logLevel;
            return this;
        }

        public bool IsConnected()
        {
            return this.ClientMonitorChannel.IsConnected();
        }

        public bool IsClosed()
        {
            return this.ClientMonitorChannel.IsClosed();
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
            // Null until the connect in Start returns: a service stopped right after starting gets here first.
            this.ClientMonitorChannel?.Disconnect();
        }

        public bool Send(byte[] sendBytes)
        {
            if (this.IsClosed())
            {
                return false;
            }

            this.ClientMonitorChannel.Write(sendBytes);
            return true;
        }

        public bool SendSyncColaPasysInfoCommand()
        {
            try
            {
                if (!this.IsConnected())
                {
                    return false;
                }
                var interchangeMessage = new InterchangeMessage();
                var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
                interchangeMessage.StartingPoint = 'S';
                interchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
                interchangeMessage.SourceEndPoint = localEndPoint.ToString();
                interchangeMessage.DestinationEndPoint = remoteEndPoint.ToString();
                interchangeMessage.CreateDateTime = dateTimeString;
                interchangeMessage.Category = InterChangeMessageCategory.ClientAuthSystemCommand;
                interchangeMessage.CommandCode = InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.SyncColaPasysInfo;
                interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Null;
                interchangeMessage.InterchangeData = null;
                interchangeMessage.ReturnOption = false;
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                this.ClientMonitorChannel.Write(sendBytes);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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
            private static ClientMonitorHandler clientMonitorHandler;

            private readonly string logName;

            private readonly LogLevel logLevel = LogLevel.Debug;

            private readonly ClientAuthClientMonitorService clientMonitorService;

            public Initializer(ClientAuthClientMonitorService clientMonitorService)
            {
                this.clientMonitorService = clientMonitorService;
                clientMonitorHandler = new ClientMonitorHandler(clientMonitorService);
                this.logName = null;
            }

            public Initializer(ClientAuthClientMonitorService clientMonitorService, string logName, LogLevel logLevel)
                    : this(clientMonitorService)
            {
                this.logName = logName;
                this.logLevel = logLevel;
            }

            public override void InitChannel(IChannel channel)
            {
                channel.Carrier<ClientMonitorServiceKey>(this.clientMonitorService);
                channel.Carrier<EndPointKey>(this.clientMonitorService.remoteEndPoint);
                channel.Carrier<LocalEndPointKey>(this.clientMonitorService.localEndPoint);
                channel.Carrier<DummyEchoKey>(this.clientMonitorService.isDummyEcho);
                if (this.logName != null)
                {
                    channel.Pipe().AddLast(new LoggerHandler(this.logName, this.logLevel));
                }

                channel.Pipe()
                    .AddLast(new DummyEchoHandler())
                    .AddLast(clientMonitorHandler);
            }
        }

        private class ClientMonitorHandler : ChannelDuplexHandler
        {
            private readonly ClientAuthClientMonitorService clientMonitorService;

            public ClientMonitorHandler(ClientAuthClientMonitorService clientMonitorService)
            {
                this.clientMonitorService = clientMonitorService;
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
                    Logger.Info($"ClientAuthClientMonitorService --- Start Display Receive InterchangeMessage ---");
                    Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                    Logger.Info($"ClientAuthClientMonitorService --- End   Display Receive InterchangeMessage ---");
                    if (interchangeMessage.StartingPoint.Equals('E'))
                    {
                        Logger.Info($"Receive Response Message");
                        return;
                    }
                    else
                        ProcessClientAuthRequestCommandCode(ctx, interchangeMessage);
                }
                base.ChannelRead(ctx, msg);
            }

            public override void ChannelInActived(ChannelHandlerContext ctx)
            {
                if (this.clientMonitorService.OnInActivedTask != null && !this.clientMonitorService.IsStop)
                {
                    this.clientMonitorService.OnInActivedTask();
                }

                ctx.FireChannelInActived();
            }

            public override void ChannelUnRegistered(ChannelHandlerContext ctx)
            {
                if (!this.clientMonitorService.KeepAlive || this.clientMonitorService.IsStop)
                {
                    return;
                }

                Thread.Sleep(this.clientMonitorService.ReconnectTime);
                this.clientMonitorService.CreateConnect();
            }

            private void ProcessClientAuthRequestCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                Logger.Info($"Start Process ProcessClientAuthRequestCommandCode");
                string returnString = string.Empty;
                switch (interchangeMessage.Category.ToUpper())
                {
                    case InterChangeMessageCategory.ClientAuthVISAClient:
                        ProcessVISAClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthMASTERCARDClient:
                        ProcessMASTERCARDClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthNCCCClient:
                        ProcessNCCCClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthNCPSClient:
                        ProcessNCPSClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthSTINClient:
                        ProcessSTINClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthFISCClient:
                        ProcessFISCClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthTWINClient:
                        ProcessTWINClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthSVCSClient:
                        ProcessSVCSClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthICASHClient:
                        ProcessICASHClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthIPASSClient:
                        ProcessIPASSClientCommandCode(ctx, interchangeMessage);
                        break;
                    case InterChangeMessageCategory.ClientAuthSystemCommand:
                        returnString = ProcessSystemCommandCode(ctx, interchangeMessage, clientMonitorService.gatewayClientAuthServiceType);
                        break;
                    default:
                        Logger.Info($"ClientAuth Interchange Message No Process,Invalid Category[{interchangeMessage.Category.ToUpper()}]");
                        break;

                }

                var newInterchangeMessage = new InterchangeMessage
                {
                    StartingPoint = 'E',
                    MessageId = interchangeMessage.MessageId,
                    SourceEndPoint = interchangeMessage.DestinationEndPoint,
                    DestinationEndPoint = interchangeMessage.SourceEndPoint,
                    CreateDateTime = interchangeMessage.CreateDateTime,
                    ReturnDateTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Category = interchangeMessage.Category,
                    CommandCode = interchangeMessage.CommandCode,
                    InterchangeDataFormat = InterChangeDataFormatType.String,
                    InterchangeData = Encoding.ASCII.GetBytes(returnString),
                    ReturnOption = interchangeMessage.ReturnOption,
                    ReturnCode = "00"
                };
                Logger.Info($"ProcessClientAuthRequestCommandCode --- Start Display Send InterchangeMessage ---");
                Logger.Info(ColaCommUtility.GetObjectProperty(newInterchangeMessage));
                Logger.Info($"ProcessClientAuthRequestCommandCode --- End   Display Send InterchangeMessage ---");
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(newInterchangeMessage);
                ctx.Channel.WriteAndFlush(sendBytes);
                if (Logger.IsDebugEnabled)
                {
                    try
                    {
                        var length = sendBytes?.Length ?? 0;
                        Logger.Debug(() => $"WRITE [{ctx.Channel.LocalEndPoint()} => {ctx.Channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytes)}");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytes)}]");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytes)}]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex.Message);
                    }
                }
            }

            private void ProcessVISAClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
            }

            private void ProcessMASTERCARDClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
            }

            private void ProcessNCCCClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                if (CyberAuthNCCCResource.CyberAuthNCCC == null)
                {
                    Logger.Fatal($"NCCC  CyberAuthNCCCResource.CyberAuthNCCC no exist");
                    return;
                }

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.NormalSignOn:
                        CyberAuthNCCCResource.CyberAuthNCCC.SignOn();
                        break;
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.NormalSignOff:
                        CyberAuthNCCCResource.CyberAuthNCCC.SignOff();
                        break;
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.ChangeKey:
                        CyberAuthNCCCResource.CyberAuthNCCC.ChangeKey();
                        break;
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.RepeatKey:
                        CyberAuthNCCCResource.CyberAuthNCCC.RepeatKey();
                        break;
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.VerifyKey:
                        CyberAuthNCCCResource.CyberAuthNCCC.VerifyKey();
                        break;
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.FileMaintUpdate:
                        var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthNCCCResource.CyberAuthNCCC.FileMaint(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.NCCCClientCommandCode.EchoTest:
                        CyberAuthNCCCResource.CyberAuthNCCC.EchoTest();
                        break;
                    default:
                        Logger.Fatal($"NCCC Invalid ActionCode [{actionCode}]");
                        break;
                }
            }
            private void ProcessNCPSClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessNCPSClientCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            private void ProcessSTINClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessSTINClientCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            private void ProcessFISCClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;
                FileMaintData fileMaintData = null;

                if (CyberAuthFISCResource.CyberAuthFISC == null)
                {
                    Logger.Fatal($"FISC  CyberAuthFISCResource.CyberAuthFISC no exist");
                    return;
                }

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.FISCClientCommandCode.NormalSignOn:
                        CyberAuthFISCResource.CyberAuthFISC.SignOn();
                        break;
                    case InterChangeMessageCommandCode.FISCClientCommandCode.NormalSignOff:
                        CyberAuthFISCResource.CyberAuthFISC.SignOff();
                        break;
                    case InterChangeMessageCommandCode.FISCClientCommandCode.ChangeKey:
                        CyberAuthFISCResource.CyberAuthFISC.ChangeKey();
                        break;
                    //case InterChangeMessageCommandCode.FISCClientCommandCode.RepeatKey:
                    //    CyberAuthFISCResource.CyberAuthFISC.RepeatKey();
                    //    break;
                    //case InterChangeMessageCommandCode.FISCClientCommandCode.VerifyKey:
                    //    CyberAuthFISCResource.CyberAuthFISC.VerifyKey();
                    //    break;
                    case InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0300:
                        fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthFISCResource.CyberAuthFISC.FileMaint0300(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.FISCClientCommandCode.FileMaintUpdate0302:
                        fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthFISCResource.CyberAuthFISC.FileMaint0302(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.FISCClientCommandCode.EchoTest:
                        CyberAuthFISCResource.CyberAuthFISC.EchoTest();
                        break;
                    default:
                        Logger.Fatal($"FISC Invalid ActionCode [{actionCode}]");
                        break;
                }
            }

            private void ProcessTWINClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                if (CyberAuthTWINResource.CyberAuthTWIN == null)
                {
                    Logger.Fatal($"TWIN  CyberAuthTWINResource.CyberAuthTWIN no exist");
                    return;
                }

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.TWINClientCommandCode.NormalSignOn:
                        CyberAuthTWINResource.CyberAuthTWIN.SignOn();
                        break;
                    case InterChangeMessageCommandCode.TWINClientCommandCode.NormalSignOff:
                        CyberAuthTWINResource.CyberAuthTWIN.SignOff();
                        break;
                    case InterChangeMessageCommandCode.TWINClientCommandCode.ChangeKey:
                        CyberAuthTWINResource.CyberAuthTWIN.ChangeKey();
                        break;
                    case InterChangeMessageCommandCode.TWINClientCommandCode.RepeatKey:
                        CyberAuthTWINResource.CyberAuthTWIN.RepeatKey();
                        break;
                    case InterChangeMessageCommandCode.TWINClientCommandCode.VerifyKey:
                        CyberAuthTWINResource.CyberAuthTWIN.VerifyKey();
                        break;
                    case InterChangeMessageCommandCode.TWINClientCommandCode.FileMaintUpdate:
                        var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthTWINResource.CyberAuthTWIN.FileMaint(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.TWINClientCommandCode.EchoTest:
                        CyberAuthTWINResource.CyberAuthTWIN.EchoTest();
                        break;
                    default:
                        Logger.Fatal($"TWIN Invalid ActionCode [{actionCode}]");
                        break;
                }
            }

            private void ProcessSVCSClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                if (CyberAuthSVCSResource.CyberAuthSVCS == null)
                {
                    Logger.Fatal($"SVCS CyberAuthSVCSResource.CyberAuthSVCS no exist");
                    return;
                }

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.SVCSClientCommandCode.NormalSignOn:
                        CyberAuthSVCSResource.CyberAuthSVCS.SignOn();
                        break;
                    case InterChangeMessageCommandCode.SVCSClientCommandCode.NormalSignOff:
                        CyberAuthSVCSResource.CyberAuthSVCS.SignOff();
                        break;
                    case InterChangeMessageCommandCode.SVCSClientCommandCode.FileMaintUpdate:
                        var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthSVCSResource.CyberAuthSVCS.FileMaint(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.SVCSClientCommandCode.EchoTest:
                        CyberAuthSVCSResource.CyberAuthSVCS.EchoTest();
                        break;
                    default:
                        Logger.Fatal($"SVCS Invalid ActionCode [{actionCode}]");
                        break;
                }
            }

            private void ProcessICASHClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                if (CyberAuthICASHResource.CyberAuthICASH == null)
                {
                    Logger.Fatal($"ICASH CyberAuthICASHResource.CyberAuthICASH no exist");
                    return;
                }

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.ICASHClientCommandCode.NormalSignOn:
                        CyberAuthICASHResource.CyberAuthICASH.SignOn();
                        break;
                    case InterChangeMessageCommandCode.ICASHClientCommandCode.NormalSignOff:
                        CyberAuthICASHResource.CyberAuthICASH.SignOff();
                        break;
                    case InterChangeMessageCommandCode.ICASHClientCommandCode.FileMaintUpdate:
                        var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthICASHResource.CyberAuthICASH.FileMaint(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.ICASHClientCommandCode.EchoTest:
                        CyberAuthICASHResource.CyberAuthICASH.EchoTest();
                        break;
                    default:
                        Logger.Fatal($"ICASH Invalid ActionCode [{actionCode}]");
                        break;
                }
            }

            private void ProcessIPASSClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                if (CyberAuthIPASSResource.CyberAuthIPASS == null)
                {
                    Logger.Fatal($"IPASS CyberAuthIPASSResource.CyberAuthIPASS no exist");
                    return;
                }

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.IPASSClientCommandCode.NormalSignOn:
                        CyberAuthIPASSResource.CyberAuthIPASS.SignOn();
                        break;
                    case InterChangeMessageCommandCode.IPASSClientCommandCode.NormalSignOff:
                        CyberAuthIPASSResource.CyberAuthIPASS.SignOff();
                        break;
                    case InterChangeMessageCommandCode.IPASSClientCommandCode.FileMaintUpdate:
                        var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                        CyberAuthIPASSResource.CyberAuthIPASS.FileMaint(fileMaintData);
                        break;
                    case InterChangeMessageCommandCode.IPASSClientCommandCode.EchoTest:
                        CyberAuthIPASSResource.CyberAuthIPASS.EchoTest();
                        break;
                    default:
                        Logger.Fatal($"IPASS Invalid ActionCode [{actionCode}]");
                        break;
                }
            }

            private string ProcessSystemCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage, GatewayClientAuthServiceType gatewayClientAuthServiceType)
            {
                Logger.Info($"ProcessSystemCommandCode CommandCode[{interchangeMessage.CommandCode}] Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                string returnString = string.Empty;
                var actionCode = interchangeMessage.CommandCode;
                try
                {
                    switch (actionCode)
                    {
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.GetConnectionStatus:
                            switch (gatewayClientAuthServiceType)
                            {
                                case GatewayClientAuthServiceType.NCCC:
                                    if (!ObjectUtils.IsNull(CyberAuthNCCCResource.CyberAuthNCCC))
                                    {
                                        Logger.Info($"Process NCCC GetConnectionStatus");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthNCCCResource.CyberAuthNCCC.GetConnectionStatus();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthNCCCResource.CyberAuthNCCC no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.ICASH:
                                    if (!ObjectUtils.IsNull(CyberAuthICASHResource.CyberAuthICASH))
                                    {
                                        Logger.Info($"Process ICASH GetConnectionStatus");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthICASHResource.CyberAuthICASH.GetConnectionStatus();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthICASHResource.CyberAuthICASH no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.IPASS:
                                    if (!ObjectUtils.IsNull(CyberAuthIPASSResource.CyberAuthIPASS))
                                    {
                                        Logger.Info($"Process IPASS GetConnectionStatus");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthIPASSResource.CyberAuthIPASS.GetConnectionStatus();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthIPASSResource.CyberAuthIPASS no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.SVCS:
                                    if (!ObjectUtils.IsNull(CyberAuthSVCSResource.CyberAuthSVCS))
                                    {
                                        Logger.Info($"Process SVCS GetConnectionStatus");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthSVCSResource.CyberAuthSVCS.GetConnectionStatus();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthSVCSResource.CyberAuthSVCS no exist");
                                    }
                                    break;
                                default:
                                    Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                    break;
                            }
                            break;
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.GetColaPasysInfo:
                            switch (gatewayClientAuthServiceType)
                            {
                                case GatewayClientAuthServiceType.NCCC:
                                    if (!ObjectUtils.IsNull(CyberAuthNCCCResource.CyberAuthNCCC))
                                    {
                                        Logger.Info($"Process NCCC GetColaPasysInfo");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthNCCCResource.CyberAuthNCCC.GetColaPasysInfo();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthNCCCResource.CyberAuthNCCC no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.ICASH:
                                    if (!ObjectUtils.IsNull(CyberAuthICASHResource.CyberAuthICASH))
                                    {
                                        Logger.Info($"Process ICASH GetColaPasysInfo");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthICASHResource.CyberAuthICASH.GetColaPasysInfo();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthICASHResource.CyberAuthICASH no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.IPASS:
                                    if (!ObjectUtils.IsNull(CyberAuthIPASSResource.CyberAuthIPASS))
                                    {
                                        Logger.Info($"Process IPASS GetConnectionStatus");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthIPASSResource.CyberAuthIPASS.GetConnectionStatus();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthIPASSResource.CyberAuthIPASS no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.SVCS:
                                    if (!ObjectUtils.IsNull(CyberAuthSVCSResource.CyberAuthSVCS))
                                    {
                                        Logger.Info($"Process SVCS GetColaPasysInfo");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthSVCSResource.CyberAuthSVCS.GetColaPasysInfo();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthSVCSResource.CyberAuthSVCS no exist");
                                    }
                                    break;
                                default:
                                    Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                    break;
                            }
                            break;
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.ListReservedKeyData:
                            switch (gatewayClientAuthServiceType)
                            {
                                case GatewayClientAuthServiceType.NCCC:
                                    if (!ObjectUtils.IsNull(CyberAuthNCCCResource.CyberAuthNCCC))
                                    {
                                        Logger.Info($"Process NCCC ListReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthNCCCResource.CyberAuthNCCC.ListReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthNCCCResource.CyberAuthNCCC no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.ICASH:
                                    if (!ObjectUtils.IsNull(CyberAuthICASHResource.CyberAuthICASH))
                                    {
                                        Logger.Info($"Process ICASH ListReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthICASHResource.CyberAuthICASH.ListReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthICASHResource.CyberAuthICASH no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.IPASS:
                                    if (!ObjectUtils.IsNull(CyberAuthIPASSResource.CyberAuthIPASS))
                                    {
                                        Logger.Info($"Process IPASS ListReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthIPASSResource.CyberAuthIPASS.ListReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthIPASSResource.CyberAuthIPASS no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.SVCS:
                                    if (!ObjectUtils.IsNull(CyberAuthSVCSResource.CyberAuthSVCS))
                                    {
                                        Logger.Info($"Process SVCS ListReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthSVCSResource.CyberAuthSVCS.ListReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthSVCSResource.CyberAuthSVCS no exist");
                                    }
                                    break;
                                default:
                                    Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                    break;
                            }
                            break;
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.DeleteReservedKeyData:
                            switch (gatewayClientAuthServiceType)
                            {
                                case GatewayClientAuthServiceType.NCCC:
                                    if (!ObjectUtils.IsNull(CyberAuthNCCCResource.CyberAuthNCCC))
                                    {
                                        Logger.Info($"Process NCCC DeleteReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthNCCCResource.CyberAuthNCCC.DeleteReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthNCCCResource.CyberAuthNCCC no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.ICASH:
                                    if (!ObjectUtils.IsNull(CyberAuthICASHResource.CyberAuthICASH))
                                    {
                                        Logger.Info($"Process ICASH DeleteReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthICASHResource.CyberAuthICASH.DeleteReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthICASHResource.CyberAuthICASH no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.IPASS:
                                    if (!ObjectUtils.IsNull(CyberAuthIPASSResource.CyberAuthIPASS))
                                    {
                                        Logger.Info($"Process IPASS DeleteReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthIPASSResource.CyberAuthIPASS.DeleteReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthIPASSResource.CyberAuthIPASS no exist");
                                    }
                                    break;
                                case GatewayClientAuthServiceType.SVCS:
                                    if (!ObjectUtils.IsNull(CyberAuthSVCSResource.CyberAuthSVCS))
                                    {
                                        Logger.Info($"Process SVCS DeleteReservedKeyData");
                                        StringBuilder sb = new StringBuilder();
                                        returnString = CyberAuthSVCSResource.CyberAuthSVCS.DeleteReservedKeyData();
                                        sb.Append(returnString + Environment.NewLine);
                                        returnString = sb.ToString();
                                    }
                                    else
                                    {
                                        Logger.Warn($"SCyberAuthSVCSResource.CyberAuthSVCS no exist");
                                    }
                                    break;
                                default:
                                    Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                    break;
                            }
                            break;
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.SyncColaPasysInfo:
                            switch (gatewayClientAuthServiceType)
                            {
                                case GatewayClientAuthServiceType.NCCC:
                                    if (!ObjectUtils.IsNull(CyberAuthNCCCResource.CyberAuthNCCC))
                                    {
                                        Logger.Info($"Process NCCC SyncColaPasysInfo");
                                        returnString = CyberAuthNCCCResource.CyberAuthNCCC.SyncColaPasysInfo();
                                    }
                                    else
                                        Logger.Warn($"SCyberAuthNCCCResource.CyberAuthNCCC no exist");
                                    break;
                                case GatewayClientAuthServiceType.ICASH:
                                    if (!ObjectUtils.IsNull(CyberAuthICASHResource.CyberAuthICASH))
                                    {
                                        Logger.Info($"Process ICASH SyncColaPasysInfo");
                                        returnString = CyberAuthICASHResource.CyberAuthICASH.SyncColaPasysInfo();
                                    }
                                    else
                                        Logger.Warn($"SCyberAuthICASHResource.CyberAuthICASH no exist");
                                    break;
                                case GatewayClientAuthServiceType.IPASS:
                                    if (!ObjectUtils.IsNull(CyberAuthIPASSResource.CyberAuthIPASS))
                                    {
                                        Logger.Info($"Process IPASS SyncColaPasysInfo");
                                        returnString = CyberAuthIPASSResource.CyberAuthIPASS.SyncColaPasysInfo();
                                    }
                                    else
                                        Logger.Warn($"SCyberAuthIPASSResource.CyberAuthIPASS no exist");
                                    break;
                                case GatewayClientAuthServiceType.SVCS:
                                    if (!ObjectUtils.IsNull(CyberAuthSVCSResource.CyberAuthSVCS))
                                    {
                                        Logger.Info($"Process SVCS SyncColaPasysInfo");
                                        returnString = CyberAuthSVCSResource.CyberAuthSVCS.SyncColaPasysInfo();
                                    }
                                    else
                                        Logger.Warn($"SCyberAuthSVCSResource.CyberAuthSVCS no exist");
                                    break;
                                default:
                                    Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                    break;
                            }
                            break;
                        default:
                            Logger.Error($"ProcessSystemCommandCode Invalid ActionCode [{actionCode}]");
                            break;
                    }
                    return returnString;
                }
                catch (Exception ex)
                {
                    Logger.Error($"ProcessSystemCommandCode Exception : {ex.Message}");
                    return string.Empty;
                }
            }
        }

        private class DummyEchoHandler : ChannelInboundHandler
        {
            public override void ChannelActived(ChannelHandlerContext ctx)
            {
                Logger.Info($"DummyEchoHandler  ChannelActived {ctx.Channel.Carrier<DummyEchoKey, bool>()}");
                ctx.Channel.Carrier<EchoTimerKey>(
                    new Timer(
                        state =>
                            {
                                if (ctx.Channel.Carrier<DummyEchoKey, bool>())
                                {
                                    var sendBytes = new byte[2] { 0x00, 0x00 };
                                    ctx.Channel.WriteAndFlush(sendBytes);
                                }
                            },
                        null,
                        5000,
                        600000));
                base.ChannelActived(ctx);
            }

            public override void ChannelInActived(ChannelHandlerContext ctx)
            {
                if (!ObjectUtils.IsNull(ctx.Channel.Carrier<EchoTimerKey, Timer>()))
                {
                    ctx.Channel.Carrier<EchoTimerKey, Timer>().Dispose();
                }

                ctx.Channel.RemoveCarrier<EchoTimerKey>();
                base.ChannelInActived(ctx);
            }
        }
    }
}

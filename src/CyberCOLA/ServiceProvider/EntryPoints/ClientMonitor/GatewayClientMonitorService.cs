using System;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor
{
    #region DEPENDENCY

    using System.Net;
    using System.Threading;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Channel.TCP;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.Utils;
    using InvalidOperationException = Com.Cybersoft4u.Yetty.Exception.InvalidOperationException;

    #endregion

    public class GatewayClientMonitorService
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

        private readonly string colaRemotePortList;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatewayClientMonitorService"/> class.
        /// </summary>
        /// <param name="clientName">Name of the client.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="gatewayClientAuthServiceType"></param>
        /// <param name="isDummyEcho"></param>
        public GatewayClientMonitorService(string clientName, EndPoint remoteEndPoint, EndPoint localEndPoint, GatewayClientAuthServiceType gatewayClientAuthServiceType, bool isDummyEcho = false, string colaRemotePortList = "")
        {
            this.localEndPoint = localEndPoint;
            this.remoteEndPoint = remoteEndPoint;
            this.ClientName = clientName;
            this.Bootstrap = new Bootstrap().ChannelType(typeof(TcpSocketChannel));
            this.ReconnectTime = 10000;
            this.KeepAlive = true;
            this.gatewayClientAuthServiceType = gatewayClientAuthServiceType;
            this.isDummyEcho = isDummyEcho;
            this.colaRemotePortList = colaRemotePortList;
        }

        public delegate void OnInActived();

        public delegate void OnReceived(byte[] receiveBytes);

        public string ClientName { get; private set; }

        public bool KeepAlive { get; set; }

        public int ReconnectTime { get; set; }

        public GatewayClientMonitorService EnableLogger(string logName, LogLevel logLevel)
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

        public void Flush()
        {
            this.ClientMonitorChannel.Flush();
        }

        protected void CreateConnect()
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

            private readonly GatewayClientMonitorService gatewayClientMonitorService;

            public Initializer(GatewayClientMonitorService gatewayClientMonitorService)
            {
                this.gatewayClientMonitorService = gatewayClientMonitorService;
                clientMonitorHandler = new ClientMonitorHandler(gatewayClientMonitorService);
                this.logName = null;
            }

            public Initializer(GatewayClientMonitorService clientMonitorService, string logName, LogLevel logLevel)
                : this(clientMonitorService)
            {
                this.logName = logName;
                this.logLevel = logLevel;
            }

            public override void InitChannel(IChannel channel)
            {
                channel.Carrier<ClientMonitorServiceKey>(this.gatewayClientMonitorService);
                channel.Carrier<EndPointKey>(this.gatewayClientMonitorService.remoteEndPoint);
                channel.Carrier<LocalEndPointKey>(this.gatewayClientMonitorService.localEndPoint);
                channel.Carrier<DummyEchoKey>(this.gatewayClientMonitorService.isDummyEcho);
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
            private readonly GatewayClientMonitorService clientMonitorService;

            public ClientMonitorHandler(GatewayClientMonitorService gatewayClientMonitorService)
            {
                this.clientMonitorService = gatewayClientMonitorService;
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
                    Logger.Info($"GatewayClientMonitorService --- Start Display Receive InterchangeMessage ---");
                    Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                    Logger.Info($"GatewayClientMonitorService --- End   Display Receive InterchangeMessage ---");
                    if (interchangeMessage.StartingPoint.Equals('E'))
                    {
                        Logger.Info($"Receive Response Message");
                    }
                    else
                    {
                        switch (interchangeMessage.Category.ToUpper().Trim())
                        {
                            case InterChangeMessageCategory.GatewayVISAService:
                                ProcessVISAServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayMASTERCARDService:
                                ProcessMASTERCARDServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayMASTERCARDANRService:
                                ProcessMASTERCARDARNServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayNCCCCOLAService:
                                ProcessNCCCColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayNCPSCOLAService:
                                ProcessNCPSColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayTWMPCOLAService:
                                ProcessTWMPColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewaySTINCOLAService:
                                ProcessSTINColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayFISCCOLAService:
                                ProcessFISCColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayTWINCOLAService:
                                ProcessTWINColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewaySVCSCOLAService:
                                ProcessSVCSColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayICASHCOLAService:
                                ProcessICASHColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayIPASSCOLAService:
                                ProcessIPASSColaServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayNCCCService:
                                ProcessNCCCServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayNCPSService:
                                ProcessNCPSServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayTWMPService:
                                ProcessTWMPServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewaySTINService:
                                ProcessSTINServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayFISCService:
                                ProcessFISCServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayTWINService:
                                ProcessTWINServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewaySVCSService:
                                ProcessSVCSServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayICASHService:
                                ProcessICASHServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewayIPASSService:
                                ProcessIPASSServiceCommandCode(ctx, interchangeMessage);
                                break;
                            case InterChangeMessageCategory.GatewaySystemCommand:
                                ProcessGatewaySYSTEMCommandCode(ctx, interchangeMessage, clientMonitorService.gatewayClientAuthServiceType);
                                break;
                            default:
                                Logger.Info($"Gateway Interchange Message No Process,Invalid Category[{interchangeMessage.Category.ToUpper()}]");
                                break;
                        }
                    }
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

            /// <summary>
            /// Processes the VISA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessVISAServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessVISAServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the MASTERCARD service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessMASTERCARDServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessMASTERCARDServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the MASTERCARDARN service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessMASTERCARDARNServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessMASTERCARDARNServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the NCCC COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessNCCCColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.NCCCCOLAServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.NcccColaService))
                        {
                            Run.IgnoreException(() =>
                            {
                                CyberGWNCCCResource.NcccColaService.Stop();
                                CyberGWNCCCResource.NcccColaService = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.NcccColaService no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWNCCCResource.NcccColaService))
                        {
                            CyberGWNCCCResource.NcccColaService = new NcccColaService(this.clientMonitorService.colaRemotePortList);
                            CyberGWNCCCResource.NcccColaService.Start();
                        }
                        break;
                }
            }

            /// <summary>
            /// Processes the NCPS COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessNCPSColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessNCPSColaServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the TWMP COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessTWMPColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessTWMPColaServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the STIN COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessSTINColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessSTINColaServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }


            /// <summary>
            /// Processes the FISC COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessFISCColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.FISCCOLAServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWFISCResource.FiscColaService))
                        {
                            Run.IgnoreException(() =>
                            {
                                CyberGWFISCResource.FiscColaService.Stop();
                                CyberGWFISCResource.FiscColaService = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWFISCResource.FiscColaService no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWFISCResource.FiscColaService))
                        {
                            CyberGWFISCResource.FiscColaService = new FiscColaService();
                            CyberGWFISCResource.FiscColaService.Start();
                        }
                        break;
                }
            }

            /// <summary>
            /// Processes the TWIN COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessTWINColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.TWINCOLAServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.TwinColaService))
                        {
                            Run.IgnoreException(() =>
                            {
                                CyberGWTWINResource.TwinColaService.Stop();
                                CyberGWTWINResource.TwinColaService = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.TwinColaService no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWTWINResource.TwinColaService))
                        {
                            CyberGWTWINResource.TwinColaService = new TwinColaService();
                            CyberGWTWINResource.TwinColaService.Start();
                        }
                        break;
                }
            }

            /// <summary>
            /// Processes the SVCS COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessSVCSColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.SVCSCOLAServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWSVCSResource.SvcsColaService))
                        {
                            Run.IgnoreException(() =>
                            {
                                CyberGWSVCSResource.SvcsColaService.Stop();
                                CyberGWSVCSResource.SvcsColaService = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWSVCSResource.SvcsColaService no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.SVCSServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWSVCSResource.SvcsColaService))
                        {
                            CyberGWSVCSResource.SvcsColaService = new SvcsColaService();
                            CyberGWSVCSResource.SvcsColaService.Start();
                        }
                        break;
                }
            }

            /// <summary>
            /// Processes the ICASH COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessICASHColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.ICASHCOLAServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWICASHResource.IcashColaService))
                        {
                            Run.IgnoreException(() =>
                            {
                                CyberGWICASHResource.IcashColaService.Stop();
                                CyberGWICASHResource.IcashColaService = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWICASHResource.IcashColaService no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.ICASHServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWICASHResource.IcashColaService))
                        {
                            CyberGWICASHResource.IcashColaService = new IcashColaService();
                            CyberGWICASHResource.IcashColaService.Start();
                        }
                        break;
                }
            }

            /// <summary>
            /// Processes the IPASS COLA service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessIPASSColaServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                var actionCode = interchangeMessage.CommandCode;

                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.IPASSCOLAServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWIPASSResource.IpassColaService))
                        {
                            Run.IgnoreException(() =>
                            {
                                CyberGWIPASSResource.IpassColaService.Stop();
                                CyberGWIPASSResource.IpassColaService = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWIPASSResource.IpassColaService no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.IPASSServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWIPASSResource.IpassColaService))
                        {
                            CyberGWIPASSResource.IpassColaService = new IpassColaService();
                            CyberGWIPASSResource.IpassColaService.Start();
                        }
                        break;
                }
            }

            /// <summary>
            /// Processes the NCCC service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessNCCCServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //assign process local Port
                int gatewayLocalPortNumber = 0;
                if (interchangeMessage.InterchangeData != null)
                {
                    if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                    {
                        var data = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        if (!int.TryParse(data, out gatewayLocalPortNumber))
                            gatewayLocalPortNumber = 0;

                    }
                }
                var actionCode = interchangeMessage.CommandCode;
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.NormalSignOn:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC.SignOn(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");

                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.NormalSignOff:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC.SignOff(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.ChangeKey:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC.ChangeKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.RepeatKey:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC.RepeatKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.VerifyKey:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC.VerifyKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.EchoTest:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC?.EchoTest(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            Run.IgnoreException(() =>
                            {
                                // Clear the resource first: each dropped session's revive handler reconnects
                                // while it is still set, so a stopped gateway would come back online.
                                var gateway = CyberGWNCCCResource.CyberGWNCCC;
                                CyberGWNCCCResource.CyberGWNCCC = null;
                                gateway?.Disconnect();
                                gateway?.Stop();
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWNCCCResource.CyberGWNCCC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.NCCCServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                        {
                            CyberGWNCCCResource.CyberGWNCCC = new CyberGWNCCC();
                            CyberGWNCCCResource.CyberGWNCCC.Start();
                        }
                        break;
                }

            }

            /// <summary>
            /// Processes the NCPS service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessNCPSServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessNCPSServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the TWMP service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessTWMPServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessTWMPServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the STIN service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessSTINServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //Not implemented
                Logger.Info($"ProcessSTINServiceCommandCode not implemented at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            }

            /// <summary>
            /// Processes the FISC service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessFISCServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //assign process local Port
                int gatewayLocalPortNumber = 0;
                if (interchangeMessage.InterchangeData != null)
                {
                    if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                    {
                        var data = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        if (!int.TryParse(data, out gatewayLocalPortNumber))
                            gatewayLocalPortNumber = 0;

                    }
                }
                var actionCode = interchangeMessage.CommandCode;
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.NormalSignOn:
                        if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                        {
                            CyberGWFISCResource.CyberGWFISC.SignOn(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");

                        }
                        break;
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.NormalSignOff:
                        if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                        {
                            CyberGWFISCResource.CyberGWFISC.SignOff(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.ChangeKey:
                        if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                        {
                            CyberGWFISCResource.CyberGWFISC.ChangeKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");
                        }
                        break;
                    //case InterChangeMessageCommandCode.FISCServiceCommandCode.RepeatKey:
                    //     if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                    //     {
                    //         CyberGWFISCResource.CyberGWFISC.RepeatKey();
                    //     }
                    //     else
                    //     {
                    //         Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");
                    //     }
                    //     break;
                    //case InterChangeMessageCommandCode.FISCServiceCommandCode.VerifyKey:
                    //     if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                    //     {
                    //         CyberGWFISCResource.CyberGWFISC.VerifyKey();
                    //     }
                    //     else
                    //     {
                    //         Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");
                    //     }
                    //     break;
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.EchoTest:
                        if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                        {
                            CyberGWFISCResource.CyberGWFISC?.EchoTest(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                        {
                            Run.IgnoreException(() =>
                            {
                                // Clear the resource first: each dropped session's revive handler reconnects
                                // while it is still set, so a stopped gateway would come back online.
                                var gateway = CyberGWFISCResource.CyberGWFISC;
                                CyberGWFISCResource.CyberGWFISC = null;
                                gateway?.Disconnect();
                                gateway?.Stop();
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWFISCResource.CyberGWFISC no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.FISCServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                        {
                            CyberGWFISCResource.CyberGWFISC = new CyberGWFISC();
                            CyberGWFISCResource.CyberGWFISC.Start();
                        }
                        break;
                }

            }

            /// <summary>
            /// Processes the TWIN service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessTWINServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                //assign process local Port
                int gatewayLocalPortNumber = 0;
                if (interchangeMessage.InterchangeData != null)
                {
                    if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                    {
                        var data = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        if (!int.TryParse(data, out gatewayLocalPortNumber))
                            gatewayLocalPortNumber = 0;

                    }
                }
                var actionCode = interchangeMessage.CommandCode;
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.NormalSignOn:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN.SignOn(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");

                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.NormalSignOff:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN.SignOff(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.ChangeKey:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN.ChangeKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.RepeatKey:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN.RepeatKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.VerifyKey:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN.VerifyKey();
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.EchoTest:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN?.EchoTest(gatewayLocalPortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            Run.IgnoreException(() =>
                            {
                                // Clear the resource first: each dropped session's revive handler reconnects
                                // while it is still set, so a stopped gateway would come back online.
                                var gateway = CyberGWTWINResource.CyberGWTWIN;
                                CyberGWTWINResource.CyberGWTWIN = null;
                                gateway?.Disconnect();
                                gateway?.Stop();
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWTWINResource.CyberGWTWIN no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.TWINServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                        {
                            CyberGWTWINResource.CyberGWTWIN = new CyberGWTWIN();
                            CyberGWTWINResource.CyberGWTWIN.Start();
                        }
                        break;
                }

            }

            /// <summary>
            /// Processes the SVCS service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessSVCSServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                Logger.Info($"Start ProcessSVCSServiceCommandCode at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                //assign process local Port
                int gatewayRemotePortNumber = 0;
                if (interchangeMessage.InterchangeData != null)
                {
                    if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                    {
                        var data = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        Logger.Info($"ProcessSVCSServiceCommandCode process remote PortNumber[{data}]");
                        if (!int.TryParse(data, out gatewayRemotePortNumber))
                            gatewayRemotePortNumber = 0;

                    }
                }
                var actionCode = interchangeMessage.CommandCode;
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.SVCSServiceCommandCode.NormalSignOn:
                        if (!ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                        {
                            CyberGWSVCSResource.CyberGWSVCS.SignOn(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWSVCSResource.CyberGWSVCS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.SVCSServiceCommandCode.NormalSignOff:
                        if (!ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                        {
                            CyberGWSVCSResource.CyberGWSVCS.SignOff(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWSVCSResource.CyberGWSVCS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.SVCSServiceCommandCode.EchoTest:
                        if (!ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                        {
                            CyberGWSVCSResource.CyberGWSVCS.EchoTest(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWSVCSResource.CyberGWSVCS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.SVCSServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                        {
                            Run.IgnoreException(() =>
                            {
                                //CyberGWSVCSResource.CyberGWSVCS.Disconnect();
                                CyberGWSVCSResource.CyberGWSVCS.Stop();
                                CyberGWSVCSResource.CyberGWSVCS = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWSVCSResource.CyberGWSVCS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.SVCSServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                        {
                            CyberGWSVCSResource.CyberGWSVCS = new CyberGWSVCS();
                            CyberGWSVCSResource.CyberGWSVCS.Start();
                        }
                        break;
                }

            }

            /// <summary>
            /// Processes the ICASH service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessICASHServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                Logger.Info($"Start ProcessICASHServiceCommandCode at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                //assign process local Port
                int gatewayRemotePortNumber = 0;
                if (interchangeMessage.InterchangeData != null)
                {
                    if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                    {
                        var data = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        Logger.Info($"ProcessICASHServiceCommandCode process remote PortNumber[{data}]");
                        if (!int.TryParse(data, out gatewayRemotePortNumber))
                            gatewayRemotePortNumber = 0;

                    }
                }
                var actionCode = interchangeMessage.CommandCode;
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.ICASHServiceCommandCode.NormalSignOn:
                        if (!ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                        {
                            CyberGWICASHResource.CyberGWICASH.SignOn(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWICASHResource.CyberGWICASH no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.ICASHServiceCommandCode.NormalSignOff:
                        if (!ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                        {
                            CyberGWICASHResource.CyberGWICASH.SignOff(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWICASHResource.CyberGWICASH no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.ICASHServiceCommandCode.EchoTest:
                        if (!ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                        {
                            CyberGWICASHResource.CyberGWICASH.EchoTest(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWICASHResource.CyberGWICASH no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.ICASHServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                        {
                            Run.IgnoreException(() =>
                            {
                                //CyberGWICASHResource.CyberGWICASH.Disconnect();
                                CyberGWICASHResource.CyberGWICASH.Stop();
                                CyberGWICASHResource.CyberGWICASH = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWICASHResource.CyberGWICASH no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.ICASHServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                        {
                            CyberGWICASHResource.CyberGWICASH = new CyberGWICASH();
                            CyberGWICASHResource.CyberGWICASH.Start();
                        }
                        break;
                }

            }

            /// <summary>
            /// Processes the IPASS service action code.
            /// </summary>
            /// <param name="ctx">The CTX.</param>
            /// <param name="interchangeMessage">The interchange message.</param>
            private void ProcessIPASSServiceCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
            {
                Logger.Info($"Start ProcessIPASSServiceCommandCode at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                //assign process local Port
                int gatewayRemotePortNumber = 0;
                if (interchangeMessage.InterchangeData != null)
                {
                    if (interchangeMessage.InterchangeDataFormat == InterChangeDataFormatType.String)
                    {
                        var data = Encoding.ASCII.GetString(interchangeMessage.InterchangeData);
                        Logger.Info($"ProcessIPASSServiceCommandCode process remote PortNumber[{data}]");
                        if (!int.TryParse(data, out gatewayRemotePortNumber))
                            gatewayRemotePortNumber = 0;

                    }
                }
                var actionCode = interchangeMessage.CommandCode;
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.IPASSServiceCommandCode.NormalSignOn:
                        if (!ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                        {
                            CyberGWIPASSResource.CyberGWIPASS.SignOn(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWIPASSResource.CyberGWIPASS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.IPASSServiceCommandCode.NormalSignOff:
                        if (!ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                        {
                            CyberGWIPASSResource.CyberGWIPASS.SignOff(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWIPASSResource.CyberGWIPASS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.IPASSServiceCommandCode.EchoTest:
                        if (!ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                        {
                            CyberGWIPASSResource.CyberGWIPASS.EchoTest(gatewayRemotePortNumber);
                        }
                        else
                        {
                            Logger.Info($"CyberGWIPASSResource.CyberGWIPASS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.IPASSServiceCommandCode.StopService:
                        if (!ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                        {
                            Run.IgnoreException(() =>
                            {
                                //CyberGWIPASSResource.CyberGWIPASS.Disconnect();
                                CyberGWIPASSResource.CyberGWIPASS.Stop();
                                CyberGWIPASSResource.CyberGWIPASS = null;
                            });
                        }
                        else
                        {
                            Logger.Info($"CyberGWIPASSResource.CyberGWIPASS no exist");
                        }
                        break;
                    case InterChangeMessageCommandCode.IPASSServiceCommandCode.StartService:
                        if (ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                        {
                            CyberGWIPASSResource.CyberGWIPASS = new CyberGWIPASS();
                            CyberGWIPASSResource.CyberGWIPASS.Start();
                        }
                        break;
                }

            }


            private void ProcessGatewaySYSTEMCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage, GatewayClientAuthServiceType gatewayClientAuthServiceType)
            {
                interchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.String;
                var actionCode = interchangeMessage.CommandCode;
                string returnString = string.Empty;
                StringBuilder sb = new StringBuilder();
                switch (actionCode)
                {
                    case InterChangeMessageCommandCode.GatewaySYSTEMCommandCode.GetConnectionStatus:
                        switch (gatewayClientAuthServiceType)
                        {
                            case GatewayClientAuthServiceType.NCCC:
                                if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                                {
                                    returnString = CyberGWNCCCResource.CyberGWNCCC.GetConnectionStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.FISC:
                                if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                                {
                                    returnString = CyberGWFISCResource.CyberGWFISC.GetConnectionStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.TWIN:
                                if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                                {
                                    returnString = CyberGWTWINResource.CyberGWTWIN.GetConnectionStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.SVCS:
                                if (!ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                                {
                                    returnString = CyberGWSVCSResource.CyberGWSVCS.GetConnectionStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.ICASH:
                                if (!ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                                {
                                    returnString = CyberGWICASHResource.CyberGWICASH.GetConnectionStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.IPASS:
                                if (!ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                                {
                                    returnString = CyberGWIPASSResource.CyberGWIPASS.GetConnectionStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            default:
                                Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                break;
                        }
                        break;
                    case InterChangeMessageCommandCode.GatewaySYSTEMCommandCode.GetChannelsStatus:
                        switch (gatewayClientAuthServiceType)
                        {
                            case GatewayClientAuthServiceType.NCCC:
                                if (!ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
                                {
                                    returnString = CyberGWNCCCResource.CyberGWNCCC.GetNcccChannelInfoStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.FISC:
                                if (!ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
                                {
                                    returnString = CyberGWFISCResource.CyberGWFISC.GetFiscChannelInfoStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.TWIN:
                                if (!ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
                                {
                                    returnString = CyberGWTWINResource.CyberGWTWIN.GetTwinChannelInfoStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.SVCS:
                                if (!ObjectUtils.IsNull(CyberGWSVCSResource.CyberGWSVCS))
                                {
                                    returnString = CyberGWSVCSResource.CyberGWSVCS.GetSvcsChannelInfoStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.ICASH:
                                if (!ObjectUtils.IsNull(CyberGWICASHResource.CyberGWICASH))
                                {
                                    returnString = CyberGWICASHResource.CyberGWICASH.GetIcashChannelInfoStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            case GatewayClientAuthServiceType.IPASS:
                                if (!ObjectUtils.IsNull(CyberGWIPASSResource.CyberGWIPASS))
                                {
                                    returnString = CyberGWIPASSResource.CyberGWIPASS.GetIpassChannelInfoStatus();
                                    sb.Append(returnString + Environment.NewLine);
                                }
                                returnString = sb.ToString();
                                break;
                            default:
                                Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                                break;
                        }
                        break;
                    default:
                        Logger.Info($"Invalid ActionCode[{actionCode}],GatewayClientAuthServiceType[{gatewayClientAuthServiceType}]");
                        break;
                }

                interchangeMessage.StartingPoint = 'E';
                var ipEndPointString = interchangeMessage.SourceEndPoint;
                interchangeMessage.SourceEndPoint = interchangeMessage.DestinationEndPoint;
                interchangeMessage.DestinationEndPoint = ipEndPointString;
                interchangeMessage.ReturnDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                interchangeMessage.InterchangeData = Encoding.ASCII.GetBytes(returnString);
                if (string.IsNullOrEmpty(returnString))
                    interchangeMessage.ReturnCode = "99";
                else
                    interchangeMessage.ReturnCode = "00";
                Logger.Info($"ProcessGatewaySYSTEMCommandCode --- Start Display Send InterchangeMessage ---");
                Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                Logger.Info($"ProcessGatewaySYSTEMCommandCode --- End   Display Send InterchangeMessage ---");
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
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
                                    var sendBytes = new byte[] { 0x00, 0x00 };
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

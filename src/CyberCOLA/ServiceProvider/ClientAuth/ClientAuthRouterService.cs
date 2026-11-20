using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.ClientAuth
{
    /// <summary>
    /// Client authorization router service that manages TCP server connections for routing authorization requests.
    /// Maintains concurrent dictionaries of active client connections and channels for message routing.
    /// </summary>
    public class ClientAuthRouterService
    {
        private static string _ipAddressString;
        private static int _portNumber;
        private static int _backLog;
        private static string _clientAuthRouterServiceName;
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandler("ClientAuthRouterService");
        private static ConcurrentDictionary<string, string> _clientOfConnections = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ChannelHandlerContext> _clientOfChannels = new ConcurrentDictionary<string, ChannelHandlerContext>();
        private IChannel _channel;
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientAuthRouterService"/> class.
        /// Configures the TCP server parameters for the authorization router service.
        /// </summary>
        /// <param name="clientAuthServiceName">The name of the client authorization service.</param>
        /// <param name="ipAddressString">The IP address string to bind the server to.</param>
        /// <param name="portNumber">The port number to listen on.</param>
        /// <param name="backLog">The maximum length of the pending connections queue.</param>
        public ClientAuthRouterService(string clientAuthServiceName, string ipAddressString, int portNumber, int backLog)
        {
            _clientAuthRouterServiceName = clientAuthServiceName;
            _ipAddressString = ipAddressString;
            _portNumber = portNumber;
            _backLog = backLog;
        }

        /// <summary>
        /// Starts the client authorization router service by binding to the configured IP address and port.
        /// Creates a TCP server channel with optimized socket options and channel pipeline initializer.
        /// </summary>
        public void StartService()
        {
            var bootstrap = new ServerBootstrap();
            var channel = bootstrap.ChannelType(typeof(TcpServerSocketChannel))
                          .Handler(loggerHandler)
                          .ChildHandler(new Initializer())
                          .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                          //.SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                          .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.Backlog, _backLog)
                          .Bind(new IPEndPoint(IPAddress.Parse(_ipAddressString), _portNumber))
                          .Sync()
                          .Channel();
            _channel = channel;
        }

        /// <summary>
        /// Stops the client authorization router service by closing the TCP server channel.
        /// </summary>
        public void StopService()
        {
            ((TcpServerSocketChannel)_channel)?.Close();
            _channel = null;
        }

        /// <summary>
        /// Gets the concurrent dictionary of client connections.
        /// Maps remote endpoint addresses to connection status strings.
        /// </summary>
        /// <returns>A concurrent dictionary containing client connection information.</returns>
        public ConcurrentDictionary<string, string> GetClientOfConnections()
        {
            return (_clientOfConnections);
        }

        /// <summary>
        /// Gets the concurrent dictionary of client channel contexts.
        /// Maps remote endpoint addresses to their corresponding channel handler contexts for message routing.
        /// </summary>
        /// <returns>A concurrent dictionary containing client channel contexts.</returns>
        public ConcurrentDictionary<string, ChannelHandlerContext> GetClientOfChannels()
        {
            return (_clientOfChannels);
        }


        private class Initializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                channel.Pipe()
                       .AddLast(new LoggerHandler("ClientAuthRouterService-Logger", LogLevel.Debug))
                       .AddLast(new ProcessHandler())
                       .AddLast(new FunctionHandler());
            }

            private class ProcessHandler : ChannelInboundHandler
            {
                public override void ChannelActived(ChannelHandlerContext ctx)
                {
                    Logger.Info($"Add {ctx.Channel.RemoteEndPoint()}");
                    if (!_clientOfConnections.TryAdd(ctx.Channel.RemoteEndPoint().ToString(), "Connection"))
                    {
                        _clientOfConnections.TryUpdate(ctx.Channel.RemoteEndPoint().ToString(), "Connection", "DisConnection");
                    }
                    if (!_clientOfChannels.TryAdd(ctx.Channel.RemoteEndPoint().ToString(), ctx))
                    {
                        Logger.Info($"Add {ctx.Channel.RemoteEndPoint()} Channel Error");
                    }
                    base.ChannelActived(ctx);
                }
                public override void ChannelInActived(ChannelHandlerContext ctx)
                {
                    Logger.Info($"Remove {ctx.Channel.RemoteEndPoint()}");
                    if (!_clientOfConnections.TryRemove(ctx.Channel.RemoteEndPoint().ToString(), out _))
                    {
                        Logger.Info($"Remove {ctx.Channel.RemoteEndPoint()} Failed");
                    }

                    if (!_clientOfChannels.TryRemove(ctx.Channel.RemoteEndPoint().ToString(), out _))
                    {
                        Logger.Info($"Remove Channel {ctx.Channel.RemoteEndPoint()} Failed");
                    }
                    base.ChannelInActived(ctx);
                }
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    try
                    {
                        var bufferBytes = (ByteBuf)msg;
                        //Logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(bufferBytes.Val())}]");
                        var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(bufferBytes.Val());
                        if (interchangeMessage == null)
                        {
                            Logger.Info("Receive Message Invalid");
                            return;
                        }

                        Logger.Info($"ClientAuthRouterService --- Start Display Receive Message ---");
                        Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                        Logger.Info($"ClientAuthRouterService --- End   Display Receive Message ---");
                        base.ChannelRead(ctx, msg);
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal($"ChannelRead Exception:{ex.Message}", ex);
                    }
                }
            }

            private class FunctionHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    var bufferBytes = (ByteBuf)msg;
                    //Logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(bufferBytes.Val())}]");
                    var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(bufferBytes.Val());
                    if (string.IsNullOrEmpty(interchangeMessage.SourceEndPoint))
                        interchangeMessage.SourceEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                    if (string.IsNullOrEmpty(interchangeMessage.DestinationEndPoint))
                        interchangeMessage.DestinationEndPoint = ctx.Channel.LocalEndPoint().ToString();
                    var errorCode = "99";
                    string returnString = string.Empty;

                    switch (interchangeMessage.Category.ToUpper())
                    {
                        case InterChangeMessageCategory.ClientAuthVISAClient:
                            ProcessVISAClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthMASTERCARDClient:
                            ProcessMASTERCARDClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthNCCCClient:
                            ProcessNCCCClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthFISCClient:
                            ProcessFISCClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthTWINClient:
                            ProcessTWINClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthSVCSClient:
                            ProcessSVCSClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthICASHClient:
                            ProcessICASHClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthIPASSClient:
                            ProcessIPASSClientCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        case InterChangeMessageCategory.ClientAuthSystemCommand:
                            returnString = ProcessClientAuthSystemCommandCode(ctx, interchangeMessage);
                            errorCode = "00";
                            break;
                        default:
                            Logger.Warn($"ClientAuth Router Service Interchange Message No Process,Invalid Category[{interchangeMessage.Category.ToUpper()}]");
                            break;

                    }
                    InterchangeMessage returnInterchangeMessage = CreateResponseInterchangeMessage(interchangeMessage);
                    returnInterchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Binary;
                    returnInterchangeMessage.InterchangeData = Encoding.ASCII.GetBytes(returnString);
                    returnInterchangeMessage.ReturnCode = errorCode;
                    var sendBytes = ColaCommUtility.ObjectToXMLBytes(returnInterchangeMessage);

                    if (sendBytes == null)
                    {
                        Logger.Info($"Create Response Interchange Message Error");
                        return;
                    }
                    Logger.Info($"Command [{interchangeMessage.Category.ToUpper()}] Send Return Bytes Length {sendBytes.Length}");
                    ctx.Write(sendBytes);
                    base.ChannelRead(ctx, msg);
                }
                private void ProcessVISAClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
                {
                    //No Implement
                    Logger.Warn($"No Implement ProcessVISAClientCommandCode");
                }
                private void ProcessMASTERCARDClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
                {
                    //No Implement
                    Logger.Warn($"No Implement ProcessMASTERCARDClientCommandCode");
                }
                private void ProcessNCCCClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
                {
                    var actionCode = interchangeMessage.CommandCode;

                    if (CyberAuthNCCCResource.CyberAuthNCCC == null)
                    {
                        Logger.Fatal($"NCCC CyberAuthNCCCResource.CyberAuthNCCC no exist");
                        return;
                    }

                    switch (actionCode)
                    {
                        case InterChangeMessageCommandCode.NCCCClientCommandCode.NormalSignOn:
                            CyberAuthNCCCResource.CyberAuthNCCC.SignOn(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.NCCCClientCommandCode.NormalSignOff:
                            CyberAuthNCCCResource.CyberAuthNCCC.SignOff(interchangeMessage);
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
                            CyberAuthNCCCResource.CyberAuthNCCC.EchoTest(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"NCCC Invalid ActionCode [{actionCode}]");
                            break;
                    }
                }
                private void ProcessFISCClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
                {
                    var actionCode = interchangeMessage.CommandCode;
                    FileMaintData fileMaintData = null;

                    if (CyberAuthFISCResource.CyberAuthFISC == null)
                    {
                        Logger.Fatal($"FISC CyberAuthFISCResource.CyberAuthFISC no exist");
                        return;
                    }

                    switch (actionCode)
                    {
                        case InterChangeMessageCommandCode.FISCClientCommandCode.NormalSignOn:
                            CyberAuthFISCResource.CyberAuthFISC.SignOn(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.FISCClientCommandCode.NormalSignOff:
                            CyberAuthFISCResource.CyberAuthFISC.SignOff(interchangeMessage);
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
                            CyberAuthFISCResource.CyberAuthFISC.EchoTest(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"FISC Invalid ActionCode [{actionCode}]");
                            break;
                    }
                }
                private void ProcessTWINClientCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
                {
                    var actionCode = interchangeMessage.CommandCode;

                    if (CyberAuthTWINResource.CyberAuthTWIN == null)
                    {
                        Logger.Fatal($"TWIN CyberAuthTWINResource.CyberAuthTWIN no exist");
                        return;
                    }

                    switch (actionCode)
                    {
                        case InterChangeMessageCommandCode.TWINClientCommandCode.NormalSignOn:
                            CyberAuthTWINResource.CyberAuthTWIN.SignOn(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.TWINClientCommandCode.NormalSignOff:
                            CyberAuthTWINResource.CyberAuthTWIN.SignOff(interchangeMessage);
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
                            CyberAuthTWINResource.CyberAuthTWIN.EchoTest(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"TWIN Invalid ActionCode [{actionCode}]");
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
                            CyberAuthSVCSResource.CyberAuthSVCS.SignOn(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.SVCSClientCommandCode.NormalSignOff:
                            CyberAuthSVCSResource.CyberAuthSVCS.SignOff(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.SVCSClientCommandCode.FileMaintUpdate:
                            var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                            CyberAuthSVCSResource.CyberAuthSVCS.FileMaint(fileMaintData);
                            break;
                        case InterChangeMessageCommandCode.SVCSClientCommandCode.EchoTest:
                            CyberAuthSVCSResource.CyberAuthSVCS.EchoTest(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"SVCS Invalid ActionCode [{actionCode}]");
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
                            CyberAuthICASHResource.CyberAuthICASH.SignOn(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.ICASHClientCommandCode.NormalSignOff:
                            CyberAuthICASHResource.CyberAuthICASH.SignOff(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.ICASHClientCommandCode.FileMaintUpdate:
                            var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                            CyberAuthICASHResource.CyberAuthICASH.FileMaint(fileMaintData);
                            break;
                        case InterChangeMessageCommandCode.ICASHClientCommandCode.EchoTest:
                            CyberAuthICASHResource.CyberAuthICASH.EchoTest(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"ICASH Invalid ActionCode [{actionCode}]");
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
                            CyberAuthIPASSResource.CyberAuthIPASS.SignOn(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.IPASSClientCommandCode.NormalSignOff:
                            CyberAuthIPASSResource.CyberAuthIPASS.SignOff(interchangeMessage);
                            break;
                        case InterChangeMessageCommandCode.IPASSClientCommandCode.FileMaintUpdate:
                            var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(Encoding.ASCII.GetString(interchangeMessage.InterchangeData));
                            CyberAuthIPASSResource.CyberAuthIPASS.FileMaint(fileMaintData);
                            break;
                        case InterChangeMessageCommandCode.IPASSClientCommandCode.EchoTest:
                            CyberAuthIPASSResource.CyberAuthIPASS.EchoTest(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"IPASS Invalid ActionCode [{actionCode}]");
                            break;
                    }
                }
                private string ProcessClientAuthSystemCommandCode(ChannelHandlerContext ctx, InterchangeMessage interchangeMessage)
                {
                    var actionCode = interchangeMessage.CommandCode;
                    string returnString = string.Empty;
                    switch (actionCode)
                    {
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.GetConnectionStatus:
                            returnString = CyberAuthNCCCResource.CyberAuthNCCC.GetConnectionStatus();
                            break;
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.GetColaPasysInfo:
                            returnString = CyberAuthNCCCResource.CyberAuthNCCC.GetColaPasysInfo();
                            break;
                        case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.SyncColaPasysInfo:
                            returnString = CyberAuthNCCCResource.CyberAuthNCCC.SyncColaPasysInfo();
                            break;
                        default:
                            Logger.Warn($"ProcessClientAuthSystemCommandCode Invalid Command[{_clientAuthRouterServiceName}][{actionCode}]");
                            break;
                    }
                    return (returnString);
                }
                private InterchangeMessage CreateResponseInterchangeMessage(InterchangeMessage requestInterchangeMessage)
                {
                    try
                    {
                        InterchangeMessage responseInterchangeMessage = new InterchangeMessage();
                        responseInterchangeMessage.StartingPoint = 'E';
                        responseInterchangeMessage.MessageId = requestInterchangeMessage.MessageId;
                        responseInterchangeMessage.SourceEndPoint = requestInterchangeMessage.DestinationEndPoint;
                        responseInterchangeMessage.DestinationEndPoint = requestInterchangeMessage.SourceEndPoint;
                        responseInterchangeMessage.CreateDateTime = requestInterchangeMessage.CreateDateTime;
                        responseInterchangeMessage.ReturnDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                        responseInterchangeMessage.ReturnOption = requestInterchangeMessage.ReturnOption;
                        responseInterchangeMessage.Category = requestInterchangeMessage.Category;
                        responseInterchangeMessage.CommandCode = requestInterchangeMessage.CommandCode;
                        return (responseInterchangeMessage);
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal($"CreateResponseInterchangeMessage:{ex.Message}");
                        return null;
                    }
                }
            }
        }
    }
}

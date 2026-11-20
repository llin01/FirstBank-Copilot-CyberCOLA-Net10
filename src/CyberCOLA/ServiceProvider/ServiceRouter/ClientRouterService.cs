using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.ServiceRouter
{
    public class ClientRouterService
    {
        private static string _ipAddressString;
        private static int _portNumber;
        private static int _backLog;
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandler("ClientRouterService");
        private static ConcurrentDictionary<string, string> _clientOfConnections = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ChannelHandlerContext> _clientOfChannels = new ConcurrentDictionary<string, ChannelHandlerContext>();
        private static List<string> _clientAuthServiceLocalPorts;
        private static readonly Logger Logger = LoggerAdapter.GetLogger("LoggerHandler[ClientRouterService]");
        private IChannel _channel;
        public ClientRouterService(string ipAddressString, int portNumber, int backLog, List<string> clientAuthServiceLocalPorts)
        {
            _ipAddressString = ipAddressString;
            _portNumber = portNumber;
            _backLog = backLog;
            _clientAuthServiceLocalPorts = new List<string>(clientAuthServiceLocalPorts);
        }

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

        public void StopService()
        {
            ((TcpServerSocketChannel)_channel)?.Close();
            _channel = null;
        }

        public ConcurrentDictionary<string, string> GetClientOfConnections()
        {
            return (_clientOfConnections);
        }

        public ConcurrentDictionary<string, ChannelHandlerContext> GetClientOfChannels()
        {
            return (_clientOfChannels);
        }

        //public void SyncColaPaysys(InterchangeMessage interchangeMessage)
        //{
        //    foreach (var ctx in _clientOfChannels)
        //    {
        //        if (!interchangeMessage.SourceEndPoint.Equals(ctx.Key))
        //        {
        //            var newInterchangeMessage = new InterchangeMessage();
        //            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
        //            newInterchangeMessage.StartingPoint = 'S';
        //            newInterchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
        //            newInterchangeMessage.SourceEndPoint      = $"{_ipAddressString}:{_portNumber}";
        //            newInterchangeMessage.DestinationEndPoint = ctx.Value.Channel.RemoteEndPoint().ToString();
        //            newInterchangeMessage.CreateDateTime = dateTimeString;
        //            newInterchangeMessage.Category = InterChangeMessageCategory.ServiceCommandSystemCommand;
        //            newInterchangeMessage.CommandCode = InterChangeMessageCommandCode.SYSTEMCommandCode.SyncColaPasysInfo;
        //            newInterchangeMessage.ReturnOption = false;
        //            var sendBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
        //            ctx.Value.Write(sendBytes);
        //            Logger.Info($"SyncColaPaysys, LocalEndPoint({newInterchangeMessage.SourceEndPoint}), RemoteEndPoint{newInterchangeMessage.DestinationEndPoint}");
        //        }
        //    }
        //}

        private class Initializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                channel.Pipe()
                       .AddLast(new LoggerHandler("ClientRouterService-Logger", LogLevel.Debug))
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
                    string value;
                    if (!_clientOfConnections.TryRemove(ctx.Channel.RemoteEndPoint().ToString(), out value))
                    {
                        Logger.Info($"Remove {ctx.Channel.RemoteEndPoint()} Failed");
                    }
                    ChannelHandlerContext ctxValue;
                    if (!_clientOfChannels.TryRemove(ctx.Channel.RemoteEndPoint().ToString(), out ctxValue))
                    {
                        Logger.Info($"Remove Channel {ctx.Channel.RemoteEndPoint()} Failed");
                    }
                    base.ChannelInActived(ctx);
                }
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    var bufferBytes = (ByteBuf)msg;
                    if (bufferBytes.Val().Length == 2)
                    {
                        Logger.Info("Receive Message is Dummy Echo");
                        return;
                    }
                    //Logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(bufferBytes.Val())}]");
                    var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(bufferBytes.Val());
                    if (interchangeMessage == null)
                    {
                        Logger.Info("Receive Message Invalid");
                        return;
                    }
                    else
                    {
                        Logger.Info($"ClientRouterService --- Start Display Receive Message ---");
                        Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                        Logger.Info($"ClientRouterService --- End   Display Receive Message ---");
                        // Queue Receive message asynchronously to avoid blocking the network event loop
                        ServiceRouterLogQueue.Enqueue(interchangeMessage);
                        if (interchangeMessage.StartingPoint.Equals('E'))
                        {
                            Logger.Info($"Receive Message No Process");
                            return;
                        }
                    }
                    base.ChannelRead(ctx, msg);
                }
            }

            private class FunctionHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    var bufferBytes = (ByteBuf)msg;
                    //Logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(bufferBytes.Val())}]");
                    var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(bufferBytes.Val());
                    var category = interchangeMessage.Category.Trim().Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    byte[] sendBytes = null;
                    switch (category.ToUpper())
                    {
                        case "G":
                            Logger.Info($"ProcessGatewayCommand, Category[{interchangeMessage.Category}]");
                            sendBytes = ProcessGatewayCommand(interchangeMessage);
                            break;
                        case "C":
                            Logger.Info($"ProcessClientAuthCommand, Category[{interchangeMessage.Category}]");
                            sendBytes = ProcessClientAuthCommand(interchangeMessage);
                            break;
                        case "R":
                            Logger.Info($"ProcessRouterCommand, Category[{interchangeMessage.Category}]");
                            sendBytes = ProcessRouterCommand(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"Category Invalid {category.ToUpper()}");
                            break;
                    }
                    if (sendBytes == null)
                    {
                        Logger.Info($"Process Read InterchangeMessage Invalid[{category}]");
                        return;
                    }
                    ctx.Write(sendBytes);
                    base.ChannelRead(ctx, msg);
                }
            }
        }

        private static byte[] ProcessGatewayCommand(InterchangeMessage interchangeMessage)
        {
            var clientOfChannels = RouterServiceResource.ServerRouterService?.GetClientOfChannels();
            if (clientOfChannels == null) return null;
            if (clientOfChannels?.Count == 0) return null;
            ChannelHandlerContext ctx;
            if (clientOfChannels.TryGetValue(interchangeMessage.DestinationEndPoint, out ctx))
            {
                var sendBytesToCyberGW = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                ctx.Channel.WriteAndFlush(sendBytesToCyberGW);
                if (Logger.IsDebugEnabled)
                {
                    try
                    {
                        var length = sendBytesToCyberGW?.Length ?? 0;
                        Logger.Debug(() => $"WRITE [{ctx.Channel.LocalEndPoint()} => {ctx.Channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytesToCyberGW)}");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytesToCyberGW)}]");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytesToCyberGW)}]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex.Message);
                    }
                }
            }
            else
            {
                Logger.Error($"Can not find Destination EndPoint[{interchangeMessage.DestinationEndPoint}]");
            }
            InterchangeMessage returnInterchangeMessage = CreateResponseInterchangeMessage(interchangeMessage);
            if (returnInterchangeMessage == null)
                return null;
            returnInterchangeMessage.ReturnCode = "00";
            var sendBytes = ColaCommUtility.ObjectToXMLBytes(returnInterchangeMessage);
            return (sendBytes);
        }

        private static byte[] ProcessClientAuthCommand(InterchangeMessage interchangeMessage)
        {
            var clientOfChannels = RouterServiceResource.ServerRouterService?.GetClientOfChannels();
            if (clientOfChannels == null) return null;
            if (clientOfChannels?.Count == 0) return null;
            ChannelHandlerContext ctx;
            if (clientOfChannels.TryGetValue(interchangeMessage.DestinationEndPoint, out ctx))
            {
                var sendBytesToClientAuth = ColaCommUtility.ObjectToXMLBytes(interchangeMessage);
                ctx.Channel.WriteAndFlush(sendBytesToClientAuth);
                if (Logger.IsDebugEnabled)
                {
                    try
                    {
                        var length = sendBytesToClientAuth?.Length ?? 0;
                        Logger.Debug(() => $"WRITE [{ctx.Channel.LocalEndPoint()} => {ctx.Channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytesToClientAuth)}");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytesToClientAuth)}]");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytesToClientAuth)}]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex.Message);
                    }
                }
            }
            InterchangeMessage returnInterchangeMessage = CreateResponseInterchangeMessage(interchangeMessage);
            if (returnInterchangeMessage == null)
                return null;
            returnInterchangeMessage.ReturnCode = "00";
            var sendBytes = ColaCommUtility.ObjectToXMLBytes(returnInterchangeMessage);
            return (sendBytes);
        }

        private static byte[] ProcessRouterCommand(InterchangeMessage interchangeMessage)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                switch (interchangeMessage.CommandCode)
                {
                    case InterChangeMessageCommandCode.RouterSYSTEMCommandCode.GetConnectionStatus:
                        var clientOfConnections = RouterServiceResource.ServerRouterService?.GetClientOfConnections();
                        if (clientOfConnections == null) break;
                        if (clientOfConnections.Count == 0) break;
                        foreach (var item in clientOfConnections)
                        {
                            string[] endPoingStrings = item.Key.Trim().Split(':');
                            var isExist = _clientAuthServiceLocalPorts.Exists(x => x == endPoingStrings[1]);
                            var connectionInfoData = isExist
                                ? stringBuilder.Append($"ClientAuth#{item.Key}!")
                                : stringBuilder.Append($"Gateway#{item.Key}!");
                        }
                        break;
                    default:
                        Logger.Warn($"ProcessRouterCommand Invalid Command:[{interchangeMessage.CommandCode}])");
                        break;
                }
                InterchangeMessage returnInterchangeMessage = CreateResponseInterchangeMessage(interchangeMessage);
                if (returnInterchangeMessage == null)
                    return null;
                returnInterchangeMessage.InterchangeDataFormat = InterChangeDataFormatType.Binary;
                returnInterchangeMessage.InterchangeData = Encoding.ASCII.GetBytes(stringBuilder.ToString());
                returnInterchangeMessage.ReturnCode = "00";
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(returnInterchangeMessage);
                return (sendBytes);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ProcessRouterCommand Exception:{ex.Message}");
                return null;
            }
        }

        private static InterchangeMessage CreateResponseInterchangeMessage(InterchangeMessage requestInterchangeMessage)
        {
            try
            {
                InterchangeMessage responseInterchangeMessage = new InterchangeMessage
                {
                    StartingPoint = 'E',
                    MessageId = requestInterchangeMessage.MessageId,
                    SourceEndPoint = requestInterchangeMessage.DestinationEndPoint,
                    DestinationEndPoint = requestInterchangeMessage.SourceEndPoint,
                    CreateDateTime = requestInterchangeMessage.CreateDateTime,
                    ReturnDateTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Category = requestInterchangeMessage.Category,
                    CommandCode = requestInterchangeMessage.CommandCode,
                    InterchangeDataFormat = InterChangeDataFormatType.Null,
                    InterchangeData = null
                };
                return (responseInterchangeMessage);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateResponseInterchangeMessage Exception:{ex.Message}");
                return null;
            }
        }
    }
}

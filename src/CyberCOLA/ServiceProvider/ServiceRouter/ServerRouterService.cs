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
    public class ServerRouterService
    {
        private static string _ipAddressString;
        private static int _portNumber;
        private static int _backLog;
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandler("ServerRouterService");
        private static ConcurrentDictionary<string, string> _clientOfConnections = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ChannelHandlerContext> _clientOfChannels = new ConcurrentDictionary<string, ChannelHandlerContext>();
        private static List<string> _clientAuthServiceLocalPorts;
        private static readonly Logger Logger = LoggerAdapter.GetLogger("LoggerHandler[ServerRouterService]");
        private IChannel _channel;
        public ServerRouterService(string ipAddressString, int portNumber, int backLog, List<string> clientAuthServiceLocalPorts)
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
        public void SyncColaPaysys(InterchangeMessage interchangeMessage)
        {
            foreach (var ctx in _clientOfChannels)
            {
                var remoteEndPoint = ctx.Key;
                var remotePortNumber = remoteEndPoint.Substring(remoteEndPoint.IndexOf(':') + 1);
                if (!_clientAuthServiceLocalPorts.Contains(remotePortNumber))
                {
                    Logger.Info($"Skip RemoteEndPoint[{ctx.Key}] Of No ClientAuth ");
                    continue;
                }
                if (remoteEndPoint.Equals(interchangeMessage.SourceEndPoint))
                {
                    Logger.Info($"Skip EndPoint[{ctx.Key}]");
                    continue;
                }
                var newInterchangeMessage = new InterchangeMessage();
                var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
                newInterchangeMessage.StartingPoint = 'S';
                newInterchangeMessage.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
                newInterchangeMessage.SourceEndPoint = $"{_ipAddressString.Trim()}:{_portNumber}";
                newInterchangeMessage.DestinationEndPoint = remoteEndPoint;
                newInterchangeMessage.CreateDateTime = dateTimeString;
                newInterchangeMessage.Category = InterChangeMessageCategory.ClientAuthSystemCommand;
                newInterchangeMessage.CommandCode = InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.SyncColaPasysInfo;
                newInterchangeMessage.ReturnOption = true;
                var sendBytes = ColaCommUtility.ObjectToXMLBytes(newInterchangeMessage);
                ctx.Value.Write(sendBytes);
                Logger.Info($"SyncColaPaysys, LocalEndPoint({newInterchangeMessage.SourceEndPoint}), RemoteEndPoint{newInterchangeMessage.DestinationEndPoint}");
            }
        }
        private class Initializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                channel.Pipe()
                       .AddLast(new LoggerHandler("ServerRouterService-Logger", LogLevel.Debug))
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
                        Logger.Info("ChannelRead Receive Message Invalid");
                        return;
                    }
                    else
                    {
                        Logger.Info($"ServerRouterService --- Start Display Receive Message --- ");
                        Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                        Logger.Info($"ServerRouterService --- End   Display Receive Message --- ");
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
                    var category = interchangeMessage.Category.Trim();
                    switch (category.ToUpper())
                    {
                        case InterChangeMessageCategory.ClientAuthSystemCommand:
                            ProcessClientAuthSystemCommand(interchangeMessage);
                            break;
                        case InterChangeMessageCategory.GatewaySystemCommand:
                            ProcessGatewaySystemCommand(interchangeMessage);
                            break;
                        default:
                            Logger.Warn($"Category Invalid Category:[{category.ToUpper()}]");
                            break;
                    }
                    base.ChannelRead(ctx, msg);
                }
            }
            //private class MonitorHandler : ChannelInboundHandler
            //{
            //    private Timer timer;

            //    public override void ChannelActived(ChannelHandlerContext ctx)
            //    {
            //        Logger.Info($"Add {ctx.Channel.RemoteEndPoint()}");
            //        if (!_clientOfConnections.TryAdd(ctx.Channel.RemoteEndPoint().ToString(), "Connection"))
            //        {
            //            _clientOfConnections.TryUpdate(ctx.Channel.RemoteEndPoint().ToString(), "Connection", "DisConnection");
            //        }
            //        if (!_clientOfChannels.TryAdd(ctx.Channel.RemoteEndPoint().ToString(), ctx))
            //        {
            //            Logger.Info($"Add {ctx.Channel.RemoteEndPoint()} Channel Error");
            //        }
            //        base.ChannelActived(ctx);
            //    }
            //    public override void ChannelInActived(ChannelHandlerContext ctx)
            //    {
            //        Logger.Info($"Remove {ctx.Channel.RemoteEndPoint()}");
            //        string value;
            //        if (!_clientOfConnections.TryRemove(ctx.Channel.RemoteEndPoint().ToString(), out value))
            //        {
            //            Logger.Info($"Remove {ctx.Channel.RemoteEndPoint()} Failed");
            //        }
            //        ChannelHandlerContext ctxValue;
            //        if (!_clientOfChannels.TryRemove(ctx.Channel.RemoteEndPoint().ToString(), out ctxValue))
            //        {
            //            Logger.Info($"Remove Channel {ctx.Channel.RemoteEndPoint()} Failed");
            //        }
            //        base.ChannelInActived(ctx);
            //    }
            //    public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            //    {
            //        var bufferBytes = (ByteBuf)msg;
            //        //_logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(bufferBytes.Val())}]");
            //        var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(bufferBytes.Val());
            //        if (interchangeMessage == null)
            //        {
            //            Logger.Info("Receive Message Invalid");
            //        }
            //        else
            //        {
            //            Logger.Info($" --- Start Display Receive Message --- ");
            //            Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
            //            Logger.Info($" --- End   Display Receive Message --- ");
            //        }
            //        base.ChannelRead(ctx, msg);
            //    }
            //}
        }
        private static void ProcessClientAuthSystemCommand(InterchangeMessage interchangeMessage)
        {
            try
            {
                switch (interchangeMessage.CommandCode)
                {
                    case InterChangeMessageCommandCode.ClientAuthSYSTEMCommandCode.SyncColaPasysInfo:
                        RouterServiceResource.ServerRouterService.SyncColaPaysys(interchangeMessage);
                        break;
                    default:
                        Logger.Warn($"ProcessClientAuthSystemCommand No Implement Command Code:[{interchangeMessage.CommandCode}]");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ProcessClientAuthSystemCommand Exception:{ex.Message}");
            }

        }
        private static void ProcessGatewaySystemCommand(InterchangeMessage interchangeMessage)
        {
            try
            {
                switch (interchangeMessage.CommandCode)
                {
                    case InterChangeMessageCommandCode.GatewaySYSTEMCommandCode.GetChannelsStatus:
                        Logger.Info(" --- Display Channel Status --- ");
                        Logger.Info(interchangeMessage.InterchangeData != null ? ColaCommUtility.PrettyXml(Encoding.ASCII.GetString(interchangeMessage.InterchangeData)) : "Data Is Null");
                        Logger.Info(" --- Display Channel Status --- ");
                        break;
                    default:
                        Logger.Warn($"ProcessGatewaySystemCommand No Implement Command Code:[{interchangeMessage.CommandCode}]");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ProcessGatewaySystemCommand Exception:{ex.Message}");
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
                    CommandCode = requestInterchangeMessage.CommandCode
                };
                return (responseInterchangeMessage);
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CreateResponseInterchangeMessage Exception: {ex.Message}");
                return null;
            }
        }
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using System.Collections.Concurrent;
using System.Net;

namespace Com.Cybersoft4u.CommonLib.Network.Server
{
    public class ServerMonitorService
    {
        private static string _ipAddressString;
        private static int _portNumber;
        private static int _backLog;
        private static string _logName;
        private static ConcurrentDictionary<string, string> _clientOfConnections = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ChannelHandlerContext> _clientOfChannels = new ConcurrentDictionary<string, ChannelHandlerContext>();
        private IChannel _channel;
        public ServerMonitorService(string ipAddressString, int portNumber, int backLog, string logName)
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            _ipAddressString = ipAddressString;
            _portNumber = portNumber;
            _backLog = backLog;
            _logName = logName;
        }

        public void StartService()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            var bootstrap = new ServerBootstrap();
            var channel = bootstrap.ChannelType(typeof(TcpServerSocketChannel))
                          .Handler(new LoggerHandler(_logName, LogLevel.Debug))
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

        public class Initializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                channel.Pipe()
                       .AddLast(new LoggerHandler("ServerMonitorService-Logger", LogLevel.Debug))
                       .AddLast(new MonitorHandler());
            }

            private class EchoHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    var buffer = (ByteBuf)msg;
                    //ctx.Write(buffer);
                    Logger.Info(System.Text.Encoding.Default.GetString(buffer.Val()));
                }
            }

            private class MonitorHandler : ChannelInboundHandler
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
                    //Logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(bufferBytes.Val())}]");
                    var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(bufferBytes.Val());
                    if (interchangeMessage == null)
                    {
                        Logger.Info("Receive Message Invalid");
                    }
                    else
                    {
                        Logger.Info($"ServerMonitorService --- Start Display Receive Message ---");
                        Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
                        Logger.Info($"ServerMonitorService --- End   Display Receive Message ---");
                    }
                    base.ChannelRead(ctx, msg);
                }
            }
        }
    }
}

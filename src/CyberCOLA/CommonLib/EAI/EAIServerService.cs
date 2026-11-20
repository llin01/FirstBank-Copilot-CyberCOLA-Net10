using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Network.Utilis.CarrierKeys;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Com.Cybersoft4u.CommonLib.EAI
{
    public class EAIServerService
    {
        private static string _ipAddressString = default!;
        private static int _portNumber;
        private static int _echoIntervalInMilliSeconds;
        private static string[] _cardNumberArray = default!;
        private static bool _isWaitingSignOn;
        private static readonly Random StaticCardIndex = new();
        private static Logger? Logger = null;
        private IChannel _channel = default!;
        private static readonly ConcurrentDictionary<ChannelHandlerContext, ChannelInfo> ChannelsStatus = new ConcurrentDictionary<ChannelHandlerContext, ChannelInfo>();

        public EAIServerService(string ipAddressString, int portNumber, int echoIntervalInMilliSeconds, string cardXmlFilePath, bool isWaitingSignOn)
        {
            _ipAddressString = ipAddressString;
            _portNumber = portNumber;
            _echoIntervalInMilliSeconds = echoIntervalInMilliSeconds;
            _cardNumberArray = GetCardArray(cardXmlFilePath);
            _isWaitingSignOn = isWaitingSignOn;
        }

        public void StartService()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[EAIServerService]");
            var bootstrap = new ServerBootstrap();
            _channel = bootstrap.ChannelType(typeof(TcpServerSocketChannel))
                          .Handler(new LoggerHandler("EAIServerService", LogLevel.Debug))
                          .ChildHandler(new Initializer())
                          .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                          .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.Backlog, 1000)
                          .Bind(new IPEndPoint(IPAddress.Parse(_ipAddressString), _portNumber))
                          .Sync()
                          .Channel();
        }

        public void StopService()
        {
            try
            {
                _channel.Close();
            }
            catch (Exception ex)
            {
                Logger.Info(ex.Message, ex);
            }
        }

        private static string[] GetCardArray(string cardXmlFilePath)
        {
            var doc = XDocument.Load(cardXmlFilePath);
            return doc.Descendants("CardInformation")
              .Select(cardInformation => cardInformation.Element("CardNumber")?.Value)
              .Where(v => v != null)
              .Select(v => v!)
              .ToArray();
        }

        private class ServerInitializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                channel.Pipe()
                    .AddLast(new LoggerHandler("EAIServerService-Logger", LogLevel.Debug))
                    .AddLast(new InboundHandler());
            }

            public class ServerHandler : ChannelDuplexHandler
            {
                public override void Write(ChannelHandlerContext ctx, object msg)
                {
                    base.Write(ctx, msg);
                }

                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    base.ChannelRead(ctx, msg);
                    var buf = (ByteBuf)msg;
                    base.Write(ctx, buf);
                    Logger.Info(System.Text.Encoding.Default.GetString(buf.Val()));
                }
            }

            private class InboundHandler : ChannelInboundHandler
            {
                public override void ChannelActived(ChannelHandlerContext ctx)
                {
                    Logger.Info("InboundHandler ChannelActived");
                    ctx.Channel.Carrier<SendTimerKey>(
                    new Timer(
                        state =>
                        {
                            ctx.Write(Encoding.ASCII.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fffff")));
                            ctx.Flush();
                        },
                        null,
                        5000,
                        _echoIntervalInMilliSeconds));
                    base.ChannelActived(ctx);
                }
            }
        }

        public class Initializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                channel.Pipe()
                    .AddLast(new LoggerHandler("EAIServerService-Logger", LogLevel.Debug))
                    .AddLast(new EchoHandler())
                    .AddLast(new AuthHandler());
            }

            private class EchoHandler : ChannelInboundHandler
            {
                public override void ChannelActived(ChannelHandlerContext ctx)
                {
                    Logger.Info("InboundHandler ChannelActived");

                    ctx.Channel.Carrier<EchoTimerKey>(
                    new Timer(
                        state =>
                        {
                            byte[] retnBytes = null;
                            retnBytes = CreateEchoTestRequestMessage();
                            ctx.Channel.Write(retnBytes);
                            ctx.Flush();
                        },
                        null,
                        5000,
                        _echoIntervalInMilliSeconds));
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

            private class AuthHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    var buf = (ByteBuf)msg;
                    Logger.Info($"Server Receive Data[{HexStringConverter.FromByteArray(buf.Val())}]");

                    byte[] retnBytes;
                    if (_isWaitingSignOn)
                    {
                        retnBytes = ReceiveMessage(ctx, buf.Val());
                    }
                    else
                    {
                        retnBytes = ReceiveMessage(buf.Val());
                    }
                    if (retnBytes != null)
                    {
                        ctx.Write(retnBytes);
                    }
                    base.ChannelRead(ctx, msg);
                }

                public override void ChannelActived(ChannelHandlerContext ctx)
                {
                    Logger.Info($"Add {ctx.Channel.RemoteEndPoint()}");
                    if (!ChannelsStatus.TryAdd(ctx, new ChannelInfo { IsSignOn = false, IsWriteTimer = false }))
                    {
                        Logger.Info($"Add {ctx.Channel.RemoteEndPoint()} Channel Error");
                        return;
                    }
                }

                public override void ChannelUnRegistered(ChannelHandlerContext ctx)
                {
                    if (!ObjectUtils.IsNull(ctx.Channel.Carrier<SendTimerKey, Timer>()))
                    {
                        ctx.Channel.Carrier<SendTimerKey, Timer>().Dispose();
                    }

                    ctx.Channel.RemoveCarrier<SendTimerKey>();
                    base.ChannelUnRegistered(ctx);
                }
            }

            private static byte[] CreateEchoTestRequestMessage()
            {
                try
                {
                    var ncccRequest = NcccTransCreator.EchoTestRequest();
                    ncccRequest.Header().Decode(SystemConstantData.CommonData.TokenNetworkRequestISOHeader);
                    var updateArray = ncccRequest.Encode().Val();
                    var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(updateArray);
                    return (retnBytes);
                }
                catch (Exception ex)
                {
                    Logger.Info(ex.Message);
                    return null;
                }
            }

            private static byte[] ReceiveMessage(byte[] requestBytes)
            {
                try
                {
                    var processBytes = new byte[requestBytes.Length - 2];
                    Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                    var requestNccc = new NcccIso8583(processBytes);
                    Logger.Info("Parsing Receive Message");
                    Logger.Info($"Header[{requestNccc.Header().Encode()}]");
                    Logger.Info($"MTI [{requestNccc.Mti()}]");
                    foreach (var pair in requestNccc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }
                    var responseNccc = new NcccIso8583(processBytes);
                    if ((Convert.ToInt16(requestNccc.Mti()[2])) % 2 == 0)
                    {
                        Logger.Info($"Request Message");
                        var mti = $"{requestNccc.Mti()[0]}{requestNccc.Mti()[1]}{Convert.ToInt16(requestNccc.Mti()[2]) + 1}{requestNccc.Mti()[3]}";
                        responseNccc.Mti(mti);
                        var cardIndex = StaticCardIndex.Next(0, _cardNumberArray.Length - 1);
                        var cardNumber = _cardNumberArray[cardIndex];
                        var tempData = (string)responseNccc.Core().Field(FIELD.F_35);
                        responseNccc.Core().Field(FIELD.F_35, string.Concat(cardNumber, tempData.AsSpan(16)));
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        string field35 = (string)requestNccc.Core().Field(FIELD.F_35);
                        var field122 = $"{field35.Length:00}{field35}{new string(' ', 11)}000000";
                        responseNccc.Core().Field(FIELD.F_122, field122);
                    }
                    else
                    {
                        Logger.Info($"Response Message");
                        return null;
                    }
                    Logger.Info("Nccc Create Response Message");
                    responseNccc.Header().Decode(string.Concat(responseNccc.Header().Encode().AsSpan(0, 11), "7"));
                    foreach (var pair in responseNccc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }
                    var responseBytes = responseNccc.Encode().Val();
                    var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                    return (retnBytes);
                }
                catch (Exception ex)
                {
                    Logger.Info(ex.Message);
                    return null;
                }
            }

            private static byte[] ReceiveMessage(ChannelHandlerContext ctx, byte[] requestBytes)
            {
                try
                {
                    var processBytes = new byte[requestBytes.Length - 2];
                    Buffer.BlockCopy(requestBytes, 2, processBytes, 0, requestBytes.Length - 2);
                    var requestNccc = new NcccIso8583(processBytes);
                    bool isError = false;
                    Logger.Info("Parsing Receive Message");
                    Logger.Info($"Receive Message{ctx.Channel.RemoteEndPoint()}_{requestNccc.Val(FIELD.F_11)}");
                    Logger.Info($"Header[{requestNccc.Header().Encode()}]");
                    Logger.Info($"MTI [{requestNccc.Mti()}]");
                    foreach (var pair in requestNccc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }
                    bool isItemExists = ChannelsStatus.TryGetValue(ctx, out var currentChannelInfo);
                    if (!isItemExists) return null;
                    var updateChannelInfo = new ChannelInfo
                    {
                        IsSignOn = currentChannelInfo.IsSignOn,
                        IsWriteTimer = currentChannelInfo.IsWriteTimer
                    };

                    if (!currentChannelInfo.IsSignOn)
                    {
                        if (requestNccc.Mti().Equals("0800") |
                            requestNccc.Mti().Equals("0820") |
                            requestNccc.Mti().Equals("0821"))
                        {
                            const FIELD field = FIELD.F_70;
                            if (!ObjectUtils.IsNull(requestNccc.RawVal(field)))
                            {
                                var informationCode = (string)requestNccc.Core().Field(field);
                                if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn))
                                    updateChannelInfo.IsSignOn = true;
                                else
                                {
                                    Logger.Info($"Information Code Invalid, Information Code [{informationCode}]");
                                    isError = true;
                                }
                            }
                            else
                            {
                                Logger.Info($"Information Code No Data");
                                isError = true;
                            }
                        }
                        else
                        {
                            Logger.Info($"Must be First SignOn, MTI [{requestNccc.Mti()}]");
                            isError = true;
                        }
                    }
                    if (!isError)
                    {
                        if (updateChannelInfo.IsSignOn)
                        {
                            if (requestNccc.Mti().Equals("0800") |
                                requestNccc.Mti().Equals("0820") |
                                requestNccc.Mti().Equals("0821"))
                            {
                                const FIELD field = FIELD.F_70;
                                if (!ObjectUtils.IsNull(requestNccc.RawVal(field)))
                                {
                                    var informationCode = (string)requestNccc.Core().Field(field);
                                    if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn))
                                    {
                                        if (!updateChannelInfo.IsWriteTimer)
                                        {
                                            var remoteEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                                            var remotePortNumber = remoteEndPoint[(remoteEndPoint.IndexOf(':') + 1)..];
                                            updateChannelInfo.IsWriteTimer = true;
                                        }
                                    }
                                    else if (informationCode.Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOff))
                                    {
                                        if (updateChannelInfo.IsWriteTimer)
                                        {
                                            if (!ObjectUtils.IsNull(ctx.Channel.Carrier<SendTimerKey, Timer>()))
                                            {
                                                ctx.Channel.Carrier<SendTimerKey, Timer>().Dispose();
                                            }
                                            ctx.Channel.RemoveCarrier<SendTimerKey>();
                                            updateChannelInfo.IsWriteTimer = false;
                                            updateChannelInfo.IsSignOn = false;
                                        }
                                        updateChannelInfo.IsWriteTimer = false;
                                        updateChannelInfo.IsSignOn = false;
                                    }
                                }
                                else
                                {
                                    Logger.Info($"Information Code No Data");
                                    isError = true;
                                }
                            }
                        }
                        ChannelsStatus.TryUpdate(ctx, updateChannelInfo, currentChannelInfo);
                    }
                    var responseNccc = new NcccIso8583(processBytes);
                    if ((Convert.ToInt16(requestNccc.Mti()[2])) % 2 == 0)
                    {
                        Logger.Info($"Request Message");
                        var mti = $"{requestNccc.Mti()[0]}{requestNccc.Mti()[1]}{Convert.ToInt16(requestNccc.Mti()[2]) + 1}{requestNccc.Mti()[3]}";
                        responseNccc.Mti(mti);
                        var cardIndex = StaticCardIndex.Next(0, _cardNumberArray.Length - 1);
                        var cardNumber = _cardNumberArray[cardIndex];
                        responseNccc.Core().Field(FIELD.F_35, cardNumber);
                        responseNccc.Core().Field(FIELD.F_39, "00");
                        string field35 = (string)requestNccc.Core().Field(FIELD.F_35);
                        var field122 = $"{field35.Length:00}{field35}{new string(' ', 11)}000000";
                        responseNccc.Core().Field(FIELD.F_122, field122);
                    }
                    else
                    {
                        Logger.Info($"Response Message");
                        return null;
                    }
                    Logger.Info("Nccc Create Response Message");
                    foreach (var pair in responseNccc.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            Logger.Info($"[FIELD {key}] VALUE [{tempString}]");
                        }
                        else
                        {
                            Logger.Info($"[FIELD {key}] VALUE [{value}]");
                        }
                    }
                    var responseBytes = responseNccc.Encode().Val();
                    var retnBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                    return (retnBytes);
                }
                catch (Exception ex)
                {
                    Logger.Info(ex.Message);
                    return null;
                }
            }
        }

        class ChannelInfo
        {
            public bool IsSignOn { get; set; }
            public bool IsWriteTimer { get; set; }
        }
    }
}

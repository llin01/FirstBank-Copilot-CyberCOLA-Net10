using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfETicketClientSimulator
{
    public class ClientICASH
    {
        private static string _clientIPAddr;
        private static int _localPortStart;
        private static int _sessionCount;
        private static bool _isReturn;
        private static string _formatType;
        private static int _retainMinutes = 10;
        private static bool _isReConnection = true;
        private static string _tpduIssuerID = "0007";
        private static string _tpduHostID = "8888";
        private static readonly Logger Logger = LoggerAdapter.GetLogger("LoggerHandler[ClientICASH]");
        private static Bootstrap _bootstrap;
        private static IPEndPoint _remoteEndPoint;
        private static ConcurrentDictionary<IChannel, string> _clientChannels = new ConcurrentDictionary<IChannel, string>();
        private static ConcurrentDictionary<string, ReservedKeyData> _reservedKeyDatas = new ConcurrentDictionary<string, ReservedKeyData>();
        private static readonly object GetFreeLocalEndPointLocker = new object();
        private static List<IPEndPoint> _localEndPoints = null;
        public ClientICASH()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();

                var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-target-ip").Trim(), AddressFamily.InterNetwork);
                var portNumber = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-target-port").Trim());
                var localHostName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-local-ip").Trim();
                _clientIPAddr = CommonUtils.GetIPAddressStringByHostName(localHostName, AddressFamily.InterNetwork);
                _localPortStart = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-local-port-start").Trim());

                _sessionCount = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-sessions").Trim());
                _isReturn = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-return-flag").Default("F").ToUpper().Equals("TRUE");
                _formatType = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-format-type").Trim().ToUpper();
                _retainMinutes = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-key-data-retain-minutes").Default("10"));
                _tpduIssuerID = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-tpdu-issuerid").Trim();
                _tpduHostID = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "icash-cola-service-tpdu-clientid").Trim();
                if (!_formatType.ToUpper().Equals("ICASH"))
                {
                    Logger.Fatal("This Authorization Only Process ICASH Format");
                    throw new Exception("Accept Format Type Error");
                }
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), portNumber);
                Logger.Info($"ClientICASH Client Authorization Service ServerIPAddress[{serverIPAddr}],ClientIPAddress[{_clientIPAddr}],Port Number[{portNumber}],SessionCount[{_sessionCount}]");

                _bootstrap = new Bootstrap();
                Logger.Info("----- Start Display ColaPasysInfo -----");
                Logger.Info($"\n{SingletonOfCommonProperty.Instance.DisplayColaPasysInfo()}");
                Logger.Info("----- End   Display ColaPasysInfo -----");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"ClientICASH Constructor Exception : {ex.Message}");
                throw;
            }
        }
        public void Start()
        {
            try
            {
                _isReConnection = true;
                _localEndPoints = new List<IPEndPoint>();
                _bootstrap.ChannelType(typeof(TcpSocketChannel))
                          .Handler(new LoggerHandler("ClientICASH", LogLevel.Debug))
                          .Handler(new ClientInitializer())
                          .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true);
                //   .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true);
                for (int index = 1; index <= _sessionCount; index++)
                {
                    var localPort = _localPortStart + index - 1;
                    //ThreadPool.QueueUserWorkItem(
                    //                 state =>
                    //                 {
                    //                         var localEndPoint = new IPEndPoint(IPAddress.Parse(_clientIPAddr), localPort);
                    //                         var channel = _bootstrap.SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                    //                                                 .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                    //                                                 .Connect(_remoteEndPoint, localEndPoint).Sync().Channel();
                    //                         _localEndPoints.Add(localEndPoint);
                    //                         //var channel = bootstrap.Connect(remoteEndPoint).Sync().Channel();
                    //                         if (channel.IsConnected() && !channel.IsClosed())
                    //                         {
                    //                             if (!_clientChannels.TryAdd(channel, channel.LocalEndPoint().ToString()))
                    //                                 Logger.Info($"Add Channel[{channel.LocalEndPoint().ToString()}] Failed");
                    //                         }
                    //                 });
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(_clientIPAddr), localPort);
                    _localEndPoints.Add(localEndPoint);
                    Task.Factory.StartNew(() =>
                    {
                        var channel = _bootstrap.SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                                                .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                                                .Connect(_remoteEndPoint, localEndPoint).Sync().Channel();
                        if (channel.IsConnected() && !channel.IsClosed())
                        {
                            if (!_clientChannels.TryAdd(channel, channel.LocalEndPoint().ToString()))
                                Logger.Info($"Add Channel[{channel.LocalEndPoint().ToString()}] Failed");
                        }
                    });
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Start CyberAuthICASH Exception : {ex.Message}");
                throw;
            }
        }
        public void Stop()
        {
            _isReConnection = false;
            var channels = _clientChannels.Keys.ToList();
            foreach (var channel in channels)
            {
                try
                {
                    if (channel.IsConnected())
                    {
                        Logger.Fatal($"DisConnect LocalEndPoint[{channel.LocalEndPoint()}] --> RemoteEndPointStart[{channel.RemoteEndPoint()}]");
                        channel.Disconnect();
                        //channel.Close();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message);
                }
            }

        }
        public void SignOn()
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("SignOn No Client Channel Use");
                return;
            }
            var channel = _clientChannels.Keys.ToArray()[0];
            if (channel == null)
            {
                Logger.Error("Create Message0800 Of SignOn No Channel");
                return;
            }

            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            var requestIcashMessage = IcashTransCreator.IcashSignOnRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIcashMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestIcashMessage, tranLogKey))
            {
                Logger.Fatal("ClientICASH SignOn AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIcashMessage.Encode().Val();
            var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
            channel.WriteAndFlush(sendBytes);
            if (Logger.IsDebugEnabled)
            {
                try
                {
                    var length = sendBytes?.Length ?? 0;
                    Logger.Debug(() => $"WRITE [{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytes)}");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytes)}]");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytes)}]");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message);
                }
            }
            Logger.Info($"{channel.LocalEndPoint()} End Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
        }

        public void SignOff()
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("SignOff No Client Channel Use");
                return;
            }
            var channel = _clientChannels.Keys.ToArray()[0];
            if (channel == null)
            {
                Logger.Error("Create Message0800 Of SignOff No Channel");
                return;
            }

            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            var requestIcashMessage = IcashTransCreator.IcashSignOffRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIcashMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestIcashMessage, tranLogKey))
            {
                Logger.Fatal("ClientICASH SignOff AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIcashMessage.Encode().Val();
            var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
            channel.WriteAndFlush(sendBytes);
            if (Logger.IsDebugEnabled)
            {
                try
                {
                    var length = sendBytes?.Length ?? 0;
                    Logger.Debug(() => $"WRITE [{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytes)}");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytes)}]");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytes)}]");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message);
                }
            }
            Logger.Info($"{channel.LocalEndPoint()} End Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
        }

        public void EchoTest()
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("EchoTest No Client Channel Use");
                return;
            }
            var channel = _clientChannels.Keys.ToArray()[0];
            if (channel == null)
            {
                Logger.Error("Create IcashMessage0800 Of EchoTest No Channel");
                return;
            }

            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            var requestIcashMessage = IcashTransCreator.IcashEchoTestRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIcashMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIcashMessage.Encode().Val();
            var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
            channel.WriteAndFlush(sendBytes);
            if (Logger.IsDebugEnabled)
            {
                try
                {
                    var length = sendBytes?.Length ?? 0;
                    Logger.Debug(() => $"WRITE [{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytes)}");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytes)}]");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytes)}]");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message);
                }
            }
            Logger.Info($"{channel.LocalEndPoint()} End Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
        }

        public void SendMessageToGateway(ICASH icashMessage)
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("ClientICASH SendMessageToGateway No Client Channel Use");
                return;
            }
            //random select channel
            int clientChannels = _clientChannels.Keys.ToArray().Length;
            int channelNumber = ColaCommUtility.GetSecurityNumber(clientChannels);
            Logger.Info($"ClientChannel[{clientChannels}], Send Channel Number [{channelNumber}]");
            var channel = _clientChannels.Keys.ToArray()[channelNumber];
            if (channel == null)
            {
                Logger.Error("Send Message To IcashColaService No Channel");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, Start Process ClientICASH SendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = icashMessage.Encode().Val();
            var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
            channel.WriteAndFlush(sendBytes);
            if (Logger.IsDebugEnabled)
            {
                try
                {
                    var length = sendBytes?.Length ?? 0;
                    Logger.Debug(() => $"WRITE [{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytes)}");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytes)}]");
                    Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytes)}]");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex.Message);
                }
            }
            Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, End Process ClientICASH SendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
        }
        //this method for test
        public void AllChannelSendMessageToGateway(ICASH icashMessage)
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("ClientICASH AllChannelSendMessageToGateway No Client Channel Use");
                return;
            }
            Logger.Info($"Start Process ClientICASH AllChannelSendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var channels = _clientChannels.Keys.ToArray();
            foreach (var c1 in channels)
            {
                if (c1 == null)
                {
                    Logger.Error("Send Message To IcashColaService Channel is null");
                    continue;
                }
                Logger.Info($"{c1.LocalEndPoint()} => {c1.RemoteEndPoint()}, Start Process ClientICASH AllChannelSendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var newIcashMessage = new ICASH(icashMessage.Encode().Val());
                newIcashMessage.Header().Decode($"{_tpduHostID}{_tpduIssuerID}");
                //update field data
                var fieldName = FIELD.F_7;
                if (newIcashMessage.Core().Field(fieldName) != null)
                {
                    newIcashMessage.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                }
                var stan = SerialGenerator.Stan();
                fieldName = FIELD.F_11;
                if (newIcashMessage.Core().Field(fieldName) != null)
                {
                    newIcashMessage.Core().Field(fieldName, stan);
                }
                fieldName = FIELD.F_37;
                if (newIcashMessage.Core().Field(fieldName) != null)
                {
                    newIcashMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}{stan}");
                }
                var dataBytes = newIcashMessage.Encode().Val();
                var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
                c1.Write(sendBytes);
                if (Logger.IsDebugEnabled)
                {
                    try
                    {
                        var length = sendBytes?.Length ?? 0;
                        Logger.Debug(() => $"WRITE [{c1.LocalEndPoint()} => {c1.RemoteEndPoint()}]\nLength: {length}B\n{ColaCommUtility.PrintBytesMap(sendBytes)}");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesHex(sendBytes)}]");
                        Logger.Debug(() => $"[{ColaCommUtility.PrintBytesData(sendBytes)}]");
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex.Message);
                    }
                }
                Logger.Info($"{c1.LocalEndPoint()} => {c1.RemoteEndPoint()}, End Process ClientICASH AllChannelSendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
        }
        public bool IsHaveChannels()
        {
            return _clientChannels.Keys.ToArray().Length > 0;
        }
        public string SyncColaPasysInfo()
        {
            Logger.Debug($"Write Before --- SystemDte[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCSystemDt}], SystemUpdDte[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCSystemUpdDt}], IwkChangeDte[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCIwkChangeDt}], IwkCheckDigits[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCIwkCheckDigits}]");
            SingletonOfCommonProperty.Instance.SyncColaPasysInfoData();
            Logger.Debug($"Write After  --- SystemDte[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCSystemDt}], SystemUpdDte[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCSystemUpdDt}], IwkChangeDte[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCIwkChangeDt}], IwkCheckDigits[{SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct().NCCCIwkCheckDigits}]");
            return string.Empty;
        }

        public string GetConnectionStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}LocalEndPoint --> RemoteEndPoint{Environment.NewLine}");
            foreach (var channel in _clientChannels.Keys.ToArray())
            {
                sb.Append($"[{channel.LocalEndPoint()}] --> [{channel.RemoteEndPoint()}]{Environment.NewLine}");
            }
            return sb.ToString();
        }
        public string GetColaPasysInfo()
        {
            return SingletonOfCommonProperty.Instance.DisplayColaPasysInfo();
        }
        public string DeleteReservedKeyData()
        {
            Logger.Info($"ClientICASH, Retain Minutes[{_retainMinutes}] DeleteReservedKeyData Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var keysLists = _reservedKeyDatas.Where(x => DateTime.Now.Subtract(x.Value.CreateDateTime).TotalMinutes > (double)_retainMinutes)
                                                       .Select(x => x.Key).ToList();
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}ClientICASH DeleteReservedKeyData{Environment.NewLine}");
            foreach (var key in keysLists)
            {
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    sb.Append($"ClientICASH RemoveReservedKeyData Remove Key[{key}] Successful{Environment.NewLine}");
                }
                else
                {
                    sb.Append($"ClientICASH RemoveReservedKeyData Remove Key [{key}] Failed{Environment.NewLine}");
                }
            }
            Logger.Info($"DeleteReservedKeyData Info : [{sb.ToString()}]");
            return sb.ToString();
        }
        public string ListReservedKeyData()
        {
            Logger.Info($"ClientICASH ListReservedKeyData Start : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var dataLists = _reservedKeyDatas.OrderBy(x => x.Value.CreateDateTime)
                                                                   .Select(x => new { x.Key, x.Value.CreateDateTime }).ToList();

            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}ClientICASH ListReservedKeyData{Environment.NewLine}");
            foreach (var data in dataLists)
            {
                sb.Append($"Key[{data.Key}] --> CreateDateTime[{data.CreateDateTime:yyyy-MM-dd HH:mm:ss:fffff}]{Environment.NewLine}");
            }
            Logger.Info($"ListReservedKeyData Info:[{sb.ToString()}]");
            return sb.ToString();
        }
        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <returns>List IChannel.</returns>
        public List<IChannel> GetChannels()
        {
            return (_clientChannels?.Keys.ToList<IChannel>());
        }
        private bool AddReservedKeyData(ICASH icash, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{icash.Val(FIELD.F_7)}-{icash.Val(FIELD.F_11)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"ClientICASH AddReservedKeyData Add Key Data Mti[{icash.Mti()}, Key[{key}]");
            return (_reservedKeyDatas.TryAdd(key, keyData));
        }
        private bool AddReservedKeyDataForNetwork(ICASH icash, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{icash.Val(FIELD.F_70)}-{icash.Val(FIELD.F_11)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"ClientICASH AddReservedKeyDataForNetwork Add Key Data Mti[{icash.Mti()}, Key[{key}]");
            return (_reservedKeyDatas.TryAdd(key, keyData));
        }
        public class ClientInitializer : ChannelInitializer
        {
            private static Random rnd = new Random();
            public override void InitChannel(IChannel channel)
            {

                channel.Carrier<EndPointKey>(_remoteEndPoint);
                channel.Carrier<DisconnectKey>(false);
                channel.Carrier<BootstrapKey>(_bootstrap);

                channel.Pipe()
                    .AddLast(new LoggerHandler("ClientICASH-Logger", LogLevel.Debug))
                    .AddLast(new InboundMultiWorkerHandler())
                    .AddLast(new InboundHandler())
                    .AddLast(new ChannelReviveHandler());
            }

            public class ClientHandler : ChannelDuplexHandler
            {
                public override void Write(ChannelHandlerContext ctx, object msg)
                {
                    var buf = (ByteBuf)msg;
                    base.Write(ctx, msg);
                }

                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    base.ChannelRead(ctx, msg);
                    var buf = (ByteBuf)msg;
                    Logger.Info($"{ctx.Channel.LocalEndPoint()} Receive Data[{Encoding.Default.GetString(buf.Val())}]");
                }
            }
            public class InboundHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    Logger.Debug($"Go Hander: {GetType().FullName}");
                    base.ChannelRead(ctx, msg);
                    var receiveByteBuf = (ByteBuf)msg;
                    byte[] receiveBytes = new byte[receiveByteBuf.Val().Length - 2];
                    Buffer.BlockCopy(receiveByteBuf.Val(), 2, receiveBytes, 0, receiveByteBuf.Val().Length - 2);
                    var tempString = HexStringConverter.FromByteArray(receiveByteBuf.Val());
                    //if (Logger.IsDebugEnabled)
                    //{
                    //    Logger.Debug($"Receive Data Hex String [{tempString}]");
                    //    Logger.Debug($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                    //}
                    Logger.Info($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                    if (_isReturn) return;
                    var sendBuffer = ReceiveMessageForIcash(receiveByteBuf.Val(), ctx.Channel.LocalEndPoint().ToString());
                    if (sendBuffer == null)
                    {
                        Logger.Info("Send Buffer Data Is Null, No Process");
                        return;
                    }
                    ctx.Write(sendBuffer);
                }
            }
            public class OutboundHandler : ChannelOutboundHandler
            {
                public override void Write(ChannelHandlerContext ctx, object msg)
                {
                    var buf = (ByteBuf)msg;
                    base.Write(ctx, msg);
                }

                //public override void Write(ChannelHandlerContext ctx, object msg)
                //{
                //    var buf = (ByteBuf)msg;
                //    base.Write(ctx, msg);
                //}
            }
            public class InboundMultiWorkerHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    Logger.Debug($"Go Hander: {GetType().FullName}");
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            base.ChannelRead(ctx, msg);
                        }
                        catch (Exception ex)
                        {
                            Logger.Fatal($"InboundMultiWorkerHandler ChannelRead Exception : {ex.Message}");
                        }
                    });
                    //Logger.Debug($"Go Hander: {GetType().FullName}");
                    //ThreadPool.QueueUserWorkItem(
                    //    state =>
                    //        {
                    //            try
                    //            {
                    //                base.ChannelRead(ctx, msg);
                    //            }
                    //            catch (Exception exception)
                    //            {
                    //                Logger.Fatal(exception);
                    //            }
                    //        });
                }

                public override void ChannelReadCompleted(ChannelHandlerContext ctx)
                {
                    ThreadPool.QueueUserWorkItem(
                        state =>
                            {
                                try
                                {
                                    base.ChannelReadCompleted(ctx);
                                }
                                catch (Exception exception)
                                {
                                    Logger.Fatal(exception);
                                }
                            });
                }
            }

            public class ChannelReviveHandler : ChannelInboundHandler
            {
                private readonly int interval;

                public ChannelReviveHandler(int interval = 5000)
                {
                    this.interval = interval;
                }

                public override void ChannelUnRegistered(ChannelHandlerContext ctx)
                {
                    base.ChannelUnRegistered(ctx);
                    if (!_isReConnection) return;
                    var bootstrap = ctx.Channel.Carrier<BootstrapKey, Bootstrap>();
                    var remoteEndPoint = ctx.Channel.Carrier<EndPointKey, IPEndPoint>();
                    Logger.Info(_clientChannels.TryRemove(ctx.Channel, out var outItem)
                                ? $"Remove Channel[{ctx.Channel.LocalEndPoint()}] Successful"
                                : $"No Remove Channel");

                    TimeUtils.Sleep(interval);

                    //if (!ctx.Channel.Carrier<DisconnectKey, bool>())
                    //{
                    //    this.Logger.Fatal("Connection Disconnected without Permission, retrying...");
                    //}
                    var freeLocalEndPoint = GetFreeLocalEndPoint();
                    if (freeLocalEndPoint == null) return;
                    Logger.Info($"ChannelUnRegistered GetFreeLocalEndPoint LocalEndPoint[{freeLocalEndPoint}]");
                    var channel = bootstrap.Connect(remoteEndPoint, freeLocalEndPoint).Sync().Channel();
                    if (channel.IsConnected() && !channel.IsClosed())
                    {
                        if (!_clientChannels.TryAdd(channel, channel.LocalEndPoint().ToString()))
                            Logger.Info($"Add Channel[{channel.LocalEndPoint()}] Failed");
                        Logger.Info("Connection");
                    }
                }
            }

            private static byte[] ReceiveMessageForIcash(byte[] icashMessageBytes, string localEndPoint, bool includeLength = true)
            {
                try
                {
                    byte[] requestIcashBytes;
                    if (includeLength)
                    {
                        requestIcashBytes = new byte[icashMessageBytes.Length - 2];
                        Buffer.BlockCopy(icashMessageBytes, 2, requestIcashBytes, 0, icashMessageBytes.Length - 2);
                    }
                    else
                    {
                        requestIcashBytes = new byte[icashMessageBytes.Length];
                        Buffer.BlockCopy(icashMessageBytes, 0, requestIcashBytes, 0, icashMessageBytes.Length);
                    }
                    var requestIcash = new ICASH(requestIcashBytes);
                    string txKeySeq = string.Empty;
                    if (Convert.ToUInt16(requestIcash.Mti()[2]) % 2 == 0)
                    {
                        Logger.Info($"Request Message --> Header[{requestIcash.Header().Encode()}], MTI [{requestIcash.Mti()}]");
                    }
                    else
                    {
                        Logger.Info($"Response Message --> Header[{requestIcash.Header().Encode()}], MTI [{requestIcash.Mti()}]");
                        ReservedKeyData keyData;
                        if (requestIcash.Mti().Substring(0, 2).Equals("08"))
                            keyData = RemoveReservedKeyDataForNetwork(requestIcash);
                        else
                            keyData = RemoveReservedKeyData(requestIcash);
                        if (keyData == null)
                            Logger.Info($"Get Original Request TxKeySeq({requestIcash.Val(FIELD.F_7)}-{requestIcash.Val(FIELD.F_11)}) Error");
                        else
                            txKeySeq = keyData.TxKeySeq;
                    }
                    Logger.Info($"Request Message --> Header[{requestIcash.Header().Encode()}], MTI [{requestIcash.Mti()}]");
                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in requestIcash.Core().OrderedFields())
                        {
                            var key = pair.Key;
                            var value = pair.Value;
                            if (value is byte[] bytes)
                            {
                                var tempString = HexStringConverter.FromByteArray(bytes);
                                Logger.Debug($"[FIELD {key}] VALUE [{tempString}]");
                            }
                            else
                            {
                                Logger.Debug($"[FIELD {key}] VALUE [{value}]");
                            }
                        }
                    }
                    var responseIcash = new ICASH(requestIcashBytes);
                    switch (requestIcash.Mti())
                    {
                        case "0302":
                            responseIcash.Header().Decode($"{requestIcash.Header().DestID()}{requestIcash.Header().SrcID()}");
                            responseIcash.Mti("0312");
                            responseIcash.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0800":
                            responseIcash.Header().Decode($"{requestIcash.Header().DestID()}{requestIcash.Header().SrcID()}");
                            responseIcash.Mti("0810");
                            responseIcash.Core().Field(FIELD.F_39, "00");
                            break;
                        case "0820":
                        case "0821":
                            responseIcash.Header().Decode($"{requestIcash.Header().DestID()}{requestIcash.Header().SrcID()}");
                            responseIcash.Mti("0830");
                            responseIcash.Core().Field(FIELD.F_39, "00");
                            break;
                        default:
                            Logger.Info($"Request Message");
                            return null;
                    }
                    Logger.Info($"Response Message --> Header[{responseIcash.Header().Encode()}], MTI [{responseIcash.Mti()}]");
                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in responseIcash.Core().OrderedFields())
                        {
                            var key = pair.Key;
                            var value = pair.Value;
                            if (value is byte[] bytes)
                            {
                                var tempString = HexStringConverter.FromByteArray(bytes);
                                Logger.Debug($"[FIELD {key}] VALUE [{tempString}]");
                            }
                            else
                            {
                                Logger.Debug($"[FIELD {key}] VALUE [{value}]");
                            }
                        }
                    }
                    var responseBytes = responseIcash.Encode().Val();
                    byte[] retnProcessBytes = ColaCommUtility.AddBinaryLength2BytesArray(responseBytes);
                    return retnProcessBytes;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{ex.Message}]");
                    return null;
                }
            }

            private static ReservedKeyData RemoveReservedKeyData(ICASH icash)
            {
                string key = $"{icash.Val(FIELD.F_7)}-{icash.Val(FIELD.F_11)}";
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    Logger.Info($"ClientICASH RemoveReservedKeyData Remove Key[{key}] Successful");
                    return (keyData);
                }
                else
                {
                    Logger.Info($"ClientICASH RemoveReservedKeyData Remove Key [{key}] Failed");
                    return null;
                }
            }
            private static ReservedKeyData RemoveReservedKeyDataForNetwork(ICASH icash)
            {
                string key = $"{icash.Val(FIELD.F_70)}-{icash.Val(FIELD.F_11)}";
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    Logger.Info($"ClientICASH RemoveReservedKeyDataNetwork Remove Key[{key}] Successful");
                    return (keyData);
                }
                else
                {
                    Logger.Info($"ClientICASH RemoveReservedKeyDataNetwork Remove Key [{key}] Failed");
                    return null;
                }
            }
            private static IPEndPoint GetFreeLocalEndPoint()
            {
                try
                {
                    Monitor.Enter(GetFreeLocalEndPointLocker);
                    var localEndPointList = new List<IPEndPoint>();
                    foreach (var endPoint in _localEndPoints)
                    {
                        var channel = _clientChannels.Keys.Where(x => (x.LocalEndPoint().ToString()!.Equals(endPoint.ToString()))).ToList();
                        if (channel.Count == 0)
                        {
                            localEndPointList.Add(endPoint);
                        }
                    }
                    Logger.Info($"LocalEndPointCount[{localEndPointList.Count}]");
                    localEndPointList.ForEach(endPoint => Logger.Info($"FreeLocalEndPoint[{endPoint.ToString()}]"));
                    if (localEndPointList.Count == 0)
                        return null;
                    //random select localEndPoint
                    var localEndPointNumber = ColaCommUtility.GetSecurityNumber(localEndPointList.Count);
                    return localEndPointList[localEndPointNumber];
                }
                catch (Exception ex)
                {
                    Logger.Warn($"GetFreeLocalEndPoint Exception:{ex.Message})");
                    return null;
                }
                finally
                {
                    Monitor.Exit(GetFreeLocalEndPointLocker);
                }
            }
            public class ProcessQueueDataHandler : ChannelOutboundHandler
            {
                public override void Write(ChannelHandlerContext ctx, object msg)
                {
                    var buf = (ByteBuf)msg;
                    base.Write(ctx, msg);
                }
            }
        }
    }
}

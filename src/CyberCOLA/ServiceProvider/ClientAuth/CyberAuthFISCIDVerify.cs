using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
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

namespace Com.Cybersoft4u.ServiceProvider.ClientAuth
{
    public class CyberAuthFISCIDVerify
    {
        private readonly string _localIp;
        private readonly string[] _localPorts;
        private readonly int _sessionsCount;
        private readonly bool _startFlag;
        private readonly bool _echoFlag;
        private static bool _signonFlag;
        private static string _serviceName;
        private static int _timeoutMilliSecond;
        private static IPEndPoint _targetEndPoint;
        private static int _retainMinutes = 10;

        private static Logger Logger = null;
        private static Bootstrap _bootstrap;
        private static ConcurrentDictionary<IChannel, string> _clientChannels = new ConcurrentDictionary<IChannel, string>();
        private static ConcurrentDictionary<string, ReservedKeyData> _reservedKeyDatas = new ConcurrentDictionary<string, ReservedKeyData>();
        private static readonly PendingResponses<FISC> _pendingResponses = new PendingResponses<FISC>();
        private static readonly object GetFreeLocalEndPointLocker = new object();
        private static List<IPEndPoint> _localEndPoints = null;
        public CyberAuthFISCIDVerify()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _serviceName = "CyberAuthFISCIDVerify";
                Logger = LoggerAdapter.GetLogger($"LoggerHandler[{_serviceName}]");
                var targetIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-target-ip").Trim(), AddressFamily.InterNetwork);
                var targetPort = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-target-port").Trim();
                _localIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-local-ip").Trim(), AddressFamily.InterNetwork);
                _localPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-local-ports").Split(',');
                _sessionsCount = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-sessions").Trim());
                _startFlag = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-auto-start-flag").Default("F").ToUpper().Equals("TRUE");
                _echoFlag = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-auto-echo-flag").Default("F").ToUpper().Equals("TRUE");
                _signonFlag = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-service-auto-signon-flag").Default("F").ToUpper().Equals("TRUE");
                _timeoutMilliSecond = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-timeout-milliseconds").Trim());
                _retainMinutes = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.IDVerifyClientConfigSectionName, "fisc-key-data-retain-minutes").Default("10"));
                Logger.Info($"Client {_serviceName} Authorization Service ServerIPAddress[{targetIp}],ClientIPAddress[{_localIp}],SessionCount[{_sessionsCount}]");
                if (_localPorts.Length == _sessionsCount)
                {
                }
                else
                {
                    throw new ArgumentException("configuration has error, please fix App.config");
                }

                _targetEndPoint = new IPEndPoint(IPAddress.Parse(targetIp), Convert.ToInt32(targetPort));

                _bootstrap = new Bootstrap();
                Logger.Info("----- Start Display ColaPasysInfo -----");
                Logger.Info($"\n{SingletonOfCommonProperty.Instance.DisplayColaPasysInfo()}");
                Logger.Info("----- End   Display ColaPasysInfo -----");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CyberAuthFISCIDVerify Constructor Exception : {ex.Message}");
                throw;
            }
        }
        public void Start()
        {
            try
            {
                _localEndPoints = new List<IPEndPoint>();
                _bootstrap.ChannelType(typeof(TcpSocketChannel))
                          .Handler(new FiscLoggerHandler($"{_serviceName}-Logger", LogLevel.Debug))
                          .Handler(new ClientInitializer())
                          .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true);
                //   .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true);
                for (int index = 0; index < _sessionsCount; index++)
                {
                    var localPort = Convert.ToInt32(_localPorts[index].Trim());
                    //ThreadPool.QueueUserWorkItem(
                    //                 state =>
                    //                 {
                    //                         var localEndPoint = new IPEndPoint(IPAddress.Parse(_localIp), localPort);
                    //                         var channel = _bootstrap.SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                    //                                                 .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                    //                                                 .Connect(_targetEndPoint, localEndPoint).Sync().Channel();
                    //                         _localEndPoints.Add(localEndPoint);
                    //                         //var channel = bootstrap.Connect(remoteEndPoint).Sync().Channel();
                    //                         if (channel.IsConnected() && !channel.IsClosed())
                    //                         {
                    //                             if (!_clientChannels.TryAdd(channel, channel.LocalEndPoint().ToString()))
                    //                                 Logger.Info($"Add Channel[{channel.LocalEndPoint().ToString()}] Failed");
                    //                         }
                    //                 });
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(_localIp), localPort);
                    _localEndPoints.Add(localEndPoint);
                    Task.Factory.StartNew(() =>
                    {
                        var channel = _bootstrap.SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                                                .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                                                .Connect(_targetEndPoint, localEndPoint).Sync().Channel();
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
                Logger.Fatal($"Start CyberAuthFISCIDVerify Exception : {ex.Message}");
                throw;
            }
        }
        public void Stop()
        {
            foreach (var chanel in _clientChannels)
            {
                try
                {
                    chanel.Key.Close();
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
            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var requestFiscMessage = FISCTransCreator.FiscSignOnRequest();
            if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(FIELD.F_48)))
            {
                requestFiscMessage.Core().Field(FIELD.F_48, requestFiscMessage.Core().Field(FIELD.F_48).ToString().Remove(10, 1).Insert(10, "N"));
            }
            var dataBytes = requestFiscMessage.Encode().Val();
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
        public void SignOn(InterchangeMessage interchangeMessage)
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

            var requestFiscMessage = FISCTransCreator.FiscSignOnRequest();
            if (!ObjectUtils.IsNull(requestFiscMessage.RawVal(FIELD.F_48)))
            {
                requestFiscMessage.Core().Field(FIELD.F_48, requestFiscMessage.Core().Field(FIELD.F_48).ToString().Remove(10, 1).Insert(10, "N"));
            }
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }
            if (!AddReservedKeyDataForNetwork(requestFiscMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthFISCIDVerify SignOn AddReservedKeyDataForNetwork Error");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var isoBytes = requestFiscMessage.Encode().Val();
            var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(isoBytes);
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
            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = FISCTransCreator.FiscSignOffRequest().Encode().Val();
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
        public void SignOff(InterchangeMessage interchangeMessage)
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
            var requestFiscMessage = FISCTransCreator.FiscSignOffRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }
            if (!AddReservedKeyDataForNetwork(requestFiscMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthFISCIDVerify SignOff AddReservedKeyDataForNetwork Error");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestFiscMessage.Encode().Val();
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
                Logger.Error("Create Message0800 Of EchoTest No Channel");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = FISCTransCreator.FiscEchoTestRequest().Encode().Val();
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
        public void EchoTest(InterchangeMessage interchangeMessage)
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("EchoTest No Client Channel Use");
                return;
            }
            var channel = _clientChannels.Keys.ToArray()[0];
            if (channel == null)
            {
                Logger.Error("Create Message0800 Of EchoTest No Channel");
                return;
            }
            var requestFiscMessage = FISCTransCreator.FiscEchoTestRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestFiscMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }
            if (!AddReservedKeyDataForNetwork(requestFiscMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthFISCIDVerify EchoTest AddReservedKeyDataForNetwork Error");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestFiscMessage.Encode().Val();
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
        public static int SendMessageToTarget(FISC pushMessage, string txKeySeq)
        {
            try
            {
                if (_clientChannels.Keys.ToArray().Length == 0)
                {
                    Logger.Error($"SendMessageToTarget No Client Channel Use, Please Check Connection({_serviceName})");
                    return -1;
                }
                //random select channel
                int clientChannels = _clientChannels.Keys.ToArray().Length;
                int channelNumber = ColaCommUtility.GetSecurityNumber(clientChannels);
                Logger.Info($"ClientChannel[{clientChannels}], Send Channel Number [{channelNumber}]");
                var channel = _clientChannels.Keys.ToArray()[channelNumber];
                if (channel == null)
                {
                    Logger.Error($"Send Message To Server No Channel, Channel Count[{_clientChannels.Count}], Channel Number[{channelNumber}]");
                    return -2;
                }
                Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, Start Process CyberAuthFISCIDVerify SendMessageToTarget at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var dataBytes = pushMessage.Encode().Val();
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
                AddReservedKeyData(pushMessage, txKeySeq);
                Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, End Process CyberAuthFISCIDVerify SendMessageToTarget at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Call {new StackTrace().GetFrame(0).GetMethod().Name} Exception : [{ex.Message}]");
                return -9;
            }
        }
        public bool IsHaveChannels()
        {
            return _clientChannels.Keys.ToArray().Length > 0;
        }

        public string GetConnectionStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("LocalEndPoint --> RemoteEndPoint");
            foreach (var channel in _clientChannels.Keys.ToArray())
            {
                sb.AppendLine($"[{channel.LocalEndPoint()}] --> [{channel.RemoteEndPoint()}]");
            }
            Logger.Debug($"{GetType().Name} GetConnectionStatus[{sb.ToString()}]");
            return (sb.ToString());
        }
        public string GetColaPasysInfo()
        {
            return (SingletonOfCommonProperty.Instance.DisplayColaPasysInfo());
        }
        public string DeleteReservedKeyData()
        {
            Logger.Info($"CyberAuthFISCIDVerify, Retain Minutes[{_retainMinutes}] DeleteReservedKeyData Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var keysLists = _reservedKeyDatas.Where(x => DateTime.Now.Subtract(x.Value.CreateDateTime).TotalMinutes > (double)_retainMinutes)
                                                       .Select(x => x.Key).ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CyberAuthFISCIDVerify DeleteReservedKeyData");
            foreach (var key in keysLists)
            {
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    sb.AppendLine($"CyberAuthFISCIDVerify RemoveReservedKeyData Remove Key[{key}] Successful");
                }
                else
                {
                    sb.AppendLine($"CyberAuthFISCIDVerify RemoveReservedKeyData Remove Key [{key}] Failed");
                }
            }
            Logger.Info(sb.ToString());
            return sb.ToString();
        }
        public string ListReservedKeyData()
        {
            Logger.Info($"CyberAuthFISCIDVerify ListReservedKeyData Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var dataLists = _reservedKeyDatas.OrderBy(x => x.Value.CreateDateTime)
                                                                   .Select(x => new { x.Key, x.Value.CreateDateTime }).ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CyberAuthFISCIDVerify ListReservedKeyData");
            foreach (var data in dataLists)
            {
                sb.AppendLine($"Key[{data.Key}] --> CreateDateTime[{data.CreateDateTime:yyyy-MM-dd HH:mm:ss:fffff}]");
            }
            return (sb.ToString());
        }
        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <returns>List IChannel.</returns>
        public List<IChannel> GetChannels()
        {
            return (_clientChannels?.Keys.ToList<IChannel>());
        }
        public static bool AddReservedResponseData(FISC fisc, string txKeySeq)
        {
            Logger.Info($"CyberAuthFISCIDVerify AddReservedResponseData Add Key Data Mti[{fisc.Mti()}, Key[{txKeySeq}]");
            if (_pendingResponses.TryComplete(txKeySeq, fisc))
            {
                return true;
            }

            // Stored before, but nothing ever removed a response whose waiter had already timed out.
            Logger.Warn($"CyberAuthFISCIDVerify AddReservedResponseData No Waiter For Key[{txKeySeq}] (timed out or duplicate), Response Dropped");
            return false;
        }
        private static bool AddReservedKeyData(FISC fisc, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = string.Format(SystemConstantData.CommonData.GatewayQRCodeIDVerifyRelationIdStringFormat,
                                       $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                       $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                       $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                       );
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"CyberAuthFISCIDVerify AddReservedKeyData Add Key Data Mti[{fisc.Mti()}, Key[{key}]");
            return (_reservedKeyDatas.TryAdd(key, keyData));
        }
        private bool AddReservedKeyDataForNetwork(FISC fisc, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{fisc.Val(FIELD.F_70)}-{fisc.Val(FIELD.F_11)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"CyberAuthFISCIDVerify AddReservedKeyDataForNetwork Add Key Data Mti[{fisc.Mti()}, Key[{key}]");
            return (_reservedKeyDatas.TryAdd(key, keyData));
        }
        private static ReservedKeyData RemoveReservedKeyData(FISC fisc)
        {
            string key = string.Format(SystemConstantData.CommonData.GatewayQRCodeIDVerifyRelationIdStringFormat,
                                       $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                       $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                       $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                       );
            var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
            if (findKey)
            {
                Logger.Info($"{_serviceName} RemoveReservedKeyData Remove Key[{key}] Successful");
                return (keyData);
            }
            else
            {
                Logger.Info($"{_serviceName} RemoveReservedResponseData Remove Key [{key}] Failed");
                return null;
            }
        }
        private static ReservedKeyData RemoveReservedKeyDataForNetwork(FISC fisc)
        {
            string key = $"{fisc.Val(FIELD.F_70)}-{fisc.Val(FIELD.F_11)}";
            var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
            if (findKey)
            {
                Logger.Info($"{_serviceName} RemoveReservedKeyDataForNetwork Remove Key[{key}] Successful");
                return (keyData);
            }
            else
            {
                Logger.Info($"{_serviceName} RemoveReservedKeyDataForNetwork Remove Key [{key}] Failed");
                return null;
            }
        }
        public FISC ProcessIDVerifyMessage(FISC requestMessage, string localEndPoint, string txKeySeq)
        {
            try
            {
                //Add Send Transaction Log
                int spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                                SystemConstantData.CommonData.MessageSendDirection,
                                                                localEndPoint,
                                                                requestMessage);
                if (spRetnCode != 0)
                {
                    Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                }

                // Registered before sending so a response arriving before the wait starts is still delivered.
                if (!_pendingResponses.Register(txKeySeq))
                {
                    Logger.Error($"{_serviceName} ProcessIDVerifyMessage Key[{txKeySeq}] Already Waiting For A Response");
                    return null;
                }

                int returnCode;
                try
                {
                    returnCode = SendMessageToTarget(requestMessage, txKeySeq);
                }
                catch
                {
                    _pendingResponses.Cancel(txKeySeq);
                    throw;
                }

                if (returnCode != 0)
                {
                    _pendingResponses.Cancel(txKeySeq);
                    return null;
                }

                // Woken as soon as the response handler delivers it, instead of polling with Thread.Sleep,
                // which added up to one sleep interval to every response.
                FISC responseMessage = _pendingResponses.Wait(txKeySeq, _timeoutMilliSecond);
                if (Logger.IsDebugEnabled)
                {
                    if (responseMessage != null)
                    {
                        Logger.Debug($"Key Value   : [{txKeySeq}]");
                        Logger.Debug($"Value       : MTI{responseMessage.Mti()}, F7[{responseMessage.Val(FIELD.F_7)}], F11[{responseMessage.Val(FIELD.F_11)}]");
                        //Add Receive Transaction Log
                        spRetnCode = StoreProcUtility.InsertTranLog(txKeySeq,
                                                                    SystemConstantData.CommonData.MessageReceiveDirection,
                                                                    localEndPoint,
                                                                    responseMessage);
                        if (spRetnCode != 0)
                        {
                            Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                        }
                    }
                    else
                    {
                        Logger.Debug($"Key Value   : [{txKeySeq}] Not Found, Exceed The Time limit[{_timeoutMilliSecond}]MilliSecond");
                    }
                }
                return responseMessage;
            }
            catch (Exception ex)
            {
                Logger.Error($"Call {new StackTrace().GetFrame(0).GetMethod().Name} Exception : [{ex.Message}]");
                return null;
            }
        }
        public class ClientInitializer : ChannelInitializer
        {
            private static Random rnd = new Random();
            public override void InitChannel(IChannel channel)
            {

                channel.Carrier<EndPointKey>(_targetEndPoint);
                channel.Carrier<DisconnectKey>(false);
                channel.Carrier<BootstrapKey>(_bootstrap);
                channel.Carrier<AutoSignOnKey>(_signonFlag);

                channel.Pipe()
                    .AddLast(new LoggerHandler(_serviceName, LogLevel.Debug))
                    // Split the stream into whole messages before InboundMultiWorkerHandler (see LengthPrefixedFrameHandler).
                    .AddLast(new LengthPrefixedFrameHandler())
                    .AddLast(new IDVerifyAutoSignOnHandler())
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
                    //Logger.Info($"{ctx.Channel.LocalEndPoint()} Receive Data[{Encoding.Default.GetString(buf.Val())}]");
                    Logger.Info($"{ctx.Channel.LocalEndPoint()} Receive Data Hex String[{BitConverter.ToString(buf.Val()).Replace("-", "")}]");
                }
            }
            public class InboundHandler : ChannelInboundHandler
            {
                public override void ChannelRead(ChannelHandlerContext ctx, object msg)
                {
                    Logger.Debug($"Go Hander: {GetType().FullName}");
                    base.ChannelRead(ctx, msg);
                    var receiveByteBuf = (ByteBuf)msg;
                    //byte[] receiveBytes = new byte[receiveByteBuf.Val().Length - 2];
                    //Buffer.BlockCopy(receiveByteBuf.Val(), 2, receiveBytes, 0, receiveByteBuf.Val().Length - 2);
                    //var tempString = HexStringConverter.FromByteArray(receiveByteBuf.Val());
                    //if (Logger.IsDebugEnabled)
                    //{
                    //    Logger.Debug($"Receive Data Hex String [{tempString}]");
                    //    Logger.Debug($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                    //}
                    //Logger.Info($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                    //Logger.Info($"Receive Data Hex String(No Data Length)[{BitConverter.ToString(receiveBytes).Replace("-", "")}]");
                    var localEndPoint = ctx.Channel.LocalEndPoint().ToString();
                    var remoteEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                    Logger.Info($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Read Raw Data Length: {receiveByteBuf.Val().Length}B");
                    // The hex dump is Debug only: at Info it was most of the log volume under load.
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Length: {receiveByteBuf.Val().Length}B\n{PrintBytesMap(receiveByteBuf.Val())}");
                    }

                    var sendBuffer = ProcessIDVerifyAuth(receiveByteBuf.Val(), ctx.Channel.LocalEndPoint().ToString());
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
                    ThreadPool.QueueUserWorkItem(
                        state =>
                            {
                                try
                                {
                                    base.ChannelRead(ctx, msg);
                                }
                                catch (Exception exception)
                                {
                                    Logger.Fatal(exception);
                                }
                            });
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

            public class IDVerifyAutoSignOnHandler : ChannelInboundHandler
            {
                public override void ChannelActived(ChannelHandlerContext ctx)
                {
                    Logger.Info($"IDVerifyAutoSignOnHandler ChannelActived {ctx.Channel.Carrier<AutoSignOnKey, bool>()}");
                    if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
                    {
                        this.Logger.Info($"IDVerifyAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to {_serviceName}, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                        if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                        {
                            var fisc = FISCTransCreator.FiscSignOnRequest();
                            if (!ObjectUtils.IsNull(fisc.RawVal(FIELD.F_48)))
                            {
                                fisc.Core().Field(FIELD.F_48, fisc.Core().Field(FIELD.F_48).ToString().Remove(10, 1).Insert(10, "N"));
                            }
                            var isoBytes = fisc.Encode().Val();
                            var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(isoBytes);
                            ctx.Channel.WriteAndFlush(sendBytes);
                        }
                        else
                            this.Logger.Info($"IDVerifyAutoSignOnHandler  ChannelActived(No Connected), LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    }
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

            private static byte[] ProcessIDVerifyAuth(byte[] fiscMessageBytes, string localEndPoint, bool includeLength = true)
            {
                try
                {
                    byte[] fiscBytes;
                    if (includeLength)
                    {
                        fiscBytes = new byte[fiscMessageBytes.Length - 2];
                        Buffer.BlockCopy(fiscMessageBytes, 2, fiscBytes, 0, fiscMessageBytes.Length - 2);
                    }
                    else
                    {
                        fiscBytes = new byte[fiscMessageBytes.Length];
                        Buffer.BlockCopy(fiscMessageBytes, 0, fiscBytes, 0, fiscMessageBytes.Length);
                    }
                    var fiscMessage = new FISC(fiscBytes);
                    string txKeySeq = string.Empty;
                    ReservedKeyData keyData = null;
                    if (Convert.ToUInt16(fiscMessage.Mti()[2]) % 2 == 0)
                    {
                        Logger.Info($"Request Message --> MTI [{fiscMessage.Mti()}]");
                    }
                    else
                    {
                        Logger.Info($"Response Message --> MTI [{fiscMessage.Mti()}]");
                        if (fiscMessage.Mti().Substring(0, 2).Equals("08"))
                            keyData = RemoveReservedKeyDataForNetwork(fiscMessage);
                        else
                            keyData = RemoveReservedKeyData(fiscMessage);
                        if (keyData == null)
                        {
                            var key = string.Format(SystemConstantData.CommonData.GatewayQRCodeIDVerifyRelationIdStringFormat,
                                                    $"{fiscMessage.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                    $"{fiscMessage.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                    $"{fiscMessage.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                    );
                            Logger.Info($"Get Original Request TxKeySeq({key}) Error");
                            return null;
                        }
                        txKeySeq = keyData.TxKeySeq;
                    }

                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in fiscMessage.Core().OrderedFields())
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
                    //Check KeyData Create Time
                    byte[] respBytes = null;
                    //this transaction type BIC
                    switch (fiscMessage.Mti())
                    {
                        case "0110":
                            //Check KeyData Create Time
                            if (keyData != null)
                            {
                                var timeDiff = DateTime.Now.Subtract(keyData.CreateDateTime).TotalMilliseconds;
                                Logger.Info($"Get the Response Message Time Interval[{timeDiff}]Milliseconds");
                                if (timeDiff > (_timeoutMilliSecond + 500))
                                {
                                    Logger.Info($"Response Message Exceed Time Limit[{timeDiff}]Milliseconds");
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                            AddReservedResponseData(fiscMessage, txKeySeq);
                            break;
                        case "0800":
                        case "0820":
                        case "0821":
                            respBytes = FiscAuthFlowUtility.ProcessFISC0800(fiscMessage, localEndPoint);
                            break;
                        case "0810":
                            FiscAuthFlowUtility.ProcessFISC0810(fiscMessage, localEndPoint);
                            respBytes = null;
                            break;
                        default:
                            Logger.Debug($"Receive Invalid Transaction Data, MTI[{fiscMessage.Mti()}], PCode[{fiscMessage.Val(FIELD.F_3)}], F7[{fiscMessage.Val(FIELD.F_7)}], F11[{fiscMessage.Val(FIELD.F_11)}");
                            break;
                    }
                    return respBytes == null ? null : ColaCommUtility.ContactBytesArray(ColaCommUtility.IntegerToBinary(respBytes.Length), respBytes);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Call {new StackTrace().GetFrame(0).GetMethod().Name} Exception : [{ex.Message}]");
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
                        var channel = _clientChannels.Keys.Where(x => (x.LocalEndPoint().ToString().Equals(endPoint.ToString()))).ToList();
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
            private static string PrintBytesMap(byte[] bytes)
            {
                var dataOut = new StringBuilder();
                dataOut.Append(
                    "         +-------------------------------------------------+\n         |  0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f |\n+--------+-------------------------------------------------+----------------+");
                var length = bytes.Length;
                var index = 0;
                var byteContent = string.Empty;
                var dataContent = string.Empty;
                foreach (var b in bytes)
                {
                    if (index % 16 == 0)
                    {
                        dataOut.Append($"\n|{(index / 16).ToString().PadLeft(8, '0')}| ");
                        byteContent = string.Empty;
                        dataContent = string.Empty;
                    }

                    //byteContent += $"{b:X2} ";
                    //dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    Encoding ascii = Encoding.ASCII;
                    Encoding ebcdic = Encoding.GetEncoding("IBM037");
                    byte[] bytesArray = { b };
                    byte[] convertedByte = Encoding.Convert(ebcdic, ascii, bytesArray);
                    byteContent += $"{b:X2} ";
                    if (index <= 1)
                    {
                        dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);
                    }
                    else
                    {
                        dataContent += convertedByte[0] < 32 || convertedByte[0] == 94 || convertedByte[0] > 126 ? '.' : Convert.ToChar(convertedByte[0]);
                    }

                    if (index % 16 == 15 | index + 1 == length)
                    {
                        dataOut.AppendFormat("{0}|{1}|", byteContent.PadRight(48, ' '), dataContent.PadRight(16, ' '));
                    }

                    index++;
                }

                dataOut.Append("\n+--------+-------------------------------------------------+----------------+");
                return dataOut.ToString();
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

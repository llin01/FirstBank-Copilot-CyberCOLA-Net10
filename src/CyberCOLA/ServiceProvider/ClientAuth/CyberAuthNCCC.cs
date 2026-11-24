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
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.MCOLA;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.ClientAuth
{
    public class CyberAuthNCCC
    {
        private static string _clientIPAddr;
        private static int _localPortStart;
        private static int _sessionCount;
        private static bool _isReturn;
        private static string _formatType;
        private static int _retainMinutes = 10;
        private static bool _isReConnection = true;
        private static readonly Logger Logger = LoggerAdapter.GetLogger("LoggerHandler[CyberAuthNCCC]");
        private static Bootstrap _bootstrap;
        private static IPEndPoint _remoteEndPoint;
        private static ConcurrentDictionary<IChannel, string> _clientChannels = new ConcurrentDictionary<IChannel, string>();
        private static ConcurrentDictionary<string, ReservedKeyData> _reservedKeyDatas = new ConcurrentDictionary<string, ReservedKeyData>();
        private static readonly object GetFreeLocalEndPointLocker = new object();
        private static List<IPEndPoint> _localEndPoints = null;
        private static readonly object LockObject = new object();
        // P0 Fix: Reconnection attempt counter per channel (channel -> reconnect count)
        private static readonly ConcurrentDictionary<IChannel, int> _channelReconnectAttempts = new ConcurrentDictionary<IChannel, int>();
        // P0 Fix: Inbound message processing concurrency limiter (max 100 concurrent tasks)
        private static readonly SemaphoreSlim _inboundSemaphore = new SemaphoreSlim(100, 100);
        public CyberAuthNCCC()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();

                var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-target-ip").Trim(), AddressFamily.InterNetwork);
                var portNumber = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-target-port").Trim());
                var localHostName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-local-ip").Trim();
                _clientIPAddr = CommonUtils.GetIPAddressStringByHostName(localHostName, AddressFamily.InterNetwork);
                _localPortStart = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-local-port-start").Trim());
                _sessionCount = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-sessions").Trim());
                _isReturn = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-return-flag").Default("F").ToUpper().Equals("TRUE");
                _formatType = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-format-type").Trim().ToUpper();
                _retainMinutes = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-key-data-retain-minutes").Default("10"));
                if (!_formatType.ToUpper().Equals("NCCC"))
                {
                    Logger.Fatal("This Authorization Only Process NCCC Format");
                    throw new Exception("Accept Format Type Error");
                }
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), portNumber);
                Logger.Info($"CyberAuthNCCC Client Authorization Service ServerIPAddress[{serverIPAddr}],ClientIPAddress[{_clientIPAddr}],Port Number[{portNumber}],SessionCount[{_sessionCount}]");

                _bootstrap = new Bootstrap();
                Logger.Info("----- Start Display ColaPasysInfo -----");
                Logger.Info($"\n{SingletonOfCommonProperty.Instance.DisplayColaPasysInfo()}");
                Logger.Info("----- End   Display ColaPasysInfo -----");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CyberAuthNCCC Constructor Exception : {ex.Message}");
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
                          .Handler(new LoggerHandler("CyberAuthNCCC", LogLevel.Debug))
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
                    var task = Task.Factory.StartNew(() =>
                    {
                        var channel = _bootstrap.SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                                                .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true)
                                                .Connect(_remoteEndPoint, localEndPoint).Sync().Channel();
                        if (channel.IsConnected() && !channel.IsClosed())
                        {
                            if (!_clientChannels.TryAdd(channel, channel.LocalEndPoint().ToString()))
                                Logger.Info($"Add Channel[{channel.LocalEndPoint().ToString()}] Failed");
                        }
                    }, TaskCreationOptions.LongRunning);
                    Logger.Info($"Process[{System.AppDomain.CurrentDomain.FriendlyName}], ManagedThreadId[{Thread.CurrentThread.ManagedThreadId}], Task.CurrentId[{Task.CurrentId}], TaskScheduler.Current.Id[{TaskScheduler.Current.Id}], Task Id[{task.Id}], LocalEndpoint[{localEndPoint}], RemoteEndPoint[{_remoteEndPoint}]");

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Start CyberAuthNCCC Exception : {ex.Message}");
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
            var requestNcccMessage = NcccTransCreator.SignOnRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestNcccMessage, tranLogKey))
            {
                Logger.Fatal("CyberAuthNCCC SignOn AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestNcccMessage.Encode().Val();
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
            var requestNcccMessage = NcccTransCreator.SignOnRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestNcccMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthNCCC SignOn AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestNcccMessage.Encode().Val();
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
            var requestNcccMessage = NcccTransCreator.SignOffRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestNcccMessage, tranLogKey))
            {
                Logger.Fatal("CyberAuthNCCC SignOff AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestNcccMessage.Encode().Val();
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
            var requestNcccMessage = NcccTransCreator.SignOffRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestNcccMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthNCCC SignOff AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestNcccMessage.Encode().Val();
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
        public void ChangeKey()
        {
            try
            {
                if (_clientChannels.Keys.ToArray().Length == 0)
                {
                    Logger.Error("ChangeKey No Client Channel Use");
                    return;
                }
                var channel = _clientChannels.Keys.ToArray()[0];
                if (channel == null)
                {
                    Logger.Error("Create Message0800 Of ChangeKey No Channel");
                    return;
                }
                var requestNcccMessage = new NcccMessage0800(NcccTransactionConst.NetworkManagementInformationCode.ChangeKey).GetNcccMessage();
                if (requestNcccMessage == null)
                {
                    Logger.Info("ChangeKey Create Message0800 Error");
                    return;
                }
                string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                                SystemConstantData.CommonData.MessageSendDirection,
                                                                channel.LocalEndPoint().ToString(),
                                                                requestNcccMessage);
                if (spRetnCode != 0)
                {
                    Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                    return;
                }
                if (!AddReservedKeyDataForNetwork(requestNcccMessage, tranLogKey))
                {
                    Logger.Fatal("CyberAuthNCCC ChangeKey AddReservedKeyDataForNetwork Error");
                    return;
                }
                Logger.Info($"{channel.LocalEndPoint()} Start Process ChangeKey at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var dataBytes = requestNcccMessage.Encode().Val();
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
                Logger.Info($"{channel.LocalEndPoint()} End Process ChangeKey at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process ChangeKey Exception : [{ex.Message}]");
            }
        }
        public void RepeatKey()
        {
            try
            {
                if (_clientChannels.Keys.ToArray().Length == 0)
                {
                    Logger.Error("RepeatKey No Client Channel Use");
                    return;
                }
                var channel = _clientChannels.Keys.ToArray()[0];
                if (channel == null)
                {
                    Logger.Error("Create Message0800 Of RepeatKey No Channel");
                    return;
                }
                var requestNcccMessage = new NcccMessage0800(NcccTransactionConst.NetworkManagementInformationCode.RepeatKey).GetNcccMessage();
                if (requestNcccMessage == null)
                {
                    Logger.Info("RepeatKey Create Message0800 Error");
                    return;
                }
                string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                                SystemConstantData.CommonData.MessageSendDirection,
                                                                channel.LocalEndPoint().ToString(),
                                                                requestNcccMessage);
                if (spRetnCode != 0)
                {
                    Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                    return;
                }
                if (!AddReservedKeyDataForNetwork(requestNcccMessage, tranLogKey))
                {
                    Logger.Fatal("CyberAuthNCCC RepeatKey AddReservedKeyDataForNetwork Error");
                    return;
                }
                Logger.Info($"{channel.LocalEndPoint()} Start Process RepeatKey at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var dataBytes = requestNcccMessage.Encode().Val();
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
                Logger.Info($"{channel.LocalEndPoint()} End Process RepeatKey at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process RepeatKey Exception : [{ex.Message}]");
            }
        }
        public void VerifyKey()
        {
            try
            {
                if (_clientChannels.Keys.ToArray().Length == 0)
                {
                    Logger.Error("VerifyKey No Client Channel Use");
                    return;
                }
                var channel = _clientChannels.Keys.ToArray()[0];
                if (channel == null)
                {
                    Logger.Error("Create Message0800 Of VerifyKey No Channel");
                    return;
                }
                var requestNcccMessage = new NcccMessage0800(NcccTransactionConst.NetworkManagementInformationCode.VerifyKey).GetNcccMessage();
                if (requestNcccMessage == null)
                {
                    Logger.Info("VerifyKey Create Message0800 Error");
                    return;
                }
                string tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                                SystemConstantData.CommonData.MessageSendDirection,
                                                                channel.LocalEndPoint().ToString(),
                                                                requestNcccMessage);
                if (spRetnCode != 0)
                {
                    Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                    return;
                }
                if (!AddReservedKeyDataForNetwork(requestNcccMessage, tranLogKey))
                {
                    Logger.Fatal("CyberAuthNCCC VerifyKey AddReservedKeyDataForNetwork Error");
                    return;
                }
                Logger.Info($"{channel.LocalEndPoint()} Start Process VerifyKey at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var dataBytes = requestNcccMessage.Encode().Val();
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
                Logger.Info($"{channel.LocalEndPoint()} End Process VerifyKey at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process VerifyKey Exception : [{ex.Message}]");
            }
        }
        public void FileMaint(FileMaintData fileMaintData)
        {
            if (fileMaintData == null)
            {
                Logger.Info("File Maintenance Data Null");
                return;
            }
            try
            {
                if (_clientChannels.Keys.ToArray().Length == 0)
                {
                    Logger.Info("File Maintenance No Client Channel Use");
                    return;
                }
                //random select channel
                int clientChannels = _clientChannels.Keys.ToArray().Length;
                int channelNumber = ColaCommUtility.GetSecurityNumber(clientChannels);
                Logger.Info($"ClientChannel[{clientChannels}], Send Channel Number [{channelNumber}]");
                var channel = _clientChannels.Keys.ToArray()[channelNumber];
                if (channel == null)
                {
                    Logger.Info("File Maintenance Create NcccMessage0300 no Channel");
                    return;
                }
                Logger.Info($"File Maintenance Use Channel[{channel.LocalEndPoint()}]");
                var requestNcccMessage = new NcccMessage0300(fileMaintData).GetNcccMessage();
                if (requestNcccMessage == null)
                {
                    Logger.Info("File Maintenance Create NcccMessage0300 Error");
                    return;
                }

                int spRetnCode = StoreProcUtility.InsertTranLog(fileMaintData.TxKeySeq,
                                                                SystemConstantData.CommonData.MessageSendDirection,
                                                                channel.LocalEndPoint().ToString(),
                                                                requestNcccMessage);
                if (spRetnCode != 0)
                {
                    Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                    return;
                }
                if (!AddReservedKeyData(requestNcccMessage, fileMaintData.TxKeySeq))
                {
                    Logger.Fatal("CyberAuthNCCC FileMaint AddReservedKeyData Error");
                    return;
                }
                Logger.Info($"{channel.LocalEndPoint()} Start Process File Maintenance at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var dataBytes = requestNcccMessage.Encode().Val();
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
                Logger.Info($"{channel.LocalEndPoint()} End Process File Maintenance at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process FileMaint Exception : [{ex.Message}]");
            }
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

            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            var requestNcccMessage = NcccTransCreator.EchoTestRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestNcccMessage.Encode().Val();
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
            var requestNcccMessage = NcccTransCreator.EchoTestRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestNcccMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }
            if (!AddReservedKeyDataForNetwork(requestNcccMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthNCCC EchoTest AddReservedKeyDataForNetwork Error");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestNcccMessage.Encode().Val();
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
        public void SendMessageToGateway(NcccIso8583 ncccMessage)
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("CyberAuthNCCC SendMessageToGateway No Client Channel Use");
                return;
            }
            var channelNumber = ColaCommUtility.GetSecurityNumber(_clientChannels.Count);
            Logger.Info($"ClientChannel[{_clientChannels.Count}], Send Channel Number [{channelNumber}]");
            var channel = _clientChannels.Keys.ToArray()[channelNumber];
            if (channel == null)
            {
                Logger.Error("Send Message To NcccColaService No Channel");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, Start Process CyberAuthNCCC SendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = ncccMessage.Encode().Val();
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
            Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, End Process CyberAuthNCCC SendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
        }
        //this method for test
        public void AllChannelSendMessageToGateway(NcccIso8583 ncccMessage)
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("CyberAuthNCCC AllChannelSendMessageToGateway No Client Channel Use");
                return;
            }
            Logger.Info($"Start Process CyberAuthNCCC AllChannelSendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var channels = _clientChannels.Keys.ToArray();
            foreach (var c1 in channels)
            {
                if (c1 == null)
                {
                    Logger.Error("Send Message To NcccColaService Channel is null");
                    continue;
                }
                Logger.Info($"{c1.LocalEndPoint()} => {c1.RemoteEndPoint()}, Start Process CyberAuthNCCC AllChannelSendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var newNcccMessage = new NcccIso8583(ncccMessage.Encode().Val());
                //update field data
                var fieldName = FIELD.F_7;
                if (newNcccMessage.Core().Field(fieldName) != null)
                {
                    newNcccMessage.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");
                }
                var stan = SerialGenerator.Stan();
                fieldName = FIELD.F_11;
                if (newNcccMessage.Core().Field(fieldName) != null)
                {
                    newNcccMessage.Core().Field(fieldName, stan);
                }
                fieldName = FIELD.F_37;
                if (newNcccMessage.Core().Field(fieldName) != null)
                {
                    newNcccMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}{stan}");
                }
                var dataBytes = newNcccMessage.Encode().Val();
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
                Logger.Info($"{c1.LocalEndPoint()} => {c1.RemoteEndPoint()}, End Process CyberAuthNCCC AllChannelSendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
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
            sb.Append($"{Environment.NewLine}{"LocalEndPoint".PadLeft(27, ' ')} --> RemoteEndPoint{Environment.NewLine}");
            foreach (var channel in _clientChannels.Keys.ToArray())
            {
                var tempString = $"[{channel?.LocalEndPoint()}]".PadLeft(27, ' ');
                sb.Append($"{tempString} --> [{channel?.RemoteEndPoint()}]" + Environment.NewLine);
            }
            return sb.ToString();
        }
        public string GetColaPasysInfo()
        {
            return SingletonOfCommonProperty.Instance.DisplayColaPasysInfo();
        }
        public string DeleteReservedKeyData()
        {
            Logger.Info($"CyberAuthNCCC, Retain Minutes[{_retainMinutes}] DeleteReservedKeyData Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var keysLists = _reservedKeyDatas.Where(x => DateTime.Now.Subtract(x.Value.CreateDateTime).TotalMinutes > (double)_retainMinutes)
                                                       .Select(x => x.Key).ToList();
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}CyberAuthNCCC DeleteReservedKeyData{Environment.NewLine}");
            foreach (var key in keysLists)
            {
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    sb.Append($"CyberAuthNCCC RemoveReservedKeyData Remove Key[{key}] Successful{Environment.NewLine}");
                }
                else
                {
                    sb.Append($"CyberAuthNCCC RemoveReservedKeyData Remove Key [{key}] Failed{Environment.NewLine}");
                }
            }
            Logger.Info($"DeleteReservedKeyData Info : [{sb.ToString()}]");
            return sb.ToString();
        }
        public string ListReservedKeyData()
        {
            Logger.Info($"CyberAuthNCCC ListReservedKeyData Start : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var dataLists = _reservedKeyDatas.OrderBy(x => x.Value.CreateDateTime)
                                                                   .Select(x => new { x.Key, x.Value.CreateDateTime }).ToList();

            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}CyberAuthNCCC ListReservedKeyData{Environment.NewLine}");
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
        private bool AddReservedKeyData(NcccIso8583 nccc, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{nccc.Val(FIELD.F_7)}-{nccc.Val(FIELD.F_11)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"CyberAuthNCCC AddReservedKeyData Add Key Data Mti[{nccc.Mti()}, Key[{key}]");
            return (_reservedKeyDatas.TryAdd(key, keyData));
        }
        private bool AddReservedKeyDataForNetwork(NcccIso8583 nccc, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{nccc.Val(FIELD.F_70)}-{nccc.Val(FIELD.F_11)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"CyberAuthNCCC AddReservedKeyDataForNetwork Add Key Data Mti[{nccc.Mti()}, Key[{key}]");
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
                    .AddLast(new LoggerHandler("CyberAuthNCCC-Logger", LogLevel.Debug))
                    // Must sit before InboundMultiWorkerHandler: that handler runs each read on its own task,
                    // so a message split across reads can only be reassembled while reads are still in order.
                    .AddLast(new LengthPrefixedFrameHandler())
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
                    //try
                    //{
                    //	Logger.Debug($"Go Hander: {GetType().FullName}");
                    //	base.ChannelRead(ctx, msg);
                    //	var receiveByteBuf = (ByteBuf)msg;
                    //	byte[] receiveBytes = new byte[receiveByteBuf.Val().Length - 2];
                    //	Buffer.BlockCopy(receiveByteBuf.Val(), 2, receiveBytes, 0, receiveByteBuf.Val().Length - 2);
                    //	var tempString = HexStringConverter.FromByteArray(receiveByteBuf.Val());
                    //	//if (Logger.IsDebugEnabled)
                    //	//{
                    //	//    Logger.Debug($"Receive Data Hex String [{tempString}]");
                    //	//    Logger.Debug($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                    //	//}
                    //	Logger.Info($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                    //	if (_isReturn) return;
                    //	var sendBuffer = ProcessNCCCAuth(receiveByteBuf.Val(), ctx.Channel.LocalEndPoint().ToString());
                    //	if (sendBuffer == null)
                    //	{
                    //		Logger.Info("Send Buffer Data Is Null, No Process");
                    //		return;
                    //	}
                    //	ctx.Write(sendBuffer);
                    //}
                    //catch (Exception ex)
                    //{
                    //	Logger.Error($"ChannelRead Exception : {ex?.Message}, No Process");
                    //	return;
                    //}
                    byte[] bytes = null;

                    if (msg is ByteBuf)
                    {
                        bytes = ((ByteBuf)msg).Val();
                    }
                    try
                    {
                        var length = bytes?.Length ?? 0;
                        var localEndPoint = ctx.Channel.LocalEndPoint().ToString();
                        var remoteEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                        if (length > 0)
                        {
                            if (Logger.IsDebugEnabled)
                            {
                                Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Read Raw Data Length: {length}B\n{PrintBytesMap(bytes)}]");
                            }
                        }
                        try
                        {
                            var listReceiveBytes = GetTransactionListBytes(bytes, remoteEndPoint, localEndPoint);
                            if (listReceiveBytes.Count <= 0)
                            {
                                Logger.Warn($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], ChannelRead No Process");
                                return;
                            }
                            else
                            {
                                Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], ChannelRead Process List Count[{listReceiveBytes.Count}]");
                            }
                            foreach (var dataBytes in listReceiveBytes)
                            {
                                if (dataBytes.Length >= 2)
                                {
                                    // The one Info line per message read (the frame count and lengths are Debug), and it
                                    // reports this frame: it used the whole read, which is wrong when one read holds several.
                                    int receiveDataLength = Convert.ToInt16(dataBytes[0]) * 256 + Convert.ToInt16(dataBytes[1]);
                                    Logger.Info($"CHANNELREAD [{remoteEndPoint}] => [{localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Receive Data Length[{receiveDataLength}], Real Receive Data Length[{dataBytes.Length - 2}]");
                                }
                                try
                                {
                                    // The whole message is Debug only, as in the other Auth services: at Info it was
                                    // about a quarter of this log's volume under load.
                                    if (Logger.IsDebugEnabled)
                                    {
                                        byte[] receiveBytes = new byte[dataBytes.Length - 2];
                                        Buffer.BlockCopy(dataBytes, 2, receiveBytes, 0, dataBytes.Length - 2);
                                        Logger.Debug($"Receive Data(No Data Length)[{Encoding.ASCII.GetString(receiveBytes)}]");
                                    }
                                    if (_isReturn) return;
                                    var sendBuffer = ProcessNCCCAuth(dataBytes, ctx.Channel.LocalEndPoint().ToString());
                                    if (sendBuffer == null)
                                    {
                                        Logger.Info("Send Buffer Data Is Null, No Process");
                                        return;
                                    }
                                    ctx.Write(sendBuffer);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Exception[{ex.Message}]");
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"ChannelRead Exception : {ex?.Message}, No Process");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{ex.Message}]");
                    }
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
                    // Stamped here so AuthTiming can report how long the read waited for a pool thread.
                    var queuedAt = AuthTiming.Start();
                    // P0 Fix: Use semaphore to limit concurrent inbound processing (max 100)
                    _ = ProcessInboundAsync(ctx, msg, queuedAt);
                }

                private async Task ProcessInboundAsync(ChannelHandlerContext ctx, object msg, long queuedAt)
                {
                    await _inboundSemaphore.WaitAsync();
                    try
                    {
                        AuthTiming.MarkDequeued(queuedAt);
                        base.ChannelRead(ctx, msg);
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal($"InboundMultiWorkerHandler ChannelRead Exception : {ex.Message}");
                    }
                    finally
                    {
                        AuthTiming.ClearDequeued();
                        _inboundSemaphore.Release();
                    }
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

                    // P0 Fix: Implement reconnection limit (max 3 attempts) with exponential backoff
                    int reconnectAttempts = _channelReconnectAttempts.AddOrUpdate(ctx.Channel, 1, (key, oldValue) => oldValue + 1);
                    
                    if (reconnectAttempts > 3)
                    {
                        Logger.Fatal($"Channel[{ctx.Channel.LocalEndPoint()}] exceeded maximum reconnection attempts ({reconnectAttempts}). Stopping reconnection.");
                        _channelReconnectAttempts.TryRemove(ctx.Channel, out _);
                        return;
                    }

                    // Calculate exponential backoff: 5s, 10s, 30s
                    int backoffDelayMs = reconnectAttempts switch
                    {
                        1 => 5000,   // 5 seconds
                        2 => 10000,  // 10 seconds
                        _ => 30000   // 30 seconds for attempt 3
                    };

                    Logger.Info($"Channel[{ctx.Channel.LocalEndPoint()}] reconnection attempt {reconnectAttempts}/3, waiting {backoffDelayMs}ms before retry");
                    TimeUtils.Sleep(backoffDelayMs);

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
                        // Reset reconnection counter on successful connection
                        _channelReconnectAttempts.TryRemove(channel, out _);
                        Logger.Info($"Connection established for channel[{channel.LocalEndPoint()}], reset reconnection counter");
                    }
                }
            }

            private static byte[] ProcessNCCCAuth(byte[] ncccMessageBytes, string localEndPoint, bool includeLength = true)
            {
                try
                {
                    byte[] ncccBytes;
                    if (includeLength)
                    {
                        ncccBytes = new byte[ncccMessageBytes.Length - 2];
                        Buffer.BlockCopy(ncccMessageBytes, 2, ncccBytes, 0, ncccMessageBytes.Length - 2);
                    }
                    else
                    {
                        ncccBytes = new byte[ncccMessageBytes.Length];
                        Buffer.BlockCopy(ncccMessageBytes, 0, ncccBytes, 0, ncccMessageBytes.Length);
                    }
                    var nccc = new NcccIso8583(ncccBytes);
                    string txKeySeq = string.Empty;
                    if (Convert.ToUInt16(nccc.Mti()[2]) % 2 == 0)
                    {
                        Logger.Info($"Request Message --> Header[{nccc.Header().Encode()}], MTI [{nccc.Mti()}]");
                    }
                    else
                    {
                        Logger.Info($"Response Message --> Header[{nccc.Header().Encode()}], MTI [{nccc.Mti()}]");
                        ReservedKeyData keyData;
                        if (nccc.Mti().Substring(0, 2).Equals("08"))
                            keyData = RemoveReservedKeyDataForNetwork(nccc);
                        else
                            keyData = RemoveReservedKeyData(nccc);
                        if (keyData == null)
                            Logger.Info($"Get Original Request TxKeySeq({nccc.Val(FIELD.F_7)}-{nccc.Val(FIELD.F_11)}) Error");
                        else
                            txKeySeq = keyData.TxKeySeq;
                    }

                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in nccc.Core().OrderedFields())
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

                    // One timing line per request: where the time went (SP calls, parameter lookups, HSM, the rest).
                    var timing = AuthTiming.Begin();
                    byte[] retnProcessBytes;
                    try
                    {
                        retnProcessBytes = NcccAuthFlowUtility.ProcessTransactionFlow(nccc, localEndPoint, txKeySeq);
                    }
                    finally
                    {
                        Logger.Info(timing.Summary(nccc.Mti()));
                        AuthTiming.End();
                    }
                    if (retnProcessBytes == null)
                    {
                        if ((Convert.ToInt16(nccc.Mti()[2])) % 2 != 0)
                        {
                            Logger.Info($"Process(ProcessNCCCAuth) NcccAuthFlowUtility For Response Transaction Mti:{nccc.Mti()}");
                            return null;
                        }
                        Logger.Warn($"Process(ProcessNCCCAuth) Error , Create System Error Response Message [{nccc.Val(FIELD.F_7)}-{nccc.Val(FIELD.F_11)}]");
                        var responseMessage = new MessageBase(nccc, SystemConstantData.CommonData.SystemProcessErrorCode);
                        var dataBytes = responseMessage.GetNcccMessage().Encode().Val();
                        retnProcessBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
                        return retnProcessBytes;
                    }
                    //Check message For sync;(cut over and new key)
                    if (ColaCommUtility.IsSyncPasysInfoData(nccc) &&
                        CyberAuthNCCCResource.ClientAuthClientMonitorService != null)
                    {
                        var fieldPos = FIELD.F_70;
                        var informationCode = !ObjectUtils.IsNull(nccc.RawVal(fieldPos)) ? nccc.RawVal(fieldPos).ToString() : string.Empty;
                        Logger.Info($"Process SendSyncColaPasysInfoCommand For Information Code[{informationCode}]");
                        var isSendSyncData = CyberAuthNCCCResource.ClientAuthClientMonitorService?.SendSyncColaPasysInfoCommand();
                        if ((bool)!isSendSyncData)
                        {
                            Logger.Info("No Process SendSyncColaPasysInfoCommand");
                        }
                    }

                    byte[] responseNcccBytes = new byte[retnProcessBytes.Length - 2];
                    Array.Copy(retnProcessBytes, 2, responseNcccBytes, 0, retnProcessBytes.Length - 2);
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug($"ProcessNCCCAuth Response Bytes[{Encoding.ASCII.GetString(responseNcccBytes)}]");
                    }
                    var responseNccc = new NcccIso8583(responseNcccBytes);
                    Logger.Info($"Response Message --> Header[{responseNccc.Header().Encode()}], MTI [{responseNccc.Mti()}]");
                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in responseNccc.Core().OrderedFields())
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

                    return (retnProcessBytes);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{ex.Message}]");
                    return null;
                }
            }
            private static byte[] ProcessMCOLAAuth(byte[] mcolaMessageBytes, string localEndPoint, bool includeLength = true)
            {
                try
                {
                    byte[] mcolaBytes;
                    if (includeLength)
                    {
                        mcolaBytes = new byte[mcolaMessageBytes.Length - 2];
                        Buffer.BlockCopy(mcolaMessageBytes, 2, mcolaBytes, 0, mcolaMessageBytes.Length - 2);
                    }
                    else
                    {
                        mcolaBytes = new byte[mcolaMessageBytes.Length];
                        Buffer.BlockCopy(mcolaMessageBytes, 0, mcolaBytes, 0, mcolaMessageBytes.Length);
                    }
                    var mcola = new MCOLA(mcolaBytes);
                    string txKeySeq = string.Empty;
                    if (Convert.ToUInt16(mcola.Mti()[2]) % 2 == 0)
                    {
                        Logger.Info($"Request Message --> Header[{mcola.Header().Encode()}], MTI [{mcola.Mti()}]");
                    }
                    else
                    {
                        Logger.Info($"Response Message --> Header[{mcola.Header().Encode()}], MTI [{mcola.Mti()}]");
                        ReservedKeyData keyData = RemoveReservedKeyData(mcola);
                        if (keyData == null)
                            Logger.Info($"Get Original Request TxKeySeq({mcola.Val(FIELD.F_7)}-{mcola.Val(FIELD.F_11)}) Error");
                        else
                            txKeySeq = keyData.TxKeySeq;

                    }

                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in mcola.Core().OrderedFields())
                        {
                            var key = pair.Key;
                            var value = pair.Value;
                            var bytes = value as byte[];
                            if (bytes != null)
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

                    var timing = AuthTiming.Begin();
                    byte[] retnProcessBytes;
                    try
                    {
                        retnProcessBytes = NcccAuthFlowUtility.ProcessTransactionFlow(mcola, localEndPoint, txKeySeq);
                    }
                    finally
                    {
                        Logger.Info(timing.Summary(mcola.Mti()));
                        AuthTiming.End();
                    }
                    if (retnProcessBytes == null)
                    {
                        if ((Convert.ToInt16(mcola.Mti()[2])) % 2 != 0)
                        {
                            Logger.Info($"Process AuthFlowUtility For Response Transaction Mti:{mcola.Mti()}");
                            return null;
                        }
                        Logger.Info("Process AuthFlowUtility Error");
                        return null;
                    }
                    byte[] responseNcccBytes = new byte[retnProcessBytes.Length - 2];
                    Array.Copy(retnProcessBytes, 2, responseNcccBytes, 0, retnProcessBytes.Length - 2);
                    var responseNccc = new NcccIso8583(responseNcccBytes);
                    Logger.Info($"Response Message --> Header[{responseNccc.Header().Encode()}], MTI [{responseNccc.Mti()}]");
                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in responseNccc.Core().OrderedFields())
                        {
                            var key = pair.Key;
                            var value = pair.Value;
                            var bytes = value as byte[];
                            if (bytes != null)
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

                    return (retnProcessBytes);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{ex.Message}]");
                    return null;
                }
            }

            private static ReservedKeyData RemoveReservedKeyData(NcccIso8583 nccc)
            {
                string key = $"{nccc.Val(FIELD.F_7)}-{nccc.Val(FIELD.F_11)}";
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    Logger.Info($"CyberAuthNCCC RemoveReservedKeyData Remove Key[{key}] Successful");
                    return (keyData);
                }
                else
                {
                    Logger.Info($"CyberAuthNCCC RemoveReservedKeyData Remove Key [{key}] Failed");
                    return null;
                }
            }
            private static ReservedKeyData RemoveReservedKeyDataForNetwork(NcccIso8583 nccc)
            {
                string key = $"{nccc.Val(FIELD.F_70)}-{nccc.Val(FIELD.F_11)}";
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    Logger.Info($"CyberAuthNCCC RemoveReservedKeyDataNetwork Remove Key[{key}] Successful");
                    return (keyData);
                }
                else
                {
                    Logger.Info($"CyberAuthNCCC RemoveReservedKeyDataNetwork Remove Key [{key}] Failed");
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
                        dataOut.Append(string.Format("\n|{0}| ", (index / 16).ToString().PadLeft(8, '0')));
                        byteContent = string.Empty;
                        dataContent = string.Empty;
                    }

                    byteContent += string.Format("{0:X2} ", b);
                    dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);

                    if (index % 16 == 15 | index + 1 == length)
                    {
                        dataOut.AppendFormat("{0}|{1}|", byteContent.PadRight(48, ' '), dataContent.PadRight(16, ' '));
                    }

                    index++;
                }

                dataOut.Append("\n+--------+-------------------------------------------------+----------------+");
                return dataOut.ToString();
            }
            private static List<byte[]> GetTransactionListBytes(byte[] receiveBytes, string remoteEndPoint, string localEndPoint)
            {
                try
                {
                    lock (LockObject)
                    {
                        if (receiveBytes != null)
                        {
                            if (Logger.IsDebugEnabled)
                            {
                                Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], GetTransactionListBytes Process Original Byte Length[{receiveBytes.Length}]B\n{PrintBytesMap(receiveBytes)}]");
                            }
                            var listReceiveBytes = new List<byte[]>();
                            var startIndexPoint = 0;
                            while ((startIndexPoint + 2) <= receiveBytes.Length)
                            {
                                int processDataLength = Convert.ToInt16(receiveBytes[startIndexPoint]) * 256 + Convert.ToInt16(receiveBytes[startIndexPoint + 1]);
                                Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], Binary data length[{processDataLength}]");
                                if (receiveBytes.Length < (startIndexPoint + 2 + processDataLength))
                                {
                                    Logger.Warn($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}],ReceiveBytes data length is not enough, StartIndexPoint[{startIndexPoint}], ProcessDataLength[{processDataLength}], ReceiveBytesLength[{receiveBytes.Length}]");
                                    break;
                                }
                                var processBytes = new byte[2 + processDataLength];
                                Buffer.BlockCopy(receiveBytes, startIndexPoint, processBytes, 0, 2 + processDataLength);
                                listReceiveBytes.Add(processBytes);
                                if (Logger.IsDebugEnabled)
                                {
                                    Logger.Debug($"ListReceiveBytes CHANNELREAD [{remoteEndPoint} => {localEndPoint}] , StartIndexPoint[{startIndexPoint}], ProcessDataLength[{processDataLength}]\nLength: {processBytes.Length}B\n{PrintBytesMap(processBytes)}]");
                                }
                                startIndexPoint += (2 + processDataLength);
                            }
                            Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], GetTransactionListBytes Process Transaction Count[{listReceiveBytes.Count}]");
                            return listReceiveBytes;
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process GetTransactionListBytes Exception : [{ex.Message}]");
                    return null;
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

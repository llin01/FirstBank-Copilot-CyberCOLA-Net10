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
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.IpassMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.ClientAuth
{
    public class CyberAuthIPASS
    {
        private static string _clientIPAddr;
        private static int _localPortStart;
        private static int _sessionCount;
        private static bool _isReturn;
        private static string _formatType;
        private static int _retainMinutes = 10;
        private static bool _isReConnection = true;
        private static readonly Logger Logger = LoggerAdapter.GetLogger("LoggerHandler[CyberAuthIPASS]");
        private static Bootstrap _bootstrap;
        private static IPEndPoint _remoteEndPoint;
        private static ConcurrentDictionary<IChannel, string> _clientChannels = new ConcurrentDictionary<IChannel, string>();
        private static ConcurrentDictionary<string, ReservedKeyData> _reservedKeyDatas = new ConcurrentDictionary<string, ReservedKeyData>();
        private static readonly object GetFreeLocalEndPointLocker = new object();
        private static List<IPEndPoint> _localEndPoints = null;
        public CyberAuthIPASS()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();

                var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-target-ip").Trim(), AddressFamily.InterNetwork);
                var portNumber = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-target-port").Trim());
                var localHostName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-local-ip").Trim();
                _clientIPAddr = CommonUtils.GetIPAddressStringByHostName(localHostName, AddressFamily.InterNetwork);
                _localPortStart = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-local-port-start").Trim());
                IpassTransactionConst.MessageHeader.IpassIssuerID = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-tpdu-issuerid").Default("8093");
                _sessionCount = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-sessions").Trim());
                _isReturn = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-return-flag").Default("F").ToUpper().Equals("TRUE");
                _formatType = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-format-type").Trim().ToUpper();
                _retainMinutes = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-key-data-retain-minutes").Default("10"));
                if (!_formatType.ToUpper().Equals("IPASS"))
                {
                    Logger.Fatal("This Authorization Only Process IPASS Format");
                    throw new Exception("Accept Format Type Error");
                }
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), portNumber);
                Logger.Info($"CyberAuthIPASS Client Authorization Service ServerIPAddress[{serverIPAddr}],ClientIPAddress[{_clientIPAddr}],Port Number[{portNumber}],SessionCount[{_sessionCount}]");

                _bootstrap = new Bootstrap();
                Logger.Info("----- Start Display ColaPasysInfo -----");
                Logger.Info($"\n{SingletonOfCommonProperty.Instance.DisplayColaPasysInfo()}");
                Logger.Info("----- End   Display ColaPasysInfo -----");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"CyberAuthIPASS Constructor Exception : {ex.Message}");
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
                          .Handler(new LoggerHandler("CyberAuthIPASS", LogLevel.Debug))
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
                Logger.Fatal($"Start CyberAuthIPASS Exception : {ex.Message}");
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
            var requestIpassMessage = IpassTransCreator.IpassSignOnRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestIpassMessage, tranLogKey))
            {
                Logger.Fatal("CyberAuthIPASS SignOn AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIpassMessage.Encode().Val();
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
            var requestIpassMessage = IpassTransCreator.IpassSignOnRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestIpassMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthIPASS SignOn AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOn at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIpassMessage.Encode().Val();
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
            var requestIpassMessage = IpassTransCreator.IpassSignOffRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestIpassMessage, tranLogKey))
            {
                Logger.Fatal("CyberAuthIPASS SignOff AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIpassMessage.Encode().Val();
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
            var requestIpassMessage = IpassTransCreator.IpassSignOffRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            if (!AddReservedKeyDataForNetwork(requestIpassMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthIPASS SignOff AddReservedKeyDataForNetwork Error");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process SignOff at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIpassMessage.Encode().Val();
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
                var channel = _clientChannels.Keys.ToArray()[0];
                if (channel == null)
                {
                    Logger.Info("File Maintenance Create IpassMessage0300 no Channel");
                    return;
                }
                Logger.Info($"File Maintenance Use Channel[{channel.LocalEndPoint()}], Dest[{IpassTransactionConst.MessageHeader.IpassHostID}], Src[{IpassTransactionConst.MessageHeader.IpassIssuerID}]");
                var requestIpassMessage = new IpassMessage0302(fileMaintData).GetIpassMessage();
                if (requestIpassMessage == null)
                {
                    Logger.Info("File Maintenance Create IpassMessage0302 Error");
                    return;
                }

                int spRetnCode = StoreProcUtility.InsertTranLog(fileMaintData.TxKeySeq,
                                                                SystemConstantData.CommonData.MessageSendDirection,
                                                                channel.LocalEndPoint().ToString(),
                                                                requestIpassMessage);
                if (spRetnCode != 0)
                {
                    Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                    return;
                }
                if (!AddReservedKeyData(requestIpassMessage, fileMaintData.TxKeySeq))
                {
                    Logger.Fatal("CyberAuthIPASS FileMaint AddReservedKeyData Error");
                    return;
                }
                Logger.Info($"{channel.LocalEndPoint()} Start Process File Maintenance at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
                var dataBytes = requestIpassMessage.Encode().Val();
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
                Logger.Error("Create IpassMessage0800 Of EchoTest No Channel");
                return;
            }

            var tranLogKey = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            var requestIpassMessage = IpassTransCreator.IpassEchoTestRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(tranLogKey,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }

            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIpassMessage.Encode().Val();
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
                Logger.Error("Create IpassMessage0800 Of EchoTest No Channel");
                return;
            }
            var requestIpassMessage = IpassTransCreator.IpassEchoTestRequest();
            int spRetnCode = StoreProcUtility.InsertTranLog(interchangeMessage.MessageId,
                                                            SystemConstantData.CommonData.MessageSendDirection,
                                                            channel.LocalEndPoint().ToString(),
                                                            requestIpassMessage);
            if (spRetnCode != 0)
            {
                Logger.Fatal($"Insert Cola_Tran_Log Error,Return Code = [{spRetnCode}]");
                return;
            }
            if (!AddReservedKeyDataForNetwork(requestIpassMessage, interchangeMessage.MessageId))
            {
                Logger.Fatal("CyberAuthIPASS EchoTest AddReservedKeyDataForNetwork Error");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} Start Process EchoTest at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = requestIpassMessage.Encode().Val();
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
        public void SendMessageToGateway(IPASS ipassMessage)
        {
            if (_clientChannels.Keys.ToArray().Length == 0)
            {
                Logger.Error("CyberAuthIPASS SendMessageToGateway No Client Channel Use");
                return;
            }
            //random select channel
            int clientChannels = _clientChannels.Keys.ToArray().Length;
            int channelNumber = ColaCommUtility.GetSecurityNumber(clientChannels);
            Logger.Info($"ClientChannel[{clientChannels}], Send Channel Number [{channelNumber}]");
            var channel = _clientChannels.Keys.ToArray()[channelNumber];
            if (channel == null)
            {
                Logger.Error("Send Message To IpassColaService No Channel");
                return;
            }
            Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, Start Process CyberAuthIPASS SendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            var dataBytes = ipassMessage.Encode().Val();
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
            Logger.Info($"{channel.LocalEndPoint()} => {channel.RemoteEndPoint()}, End Process CyberAuthIPASS SendMessageToGateway at : {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
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
                sb.Append($"{tempString} --> [{channel?.RemoteEndPoint()}]{Environment.NewLine}");
            }
            return sb.ToString();
        }
        public string GetColaPasysInfo()
        {
            return SingletonOfCommonProperty.Instance.DisplayColaPasysInfo();
        }
        public string DeleteReservedKeyData()
        {
            Logger.Info($"CyberAuthIPASS, Retain Minutes[{_retainMinutes}] DeleteReservedKeyData Start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var keysLists = _reservedKeyDatas.Where(x => DateTime.Now.Subtract(x.Value.CreateDateTime).TotalMinutes > (double)_retainMinutes)
                                                       .Select(x => x.Key).ToList();
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}CyberAuthIPASS DeleteReservedKeyData{Environment.NewLine}");
            foreach (var key in keysLists)
            {
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    sb.Append($"CyberAuthIPASS RemoveReservedKeyData Remove Key[{key}] Successful{Environment.NewLine}");
                }
                else
                {
                    sb.Append($"CyberAuthIPASS RemoveReservedKeyData Remove Key [{key}] Failed{Environment.NewLine}");
                }
            }
            Logger.Info($"DeleteReservedKeyData Info : [{sb.ToString()}]");
            return sb.ToString();
        }
        public string ListReservedKeyData()
        {
            Logger.Info($"CyberAuthIPASS ListReservedKeyData Start : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            var dataLists = _reservedKeyDatas.OrderBy(x => x.Value.CreateDateTime)
                                                                   .Select(x => new { x.Key, x.Value.CreateDateTime }).ToList();

            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}CyberAuthIPASS ListReservedKeyData{Environment.NewLine}");
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
        private bool AddReservedKeyData(IPASS ipass, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{ipass.Val(FIELD.F_2)}-{ipass.Val(FIELD.F_36)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"CyberAuthIPASS AddReservedKeyData Add Key Data Mti[{ipass.Mti()}, Key[{key}]");
            return (_reservedKeyDatas.TryAdd(key, keyData));
        }
        private bool AddReservedKeyDataForNetwork(IPASS ipass, string txKeySeq)
        {
            ReservedKeyData keyData = new ReservedKeyData();
            string key = $"{ipass.Val(FIELD.F_70)}-{ipass.Val(FIELD.F_11)}";
            keyData.CreateDateTime = DateTime.Now;
            keyData.TxKeySeq = txKeySeq;
            Logger.Info($"CyberAuthIPASS AddReservedKeyDataForNetwork Add Key Data Mti[{ipass.Mti()}, Key[{key}]");
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
                    .AddLast(new LoggerHandler("CyberAuthIPASS-Logger", LogLevel.Debug))
                    // Split the stream into whole messages before InboundMultiWorkerHandler (see LengthPrefixedFrameHandler).
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
                    var localEndPoint = ctx.Channel.LocalEndPoint().ToString();
                    var remoteEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                    Logger.Info($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Read Raw Data Length: {receiveByteBuf.Val().Length}B");
                    // The hex dump is Debug only: at Info it was most of the log volume under load.
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Length: {receiveByteBuf.Val().Length}B\n{PrintBytesMap(receiveByteBuf.Val())}");
                    }

                    if (_isReturn) return;
                    var sendBuffer = ProcessIPASSAuth(receiveByteBuf.Val(), ctx.Channel.LocalEndPoint().ToString());
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

            private static byte[] ProcessIPASSAuth(byte[] ipassMessageBytes, string localEndPoint, bool includeLength = true)
            {
                try
                {
                    byte[] ipassBytes;
                    if (includeLength)
                    {
                        ipassBytes = new byte[ipassMessageBytes.Length - 2];
                        Buffer.BlockCopy(ipassMessageBytes, 2, ipassBytes, 0, ipassMessageBytes.Length - 2);
                    }
                    else
                    {
                        ipassBytes = new byte[ipassMessageBytes.Length];
                        Buffer.BlockCopy(ipassMessageBytes, 0, ipassBytes, 0, ipassMessageBytes.Length);
                    }
                    var ipass = new IPASS(ipassBytes);
                    string txKeySeq = string.Empty;
                    if (Convert.ToUInt16(ipass.Mti()[2]) % 2 == 0)
                    {
                        Logger.Info($"Request Message --> Header[{ipass.Header().Encode()}], MTI [{ipass.Mti()}]");
                    }
                    else
                    {
                        Logger.Info($"Response Message --> Header[{ipass.Header().Encode()}], MTI [{ipass.Mti()}]");
                        ReservedKeyData keyData;
                        if (ipass.Mti().Substring(0, 2).Equals("08"))
                            keyData = RemoveReservedKeyDataForNetwork(ipass);
                        else
                            keyData = RemoveReservedKeyData(ipass);
                        if (keyData == null)
                            Logger.Info($"Get Original Request TxKeySeq({ipass.Val(FIELD.F_7)}-{ipass.Val(FIELD.F_11)}) Error");
                        else
                            txKeySeq = keyData.TxKeySeq;
                    }

                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in ipass.Core().OrderedFields())
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

                    byte[] retnProcessBytes = IpassAuthFlowUtility.ProcessTransactionFlow(ipass, localEndPoint, txKeySeq);
                    if (retnProcessBytes == null)
                    {
                        if ((Convert.ToInt16(ipass.Mti()[2])) % 2 != 0)
                        {
                            Logger.Info($"Process(ProcessIPASSAuth) IpassAuthFlowUtility For Response Transaction Mti:{ipass.Mti()}");
                            return null;
                        }
                        Logger.Warn($"Process(ProcessIPASSAuth) Error , Create System Error Response Message [{ipass.Val(FIELD.F_7)}-{ipass.Val(FIELD.F_11)}]");
                        var responseMessage = new IpassMessageBase(ipass, SystemConstantData.CommonData.SystemProcessErrorCode);
                        var dataBytes = responseMessage.GetIpassMessage().Encode().Val();
                        retnProcessBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
                        return retnProcessBytes;
                    }

                    byte[] responseIpassBytes = new byte[retnProcessBytes.Length - 2];
                    Array.Copy(retnProcessBytes, 2, responseIpassBytes, 0, retnProcessBytes.Length - 2);
                    // The whole response is Debug only, as in CyberAuthNCCC: at Info it was a large share of the log.
                    if (Logger.IsDebugEnabled)
                    {
                        Logger.Debug($"ProcessIPASSAuth Response Bytes[{Encoding.ASCII.GetString(responseIpassBytes)}]");
                    }
                    var responseIpass = new IPASS(responseIpassBytes);
                    Logger.Info($"Response Message --> Header[{responseIpass.Header().Encode()}], MTI [{responseIpass.Mti()}]");
                    if (Logger.IsDebugEnabled)
                    {
                        foreach (var pair in responseIpass.Core().OrderedFields())
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

            private static ReservedKeyData RemoveReservedKeyData(IPASS ipass)
            {
                string key = $"{ipass.Val(FIELD.F_2)}-{ipass.Val(FIELD.F_36)}";
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    Logger.Info($"CyberAuthIPASS RemoveReservedKeyData Remove Key[{key}] Successful");
                    return (keyData);
                }
                else
                {
                    Logger.Info($"CyberAuthIPASS RemoveReservedKeyData Remove Key [{key}] Failed");
                    return null;
                }
            }
            private static ReservedKeyData RemoveReservedKeyDataForNetwork(IPASS ipass)
            {
                string key = $"{ipass.Val(FIELD.F_70)}-{ipass.Val(FIELD.F_11)}";
                var findKey = _reservedKeyDatas.TryRemove(key, out var keyData);
                if (findKey)
                {
                    Logger.Info($"CyberAuthIPASS RemoveReservedKeyDataNetwork Remove Key[{key}] Successful");
                    return (keyData);
                }
                else
                {
                    Logger.Info($"CyberAuthIPASS RemoveReservedKeyDataNetwork Remove Key [{key}] Failed");
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

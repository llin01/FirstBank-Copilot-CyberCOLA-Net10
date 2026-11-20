using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using IBM.WMQ;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;

namespace Com.Cybersoft4u.Simulator.Web.WebVDMQSimulator
{
    // --- Web Log Broadcaster for UI Parity ---
    public static class VDMQLogBroadcaster
    {
        private static readonly ConcurrentQueue<string> SystemLogQueue = new();
        private static readonly ConcurrentQueue<string> DetailLogQueue = new();

        public static event Action<string>? OnSystemLogAdded;
        public static event Action<string>? OnDetailLogAdded;

        public static void LogSystem(string message, bool isError = false)
        {
            var formatted = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            SystemLogQueue.Enqueue(formatted);
            OnSystemLogAdded?.Invoke(formatted);
            while (SystemLogQueue.Count > 400)
            {
                SystemLogQueue.TryDequeue(out _);
            }
        }

        public static void LogDetail(string message)
        {
            var formatted = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            DetailLogQueue.Enqueue(formatted);
            OnDetailLogAdded?.Invoke(formatted);
            while (DetailLogQueue.Count > 600)
            {
                DetailLogQueue.TryDequeue(out _);
            }
        }

        public static IEnumerable<string> GetSystemHistory() => SystemLogQueue.ToArray();
        public static IEnumerable<string> GetDetailHistory() => DetailLogQueue.ToArray();

        public static void Clear()
        {
            SystemLogQueue.Clear();
            DetailLogQueue.Clear();
        }
    }

    // --- VDMQ Simulator Manager class ---
    public class VDMQSimulatorManager : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IConfiguration _config;

        private bool _stopVDMQSimulator = false;
        private int _waitIntervalMilliSecs = 5000;
        private string[]? _availBalAcctArray;
        private readonly Random _random = new();

        private MQQueueManager? _mqQMgr = null;
        private MQQueue? _putQueue = null;
        private MQQueue? _getQueue = null;
        private MQGetMessageOptions? _getMessageOptions = null;
        private MQPutMessageOptions? _putMessageOptions = null;
        private CancellationTokenSource? _workerCts;

        public bool IsRunning { get; private set; } = false;
        public int WaitIntervalMilliSecs
        {
            get => _waitIntervalMilliSecs;
            set => _waitIntervalMilliSecs = value;
        }

        public VDMQSimulatorManager(IConfiguration config)
        {
            _config = config;
            InitializeAccounts();
        }

        private void InitializeAccounts()
        {
            try
            {
                // Parity with WPF InitialSystemParameter xml loading
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var availBalAcctInfosXmlDataFile = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "availbalacctinfos-xml-data-file")?.Trim();

                if (string.IsNullOrEmpty(availBalAcctInfosXmlDataFile) || !File.Exists(availBalAcctInfosXmlDataFile))
                {
                    // Fallback to searching Config directory or local mock
                    availBalAcctInfosXmlDataFile = Path.Combine(AppContext.BaseDirectory, "Data", "AvailBalAcctInfos.xml");
                }

                if (File.Exists(availBalAcctInfosXmlDataFile))
                {
                    var doc = XDocument.Load(availBalAcctInfosXmlDataFile);
                    var availBalAcctInfos = from results in doc.Descendants("AvailBalAcctInfo")
                                            select results.Element("AvailBalAcct")?.Value;
                    _availBalAcctArray = availBalAcctInfos.Where(v => v != null).ToArray()!;
                    VDMQLogBroadcaster.LogSystem($"Successfully loaded {_availBalAcctArray.Length} mock accounts.");
                }
                else
                {
                    // Minimal fallback
                    _availBalAcctArray = new[] { "100000", "250000", "50000", "75000" };
                    VDMQLogBroadcaster.LogSystem("Warning: Account configuration XML not found. Using default mock accounts.", true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize mock accounts");
                VDMQLogBroadcaster.LogSystem($"InitializeAccounts Error: {ex.Message}", true);
                _availBalAcctArray = new[] { "100000", "250000", "50000", "75000" };
            }
        }

        public string GetMQDetails()
        {
            try
            {
                var mqServer = SingletonOfMQServerInfo.Instance.GetMQServerInfo().MQServers.MQServer[0];
                return $"MQQueueManagerListener: {mqServer.MQQueueManagerListener} | MQChannelInfoListener: {mqServer.MQChannelInfoListener} | PutQueue: {mqServer.MQGetQueueName} | GetQueue: {mqServer.MQPutQueueName}";
            }
            catch (Exception ex)
            {
                return $"Error loading MQ Details: {ex.Message}";
            }
        }

        public bool Start(int interval)
        {
            if (IsRunning) return true;

            _waitIntervalMilliSecs = interval;
            _stopVDMQSimulator = false;
            _workerCts = new CancellationTokenSource();

            VDMQLogBroadcaster.LogSystem($"VDMQServer Simulator starting...");

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var mqServer = SingletonOfMQServerInfo.Instance.GetMQServerInfo().MQServers.MQServer[0];

                int retCode = InitialMQServer(mqServer);
                if (retCode != 0)
                {
                    VDMQLogBroadcaster.LogSystem("InitialMQServer Error", true);
                    return false;
                }

                IsRunning = true;
                Task.Run(() => ReadMQMessage(mqServer, _workerCts.Token));
                VDMQLogBroadcaster.LogSystem("VDMQServer Simulator started successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Start simulator failed");
                VDMQLogBroadcaster.LogSystem($"Start Error: {ex.Message}", true);
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            _stopVDMQSimulator = true;
            IsRunning = false;

            try
            {
                _workerCts?.Cancel();
            }
            catch { /* Ignore cancellation errors on shutdown */ }

            ReleaseMQResources();
            VDMQLogBroadcaster.LogSystem("VDMQServer Simulator stopped.");
        }

        private int InitialMQServer(MQServer mqServer)
        {
            try
            {
                var retnString = ParsingMQChannelInfo(mqServer.MQChannelInfoListener);
                if (retnString == null)
                {
                    VDMQLogBroadcaster.LogSystem("ParsingMQChannelInfo Parsing Invalid", true);
                    return -1;
                }

                var queueProperties = new Hashtable
                {
                    [MQC.HOST_NAME_PROPERTY] = retnString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = retnString[0],
                    [MQC.CCSID_PROPERTY] = Convert.ToInt32(mqServer.MQServerCCSID.Trim())
                };

                if (!string.IsNullOrEmpty(mqServer.MQUserName))
                {
                    queueProperties[MQC.USER_ID_PROPERTY] = mqServer.MQUserName;
                    queueProperties[MQC.PASSWORD_PROPERTY] = "";
                }

                _mqQMgr = new MQQueueManager(mqServer.MQQueueManagerListener, queueProperties);

                const int getOpenOptions = MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING;
                const int putOpenOptions = MQC.MQOO_OUTPUT;

                _putQueue = _mqQMgr.AccessQueue(mqServer.MQGetQueueName, putOpenOptions);
                _getQueue = _mqQMgr.AccessQueue(mqServer.MQPutQueueName, getOpenOptions);

                _getMessageOptions = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT | MQC.MQGMO_FAIL_IF_QUIESCING,
                    WaitInterval = Convert.ToInt32(mqServer.MQWaitIntervalMilliSec)
                };

                _putMessageOptions = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_FAIL_IF_QUIESCING
                };

                VDMQLogBroadcaster.LogSystem($"MQ Connected. Listener: {mqServer.MQQueueManagerListener}");
                return 0;
            }
            catch (MQException mqex)
            {
                VDMQLogBroadcaster.LogSystem($"MQException: {mqex.Message}, Reason Code: {mqex.ReasonCode}", true);
                Log.Fatal(mqex, $"MQException: {mqex.Message}, Reason Code: {mqex.ReasonCode}");
                return -1;
            }
            catch (Exception ex)
            {
                VDMQLogBroadcaster.LogSystem($"Exception during MQ initialization: {ex.Message}", true);
                Log.Fatal(ex, "MQ Initial failed");
                return -1;
            }
        }

        private void ReadMQMessage(MQServer mqServer, CancellationToken token)
        {
            try
            {
                while (!_stopVDMQSimulator && !token.IsCancellationRequested)
                {
                    var getMessage = new MQMessage
                    {
                        CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                        Format = MQC.MQFMT_STRING,
                    };

                    try
                    {
                        if (_getQueue == null || _getMessageOptions == null)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                        _getQueue.Get(getMessage, _getMessageOptions);
                    }
                    catch (MQException ex)
                    {
                        if (ex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                        {
                            VDMQLogBroadcaster.LogSystem($"No message received (timeout/waiting).");

                            // Sleep with cancellation check
                            for (int i = 0; i < _waitIntervalMilliSecs / 100; i++)
                            {
                                if (_stopVDMQSimulator || token.IsCancellationRequested) break;
                                Thread.Sleep(100);
                            }
                            continue;
                        }
                        else
                        {
                            VDMQLogBroadcaster.LogSystem($"MQException during Get: {ex.Message}", true);
                            break;
                        }
                    }

                    // Process incoming message
                    string readMessage = getMessage.ReadString(getMessage.DataLength);
                    VDMQLogBroadcaster.LogDetail($"Read Message [{readMessage}]");

                    try
                    {
                        var serializer = new XmlSerializer(typeof(VDMessage.RequestXML));
                        using var stringReader = new StringReader(readMessage);
                        var requestXML = (VDMessage.RequestXML)serializer.Deserialize(stringReader)!;

                        var responseXML = new VDMessage.ResponseXML();
                        var responseXMLHeader = new VDMessage.ResponseHeader
                        {
                            TxID = requestXML.RequestHeader.TxID,
                            SystemKey = requestXML.RequestHeader.SystemKey,
                            TxSeqNo = requestXML.RequestHeader.TxSeqNo,
                            MsgSeqNo = requestXML.RequestHeader.MsgSeqNo,
                            SvrTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            MsgDirection = "RSQ",
                            System = "XXXX",
                            StatusCode = "0000",
                            StatusDesc = "RS"
                        };
                        responseXML.ResponseHeader = responseXMLHeader;

                        var txRs = new VDMessage.TxRs
                        {
                            BusType = "X",
                            WsID = "2S",
                            HostSeqNo = "XXXXX",
                            TxDate = DateTime.Now.ToString("yyyyMMdd"),
                            TxTime = DateTime.Now.ToString("HH:mm:ss"),
                            TxBrn = "XXX",
                            FuncCode = requestXML.TxRq.FuncCode,
                            AcctNo = requestXML.TxRq.AcctNo,
                            CardNo = requestXML.TxRq.CardNo,
                            CreditNo = requestXML.TxRq.CreditNo,
                            AuthNo = requestXML.TxRq.AuthNo,
                            TxnAmt = requestXML.TxRq.TxAmt
                        };

                        if (_availBalAcctArray != null && _availBalAcctArray.Length > 0)
                        {
                            var randomNumber = _random.Next(0, _availBalAcctArray.Length);
                            txRs.AvaiBal = _availBalAcctArray[randomNumber];
                        }
                        else
                        {
                            txRs.AvaiBal = "100000";
                        }

                        responseXML.ResponseHeader.StatusCode = "T100";
                        responseXML.TxRs = txRs;

                        var responseXMLString = ColaCommUtility.ObjectToXMLString<VDMessage.ResponseXML>(responseXML, true);

                        // Put response message
                        var putMessage = new MQMessage
                        {
                            CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                            Format = MQC.MQFMT_STRING
                        };
                        putMessage.WriteString(responseXMLString);
                        putMessage.MessageType = MQC.MQMT_REQUEST;
                        putMessage.Report = MQC.MQRO_COPY_MSG_ID_TO_CORREL_ID;
                        putMessage.ReplyToQueueName = mqServer.MQGetQueueName;
                        putMessage.ReplyToQueueManagerName = mqServer.MQQueueManagerListener;
                        putMessage.CorrelationId = getMessage.CorrelationId;

                        if (_putQueue != null && _putMessageOptions != null)
                        {
                            Log.Info("Put Message[{0}] To Queue Name[{1}] For Queue Manager Name[{2}]", responseXMLString, _putQueue.Name.Trim(), _putQueue.ConnectionReference.Name.Trim());
                            VDMQLogBroadcaster.LogDetail($"Put Message[{responseXMLString}] To Queue Name[{_putQueue.Name.Trim()}] For Queue Manager Name[{_putQueue.ConnectionReference.Name.Trim()}]");
                            _putQueue.Put(putMessage, _putMessageOptions);
                        }
                    }
                    catch (Exception valEx)
                    {
                        VDMQLogBroadcaster.LogSystem($"Error parsing or responding to XML message: {valEx.Message}", true);
                        Log.Error(valEx, "Error during message serialization/deserialization");
                    }
                }
            }
            catch (Exception ex)
            {
                VDMQLogBroadcaster.LogSystem($"Worker Thread Exception: {ex.Message}", true);
                Log.Fatal(ex, "Worker Thread exception");
            }
            finally
            {
                ReleaseMQResources();
            }
        }

        private void ReleaseMQResources()
        {
            try
            {
                _putQueue?.Close();
            }
            catch { /* Ignore queue close error */ }
            finally { _putQueue = null; }

            try
            {
                _getQueue?.Close();
            }
            catch { /* Ignore queue close error */ }
            finally { _getQueue = null; }

            try
            {
                _mqQMgr?.Disconnect();
            }
            catch { /* Ignore queue manager disconnect error */ }
            finally { _mqQMgr = null; }
        }

        private string[]? ParsingMQChannelInfo(string mqChannelInfo)
        {
            try
            {
                const string pattern = @"(\/)|(\/)|(\()|(\))";
                var stringList = new List<string>();
                foreach (var result in Regex.Split(mqChannelInfo, pattern))
                {
                    if (string.IsNullOrEmpty(result)) continue;
                    Match match = Regex.Match(result, pattern);
                    if (!match.Success) stringList.Add(result);
                }
                if (stringList.Count != 4) return null;
                return stringList.ToArray();
            }
            catch (Exception ex)
            {
                Log.Fatal($"ParsingMqChannelInfo Exception:{ex.Message}");
                VDMQLogBroadcaster.LogSystem($"ParsingMqChannelInfo Exception:{ex.Message}", true);
                return null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

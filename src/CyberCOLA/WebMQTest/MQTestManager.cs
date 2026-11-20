using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using IBM.WMQ;
using NLog;
using System.Collections;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebMQTest
{
    public class MQTestManager : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // --- MQ Connection Options ---
        private static readonly int QueueSendOptions = MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING;
        private static readonly int QueueReceiveOptions = MQC.MQOO_INPUT_AS_Q_DEF + MQC.MQOO_FAIL_IF_QUIESCING;

        // --- Active MQ Instances ---
        private MQQueueManager? _mqSendQMgr;
        private MQQueue? _mqSendQueue;
        private MQQueueManager? _mqReceiveQMgr;
        private MQQueue? _mqReceiveQueue;

        private bool _isStopReceive = true;
        private readonly Random _rnd = new();
        private static readonly SingletonOfMQServerInfo SingletonOfMQServerInfo = SingletonOfMQServerInfo.Instance;

        public MQServerInfo? MQServerInfo { get; private set; }
        public List<CardInfo> CardInfosList { get; private set; } = new();

        // --- Events for Blazor UI Notification ---
        public event Action<string, bool>? OnProcessLog; // Message, IsError
        public event Action<string>? OnMessageReceived; // Received Payload String

        public MQTestManager()
        {
            // Register CodePages for IBM037/Big5 compatibility under .NET 10
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // --- Load Configs & XMLs ---
        public void LoadConfigurations()
        {
            try
            {
                var loader = new ConfigurationLoaderRoot();
                MQServerInfo = SingletonOfMQServerInfo.GetMQServerInfo();

                var cardPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "cardinfos-xml-data-file")?.Trim();
                if (!string.IsNullOrEmpty(cardPath) && File.Exists(cardPath))
                {
                    var doc = XDocument.Load(cardPath);
                    var cardInfos = from results in doc.Descendants("CardInfo")
                                    select new CardInfo
                                    {
                                        AcctNbr = results.Element("AcctNbr")?.Value ?? string.Empty,
                                        CardNbr = results.Element("CardNbr")?.Value ?? string.Empty
                                    };
                    CardInfosList = cardInfos.ToList();
                }

                TriggerProcessLog($"Configurations loaded. Found {CardInfosList.Count} card records.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load configurations");
                TriggerProcessLog($"Config load error: {ex.Message}", true);
            }
        }

        // --- Connect with dynamic panel parameters ---
        public int CreateMQConnection(
            string sendQMgr, string sendQueue, string sendChannel,
            string receiveQMgr, string receiveQueue, string receiveChannel)
        {
            try
            {
                ReleaseMQObject();

                // 1. Configure Send Queue
                string[] sendString = MQUtils.ParsingMQChannelInfo(sendChannel);
                var sendProps = new Hashtable
                {
                    [MQC.HOST_NAME_PROPERTY] = sendString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(sendString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = sendString[0]
                };

                TriggerProcessLog($"Connecting Send QM [{sendQMgr}] at {sendString[2]}:{sendString[3]}...");
                _mqSendQMgr = new MQQueueManager(sendQMgr, sendProps);
                _mqSendQueue = _mqSendQMgr.AccessQueue(sendQueue, QueueSendOptions);

                // 2. Configure Receive Queue
                string[] recvString = MQUtils.ParsingMQChannelInfo(receiveChannel);
                var recvProps = new Hashtable
                {
                    [MQC.HOST_NAME_PROPERTY] = recvString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(recvString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = recvString[0]
                };

                TriggerProcessLog($"Connecting Receive QM [{receiveQMgr}] at {recvString[2]}:{recvString[3]}...");
                _mqReceiveQMgr = new MQQueueManager(receiveQMgr, recvProps);
                _mqReceiveQueue = _mqReceiveQMgr.AccessQueue(receiveQueue, QueueReceiveOptions);

                TriggerProcessLog("MQ Connections established successfully.");
                return 0;
            }
            catch (MQException mqex)
            {
                Log.Error(mqex, $"MQ Connection failed. ReasonCode: {mqex.ReasonCode}");
                TriggerProcessLog($"MQ Connection failed: {mqex.Message} (Reason: {mqex.ReasonCode})", true);
                return -1;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Connection Exception");
                TriggerProcessLog($"Connection Error: {ex.Message}", true);
                return -2;
            }
        }

        // --- Active MQ Sending ---
        public void MQSend(string queueData, string receiveQueueName, string receiveQMgrName)
        {
            if (_mqSendQueue == null)
            {
                TriggerProcessLog("Error: MQ Send queue is not initialized.", true);
                return;
            }

            try
            {
                var sendMessage = new MQMessage
                {
                    CharacterSet = 1208,
                    Format = MQC.MQFMT_STRING,
                    ReplyToQueueName = receiveQueueName,
                    ReplyToQueueManagerName = receiveQMgrName,
                    Persistence = MQC.MQPER_NOT_PERSISTENT
                };

                sendMessage.WriteString(queueData);

                var correlationId = $"{"FCB4D"}{DateTime.Now:yyyyMMddHHmmssfffff}";
                sendMessage.CorrelationId = Encoding.UTF8.GetBytes(correlationId);
                sendMessage.MessageId = sendMessage.CorrelationId;

                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_NO_SYNCPOINT + MQC.MQPMO_DEFAULT_CONTEXT
                };

                _mqSendQueue.Put(sendMessage, pmo);

                TriggerProcessLog($"Sent data length [{queueData.Length}] with CorrId [{correlationId}] to Queue [{_mqSendQueue.Name.Trim()}]");
            }
            catch (MQException mqex)
            {
                Log.Error(mqex, "Put message MQ exception");
                TriggerProcessLog($"MQ Send Error: {mqex.Message} (Reason: {mqex.ReasonCode})", true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Put message exception");
                TriggerProcessLog($"Send Error: {ex.Message}", true);
            }
        }

        // --- Dynamic Single Receive ---
        public string MQReceiveSingle(int waitMs)
        {
            if (_mqReceiveQueue == null)
            {
                TriggerProcessLog("Error: MQ Receive queue is not initialized.", true);
                return string.Empty;
            }

            try
            {
                var receiveMessage = new MQMessage { CharacterSet = 1208 };
                var gmo = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT,
                    WaitInterval = waitMs
                };

                TriggerProcessLog($"Polling message from [{_mqReceiveQueue.Name.Trim()}] (Wait {waitMs}ms)...");
                _mqReceiveQueue.Get(receiveMessage, gmo);

                string content = receiveMessage.ReadString(receiveMessage.MessageLength);
                TriggerProcessLog($"Received message. MsgId: [{Encoding.UTF8.GetString(receiveMessage.MessageId)}]");
                return content;
            }
            catch (MQException mqex)
            {
                if (mqex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    TriggerProcessLog("No message available in the Queue.");
                    return string.Empty;
                }
                Log.Error(mqex, "Get message failure");
                TriggerProcessLog($"MQ Receive Error: {mqex.Message} (Reason: {mqex.ReasonCode})", true);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Get message exception");
                TriggerProcessLog($"Receive Exception: {ex.Message}", true);
                return string.Empty;
            }
        }

        // --- Background Continuous Receive Loop ---
        public void StartContinuousReceive(string receiveQMgr, string receiveQueue, string receiveChannel)
        {
            _isStopReceive = false;
            Task.Run(() =>
            {
                MQQueueManager? localQMgr = null;
                MQQueue? localQueue = null;

                try
                {
                    string[] retnString = MQUtils.ParsingMQChannelInfo(receiveChannel);
                    var recvProps = new Hashtable
                    {
                        [MQC.HOST_NAME_PROPERTY] = retnString[2],
                        [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                        [MQC.CHANNEL_PROPERTY] = retnString[0]
                    };

                    localQMgr = new MQQueueManager(receiveQMgr, recvProps);
                    localQueue = localQMgr.AccessQueue(receiveQueue, QueueReceiveOptions);

                    TriggerProcessLog($"Background Continuous Receiver started for queue: {receiveQueue}");

                    while (!_isStopReceive)
                    {
                        try
                        {
                            var receiveMessage = new MQMessage { CharacterSet = 1208 };
                            var gmo = new MQGetMessageOptions
                            {
                                Options = MQC.MQGMO_WAIT,
                                WaitInterval = 1000 // Poll interval
                            };

                            localQueue.Get(receiveMessage, gmo);
                            string content = receiveMessage.ReadString(receiveMessage.MessageLength);

                            if (!string.IsNullOrEmpty(content))
                            {
                                var logText = $"Read Message at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}\n" +
                                              $"----- Begin MQ Message -----\n{content}\n" +
                                              $"----- End MQ Message -----";
                                Log.Info(logText);
                                OnMessageReceived?.Invoke(logText);
                            }
                        }
                        catch (MQException mqex)
                        {
                            if (mqex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                            {
                                continue; // Queue empty, retry
                            }
                            Log.Error(mqex, "Continuous receive loop exception");
                            TriggerProcessLog($"Continuous Loop Error: {mqex.Message} (Reason: {mqex.ReasonCode})", true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Continuous receive exception");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Continuous receive startup exception");
                    TriggerProcessLog($"Continuous receive startup failed: {ex.Message}", true);
                }
                finally
                {
                    localQueue?.Close();
                    localQMgr?.Close();
                    TriggerProcessLog("Background Continuous Receiver stopped.");
                }
            });
        }

        public void StopContinuousReceive()
        {
            _isStopReceive = true;
            TriggerProcessLog("Requested continuous receiver to stop.");
        }

        // --- Create VDMQ Message XML & Loop Send ---
        public void CreateAndSendVDMQMessages(string templateXml, int repeatCount, string receiveQueue, string receiveQMgr)
        {
            if (CardInfosList.Count == 0)
            {
                TriggerProcessLog("Error: Card list is empty. Cannot generate VDMQ simulation.", true);
                return;
            }

            try
            {
                string baseXml = !string.IsNullOrEmpty(templateXml) ? templateXml : CreateDefaultVDRequestXml();

                for (int loop = 1; loop <= repeatCount; loop++)
                {
                    var serializer = new XmlSerializer(typeof(VDMessage.RequestXML));
                    using var stringReader = new StringReader(baseXml);
                    var requestXml = (VDMessage.RequestXML)serializer.Deserialize(stringReader)!;

                    var cardIndex = _rnd.Next(0, CardInfosList.Count);
                    var card = CardInfosList[cardIndex];

                    // Modify elements dynamically matching WPF
                    ulong txSeqNo = (ulong)_rnd.Next(0, 999999);
                    requestXml.RequestHeader.TxSeqNo = DateTime.Now.ToString("yyyyMMdd") + txSeqNo.ToString("D6");

                    ulong msgSeqNo = (ulong)_rnd.Next(0, 99999);
                    requestXml.RequestHeader.MsgSeqNo = msgSeqNo.ToString("D5");

                    requestXml.RequestHeader.CltTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                    ulong custId = (ulong)_rnd.Next(0, 99999999);
                    requestXml.RequestHeader.CustID = custId.ToString("D8");
                    requestXml.RequestHeader.UserID = "0000";
                    requestXml.RequestHeader.AcctNo = card.AcctNbr;

                    requestXml.TxRq.AcctNo = card.AcctNbr;

                    ulong cardNo = (ulong)_rnd.Next(0, 99999999);
                    requestXml.TxRq.CardNo = cardNo.ToString("D8");
                    requestXml.TxRq.CreditNo = card.CardNbr;

                    ulong authNo = (ulong)_rnd.Next(0, 999999);
                    requestXml.TxRq.AuthNo = authNo.ToString("D6");

                    ulong txAmt = (ulong)_rnd.Next(0, 9999);
                    requestXml.TxRq.TxAmt = txAmt.ToString() + "00";

                    string sendXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE TX_FCB91125S>" +
                                     ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(requestXml, true);

                    TriggerProcessLog($"Generating and placing VDMQ Msg #{loop} (Amt: {txAmt}.00)");

                    // Fire-and-forget put matching WPF
                    Task.Run(() => MQSend(sendXml, receiveQueue, receiveQMgr));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to compile/send VDMQ Message simulation batch");
                TriggerProcessLog($"VDMQ Batch Error: {ex.Message}", true);
            }
        }

        public string CreateDefaultVDRequestXml()
        {
            var vdMessage = new VDMessage.RequestXML();
            var header = new VDMessage.RequestHeader
            {
                TxID = "TX_FCB91125S",
                SystemKey = "FCB4B",
                TxSeqNo = DateTime.Now.ToString("MMddHHmmssfff"),
                MsgSeqNo = "00001",
                CltTimeStamp = "2023-02-20 09:25:00.000",
                CustID = "",
                UserID = "",
                AcctNo = "",
                MsgDirection = "RQ"
            };
            vdMessage.RequestHeader = header;

            var txRq = new VDMessage.TxRq
            {
                FuncCode = "1",
                AcctNo = "4563123456789012",
                CardNo = "00000001",
                CreditNo = "4563123456789012",
                AuthNo = "123456",
                TxAmt = "1000.00"
            };
            vdMessage.TxRq = txRq;

            return ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdMessage, true);
        }

        // --- Release MQ Handles ---
        public void ReleaseMQObject()
        {
            try
            {
                _mqSendQueue?.Close();
                _mqSendQMgr?.Close();
                _mqReceiveQueue?.Close();
                _mqReceiveQMgr?.Close();

                _mqSendQueue = null;
                _mqSendQMgr = null;
                _mqReceiveQueue = null;
                _mqReceiveQMgr = null;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to cleanly release MQ objects");
            }
        }

        private void TriggerProcessLog(string message, bool isError = false)
        {
            OnProcessLog?.Invoke(message, isError);
        }

        public void Dispose()
        {
            _isStopReceive = true;
            ReleaseMQObject();
        }
    }
}

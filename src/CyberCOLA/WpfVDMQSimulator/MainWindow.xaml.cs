using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using IBM.WMQ;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;
using Logger = NLog.Logger;


namespace Com.Cybersoft4u.Simulator.Wpf.WpfVDMQSimulator
{
    /// <summary>
    /// Main window for VDMQ (Virtual Data Message Queue) simulator.
    /// Simulates message queue operations for testing asynchronous message processing.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        #region RichTextBox Target
        private const string ConsoleTargetName = "WpfRichTextBox";
        #endregion //RichTextBox Target

        private static readonly SingletonOfMQServerInfo SingletonOfMQServerInfo = SingletonOfMQServerInfo.Instance;
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);
        private string _autoStartService;
        private bool _stopVDMQSimulator = false;
        private int _waitIntervalMilliSecs = 5000;
        private string[] AvailBalAcctArray;
        private Random random = new Random();
        MQQueueManager? mqQMgr = null;
        MQQueue? PutQueue = null;
        MQQueue? GetQueue = null;
        MQGetMessageOptions? GetMessageOptions = null;
        MQPutMessageOptions? PutMessageOptions = null;
        public MainWindow()
        {
            try
            {
                _autoStartService = (Application.Current as Simulator.Wpf.WpfVDMQSimulator.App)?.AutoStartService;
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            this.TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitialSystemParameter()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                var availBalAcctInfosXmlDataFile = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "availbalacctinfos-xml-data-file").Trim();
                var doc = XDocument.Load(availBalAcctInfosXmlDataFile);
                IEnumerable<string> availBalAcctInfos = from results in doc.Descendants("AvailBalAcctInfo")
                                                        select results.Element("AvailBalAcct")?.Value; ;
                AvailBalAcctArray = availBalAcctInfos.ToArray();
                SetMessageText(RichTextBox1, $"MQQueueManagerListener[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQQueueManagerListener}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"MQChannelInfoListener[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQChannelInfoListener}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"MQPutQueueNam[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQPutQueueName}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"MQGetQueueNam[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQGetQueueName}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"MQServerCCSID[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQServerCCSID}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"MQClientCCSID[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQClientCCSID}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"MQWaitIntervalMilliSec[{SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0].MQWaitIntervalMilliSec}]", Brushes.Red);
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                if ((!string.IsNullOrEmpty(_autoStartService)) && _autoStartService.Trim().Equals("AUTO"))
                {
                    BtnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            RichTextBox rtb = richTextBox as RichTextBox;
            if (rtb == null) return;
            if (rtb.Dispatcher.CheckAccess())
            {
                TextRange rangeOfText1 = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd) { Text = message + '\r' };
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                rtb.ScrollToEnd();
            } //if
            else
            {
                rtb.Dispatcher.BeginInvoke(new UpdateRichTextBox(SetMessageText), richTextBox, message, brush);
            }
        }

        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Clear Message Content";
            RichTextBox1.Document.Blocks.Clear();
            RichTextBox2.Document.Blocks.Clear();
        }

        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Closed Window";
            this.Close();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _stopVDMQSimulator = false;
            var message = $"VDMQServer Simulator start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            Log.Info(message);
            SetMessageText(RichTextBox1, message, Brushes.Black);
            try
            {
                if (!int.TryParse(TextBoxWaitIntervalMiliSecs.Text.Trim(), out _waitIntervalMilliSecs))
                {
                    TextBoxWaitIntervalMiliSecs.Focus();
                    SetMessageText(RichTextBox1, "Input Wait Interval Milliseconds Invalid", Brushes.Red);
                    return;
                }
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var mqServer = SingletonOfMQServerInfo.GetMQServerInfo().MQServers.MQServer[0];
                int retCode = InitialMQServer(mqServer);
                if (retCode != 0)
                {
                    Log.Error("InitialMQServer Error");
                    SetMessageText(RichTextBox1, "InitialMQServer Error", Brushes.Red);
                    return;
                }
                Task.Run(() => ReadMQMessage(mqServer));
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var message = $"VDMQServer Simulator stop at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}";
            Log.Info(message);
            SetMessageText(RichTextBox1, message, Brushes.Black);
            try
            {
                _stopVDMQSimulator = true;
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
            }
            catch (Exception ex)
            {
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string[] ParsingMQChannelInfo(string mqChannelInfo)
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
                return (stringList.ToArray());
            }
            catch (Exception ex)
            {
                Log.Fatal($"ParsingMqChannelInfo Exception:{ex.Message}");
                SetMessageText(RichTextBox1, $"ParsingMqChannelInfo Exception:{ex.Message}", Brushes.Red);
                return null;
            }
        }

        private int InitialMQServer(MQServer mqServer)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                var retnString = ParsingMQChannelInfo(mqServer.MQChannelInfoListener);
                if (retnString == null)
                {
                    Log.Error("ParsingMQChannelInfo Parsing Invalid");
                    SetMessageText(RichTextBox1, "ParsingMQChannelInfo Parsing Invalid", Brushes.Red);
                    return -1;
                }
                //PATSB02.SVRCONN/TCP/PATSB02-152(1414)
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

                //create Queue Manager
                mqQMgr = new MQQueueManager(mqServer.MQQueueManagerListener, queueProperties);
                const int getOpenOptions = MQC.MQOO_INPUT_AS_Q_DEF // open queue for input
                                         | MQC.MQOO_FAIL_IF_QUIESCING; // but not if MQM stopping
                const int putOpenOptions = MQC.MQOO_OUTPUT; // open queue for output
                //create Queue
                PutQueue = mqQMgr.AccessQueue(mqServer.MQGetQueueName, putOpenOptions);
                GetQueue = mqQMgr.AccessQueue(mqServer.MQPutQueueName, getOpenOptions);
                GetMessageOptions = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT | MQC.MQGMO_FAIL_IF_QUIESCING,
                    WaitInterval = Convert.ToInt32(mqServer.MQWaitIntervalMilliSec)
                };
                PutMessageOptions = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_FAIL_IF_QUIESCING
                };
                return 0;
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBox1, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                Log.Fatal(mqex, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                TextBlockMessage.Text = mqex.Message;
                return -1;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal(ex, $"Exception[{ex.Message}]");
                TextBlockMessage.Text = ex.Message;
                return -1;
            }
        }

        private void ReadMQMessage(MQServer mqServer)
        {
            // update the UI from the background thread using the Dispatcher
            //Dispatcher.Invoke(() =>
            //{
            //    // update UI elements here
            //});
            try
            {
                while (!_stopVDMQSimulator)
                {
                    var getMessage = new MQMessage
                    {
                        CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                        Format = MQC.MQFMT_STRING,
                    };
                    try
                    {
                        GetQueue.Get(getMessage, GetMessageOptions);
                    }
                    catch (MQException ex)
                    {
                        if (ex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                        {
                            // No message available, wait for 5 seconds before trying again
                            SetMessageText(RichTextBox1, $"at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} no message received", Brushes.Red);
                            Thread.Sleep(_waitIntervalMilliSecs);
                            continue;
                        }
                        else
                        {
                            SetMessageText(RichTextBox1, $"MQException: {ex.Message}", Brushes.Red);
                            break;
                        }
                    }

                    // Process the message data
                    string readMessage = getMessage.ReadString(getMessage.DataLength);
                    SetMessageText(RichTextBox2, $"Read Message [{readMessage}]", Brushes.Black);
                    XmlSerializer serializer = new XmlSerializer(typeof(VDMessage.RequestXML));
                    StringReader stringReader = new StringReader(readMessage);
                    var requestXML = (VDMessage.RequestXML)serializer.Deserialize(stringReader);

                    var responseXML = new VDMessage.ResponseXML();
                    var responseXMLHeader = new VDMessage.ResponseHeader();

                    responseXMLHeader.TxID = requestXML.RequestHeader.TxID;
                    responseXMLHeader.SystemKey = requestXML.RequestHeader.SystemKey;
                    responseXMLHeader.TxSeqNo = requestXML.RequestHeader.TxSeqNo;
                    responseXMLHeader.MsgSeqNo = requestXML.RequestHeader.MsgSeqNo;
                    responseXMLHeader.SvrTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    responseXMLHeader.MsgDirection = "RSQ";
                    responseXMLHeader.System = "XXXX";
                    responseXMLHeader.StatusCode = "0000";
                    responseXMLHeader.StatusDesc = "RS";
                    responseXML.ResponseHeader = responseXMLHeader;

                    var txRs = new VDMessage.TxRs();
                    txRs.BusType = "X";
                    txRs.WsID = "2S";
                    txRs.HostSeqNo = "XXXXX";
                    txRs.TxDate = DateTime.Now.ToString("yyyMMdd");
                    txRs.TxTime = DateTime.Now.ToString("HH:mm:ss");
                    txRs.TxBrn = "XXX";
                    txRs.FuncCode = requestXML.TxRq.FuncCode;
                    //sp assign
                    txRs.AcctNo = requestXML.TxRq.AcctNo;
                    //sp assign
                    txRs.CardNo = requestXML.TxRq.CardNo;
                    txRs.CreditNo = requestXML.TxRq.CreditNo;
                    txRs.AuthNo = requestXML.TxRq.AuthNo;
                    txRs.TxnAmt = requestXML.TxRq.TxAmt;
                    //sp assign
                    var randomNumber = random.Next(0, AvailBalAcctArray.Length);
                    txRs.AvaiBal = AvailBalAcctArray[randomNumber];
                    //sp assign
                    responseXML.ResponseHeader.StatusCode = "T100";
                    responseXML.TxRs = txRs;
                    var responseXMLString = ColaCommUtility.ObjectToXMLString<CommonLib.MQ.VDMessage.ResponseXML>(responseXML, true);

                    // Put the processed data onto the output queue
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
                    Log.Info("Put Message[{0}] To Queue Name[{1}] For Queue Manager Name[{2}]", responseXMLString, PutQueue.Name.Trim(), PutQueue.ConnectionReference.Name.Trim());
                    SetMessageText(RichTextBox2, $"Put Message[{responseXMLString}] To Queue Name[{PutQueue.Name.Trim()}] For Queue Manager Name[{PutQueue.ConnectionReference.Name.Trim()}]", Brushes.Black);
                    PutQueue.Put(putMessage, PutMessageOptions);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"ParsingMqChannelInfo Exception:{ex.Message}");
            }
            finally
            {
                if (mqServer != null)
                {
                    // Close the queues and queue manager
                    PutQueue?.Close();
                    GetQueue?.Close();
                    mqQMgr?.Disconnect();
                }
            }
        }
    }
}

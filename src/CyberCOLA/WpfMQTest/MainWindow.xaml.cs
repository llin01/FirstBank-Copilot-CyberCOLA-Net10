using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using IBM.WMQ;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfMQTest
{
    /// <summary>
    /// MainWindow.xaml的互動邏輯
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        #region NLog
        private static Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SingletonOfMQServerInfo SingletonOfMQServerInfo = SingletonOfMQServerInfo.Instance;
        private MQQueueManager _mqSendQMgr = null;
        private MQQueue _mqSendQueue = null;
        private MQQueueManager _mqReceiveQMgr = null;
        private MQQueue _mqReceiveQueue = null;
        private bool _isStopReceive = false;
        private const int QueueReceiveOptions = MQC.MQOO_INPUT_AS_Q_DEF                // open queue for input
                                              | MQC.MQOO_FAIL_IF_QUIESCING;            // but not if MQM stopping
        private const int QueueSendOptions = MQC.MQOO_OUTPUT;                          // open queue for output
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);
        private MQServerInfo MQServerInfo;
        private CardInfo[] CardInfosArray;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                BtnInitial.IsEnabled = true;
                BtnReset.IsEnabled = false;
                BtnSend.IsEnabled = false;
                BtnCreateVDMQMessage.IsEnabled = false;
                BtnOneReceive.IsEnabled = false;
                BtnForeverReceive.IsEnabled = false;
                SetMessageText(RichTextBoxProcessMessage, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss:fffff") + " Press Initial Button", Brushes.Red);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            this.TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private int InitialRoutine()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                MQServerInfo = SingletonOfMQServerInfo.GetMQServerInfo();
                RichTextBoxProcessMessage.Document.Blocks.Clear();
                RichTextBoxLogMessage.Document.Blocks.Clear();
                InitialTextBoxFieldData();
                SetMessageText(RichTextBoxProcessMessage, $"start at : {DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}", Brushes.Black);
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                string fileDirectory = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "send-file-directory");
                string[] files = Directory.GetFiles(fileDirectory);
                if (files.Length < 0)
                    TextBoxSendSelectFileName.Text = fileDirectory;
                else
                    TextBoxSendSelectFileName.Text = files[0];
                if (CreateMQConnection() != 0)
                {
                    SetMessageText(RichTextBoxProcessMessage, "Create MQObject Error", Brushes.Red);
                    return (1);
                }
                var cardinfosXmlDataFile = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "cardinfos-xml-data-file").Trim();
                var doc = XDocument.Load(cardinfosXmlDataFile);
                IEnumerable<CardInfo> cardInfos = from results in doc.Descendants("CardInfo")
                                                  select new CardInfo
                                                  {
                                                      AcctNbr = results.Element("AcctNbr")?.Value,
                                                      CardNbr = results.Element("CardNbr")?.Value
                                                  };
                CardInfosArray = cardInfos.ToArray();
                //var cardInfosArray = from results in doc.Descendants("CardInfo")
                //                     select new
                //                     {
                //                         AcctNbr = results.Element("AcctNbr")?.Value,
                //                         CardNbr = results.Element("CardNbr")?.Value
                //                     };
                foreach (var cardInfo in CardInfosArray)
                {
                    SetMessageText(RichTextBoxProcessMessage, cardInfo.AcctNbr, Brushes.Red);
                    SetMessageText(RichTextBoxProcessMessage, cardInfo.CardNbr, Brushes.Red);
                }
                TextBoxSendQueueManagerName.Text = MQServerInfo.MQServers.MQServer[0].MQQueueManagerListener;
                TextBoxSendQueueName.Text = MQServerInfo.MQServers.MQServer[0].MQPutQueueName;
                TextBoxSendChannelInfo.Text = MQServerInfo.MQServers.MQServer[0].MQChannelInfoListener;
                if (MQServerInfo.MQServers.MQServer.Count == 2)
                {
                    TextBoxReceiveQueueManagerName.Text = MQServerInfo.MQServers.MQServer[1].MQQueueManagerListener;
                    TextBoxReceiveQueueName.Text = MQServerInfo.MQServers.MQServer[1].MQGetQueueName;
                    TextBoxReceiveChannelInfo.Text = MQServerInfo.MQServers.MQServer[1].MQChannelInfoListener;
                }
                else
                {
                    TextBoxReceiveQueueManagerName.Text = MQServerInfo.MQServers.MQServer[0].MQQueueManagerListener;
                    TextBoxReceiveQueueName.Text = MQServerInfo.MQServers.MQServer[0].MQGetQueueName;
                    TextBoxReceiveChannelInfo.Text = MQServerInfo.MQServers.MQServer[0].MQChannelInfoListener;
                }

                return 0;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, ex.Message, Brushes.Red);
                Log.Fatal(ex, ex.Message);
                TextBlockMessage.Text = ex.Message;
                return 2;
            }
            finally
            {
                Mouse.OverrideCursor = null;
                BtnInitial.IsEnabled = true;
                BtnReset.IsEnabled = false;
                BtnSend.IsEnabled = false;
                BtnCreateVDMQMessage.IsEnabled = false;
                BtnOneReceive.IsEnabled = false;
                BtnForeverReceive.IsEnabled = false;
            }
        }

        private void InitialTextBoxFieldData()
        {
            TextBoxSendQueueManagerName.Text = MQServerInfo.MQServers.MQServer[0].MQQueueManagerListener;
            TextBoxSendChannelInfo.Text = MQServerInfo.MQServers.MQServer[0].MQChannelInfoListener;
            TextBoxSendQueueName.Text = MQServerInfo.MQServers.MQServer[0].MQPutQueueName;
            TextBoxSendMilliSecOfGet.Text = "1000";

            TextBoxReceiveQueueManagerName.Text = MQServerInfo.MQServers.MQServer[1].MQQueueManagerListener;
            TextBoxReceiveChannelInfo.Text = MQServerInfo.MQServers.MQServer[1].MQChannelInfoListener;
            TextBoxReceiveQueueName.Text = MQServerInfo.MQServers.MQServer[1].MQGetQueueName;
            TextBoxReceiveMilliSecOfGet.Text = "1000";
        }
        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            if (!(richTextBox is RichTextBox rtb)) return;
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
            RichTextBoxProcessMessage.Document.Blocks.Clear();
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            _isStopReceive = true;
            try
            {
                ReleaseMQObject();
                this.Close();
            }
            catch (MQException mqex)
            {
                Log.Fatal(mqex, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Exception[{ex.Message}]");
            }
            TextBlockMessage.Text = "Closed Window";
            this.Close();
        }


        private int CreateMQConnection(MQServer mqserver)
        {
            try
            {
                string[] retnString = MQUtils.ParsingMQChannelInfo(mqserver.MQChannelInfoListener);
                var queueProperties = new Hashtable();
                queueProperties[MQC.HOST_NAME_PROPERTY] = retnString[2];
                queueProperties[MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim());
                queueProperties[MQC.CHANNEL_PROPERTY] = retnString[0];
                //FCB 不啟用
                //queueProperties[MQC.USER_ID_PROPERTY] = "MUSR_MQADMIN";
                //queueProperties[MQC.PASSWORD_PROPERTY] = "";
                SetMessageText(RichTextBoxProcessMessage, $"Send Information Host[{retnString[2]}],PortNumber[{Convert.ToInt32(retnString[3].Trim())}],Channel[{retnString[0]}]", Brushes.Black);
                _mqSendQMgr = new MQQueueManager(mqserver.MQQueueManagerListener, queueProperties);
                _mqSendQueue = _mqSendQMgr.AccessQueue(mqserver.MQPutQueueName, QueueSendOptions);
                //Declare Receive Queue
                retnString = MQUtils.ParsingMQChannelInfo(mqserver.MQChannelInfoListener);
                queueProperties = new Hashtable();
                queueProperties[MQC.HOST_NAME_PROPERTY] = retnString[2];
                queueProperties[MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim());
                queueProperties[MQC.CHANNEL_PROPERTY] = retnString[0];
                //queueProperties[MQC.USER_ID_PROPERTY] = "MUSR_MQADMIN";
                //queueProperties[MQC.PASSWORD_PROPERTY] = "";
                SetMessageText(RichTextBoxProcessMessage, $"Receive Information Host[{retnString[2]}],PortNumber[{Convert.ToInt32(retnString[3].Trim())}],Channel[{retnString[0]}]", Brushes.Black);
                _mqReceiveQMgr = new MQQueueManager(mqserver.MQQueueManagerListener, queueProperties);
                _mqReceiveQueue = _mqReceiveQMgr.AccessQueue(mqserver.MQGetQueueName, QueueReceiveOptions);
                return (0);
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                return (-1);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal($"Exception[{ex.Message}]");
                return (-2);
            }
        }

        private int CreateMQConnection()
        {
            try
            {
                //Declare Send Queue
                string[] retnString = MQUtils.ParsingMQChannelInfo(TextBoxSendChannelInfo.Text.Trim());
                var queueProperties = new Hashtable
                {
                    //[MQC.TRANSPORT_PROPERTY] = MQC.TRANSPORT_PROPERTY,
                    //[MQC.TRANSPORT_MQSERIES_MANAGED]=MQC.TRANSPORT_MQSERIES_MANAGED,
                    [MQC.HOST_NAME_PROPERTY] = retnString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = retnString[0],
                    // [MQC.CONNECTION_NAME_PROPERTY] = $"{retnString[2]}({Convert.ToInt32(retnString[3].Trim())})",
                    //[MQC.CONNECT_OPTIONS_PROPERTY] = MQC.MQCNO_RECONNECT_Q_MGR
                    //  [MQC.CCSID_PROPERTY] = 1208,
                    // [MQC.USER_ID_PROPERTY] = "",
                    // [MQC.PASSWORD_PROPERTY] = ""
                };
                SetMessageText(RichTextBoxProcessMessage, $"Send Information", Brushes.Black);
                SetMessageText(RichTextBoxProcessMessage, $"Host[{retnString[2]}]", Brushes.Black);
                SetMessageText(RichTextBoxProcessMessage, $"PortNumber[{Convert.ToInt32(retnString[3].Trim())}]", Brushes.Black);
                SetMessageText(RichTextBoxProcessMessage, $"Channel[{retnString[0]}]", Brushes.Black);
                _mqSendQMgr = new MQQueueManager(TextBoxSendQueueManagerName.Text.Trim(), queueProperties);
                _mqSendQueue = _mqSendQMgr.AccessQueue(TextBoxSendQueueName.Text.Trim(), QueueSendOptions);
                //Declare Receive Queue
                retnString = MQUtils.ParsingMQChannelInfo(TextBoxReceiveChannelInfo.Text.Trim());
                queueProperties = new Hashtable
                {
                    //[MQC.TRANSPORT_PROPERTY] = MQC.TRANSPORT_PROPERTY,
                    //[MQC.TRANSPORT_MQSERIES_MANAGED] = MQC.TRANSPORT_MQSERIES_MANAGED,
                    [MQC.HOST_NAME_PROPERTY] = retnString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = retnString[0],
                    //[MQC.CONNECTION_NAME_PROPERTY] = $"{retnString[2]}({Convert.ToInt32(retnString[3].Trim())})",
                    //[MQC.CONNECT_OPTIONS_PROPERTY] = MQC.MQCNO_RECONNECT_Q_MGR
                    //[MQC.USER_ID_PROPERTY] = "MUSR_MQADMIN",
                    //[MQC.PASSWORD_PROPERTY] = ""
                };
                SetMessageText(RichTextBoxProcessMessage, $"Receive Information Host[{retnString[2]}],PortNumber[{Convert.ToInt32(retnString[3].Trim())}],Channel[{retnString[0]}]", Brushes.Black);
                _mqReceiveQMgr = new MQQueueManager(TextBoxReceiveQueueManagerName.Text.Trim(), queueProperties);
                _mqReceiveQueue = _mqReceiveQMgr.AccessQueue(TextBoxReceiveQueueName.Text.Trim(), QueueReceiveOptions);
                return (0);
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                return (-1);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal($"Exception[{ex.Message}]");
                return (-2);
            }
        }
        private void ReleaseMQObject()
        {
            try
            {
                if (_mqSendQueue != null)
                {
                    if (_mqSendQueue.OpenStatus)
                        _mqSendQueue.Close();
                }
                if (_mqSendQMgr != null)
                {
                    if (_mqSendQMgr.IsConnected)
                        _mqSendQMgr.Disconnect();
                }
                if (_mqReceiveQueue != null)
                {
                    if (_mqReceiveQueue.OpenStatus)
                        _mqReceiveQueue.Close();
                }
                if (_mqReceiveQMgr != null)
                {
                    if (_mqReceiveQMgr.IsConnected)
                        _mqReceiveQMgr.Disconnect();
                }
            }
            catch (MQException mqex)
            {
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
            }
            catch (Exception ex)
            {
                Log.Fatal($"Exception[{ex.Message}]");
            }
        }

        private string GetSendQueueData()
        {
            string selectFilePath = TextBoxSendSelectFileName.Text.Trim();
            string content = string.Empty;
            try
            {
                //read file encoding 950
                byte[] readBuffer = File.ReadAllBytes(selectFilePath);
                string readString = System.Text.Encoding.Default.GetString(readBuffer);
                var sb = new StringBuilder();
                sb.AppendLine($"Read Test MQ Data at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}")
                  .AppendLine("----- Begin Here -----")
                  .AppendLine(readString)
                  .AppendLine("----- End   Here -----");
                SetMessageText(RichTextBoxProcessMessage, sb.ToString(), Brushes.Black);
                return (readString);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, "Exception :" + ex.Message, Brushes.Red);
                return null;
            }
        }

        private void MQSend(string queueData)
        {
            try
            {
                //Create RequestMessage
                var sendMessage = new MQMessage
                {
                    CharacterSet = 1208,
                    //Encoding = Encoding.UTF8.CodePage,
                    Format = MQC.MQFMT_STRING
                };
                SetMessageText(RichTextBoxProcessMessage, $"Send Queue data [{queueData}]", Brushes.Red);
                sendMessage.WriteString(queueData);
                // sendMessage.CorrelationId = MQC.MQCI_NONE;
                // sendMessage.MessageId = MQC.MQCI_NONE;
                var correlationId = $"{"FCB4D"}{DateTime.Now:yyyyMMddHHmmssfffff}";
                sendMessage.CorrelationId = Encoding.UTF8.GetBytes(correlationId);
                sendMessage.MessageId = sendMessage.CorrelationId;
                SetMessageText(RichTextBoxProcessMessage, $"ReplyToQueueName[{TextBoxReceiveQueueName.Text.Trim()}], ReplyToQueueManagerName[{TextBoxReceiveQueueManagerName.Text.Trim()}]", Brushes.Red);
                SetMessageText(RichTextBoxProcessMessage, $"Put CorrelationId [{correlationId}]", Brushes.Red);
                sendMessage.ReplyToQueueName = TextBoxReceiveQueueName.Text.Trim();
                sendMessage.ReplyToQueueManagerName = TextBoxReceiveQueueManagerName.Text.Trim();
                sendMessage.Persistence = MQC.MQPER_NOT_PERSISTENT;
                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_NO_SYNCPOINT +
                              MQC.MQPMO_DEFAULT_CONTEXT
                };
                SetMessageText(RichTextBoxProcessMessage, $"Send Message To Queue Name[{_mqSendQueue.Name.Trim()}] For Queue Manager Name[{_mqSendQueue.ConnectionReference.Name.Trim()}]", Brushes.Red);
                _mqSendQueue.Put(sendMessage, pmo);
                SetMessageText(RichTextBoxProcessMessage, $"sendMessage Message MessageId [{System.Text.Encoding.Default.GetString(sendMessage.MessageId)}]", Brushes.Black);
                SetMessageText(RichTextBoxProcessMessage, $"sendMessage Message CorrelationId [{System.Text.Encoding.Default.GetString(sendMessage.CorrelationId)}]", Brushes.Black);
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", mqex);
                return;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal($"Exception[{ex.Message}]");
                return;
            }
        }

        private string MQReceive(MQQueue mqQueue, int waitInterval = 1000)
        {
            try
            {
                //Create ReplyMessage
                var receiveMessage = new MQMessage
                {
                    CharacterSet = 1208
                };
                //replyMessage.CorrelationId = requestMessage.MessageId;
                var gmo = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT,
                    WaitInterval = waitInterval
                };
                Log.Info($"Receive Message From Queue Name[{_mqReceiveQueue.Name.Trim()}] For Queue Manager Name[{_mqReceiveQueue.ConnectionReference.Name.Trim()}]", Brushes.Black);
                mqQueue.Get(receiveMessage, gmo);
                string replyString = receiveMessage.ReadString(receiveMessage.MessageLength);
                return (replyString);
            }
            catch (MQException mqex)
            {
                if (mqex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    Log.Info($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                    return string.Empty;
                }
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal($"Exception[{ex.Message}]");
                return null;
            }
        }

        private string MQReceive()
        {
            try
            {
                //Create ReplyMessage
                var receiveMessage = new MQMessage
                {
                    CharacterSet = 1208
                };
                //replyMessage.CorrelationId = requestMessage.MessageId;
                var gmo = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT,
                    WaitInterval = TextBoxSendMilliSecOfGet.IsEnabled == false ? MQC.MQWI_UNLIMITED : Convert.ToInt16(TextBoxSendMilliSecOfGet.Text.Trim())
                };
                SetMessageText(RichTextBoxProcessMessage, $"Receive Message From Queue Name[{_mqReceiveQueue.Name.Trim()}] For Queue Manager Name[{_mqReceiveQueue.ConnectionReference.Name.Trim()}]", Brushes.Black);
                _mqReceiveQueue.Get(receiveMessage, gmo);
                SetMessageText(RichTextBoxProcessMessage, $"Receive Message MessageId [{System.Text.Encoding.UTF8.GetString(receiveMessage.MessageId)}]", Brushes.Black);
                SetMessageText(RichTextBoxProcessMessage, $"Receive Message CorrelationId [{System.Text.Encoding.UTF8.GetString(receiveMessage.CorrelationId)}]", Brushes.Black);
                string replyString = receiveMessage.ReadString(receiveMessage.MessageLength);
                SetMessageText(RichTextBoxProcessMessage, $"Receive Message [{replyString}]", Brushes.Black);
                return (replyString);
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                if (mqex.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
                {
                    Log.Info($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                    return string.Empty;
                }
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                return null;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal($"Exception[{ex.Message}]");
                return null;
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _isStopReceive = true;
            ReleaseMQObject();
            BtnInitial.IsEnabled = true;
            BtnReset.IsEnabled = false;
            BtnSend.IsEnabled = false;
            BtnCreateVDMQMessage.IsEnabled = false;
            BtnOneReceive.IsEnabled = false;
            BtnForeverReceive.IsEnabled = false;
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                string sendMessageString = GetSendQueueData();
                if (string.IsNullOrEmpty(sendMessageString))
                {
                    SetMessageText(RichTextBoxProcessMessage, $"Send & Receive Message Queue Complete at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    return;
                }
                MQSend(sendMessageString);
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal($"Exception[{ex.Message}]");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialogFileName = new OpenFileDialog();
            openFileDialogFileName.FileName = string.Empty;
            //openFileDialogFileName.InitialDirectory = TextBoxSendSelectFileName.Text;
            if (openFileDialogFileName.ShowDialog() == true)
            {
                TextBoxSendSelectFileName.Text = openFileDialogFileName.FileName;
            }
        }

        private void BtnOneReceive_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            BtnOneReceive.IsEnabled = false;
            BtnForeverReceive.IsEnabled = false;
            try
            {
                string receiveMessageString = MQReceive();
                if (string.IsNullOrEmpty(receiveMessageString))
                {
                    SetMessageText(RichTextBoxProcessMessage, $"Receive Message Queue Complete at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    BtnOneReceive.IsEnabled = true;
                    BtnForeverReceive.IsEnabled = true;
                    return;
                }
                var sb = new StringBuilder();
                sb.AppendLine($"Read Message Data at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}")
                  .AppendLine("----- Begin Here -----")
                  .AppendLine(receiveMessageString)
                  .AppendLine("----- End   Here -----");
                SetMessageText(RichTextBoxLogMessage, sb.ToString(), Brushes.Black);
                BtnOneReceive.IsEnabled = true;
                BtnForeverReceive.IsEnabled = true;
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Black);
                Log.Fatal($"Exception[{ex.Message}]");
            }
            finally
            {
                BtnOneReceive.IsEnabled = true;
                BtnForeverReceive.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnInitial_Click(object sender, RoutedEventArgs e)
        {
            var returnString = CreateVDRequestMessage();
            SetMessageText(RichTextBoxProcessMessage, $"[{returnString}]", Brushes.Red);
            SetMessageText(RichTextBoxProcessMessage, $"Data[{returnString}], Length[{returnString.Length}], Byte Count{Encoding.Default.GetByteCount(returnString)}], Bytes Length[{Encoding.Default.GetBytes(returnString).Length}]", Brushes.Red);
            if (InitialRoutine() != 0)
            {
                SetMessageText(RichTextBoxProcessMessage, "Initial MQ Error", Brushes.Red);
                return;
            }
            BtnInitial.IsEnabled = false;
            BtnReset.IsEnabled = true;
            BtnSend.IsEnabled = true;
            BtnCreateVDMQMessage.IsEnabled = true;
            BtnOneReceive.IsEnabled = true;
            BtnForeverReceive.IsEnabled = true;
        }

        private async void BtnForeverReceive_Click(object sender, RoutedEventArgs e)
        {
            _isStopReceive = false;
            var queueManagerName = TextBoxReceiveQueueManagerName.Text.Trim();
            var queueName = TextBoxReceiveQueueName.Text.Trim();
            var channelInfo = TextBoxReceiveChannelInfo.Text.Trim();
            BtnOneReceive.IsEnabled = false;
            BtnForeverReceive.IsEnabled = false;
            await Task.Run(() => StartForeverReceive(queueManagerName, queueName, channelInfo));
        }

        private void StartForeverReceive(string queueManagerName, string queueName, string channelInfo)
        {
            //Declare Receive Queue
            var retnString = MQUtils.ParsingMQChannelInfo(channelInfo);
            var queueProperties = new Hashtable();
            queueProperties[MQC.HOST_NAME_PROPERTY] = retnString[2];
            queueProperties[MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim());
            queueProperties[MQC.CHANNEL_PROPERTY] = retnString[0];
            // queueProperties[MQC.CCSID_PROPERTY] = 1208;
            // queueProperties[MQC.USER_ID_PROPERTY] = "MUSR_MQADMIN";
            //queueProperties[MQC.PASSWORD_PROPERTY] = "";
            //SetMessageText(RichTextBoxProcessMessage, $"Receive Information Host[{retnString[2]}],PortNumber[{Convert.ToInt32(retnString[3].Trim())}],Channel[{retnString[0]}]", Brushes.Black);
            var mqReceiveQMgr = new MQQueueManager(queueManagerName, queueProperties);
            var mqReceiveQueue = _mqReceiveQMgr.AccessQueue(queueName, QueueReceiveOptions);
            while (!_isStopReceive)
            {
                string receiveMessageString = MQReceive(mqReceiveQueue);
                if (receiveMessageString == null)
                {
                    Log.Info($"Receive Message Queue Complete at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    break;
                }
                if (receiveMessageString.Equals(string.Empty))
                {
                    Log.Info($"Receive Message Queue Empty at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    continue;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Read Message Data at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}")
                  .AppendLine("----- Begin Here -----")
                  .AppendLine(receiveMessageString)
                  .AppendLine("----- End   Here -----");
                Dispatcher.Invoke(() =>
                {
                    RichTextBoxLogMessage.AppendText(sb.ToString());
                });
                //var op = Dispatcher.BeginInvoke((Action)(() =>
                //{
                //    RichTextBoxLogMessage.AppendText(sb.ToString());
                //}));

                Log.Info(sb.ToString());
            }
        }

        private void BtnCreateVDMQMessage_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Random random = new Random();
            ulong randomNumber;
            try
            {
                if (!int.TryParse(TextBoxRepeatCount.Text.Trim(), out var repeatCount))
                {
                    SetMessageText(RichTextBoxProcessMessage, $"Input repeat count[{TextBoxRepeatCount.Text.Trim()}] not numeric", Brushes.Red);
                    TextBoxRepeatCount.Focus();
                    return;
                }
                string sendMessageString = GetSendQueueData();
                if (string.IsNullOrEmpty(sendMessageString))
                {
                    SetMessageText(RichTextBoxProcessMessage, $"Send & Receive Message Queue Complete at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                    return;
                }

                //XmlSerializer serializer = new XmlSerializer(typeof(VDMessage.RequestXML));
                //StringReader stringReader = new StringReader(sendMessageString);
                for (var loop = 1; loop <= repeatCount; loop++)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(VDMessage.RequestXML));
                    StringReader stringReader = new StringReader(sendMessageString);
                    var requestXML = (VDMessage.RequestXML)serializer.Deserialize(stringReader);
                    var cardLinex = random.Next(0, CardInfosArray.Length);
                    //head
                    randomNumber = (ulong)random.Next(0, 999999);
                    requestXML.RequestHeader.TxSeqNo = DateTime.Now.ToString("yyyyMMdd") + randomNumber.ToString("D6");

                    randomNumber = (ulong)random.Next(0, 99999);
                    requestXML.RequestHeader.MsgSeqNo = randomNumber.ToString("D5");

                    requestXML.RequestHeader.CltTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                    randomNumber = (ulong)random.Next(0, 99999999);
                    requestXML.RequestHeader.CustID = randomNumber.ToString("D8");

                    requestXML.RequestHeader.UserID = "0000";

                    requestXML.RequestHeader.AcctNo = CardInfosArray[cardLinex].AcctNbr;
                    //detail
                    requestXML.TxRq.AcctNo = CardInfosArray[cardLinex].AcctNbr;

                    randomNumber = (ulong)random.Next(0, 99999999);
                    requestXML.TxRq.CardNo = randomNumber.ToString("D8");

                    requestXML.TxRq.CreditNo = CardInfosArray[cardLinex].CardNbr;

                    randomNumber = (ulong)random.Next(0, 999999);
                    requestXML.TxRq.AuthNo = randomNumber.ToString("D6");

                    randomNumber = (ulong)random.Next(0, 9999);
                    requestXML.TxRq.TxAmt = randomNumber.ToString() + "00";

                    var sendMQMessageString = ColaCommUtility.ObjectToXMLString<CommonLib.MQ.VDMessage.RequestXML>(requestXML, true);
                    SetMessageText(RichTextBoxProcessMessage, $"Count[{loop}], Data[{sendMQMessageString}]", Brushes.Red);
                    Task.Run(() => MQSend(sendMQMessageString));
                }
            }
            catch (MQException mqex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]", Brushes.Red);
                Log.Fatal($"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxProcessMessage, $"Exception[{ex.Message}]", Brushes.Red);
                Log.Fatal($"Exception[{ex.Message}]");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private string CreateVDRequestMessage()
        {

            var vdMessage = new CommonLib.MQ.VDMessage.RequestXML();
            var header = new CommonLib.MQ.VDMessage.RequestHeader();
            header.TxID = "TX_FCB91125S";
            header.SystemKey = "FCB4B";
            // header.TxSeqNo = "20230220000021";
            header.TxSeqNo = DateTime.Now.ToString("MMddHHmmssfff");
            header.MsgSeqNo = "00001";
            header.CltTimeStamp = "2023 - 02 - 20 09:25:00.000";
            header.CustID = "";
            header.UserID = "";
            header.AcctNo = "";
            header.MsgDirection = "RQ";
            vdMessage.RequestHeader = header;
            var txRq = new CommonLib.MQ.VDMessage.TxRq();
            txRq.FuncCode = "1";
            txRq.AcctNo = "4563123456789012";
            txRq.CardNo = "00000001";
            txRq.CreditNo = "4563123456789012";
            txRq.AuthNo = "123456";
            txRq.TxAmt = "1000.00";
            vdMessage.TxRq = txRq;
            var xmlString = String.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE TX_FCB91125S>{0}",
              ColaCommUtility.ObjectToXMLString<CommonLib.MQ.VDMessage.RequestXML>(vdMessage, true));
            return xmlString;
        }

        private class CardInfo
        {
            public string AcctNbr { get; set; }
            public string CardNbr { get; set; }
        }
    }
}


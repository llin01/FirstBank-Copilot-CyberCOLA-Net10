using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using IBM.WMQ;
using NLog;

namespace Com.Cybersoft4u.CommonLib.MQ
{
    public static class MQUtils
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion //NLog
        /// <summary>
        /// Get IPAddress By HostName
        /// </summary>
        ///<param name="mqChannelInfo">MQ ChannelInformation</param>
        ///<returns>string Array</returns> 
        public static string[] ParsingMQChannelInfo(string mqChannelInfo)
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
                return null;
            }
        }

        /// <summary>
        /// Sends a message to MQ and receives the reply message.
        /// </summary>
        /// <param name="mqServer">The MQ server configuration.</param>
        /// <param name="queueData">The data to send to the queue.</param>
        /// <returns>The reply message string, or null if an error occurred.</returns>
        public static string MQMessageSendReceive(MQServer mqServer, string queueData)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            MQQueueManager mqQMgr = null;
            MQQueue putQueue = null;
            MQQueue getQueue = null;
            try
            {
                if (string.IsNullOrEmpty(queueData))
                {
                    Log.Error("MQMessageSendReceive input queueData Invalid");
                    return null;
                }
                queueData = String.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE TX_FCB91125S>{0}", queueData);
                var retnString = ParsingMQChannelInfo(mqServer.MQChannelInfoListener);
                if (retnString == null)
                {
                    Log.Error("ParsingMQChannelInfo Parsing Invalid");
                    return null;
                }
                //forcbcard/TCP/HostName(1415)
                var queueProperties = new Hashtable
                {
                    [MQC.HOST_NAME_PROPERTY] = retnString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = retnString[0] //,
                    //[MQC.CCSID_PROPERTY] = Convert.ToInt32(mqServer.MQServerCCSID.Trim())
                };
                if (!string.IsNullOrEmpty(mqServer.MQUserName))
                {
                    queueProperties[MQC.USER_ID_PROPERTY] = mqServer.MQUserName;
                    queueProperties[MQC.PASSWORD_PROPERTY] = "";
                }

                //queueProperties[MQC.USER_ID_PROPERTY] = mqServer.MQUserName;
                //queueProperties[MQC.PASSWORD_PROPERTY] = "";
                mqQMgr = new MQQueueManager(mqServer.MQQueueManagerListener, queueProperties);
                const int getOpenOptions = MQC.MQOO_INPUT_AS_Q_DEF // open queue for input
                                         | MQC.MQOO_FAIL_IF_QUIESCING; // but not if MQM stopping
                const int putOpenOptions = MQC.MQOO_OUTPUT; // open queue for output
                putQueue = mqQMgr.AccessQueue(mqServer.MQPutQueueName, putOpenOptions);
                getQueue = mqQMgr.AccessQueue(mqServer.MQGetQueueName, getOpenOptions);

                //Create RequestMessage
                var putMessage = new MQMessage
                {
                    CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                    Format = MQC.MQFMT_STRING
                };
                putMessage.WriteString(queueData);
                //var correlationIdSeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                //var correlationId = $"{"FCB4D"}{correlationIdSeq.Substring(3, 19)}";
                var correlationId = $"{"FCB4D"}{DateTime.Now:yyyyMMddHHmmssfffff}";
                putMessage.CorrelationId = Encoding.UTF8.GetBytes(correlationId);
                putMessage.MessageId = putMessage.CorrelationId;
                putMessage.MessageType = MQC.MQMT_REQUEST;
                putMessage.Report = MQC.MQRO_COPY_MSG_ID_TO_CORREL_ID;
                putMessage.ReplyToQueueName = mqServer.MQGetQueueName;
                putMessage.ReplyToQueueManagerName = mqServer.MQQueueManagerListener;
                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_FAIL_IF_QUIESCING
                };
                Log.Info("Put Message To Queue Message[{0}]", queueData);
                Log.Info("Put Message To Queue Name[{0}] For Queue Manager Name[{1}] Put Queue CorrelationId[{2}]", putQueue.Name.Trim(), putQueue.ConnectionReference.Name.Trim(), correlationId);
                putQueue.Put(putMessage, pmo);
                //Create ReplyMessage
                var getMessage = new MQMessage
                {
                    CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                    Format = MQC.MQFMT_STRING,
                    CorrelationId = putMessage.CorrelationId,
                    MessageId = putMessage.MessageId
                };
                var gmo = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT + MQC.MQGMO_FAIL_IF_QUIESCING,
                    WaitInterval = Convert.ToInt32(mqServer.MQWaitIntervalMilliSec.Trim()),
                    //MatchOptions = MQC.MQMO_MATCH_MSG_ID
                    MatchOptions = MQC.MQMO_MATCH_CORREL_ID
                };
                Log.Info("Get Message From Queue Name[{0}] For Queue Manager Name[{1}]", getQueue.Name.Trim(), getQueue.ConnectionReference.Name.Trim());
                getQueue.Get(getMessage, gmo);
                string replyString = getMessage.ReadString(getMessage.MessageLength);
                return (replyString);
            }
            catch (MQException mqex)
            {
                Log.Fatal(mqex, $"MQMessageSendReceive MQException[{mqex.Message}],Complete Code [{mqex.CompCode}],Reason Code[{mqex.Reason}]");
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"MQMessageSendReceive Exception:{ex.Message}");
                return null;
            }
            finally
            {
                try
                {
                    if (putQueue != null)
                    {
                        if (putQueue.OpenStatus)
                            putQueue.Close();
                    }
                    if (getQueue != null)
                    {
                        if (getQueue.OpenStatus)
                            getQueue.Close();
                    }
                    if (mqQMgr != null)
                    {
                        if (mqQMgr.IsConnected)
                            mqQMgr.Disconnect();
                    }
                }
                catch (MQException mqex)
                {
                    Log.Fatal(mqex, $"MQMessageSendReceive MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"MQMessageSendReceive Exception:{ex.Message}");
                }
            }
        }

        /// <summary>
        /// the message send to reply.
        /// </summary>
        /// <param name="mqServer">The mq server.</param>
        /// <param name="queueData">The queue data.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <returns></returns>
        public static int MQMessageSendToReply(MQServer mqServer, string queueData, byte[] messageId)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            MQQueueManager mqQMgr = null;
            MQQueue putQueue = null;
            try
            {
                if (string.IsNullOrEmpty(queueData))
                {
                    Log.Error("MQMessageSendToReply input queueData Invalid");
                    return -1;
                }
                queueData = String.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE TX_FCB91125S>{0}", queueData);
                var retnString = ParsingMQChannelInfo(mqServer.MQChannelInfoListener);
                if (retnString == null)
                {
                    Log.Error("ParsingMQChannelInfo Parsing Invalid");
                    return -2;
                }
                //PATSB02.SVRCONN/TCP/PATSB02-152(1414)
                var queueProperties = new Hashtable
                {
                    [MQC.HOST_NAME_PROPERTY] = retnString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = retnString[0] //,
                    //[MQC.CCSID_PROPERTY] = Convert.ToInt32(mqServer.MQServerCCSID.Trim())
                };
                if (!string.IsNullOrEmpty(mqServer.MQUserName))
                {
                    queueProperties[MQC.USER_ID_PROPERTY] = mqServer.MQUserName;
                    queueProperties[MQC.PASSWORD_PROPERTY] = "";
                }

                mqQMgr = new MQQueueManager(mqServer.MQQueueManagerListener, queueProperties);
                const int putOpenOptions = MQC.MQOO_OUTPUT
                                         | MQC.MQOO_FAIL_IF_QUIESCING; // open queue for output
                putQueue = mqQMgr.AccessQueue(mqServer.MQPutQueueName, putOpenOptions);

                //Write Queue To Reply Queue
                MQMessage putMessage = new MQMessage
                {
                    CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                    Format = MQC.MQFMT_STRING,
                    MessageType = MQC.MQMT_REPLY,
                    MessageId = messageId
                };
                putMessage.WriteString(queueData);
                MQPutMessageOptions pmo = new MQPutMessageOptions();
                //pmo.Options = MQC.MQPMO_NEW_MSG_ID | MQC.MQPMO_NEW_CORREL_ID | MQC.MQPMO_FAIL_IF_QUIESCING;
                Log.Info("Put Message To Queue Name[{0}] For Queue Manager Name[{1}]", putQueue.Name.Trim(), putQueue.ConnectionReference.Name.Trim());
                putQueue.Put(putMessage, pmo);
                return (0);
            }
            catch (MQException mqex)
            {
                Log.Fatal(mqex, $"MQException[{mqex.Message}],Complete Code [{mqex.CompCode}],Reason Code[{mqex.Reason}]");
                return (-3);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Exception[{ex.Message}]");
                return (-4);
            }
            finally
            {
                try
                {
                    if (putQueue != null)
                    {
                        if (putQueue.OpenStatus)
                            putQueue.Close();
                    }
                    if (mqQMgr != null)
                    {
                        if (mqQMgr.IsConnected)
                            mqQMgr.Disconnect();
                    }
                }
                catch (MQException mqex)
                {
                    Log.Fatal(mqex, $"MQMessageSendToReply MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"MQMessageSendToReply Exception:{ex.Message}");
                }
            }
        }

        /// <summary>
        /// the message send to VD MQ.
        /// </summary>
        /// <param name="mqServer">The mq server.</param>
        /// <param name="queueData">The queue data.</param>
        /// <returns></returns>
        public static int MQMessageSendToVDMQ(MQServer mqServer, string queueData)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            MQQueueManager mqQMgr = null;
            MQQueue putQueue = null;
            try
            {
                string[] retnString = ParsingMQChannelInfo(mqServer.MQChannelInfoListener);
                if (retnString == null)
                {
                    return -1;
                }
                if (string.IsNullOrEmpty(queueData))
                {
                    return -2;
                }
                queueData = String.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE TX_FCB91125S>{0}", queueData);
                //PATSB02.SVRCONN/TCP/PATSB02-152(1414)
                var queueProperties = new Hashtable
                {
                    [MQC.HOST_NAME_PROPERTY] = retnString[2],
                    [MQC.PORT_PROPERTY] = Convert.ToInt32(retnString[3].Trim()),
                    [MQC.CHANNEL_PROPERTY] = retnString[0] //,
                                                           //  [MQC.CCSID_PROPERTY] = Convert.ToInt32(mqServer.MQServerCCSID.Trim())
                };
                if (!string.IsNullOrEmpty(mqServer.MQUserName))
                {
                    queueProperties[MQC.USER_ID_PROPERTY] = mqServer.MQUserName;
                    queueProperties[MQC.PASSWORD_PROPERTY] = "";
                }

                mqQMgr = new MQQueueManager(mqServer.MQQueueManagerListener, queueProperties);
                const int putOpenOptions = MQC.MQOO_OUTPUT
                                         | MQC.MQOO_FAIL_IF_QUIESCING; // open queue for output
                putQueue = mqQMgr.AccessQueue(mqServer.MQPutQueueName, putOpenOptions);

                //Create RequestMessage
                var putMessage = new MQMessage
                {
                    CharacterSet = Convert.ToInt32(mqServer.MQClientCCSID.Trim()),
                    Format = MQC.MQFMT_STRING
                };
                putMessage.WriteString(queueData);
                putMessage.CorrelationId = MQC.MQCI_NONE;
                putMessage.MessageId = MQC.MQCI_NONE;
                putMessage.ReplyToQueueName = mqServer.MQGetQueueName;
                putMessage.ReplyToQueueManagerName = mqServer.MQQueueManagerListener;
                putMessage.Persistence = MQC.MQPER_NOT_PERSISTENT;
                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_NO_SYNCPOINT + MQC.MQPMO_DEFAULT_CONTEXT
                };
                Log.Info("Put Message To Queue Name[{0}] For Queue Manager Name[{1}]", putQueue.Name.Trim(), putQueue.ConnectionReference.Name.Trim());
                putQueue.Put(putMessage, pmo);

                return (0);
            }
            catch (MQException mqex)
            {
                Log.Fatal(mqex, $"MQMessageSendToPRISM MQException[{mqex.Message}],Complete Code [{mqex.CompCode}],Reason Code[{mqex.Reason}]");
                return (-1);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"MQMessageSendToPRISM Exception:{ex.Message}");
                return (-2);
            }
            finally
            {
                try
                {
                    if (putQueue != null)
                    {
                        if (putQueue.OpenStatus)
                            putQueue.Close();
                    }
                    if (mqQMgr != null)
                    {
                        if (mqQMgr.IsConnected)
                            mqQMgr.Disconnect();
                    }
                }
                catch (MQException mqex)
                {
                    Log.Fatal(mqex, $"MQException[{mqex.Message}], Reason Code[{mqex.ReasonCode}]");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"Exception[{ex.Message}]");
                }
            }
        }
    }
}

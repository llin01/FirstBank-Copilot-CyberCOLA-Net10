using System;
using System.Net.Sockets;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Newtonsoft.Json;
using NLog;

namespace Com.Cybersoft4u.ServiceProvider.IDVerifyServerService
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public class Token : IDisposable
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly Socket _connection;

        private readonly byte[] _receiveBuffer;

        private int _currentIndex;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="connection">Socket to accept incoming data.</param>
        /// <param name="bufferSize">Buffer size for accepted data.</param>
        internal Token(Socket connection, int bufferSize)
        {
            _connection = connection;
            _receiveBuffer = new byte[bufferSize];
        }

        /// <summary>
        /// Accept socket.
        /// </summary>
        internal Socket Connection => _connection;

        /// <summary>
        /// Process data received from the client.
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs used in the operation.</param>
        internal void ProcessData(SocketAsyncEventArgs args)
        {
            try
            {
                // Get the message received from the client.
                byte[] receiveBytes = new byte[_currentIndex];
                Buffer.BlockCopy(_receiveBuffer, 0, receiveBytes, 0, _currentIndex);
                var receiveString = Encoding.Default.GetString(receiveBytes);
                Log.Info($"Receiver: [{receiveString}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has read {_currentIndex} bytes.");
                //Process Receive EAI Message
                //Format Check Message
                //Convert Incoming Message to Class
                var reqJSONObject = JsonConvert.DeserializeObject<OASVRFY1.OASVRFY1_REQ>(receiveString);
                if (reqJSONObject == null)
                {
                    var sendBytes = EAICommUtility.CreateIDVerifyErrorMessage();
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    Log.Info($"Receive Message Format Error, Sender: [{Encoding.Default.GetString(sendBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                    return;
                }
                var checkRequestMessageFormat = EAICommUtility.CheckOASVRFY1MessageFormat(reqJSONObject);
                if (checkRequestMessageFormat != SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess)
                {
                    var sendBytes = EAICommUtility.ConvertOASVRFY1Incoming2Outgoing(reqJSONObject, checkRequestMessageFormat);
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    Log.Info($"Receive Message Format Error, Sender: [{Encoding.Default.GetString(sendBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                    return;
                }
                if (Log.IsDebugEnabled)
                {
                    Log.Info("----- Start Display ID Verify Incoming Message -----");
                    Log.Info(receiveString);
                    Log.Info("----- End   Display ID Verify Incoming Message -----");
                }
                //Convert ID Verify Message to FISC
                var fisc = EAICommUtility.ConvertOASVRFY1Message2FISC(reqJSONObject);
                if (fisc == null)
                {
                    Log.Warn("Convert Incoming Message To FISC Error");
                    var sendBytes = EAICommUtility.ConvertOASVRFY1Incoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.SystemError);
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    Log.Warn(($"Sender: [{Encoding.ASCII.GetString(sendBytes)}]. The server has send {sendBytes.Length} bytes."));
                    return;
                }
                //Create Transaction Log Key
                string txKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                //Send FISC(Update this)
                var processBytes = ProcessSendFISC(fisc, reqJSONObject, txKeySeq);
                args.SetBuffer(processBytes, 0, processBytes.Length);
                Log.Info(($"Sender: [{Encoding.Default.GetString(processBytes)}]. The server has send {processBytes.Length} bytes."));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ProcessData Exception:{ex.Message}");
            }
            finally
            {
                // Reset Index, so it can receive more data from a keep-alive connection client.
                _currentIndex = 0;
            }
        }

        /// <summary>
        /// Set data received from the client.
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs used in the operation.</param>
        internal void SetData(SocketAsyncEventArgs args)
        {
            Int32 count = args.BytesTransferred;

            if (_currentIndex + count > _receiveBuffer.Length)
            {
                throw new ArgumentOutOfRangeException($"count Adding {count} bytes on buffer which has {_currentIndex} bytes, the listener buffer will overflow.");
            }

            Buffer.BlockCopy(args.Buffer, args.Offset, _receiveBuffer, _currentIndex, count);
            _currentIndex += count;
        }

        /// <summary></summary>
        /// <param name="requestMessage"></param>
        /// <param name="eaiXTWAIncomingMessageStruct"></param>
        /// <param name="txKeySeq"></param>
        private byte[] ProcessSendFISC(FISC requestMessage, OASVRFY1.OASVRFY1_REQ reqJSONObject, string txKeySeq)
        {
            try
            {
                Log.Info($"ProcessSendFISC, LocalEndPoint[{_connection.LocalEndPoint.ToString()}, RemoteEndPoint[{_connection.RemoteEndPoint.ToString()}, Key[{txKeySeq}]");
                FISC responseMessage = null;
                if (CyberAuthFISCIDVerifyResource.CyberAuthFISCIDVerify == null)
                {
                    Log.Error($"CyberAuthFISCIDVerifyResource.CyberAuthFISCIDVerify Not Create, Can Not Send To FISC ID Verification");
                    var sendBytes = EAICommUtility.ConvertOASVRFY1Incoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.SystemError);
                    return sendBytes;
                }
                else
                {
                    responseMessage = CyberAuthFISCIDVerifyResource.CyberAuthFISCIDVerify.ProcessIDVerifyMessage(requestMessage, _connection.RemoteEndPoint.ToString(), txKeySeq);
                }
                if (responseMessage == null)
                {
                    Log.Warn("Not Get ID Verify Return Message");
                    var sendBytes = EAICommUtility.ConvertOASVRFY1Incoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.SystemError);
                    return sendBytes;
                }
                else
                {
                    string reponseCode = string.Format("@isoP38AuthCode[{0}]-@isoP39Rsp[{1}]", $"{responseMessage.Core().Field(FIELD.F_38) ?? string.Empty}"
                                                                                             , $"{responseMessage.Core().Field(FIELD.F_39) ?? string.Empty}");
                    Log.Info($"ProcessSendFISC Process , {reponseCode}");
                    byte[] sendBytes = EAICommUtility.ConvertOASVRFY1Incoming2Outgoing(responseMessage, reqJSONObject, SystemConstantData.EAISystemCodeAndData.Success);
                    return sendBytes;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ProcessSendNCCC Exception:{ex.Message}");
                var sendBytes = EAICommUtility.ConvertOASVRFY1Incoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.Success);
                return sendBytes;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Release instance.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _connection.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Dispose Exception:{ex.Message}");
            }
            finally
            {
                _connection.Close();
            }
        }

        #endregion
    }
}

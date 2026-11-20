using System;
using System.Net.Sockets;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Newtonsoft.Json;
using NLog;

namespace Com.Cybersoft4u.ServiceProvider.QRCodeServerService
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
                var reqJSONObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_REQ>(receiveString);
                if (reqJSONObject == null)
                {
                    var sendBytes = EAICommUtility.CreateQRCodePaymentErrorMessage();
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    Log.Info($"Receive Message Format Error, Sender: [{Encoding.Default.GetString(sendBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                    return;
                }
                var checkRequestMessageFormat = EAICommUtility.CheckFCB9GQMessageFormat(reqJSONObject);
                if (checkRequestMessageFormat != SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess)
                {
                    var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, checkRequestMessageFormat);
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    //Log.Info($"Receive Message Format Error, Sender: [{Encoding.Default.GetString(sendBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                    Log.Info($"Receive Message Format Error at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                    return;
                }
                if (Log.IsDebugEnabled)
                {
                    Log.Info("----- Start Display QRCode Payment Incoming Message -----");
                    Log.Info(receiveString);
                    Log.Info("----- End   Display QRCode Payment Incoming Message -----");
                }
                //Convert QRCode Payment Message to NcccIso8583
                var nccc = EAICommUtility.ConvertFCB9GQMessage2NcccIso8583(reqJSONObject);
                if (nccc == null)
                {
                    Log.Warn("Convert Incoming Message To NcccIso8583 Error");
                    var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.SystemError);
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    //Log.Warn(($"Sender: [{Encoding.ASCII.GetString(sendBytes)}]. The server has send {sendBytes.Length} bytes."));
                    Log.Warn(($"The server has send {sendBytes.Length} bytes. at {{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff"));
                    return;
                }
                //Create Transaction Log Key
                string txKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
                const int returnArrayCount = 4;
                var returnStrings = QRCodeAuthFlowUtility.ProcessQRCode0200(nccc, reqJSONObject, _connection.RemoteEndPoint.ToString(), txKeySeq);
                //Check return result, returnStrings[2] = "A"
                if ((returnStrings == null) || returnStrings.Length != returnArrayCount)
                {
                    Log.Warn("Call StoreProcedure QRCodeAuthFlowUtility.ProcessQRCode0200 Error");
                    var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.SystemError);
                    args.SetBuffer(sendBytes, 0, sendBytes.Length);
                    //Log.Info(($"Sender: [{Encoding.Default.GetString(sendBytes)}]. The server has send {sendBytes.Length} bytes."));
                    Log.Info(($"The server has send {sendBytes.Length} bytes. at {{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff"));
                    return;
                }
                else
                {
                    if (!returnStrings[2].Equals("A") || !returnStrings[1].Equals("00"))
                    {
                        Log.Warn($"No Process PushPayment, @isoP39Rsp[{returnStrings[0]}], @isoP39Rsp[{returnStrings[1]}], @isoP39RspAction[{returnStrings[2]}], @disPatchSwitch[{returnStrings[3]}]");
                        // var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.Success);

                        var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, returnStrings[1]);
                        args.SetBuffer(sendBytes, 0, sendBytes.Length);
                        //Log.Info(($"Sender: [{Encoding.ASCII.GetString(sendBytes)}]. The server has send {sendBytes.Length} bytes."));
                        Log.Info(($"The server has send {sendBytes.Length} bytes. at {{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff"));
                        return;
                    }
                }
                //Send NCCC
                var processBytes = ProcessSendNCCC(nccc, reqJSONObject, txKeySeq);
                args.SetBuffer(processBytes, 0, processBytes.Length);
                //Log.Info(($"Sender: [{Encoding.Default.GetString(processBytes)}]. The server has send {processBytes.Length} bytes."));
                Log.Info(($"The server has send {processBytes.Length} bytes. at {{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff"));
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
        private byte[] ProcessSendNCCC(NcccIso8583 requestMessage, FCB9GQ.FCB9GQ_REQ reqJSONObject, string txKeySeq)
        {
            try
            {
                Log.Info($"ProcessSendNCCC, LocalEndPoint[{_connection.LocalEndPoint.ToString()}, RemoteEndPoint[{_connection.RemoteEndPoint.ToString()}, Key[{txKeySeq}]");
                NcccIso8583 responseMessage = null;
                if (CyberAuthNCCCResource.CyberAuthNcccQRCode == null)
                {
                    Log.Error($"CyberAuthNCCCResource.CyberAuthNcccQRCode not create");
                }
                else
                {
                    responseMessage = CyberAuthNCCCResource.CyberAuthNcccQRCode.ProcessQRCodeMessage(requestMessage, _connection.RemoteEndPoint.ToString(), txKeySeq);
                }
                var returnStrings = QRCodeAuthFlowUtility.ProcessQRCodeTxUpdate(requestMessage, responseMessage, reqJSONObject, _connection.RemoteEndPoint.ToString(), txKeySeq);
                if ((returnStrings == null) || returnStrings.Length != 3)
                {
                    Log.Warn("Call StoreProcedure QRCodeAuthFlowUtility.ProcessQRCodeTxUpdate Error");
                    var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.SystemError);
                    return sendBytes;
                }
                else
                {
                    Log.Info($"QRCodeAuthFlowUtility.ProcessQRCodeTxUpdate Process , @isoP38AuthCode[{returnStrings[0]}], @isoP39Rsp[{returnStrings[1]}], @isoP39RspAction[{returnStrings[2]}] ");
                    var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(requestMessage, reqJSONObject, returnStrings, SystemConstantData.EAISystemCodeAndData.Success);
                    return sendBytes;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ProcessSendNCCC Exception:{ex.Message}");
                var sendBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, SystemConstantData.EAISystemCodeAndData.Success);
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

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.SMS;
using NLog;
using System;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.Simulator.Web.WebSMSSimulator
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public class SMSToken : IDisposable
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
        internal SMSToken(Socket connection, int bufferSize)
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
                Log.Info($"Receive: [{Encoding.Default.GetString(receiveBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has read {_currentIndex} bytes.");
                //Process Receive ETS3 SMS Message
                //Convert Bytes array To Structure
                var SMSOutgoingMessageStruct = new SMSOutgoingMessageStruct();
                SMSOutgoingMessageStruct = (SMSOutgoingMessageStruct)ColaCommUtility.RawDeserialize(receiveBytes, SMSOutgoingMessageStruct.GetType());
                SMSSocketListener.Metrics.Increment();
                if (Log.IsDebugEnabled)
                {
                    Log.Info("----- Start Display SMS Outgoing Message -----");
                    var displayMessage = ColaCommUtility.GetStructProperty<SMSOutgoingMessageStruct>(SMSOutgoingMessageStruct);
                    Log.Info(displayMessage);
                    Log.Info("----- End   Display SMS Outgoing Message -----");
                }
                //Create SMS Return Message
                SMSOutgoingMessageStruct.mob_request[0] = Convert.ToByte('P');
                var sendBytes = ColaCommUtility.RawSerialize<SMSOutgoingMessageStruct>(SMSOutgoingMessageStruct);
                args.SetBuffer(sendBytes, 0, sendBytes.Length);
                Log.Info($"Send: [{Encoding.Default.GetString(sendBytes)}]. The server has send {sendBytes.Length} bytes.");
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
        /// <param name="SMSOutgoingMessageStruct"></param>
        /// <param name="SMSReturnCode"></param>
        private byte[] ConvertSMSOutgoing2Incoming(SMSOutgoingMessageStruct SMSOutgoingMessageStruct, string SMSReturnCode)
        {
            try
            {
                var sendBytes = ColaCommUtility.RawSerialize<SMSOutgoingMessageStruct>(SMSOutgoingMessageStruct);
                return sendBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertSMSOutgoing2Incoming Exception:{ex.Message}");
                return null;
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
                Log.Fatal(ex, $"Dispose exception:{ex.Message}");
            }
            finally
            {
                _connection.Close();
            }
        }

        #endregion
    }
}

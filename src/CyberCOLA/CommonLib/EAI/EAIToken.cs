using Newtonsoft.Json;
using NLog;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.EAI
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public class EAIToken : IDisposable
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly Socket _connection;
        private readonly byte[] _receiveBuffer;
        private int _currentIndex;
        private bool _disposed;

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="connection">Socket to accept incoming data.</param>
        /// <param name="bufferSize">Buffer size for accepted data.</param>
        public EAIToken(Socket connection, int bufferSize)
        {
            _connection = connection;
            _receiveBuffer = new byte[bufferSize];
        }

        /// <summary>
        /// Accept socket.
        /// </summary>
        public Socket Connection => _connection;

        /// <summary>
        /// Process data received from the client.
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs used in the operation.</param>
        public void ProcessData(SocketAsyncEventArgs args)
        {
            try
            {
                byte[] receiveBytes = new byte[_currentIndex];
                Buffer.BlockCopy(_receiveBuffer, 0, receiveBytes, 0, _currentIndex);
                Log.Info($"Receive: [{Encoding.ASCII.GetString(receiveBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has read {_currentIndex} bytes.");
                var reqJSONString = Encoding.ASCII.GetString(receiveBytes);
                var reqJSONObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_REQ>(reqJSONString);
                byte[] processBytes;
                if (reqJSONObject == null)
                {
                    Log.Info(($"Convert Object Error"));
                    processBytes = EAICommUtility.CreateQRCodePaymentErrorMessage();
                }
                else
                {
                    processBytes = EAICommUtility.ConvertFCB9GQIncoming2Outgoing(reqJSONObject, "0000");
                }
                args.SetBuffer(processBytes, 0, processBytes.Length);
                Log.Info(($"Send: [{Encoding.Default.GetString(processBytes)}]. The server has send {processBytes.Length} bytes."));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ProcessData Exception:{ex.Message}");
            }
            finally
            {
                _currentIndex = 0;
            }
        }

        /// <summary>
        /// Set data received from the client.
        /// </summary>
        /// <param name="args">SocketAsyncEventArgs used in the operation.</param>
        public void SetData(SocketAsyncEventArgs args)
        {
            Int32 count = args.BytesTransferred;

            if (_currentIndex + count > _receiveBuffer.Length)
            {
                throw new ArgumentOutOfRangeException($"count Adding {count} bytes on buffer which has {_currentIndex} bytes, the listener buffer will overflow.");
            }

            if (args.Buffer != null)
            {
                Buffer.BlockCopy(args.Buffer, args.Offset, _receiveBuffer, _currentIndex, count);
                _currentIndex += count;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Release instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation.
        /// </summary>
        /// <param name="disposing">True if called from Dispose; false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
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

            _disposed = true;
        }

        #endregion
    }
}

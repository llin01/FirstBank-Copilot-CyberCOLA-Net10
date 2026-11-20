using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.Network.Client
{
    /// <summary>
    /// Implements the connection logic for the socket client.
    /// </summary>
    public class SocketAsyncEventArgsClient : IDisposable
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        /// <summary>
        /// Constants for socket operations.
        /// </summary>
        private const int ReceiveOperation = 1, SendOperation = 0;

        /// <summary>
        /// The socket used to send/receive messages.
        /// </summary>
        private readonly Socket _clientSocket;

        /// <summary>
        /// Flag for connected socket.
        /// </summary>
        private Boolean _connected = false;

        /// <summary>
        /// Listener endpoint.
        /// </summary>
        private readonly IPEndPoint _serverEndPoint;

        /// <summary>
        /// Connection Timeout Millisecond.
        /// </summary>
        private readonly int _connectionTimeout;

        /// <summary>
        /// Send & Receive Timeout Millisecond.
        /// </summary>
        private readonly int _sendReceiveTimeout;

        /// <summary>
        /// Signals a connection.
        /// </summary>
        private readonly AutoResetEvent _autoConnectEvent = new AutoResetEvent(false);

        /// <summary>
        /// Signals the send/receive operation.
        /// </summary>
        private readonly AutoResetEvent[] _autoSendReceiveEvents = new AutoResetEvent[]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false)
        };

        private SocketShutdown socketShutdown = SocketShutdown.Both;
        /// <summary>
        /// Create an uninitialized client instance.
        /// To start the send/receive processing
        /// call the Connect method followed by SendReceive method.
        /// </summary>
        /// <param name="hostName">Name of the host where the listener is running.</param>
        /// <param name="port">Number of the TCP port from the listener.</param>
        /// <param name="connectionTimeout"></param>
        /// <param name="sendReceiveTimeout"></param>
        internal SocketAsyncEventArgsClient(string hostName, int port, int connectionTimeout, int sendReceiveTimeout)
        {
            var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(hostName, AddressFamily.InterNetwork);
            // Instantiates the endpoint and socket.
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), port);
            _clientSocket = new Socket(_serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _connectionTimeout = connectionTimeout;
            _sendReceiveTimeout = sendReceiveTimeout;
        }

        internal SocketAsyncEventArgsClient(IPEndPoint serverEndPoint, int connectionTimeout, int sendReceiveTimeout)
        {
            _serverEndPoint = serverEndPoint;
            _clientSocket = new Socket(_serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _connectionTimeout = connectionTimeout;
            _sendReceiveTimeout = sendReceiveTimeout;
        }

        /// <summary>
        /// Connect to the host.
        /// </summary>
        /// <returns>True if connection has succeeded, else false.</returns>
        internal void Connect()
        {
            try
            {
                SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();

                connectArgs.UserToken = _clientSocket;
                connectArgs.RemoteEndPoint = _serverEndPoint;
                connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

                _clientSocket.ConnectAsync(connectArgs);
                _autoConnectEvent.WaitOne(new TimeSpan(0, 0, 0, 0, _connectionTimeout));

                SocketError errorCode = connectArgs.SocketError;
                if (errorCode != SocketError.Success)
                {
                    throw new SocketException((int)errorCode);
                }
                Log.Info($"Client [{_clientSocket?.LocalEndPoint} Connect To [{_serverEndPoint}] Connect");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Client [{_clientSocket?.LocalEndPoint} Connect To [{_serverEndPoint}] Exception : {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Disconnect from the host.
        /// </summary>
        internal void Disconnect(SocketShutdown socketShutdown)
        {
            //clientSocket.Disconnect(false);
            try
            {
                Log.Info($"Client [{_clientSocket?.LocalEndPoint} To [{_serverEndPoint?.ToString()}] Disconnect");
                this.socketShutdown = socketShutdown;
                Dispose();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Client [{_clientSocket?.LocalEndPoint} To [{_serverEndPoint?.ToString()}] Disconnect Exception : {ex.Message}");
            }
        }

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                // Signals the end of connection.
                _autoConnectEvent.Set();

                // Set the flag for socket connected.
                _connected = (e.SocketError == SocketError.Success);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
            }
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                // Signals the end of receive.
                _autoSendReceiveEvents[SendOperation].Set();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
                //throw;
            }
        }

        private void OnSend(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                // Signals the end of send.
                Log.Info("SocketAsyncEventArgsClient OnSend Process");
                _autoSendReceiveEvents[ReceiveOperation].Set();

                if (e.SocketError == SocketError.Success)
                {
                    if (e.LastOperation == SocketAsyncOperation.Send)
                    {
                        // Prepare receiving.
                        Socket s = e.UserToken as Socket;

                        byte[] receiveBuffer = new byte[2048];
                        e.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                        e.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
                        //s.ReceiveAsync(e);
                        if (!s.ReceiveAsync(e))
                        {
                            OnReceive(null, e);
                        }
                    }
                }
                else
                {
                    ProcessError(e);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
            }
        }

        /// <summary>
        /// Close socket in case of failure and throws a SocketException according to the SocketError.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the failed operation.</param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            if (s.Connected)
            {
                // close the socket associated with the client
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Log.Error($"ProcessError Exception:{ex.Message}");
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                }
            }
            Log.Info($"ProcessError SocketError [{(int)e.SocketError}] ");
        }

        /// <summary>
        /// Exchange a message with the host.
        /// </summary>
        /// <param name="sendBytes">Message to send.</param>
        /// <returns>Message sent by the host.</returns>
        internal byte[] SendReceive(byte[] sendBytes)
        {
            try
            {
                if (_connected)
                {
                    Log.Debug($"SocketAsyncEventArgsClient Send Message [{Encoding.ASCII.GetString(sendBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff} ");
                    // Prepare arguments for send/receive operation.
                    SocketAsyncEventArgs completeArgs = new SocketAsyncEventArgs();
                    completeArgs.SetBuffer(sendBytes, 0, sendBytes.Length);
                    completeArgs.UserToken = _clientSocket;
                    completeArgs.RemoteEndPoint = _serverEndPoint;
                    completeArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

                    // Start sending asynchronously.
                    //_clientSocket.SendAsync(completeArgs);
                    if (!_clientSocket.SendAsync(completeArgs))
                    {
                        OnSend(null, completeArgs);
                    }
                    // Wait for the send/receive completed.
                    var isCompleted = WaitHandle.WaitAll(_autoSendReceiveEvents, new TimeSpan(0, 0, 0, 0, _sendReceiveTimeout));
                    if (!isCompleted)
                    { return null; }
                    // Return data from SocketAsyncEventArgs buffer.
                    var returnBytes = new byte[completeArgs.BytesTransferred];
                    Buffer.BlockCopy(completeArgs.Buffer, completeArgs.Offset, returnBytes, 0, completeArgs.BytesTransferred);
                    Log.Debug($"SocketAsyncEventArgsClient Receive Message [{Encoding.ASCII.GetString(sendBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff} ");
                    return returnBytes;
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $" SendReceive Exception : {ex.Message}");
                return null;
                //throw;
            }
        }

        /// <summary>
        /// Exchange a message with the host.
        /// </summary>
        /// <param name="sendBytes">Message to send.</param>
        /// <returns>Message sent by the host.</returns>
        internal void Send(byte[] sendBytes)
        {
            try
            {
                if (_connected)
                {
                    Log.Debug($"SocketAsyncEventArgsClient Send Message [{Encoding.ASCII.GetString(sendBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff} ");
                    // Prepare arguments for send/receive operation.
                    SocketAsyncEventArgs completeArgs = new SocketAsyncEventArgs();
                    completeArgs.SetBuffer(sendBytes, 0, sendBytes.Length);
                    completeArgs.UserToken = _clientSocket;
                    completeArgs.RemoteEndPoint = _serverEndPoint;
                    completeArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

                    // Start sending asynchronously.
                    _clientSocket.SendAsync(completeArgs);

                    // Wait for the send/receive completed.
                    var isCompleted = WaitHandle.WaitAny(_autoSendReceiveEvents, new TimeSpan(0, 0, 0, 0, _sendReceiveTimeout));
                    Log.Debug($"SocketAsyncEventArgsClient Send Message [{Encoding.ASCII.GetString(sendBytes)}] Completed at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff} ");
                    return;
                }
                Log.Debug($"SocketAsyncEventArgsClient Not Connection, No Send Message [{Encoding.ASCII.GetString(sendBytes)}] at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff} ");
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $" SendReceive Exception : {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Exchange a message with the host.
        /// </summary>
        /// <param name="sendBytes">Message to send.</param>
        /// <returns>Message sent by the host.</returns>
        internal byte[] ProcessSendReceive(byte[] sendBytes)
        {
            try
            {
                if (_connected)
                {
                    Log.Info($"Start ProcessSendReceive at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff} ");
                    // ³]¸mSocketAsyncEventArgs
                    // Prepare arguments for send operation.
                    SocketAsyncEventArgs _sendEventArgs = new SocketAsyncEventArgs();
                    _sendEventArgs.SetBuffer(sendBytes, 0, sendBytes.Length);
                    _sendEventArgs.UserToken = _clientSocket;
                    _sendEventArgs.RemoteEndPoint = _serverEndPoint;
                    _sendEventArgs.Completed += SendCompleted;
                    StartSend(_sendEventArgs);

                    // Prepare arguments for receive operation.
                    //byte[] _receiveBuffer = new byte[2048];
                    //SocketAsyncEventArgs _receiveEventArgs = new SocketAsyncEventArgs();
                    //_receiveEventArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                    //_receiveEventArgs.Completed += ReceiveCompleted;
                    //StartSend(_sendEventArgs);

                    SocketAsyncEventArgs _receiveEventArgs = new SocketAsyncEventArgs();
                    byte[] _receiveBuffer = new byte[2048];
                    _receiveEventArgs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                    _receiveEventArgs.Completed += ReceiveCompleted;

                    StartReceive(_receiveEventArgs);
                    var isCompleted = WaitHandle.WaitAll(_autoSendReceiveEvents, new TimeSpan(0, 0, 0, 0, _sendReceiveTimeout));
                    if (!isCompleted)
                    { return null; }
                    if (_receiveEventArgs.BytesTransferred > 0 && _receiveEventArgs.SocketError == SocketError.Success)
                    {
                        var returnBytes = new byte[_receiveEventArgs.BytesTransferred];
                        Buffer.BlockCopy(_receiveEventArgs.Buffer, _receiveEventArgs.Offset, returnBytes, 0, _receiveEventArgs.BytesTransferred);
                        return returnBytes;
                    }
                    else
                    {
                        Log.Fatal($"Receive Message Error: {_receiveEventArgs.SocketError}");
                        return null;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ProcessSendReceive Exception : {ex.Message}");
                return null;
                //throw;
            }
        }

        private void StartSend(SocketAsyncEventArgs e)
        {
            if (!_clientSocket.SendAsync(e))
            {
                SendCompleted(null, e);
            }
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                _autoSendReceiveEvents[ReceiveOperation].Set();
                Log.Info("SendComplete successful");
            }
            else
            {
                Log.Info($"SendComplete failed: {e.SocketError}");
            }
        }
        private void StartReceive(SocketAsyncEventArgs e)
        {
            if (!_clientSocket.ReceiveAsync(e))
            {
                ReceiveCompleted(null, e);
            }
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                string receivedMessage = Encoding.ASCII.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                _autoSendReceiveEvents[SendOperation].Set();
                Log.Info($"ReceiveCompleted successful : [{receivedMessage}]");
            }
            else
            {
                Log.Info($"ReceiveCompleted failed: {e.SocketError}");
            }
        }
        #region IDisposable Members

        /// <summary>
        /// Disposes the instance of SocketClient.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_clientSocket.Connected)
                {
                    _clientSocket?.Shutdown(socketShutdown);
                    _clientSocket?.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Dispose Exception : {ex.Message}");
            }
        }

        #endregion
    }
}

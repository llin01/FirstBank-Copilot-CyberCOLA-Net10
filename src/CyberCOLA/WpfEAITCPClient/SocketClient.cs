using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.Test.WpfEAITCPClient
{
    /// <summary>
    /// Implements the connection logic for the socket client.
    /// </summary>
    public class SocketClient : IDisposable
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

        /// <summary>
        /// Create an uninitialized client instance.  
        /// To start the send/receive processing
        /// call the Connect method followed by SendReceive method.
        /// </summary>
        /// <param name="hostName">Name of the host where the listener is running.</param>
        /// <param name="port">Number of the TCP port from the listener.</param>
        /// <param name="connectionTimeout"></param>
        /// <param name="sendReceiveTimeout"></param>
        internal SocketClient(string hostName, int port, int connectionTimeout, int sendReceiveTimeout)
        {
            var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(hostName, AddressFamily.InterNetwork);
            // Instantiates the endpoint and socket.
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), port);
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
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Disconnect from the host.
        /// </summary>
        internal void Disconnect()
        {
            //clientSocket.Disconnect(false);
            Dispose();
        }

        // Mark the sender parameter of OnConnect as non-nullable to conform to the EventHandler delegate
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
                //throw;
            }
        }

        private void OnReceive(object? sender, SocketAsyncEventArgs e)
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

        private void OnSend(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                // Signals the end of send.
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
                        s.ReceiveAsync(e);
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
                //throw;
            }
        }

        /// <summary>
        /// Close socket in case of failure and throws a SocketException according to the SocketError.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the failed operation.</param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket? s = e.UserToken as Socket;
            if (s.Connected)
            {
                // close the socket associated with the client
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Log.Info($"ProcessError Exception:{ex.Message}");
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                }
            }
            Log.Error($"ProcessError SocketError [{(int)e.SocketError}] ");
        }

        /// <summary>
        /// Exchange a message with the host.
        /// </summary>
        /// <param name="sendBytes">Message to send.</param>
        /// <returns>Message sent by the host.</returns>
        internal byte[]? SendReceive(byte[] sendBytes)
        {
            try
            {
                if (_connected)
                {
                    // Prepare arguments for send/receive operation.
                    SocketAsyncEventArgs completeArgs = new SocketAsyncEventArgs();
                    completeArgs.SetBuffer(sendBytes, 0, sendBytes.Length);
                    completeArgs.UserToken = _clientSocket;
                    completeArgs.RemoteEndPoint = _serverEndPoint;
                    completeArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

                    // Start sending asynchronously.
                    _clientSocket.SendAsync(completeArgs);
                    if (completeArgs.SocketError == SocketError.Success)
                    {
                        completeArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceive);
                        _clientSocket.ReceiveAsync(completeArgs);
                    }

                    //Wait for the send/receive completed.
                    var isCompleted = WaitHandle.WaitAll(_autoSendReceiveEvents, new TimeSpan(0, 0, 0, 0, _sendReceiveTimeout));
                    if (!isCompleted) return null;

                    // Check if Buffer is null
                    if (completeArgs.Buffer == null)
                    {
                        Log.Error("SocketAsyncEventArgs.Buffer is null, cannot copy");
                        return null;
                    }

                    var returnBytes = new byte[completeArgs.BytesTransferred];
                    Buffer.BlockCopy(completeArgs.Buffer, completeArgs.Offset, returnBytes, 0, completeArgs.BytesTransferred);
                    return returnBytes;
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
                return null;
                //throw;
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
                _autoConnectEvent.Close();
                _autoSendReceiveEvents[SendOperation].Close();
                _autoSendReceiveEvents[ReceiveOperation].Close();
                if (_clientSocket.Connected)
                {
                    _clientSocket.Shutdown(SocketShutdown.Both);
                    _clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
            }
        }

        #endregion
    }
}

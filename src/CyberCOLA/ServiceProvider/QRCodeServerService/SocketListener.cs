using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;

namespace Com.Cybersoft4u.ServiceProvider.QRCodeServerService
{
    /// <summary>
    /// Based on example from http://msdn2.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.aspx
    /// Implements the connection logic for the socket server.  
    /// After accepting a connection, all data read from the client is sent back. 
    /// The read and echo back to the client pattern is continued until the client disconnects.
    /// </summary>
    public class SocketListener
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        /// <summary>
        /// The socket used to listen for incoming connection requests.
        /// </summary>
        private readonly IPEndPoint _serverEP;

        /// <summary>
        /// The socket used to listen for incoming connection requests.
        /// </summary>
        private Socket _listenSocket;

        /// <summary>
        /// Mutex to synchronize server execution.
        /// </summary>
        private static readonly Mutex Mutex = new Mutex();

        /// <summary>
        /// Buffer size to use for each socket I/O operation.
        /// </summary>
        private readonly int _bufferSize;

        /// <summary>
        /// The total number of clients connected to the server.
        /// </summary>
        private int _numConnectedSockets;

        /// <summary>
        /// the maximum number of connections the sample is designed to handle simultaneously.
        /// </summary>
        private readonly int _numConnections;

        /// <summary>
        /// Pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations.
        /// </summary>
        private readonly SocketAsyncEventArgsPool _readWritePool;

        /// <summary>
        /// Controls the total number of clients connected to the server.
        /// </summary>
        private readonly Semaphore _semaphoreAcceptedClients;

        /// <summary>
        /// Server Is Running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Create an uninitialized server instance.  
        /// To start the server listening for connection requests
        /// call the Init method followed by Start method.
        /// </summary>
        /// <param name="serverEP">Server EndPoint</param>
        /// <param name="numConnections">Maximum number of connections to be handled simultaneously.</param>
        /// <param name="bufferSize">Buffer size to use for each socket I/O operation.</param>
        public SocketListener(IPEndPoint serverEP, int numConnections, int bufferSize)
        {
            _serverEP = serverEP;
            _numConnectedSockets = 0;
            _numConnections = numConnections;
            _bufferSize = bufferSize;
            IsRunning = false;

            _readWritePool = new SocketAsyncEventArgsPool(numConnections);
            _semaphoreAcceptedClients = new Semaphore(numConnections, numConnections);

            // Preallocate pool of SocketAsyncEventArgs objects.
            for (int i = 0; i < _numConnections; i++)
            {
                SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                readWriteEventArg.SetBuffer(new byte[_bufferSize], 0, this._bufferSize);

                // Add SocketAsyncEventArg to the pool.
                _readWritePool.Push(readWriteEventArg);
            }
        }

        /// <summary>
        /// Close the socket associated with the client.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed send/receive operation.</param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            var token = e.UserToken as Token;
            CloseClientSocket(token, e);
        }

        private void CloseClientSocket(Token token, SocketAsyncEventArgs e)
        {
            token.Dispose();

            // Decrement the counter keeping track of the total number of clients connected to the server.
            _semaphoreAcceptedClients.Release();
            Interlocked.Decrement(ref _numConnectedSockets);
            Log.Info($"A client has been disconnected from the server. There are {_numConnectedSockets} clients connected to the server at: {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
            // Free the SocketAsyncEventArg so they can be reused by another client.
            _readWritePool.Push(e);
        }

        /// <summary>
        /// Callback method associated with Socket.AcceptAsync 
        /// operations and is invoked when an accept operation is complete.
        /// </summary>
        /// <param name="sender">Object who raised the event.</param>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// Callback called whenever a receive or send operation is completed on a socket.
        /// </summary>
        /// <param name="sender">Object who raised the event.</param>
        /// <param name="e">SocketAsyncEventArg associated with the completed send/receive operation.</param>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Determine which type of operation just completed and call the associated handler.
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        /// <summary>
        /// Process the accept for the socket listener.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket s = e.AcceptSocket;
            if (s.Connected)
            {
                try
                {
                    SocketAsyncEventArgs readEventArgs = _readWritePool.Pop();
                    if (readEventArgs != null)
                    {
                        // Get the socket for the accepted client connection and put it into the 
                        // ReadEventArg object user token.
                        readEventArgs.UserToken = new Token(s, _bufferSize);

                        Interlocked.Increment(ref _numConnectedSockets);
                        Log.Info($"Client connection accepted. There are {_numConnectedSockets} clients connected to the server at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");

                        if (!s.ReceiveAsync(readEventArgs))
                        {
                            ProcessReceive(readEventArgs);
                        }
                    }
                    else
                    {
                        Log.Info("There are no more available sockets to allocate.");
                    }
                }
                catch (SocketException ex)
                {
                    if (e.UserToken is Token token)
                        Log.Fatal(ex, $"Error when processing data received from {token.Connection.RemoteEndPoint}, exception: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"ProcessAccept Exception:{ex.Message}");
                }

                // Accept the next connection request.
                StartAccept(e);
            }
        }

        private void ProcessError(SocketAsyncEventArgs e)
        {
            Token token = e.UserToken as Token;
            IPEndPoint localEp = token?.Connection.LocalEndPoint as IPEndPoint;

            CloseClientSocket(token, e);

            Log.Fatal($"Socket error {(int)e.SocketError} on endpoint {localEp} during {e.LastOperation}.");
        }

        /// <summary>
        /// This method is invoked when an asynchronous receive operation completes. 
        /// If the remote host closed the connection, then the socket is closed.  
        /// If data was received then the data is echoed back to the client.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed receive operation.</param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // Check if the remote host closed the connection.
            if (e.BytesTransferred > 0)
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (e.UserToken is Token token)
                    {
                        token.SetData(e);

                        Socket s = token.Connection;
                        if (s.Available == 0)
                        {
                            // Set return buffer.
                            token.ProcessData(e);
                            if (!s.SendAsync(e))
                            {
                                // Set the buffer to send back to the client.
                                ProcessSend(e);
                            }
                        }
                        else if (!s.ReceiveAsync(e))
                        {
                            // Read the next block of data sent by client.
                            ProcessReceive(e);
                        }
                    }
                }
                else
                {
                    ProcessError(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        /// <summary>
        /// This method is invoked when an asynchronous send operation completes.  
        /// The method issues another receive on the socket to read any additional 
        /// data sent from the client.
        /// </summary>
        /// <param name="e">SocketAsyncEventArg associated with the completed send operation.</param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // Done echoing data back to the client.

                if (e.UserToken is Token token && !token.Connection.ReceiveAsync(e))
                {
                    // Read the next block of data send from the client.
                    ProcessReceive(e);
                }
            }
            else
            {
                ProcessError(e);
            }
        }

        /// <summary>
        /// Starts the server listening for incoming connection requests.
        /// </summary>
        public void Start()
        {
            try
            {
                // Create the socket which listens for incoming connections.
                _listenSocket = new Socket(_serverEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = _bufferSize,
                    SendBufferSize = _bufferSize
                };

                if (_serverEP.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Set dual-mode (IPv4 & IPv6) for the socket listener.
                    // 27 is equivalent to IPV6_V6ONLY socket option in the winsock snippet below,
                    // based on http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                    _listenSocket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _listenSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, _serverEP.Port));
                }
                else
                {
                    // Associate the socket with the local endpoint.
                    _listenSocket.Bind(_serverEP);
                }

                // Start the server.
                _listenSocket.Listen(_numConnections);
                IsRunning = true;
                // Post accepts on the listening socket.
                StartAccept(null);

                // Blocks the current thread to receive incoming messages.
                //Mutex.WaitOne();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ProcessAccept Exception:{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Begins an operation to accept a connection request from the client.
        /// </summary>
        /// <param name="acceptEventArg">The context object to use when issuing 
        /// the accept operation on the server's listening socket.</param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
            {
                // Socket must be cleared since the context object is being reused.
                acceptEventArg.AcceptSocket = null;
            }

            _semaphoreAcceptedClients.WaitOne();
            if (!_listenSocket.AcceptAsync(acceptEventArg))
            {
                ProcessAccept(acceptEventArg);
            }
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            try
            {
                Log.Info($"SocketListener IsRunning[{IsRunning}]");
                //Mutex.ReleaseMutex();
                if (IsRunning)
                    _listenSocket.Close();
                else
                {
                    Log.Warn($"Server not start at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:ffff} ");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Stop Exception:{ex.Message}");
            }
            //Mutex.ReleaseMutex();
        }
    }
}

using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System.Net;
using System.Net.Sockets;

namespace Com.Cybersoft4u.Test.Web.WebEAITCPClient
{
    /// <summary>
    /// Implements the connection logic for the socket client.
    /// </summary>
    public class SocketClient : IDisposable
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private const int ReceiveOperation = 1, SendOperation = 0;
        private readonly Socket _clientSocket;
        private Boolean _connected = false;
        private readonly IPEndPoint _serverEndPoint;
        private readonly int _connectionTimeout;
        private readonly int _sendReceiveTimeout;
        private readonly AutoResetEvent _autoConnectEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent[] _autoSendReceiveEvents = new AutoResetEvent[]
        {
            new AutoResetEvent(false),
            new AutoResetEvent(false)
        };

        internal SocketClient(string hostName, int port, int connectionTimeout, int sendReceiveTimeout)
        {
            var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(hostName, AddressFamily.InterNetwork);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), port);
            _clientSocket = new Socket(_serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _connectionTimeout = connectionTimeout;
            _sendReceiveTimeout = sendReceiveTimeout;
        }

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

        internal void Disconnect()
        {
            Dispose();
        }

        private void OnConnect(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                _autoConnectEvent.Set();
                _connected = (e.SocketError == SocketError.Success);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
            }
        }

        private void OnReceive(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                _autoSendReceiveEvents[SendOperation].Set();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
            }
        }

        private void OnSend(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                _autoSendReceiveEvents[ReceiveOperation].Set();

                if (e.SocketError == SocketError.Success)
                {
                    if (e.LastOperation == SocketAsyncOperation.Send)
                    {
                        Socket? s = e.UserToken as Socket;
                        if (s == null) return;

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
            }
        }

        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket? s = e.UserToken as Socket;
            if (s != null && s.Connected)
            {
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"Shutdown error: {ex.Message}");
                }
                finally
                {
                    s.Close();
                }
            }

            throw new SocketException((int)e.SocketError);
        }

        internal byte[]? SendReceive(byte[] message)
        {
            byte[]? receiveBuffer = null;

            try
            {
                if (_connected)
                {
                    SocketAsyncEventArgs completeArgs = new SocketAsyncEventArgs();
                    completeArgs.UserToken = _clientSocket;
                    completeArgs.RemoteEndPoint = _serverEndPoint;
                    completeArgs.SetBuffer(message, 0, message.Length);
                    completeArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

                    _clientSocket.SendAsync(completeArgs);

                    _autoSendReceiveEvents[ReceiveOperation].WaitOne(new TimeSpan(0, 0, 0, 0, _sendReceiveTimeout));

                    _autoSendReceiveEvents[SendOperation].WaitOne(new TimeSpan(0, 0, 0, 0, _sendReceiveTimeout));

                    Byte errorVal = Convert.ToByte(completeArgs.SocketError);

                    if (errorVal == 0)
                    {
                        if (completeArgs.BytesTransferred > 0)
                        {
                            receiveBuffer = new byte[completeArgs.BytesTransferred];
                            Buffer.BlockCopy(completeArgs.Buffer!, 0, receiveBuffer, 0, completeArgs.BytesTransferred);
                        }
                    }
                    else
                    {
                        throw new SocketException(errorVal);
                    }
                }
                else
                {
                    throw new SocketException((Int32)SocketError.NotConnected);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{ex.Message}");
                throw new Exception(ex.Message);
            }

            return receiveBuffer;
        }

        public void Dispose()
        {
            _autoConnectEvent.Dispose();
            _autoSendReceiveEvents[SendOperation].Dispose();
            _autoSendReceiveEvents[ReceiveOperation].Dispose();

            if (_clientSocket.Connected)
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
            }
            _clientSocket.Close();
        }
    }
}

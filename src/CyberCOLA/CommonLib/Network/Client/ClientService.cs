using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Com.Cybersoft4u.CommonLib.Network.Client
{
    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket WorkSocket;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] ReceiveBuffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder ResponseStringBuilder = new StringBuilder();

        // Received data bytes.
        public byte[] ResponseBytes = new byte[BufferSize * 2];
    }

    public class ClientService
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        // The port number for the remote device.
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private IPEndPoint _remoteEP;
        private Socket _client;
        private bool _isConnectionTimeOut = true;
        private bool _isReceiveTimeOut = true;
        private bool _isSendTimeOut = true;
        private readonly int _clientServiceOption;
        private int _portNumber = 0;
        private int _connectionTimeOut = 0;
        private int _sendTimeOut = 0;
        private int _receiveTimeOut = 0;

        // ManualResetEvent instances signal completion.
        private ManualResetEvent _connectDone = new ManualResetEvent(false);
        private ManualResetEvent _sendDone = new ManualResetEvent(false);
        private ManualResetEvent _receiveDone = new ManualResetEvent(false);

        // Orders a connect callback against the switch to the next server's socket.
        private readonly object _connectLock = new object();

        // The response from the remote device.
        private byte[] _response;



        /// <summary>
        /// Initializes a new instance of the <see cref="ClientService"/> class.
        /// </summary>
        /// <param name="option">The option(1.NCCC(9903), 2:NCPS(9913), 3:IAG(9923), 4:STIN(9933)</param>
        /// <exception cref="ArgumentException">ClientService Constructor Error</exception>
        public ClientService(int option = 1)
        {
            try
            {
                _connectDone = new ManualResetEvent(false);
                _sendDone = new ManualResetEvent(false);
                _receiveDone = new ManualResetEvent(false);
                _response = null;
                _clientServiceOption = option;
            }
            catch (Exception ex1)
            {
                Log.Fatal("Method:{0} -- Error:[{1}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ClientService Constructor Error:" + ex1.Message);
                DisconnectToServer();
                throw new ArgumentException("ClientService Constructor Error");
            }
        }


        /// <summary>
        /// Gets the client service.
        /// </summary>
        /// <returns></returns>
        public byte[] GetClientService()
        {
            try
            {
                if (_client != null)
                {
                    if (_client.Connected)
                    {
                        return Encoding.ASCII.GetBytes("01");
                    }
                }
                bool isGetClient = false;
                string[] serverArray = new string[2];
                switch (_clientServiceOption)
                {
                    case 1:
                        serverArray[0] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.PrimaryServerName.Trim();
                        serverArray[1] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.BackupServerName.Trim();
                        _portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.ListenerPortNumber.Trim());
                        _connectionTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.ConnectionTimeOut.Trim());
                        _sendTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.SendTimeOut.Trim());
                        _receiveTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCCCClientAuthRouterService.ReceiveTimeOut.Trim());
                        break;
                    case 2:
                        serverArray[0] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.PrimaryServerName.Trim();
                        serverArray[1] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.BackupServerName.Trim();
                        _portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.ListenerPortNumber.Trim());
                        _connectionTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.ConnectionTimeOut.Trim());
                        _sendTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.SendTimeOut.Trim());
                        _receiveTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.ICASHClientAuthRouterService.ReceiveTimeOut.Trim());
                        break;
                        //case 2:
                        //    serverArray[0] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCPSClientAuthRouterService.PrimaryServerName.Trim();
                        //    serverArray[1] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCPSClientAuthRouterService.BackupServerName.Trim();
                        //    _portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCPSClientAuthRouterService.ListenerPortNumber.Trim());
                        //    _connectionTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCPSClientAuthRouterService.ConnectionTimeOut.Trim());
                        //    _sendTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCPSClientAuthRouterService.SendTimeOut.Trim());
                        //    _receiveTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.NCPSClientAuthRouterService.ReceiveTimeOut.Trim());
                        //    break;
                        //case 3:
                        //    serverArray[0] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.IAGClientAuthRouterService.PrimaryServerName;
                        //    serverArray[1] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.IAGClientAuthRouterService.BackupServerName;
                        //    _portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.IAGClientAuthRouterService.ListenerPortNumber.Trim());
                        //    _connectionTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.IAGClientAuthRouterService.ConnectionTimeOut.Trim());
                        //    _sendTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.IAGClientAuthRouterService.SendTimeOut.Trim());
                        //    _receiveTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.IAGClientAuthRouterService.ReceiveTimeOut.Trim());
                        //    break;
                        //case 4:
                        //    serverArray[0] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.STINClientAuthRouterService.PrimaryServerName.Trim();
                        //    serverArray[1] = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.STINClientAuthRouterService.BackupServerName.Trim();
                        //    _portNumber = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.STINClientAuthRouterService.ListenerPortNumber.Trim());
                        //    _connectionTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.STINClientAuthRouterService.ConnectionTimeOut.Trim());
                        //    _sendTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.STINClientAuthRouterService.SendTimeOut.Trim());
                        //    _receiveTimeOut = Convert.ToInt32(SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices.STINClientAuthRouterService.ReceiveTimeOut.Trim());
                        //    break;
                }

                foreach (var serverName in serverArray)
                {
                    var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                    _remoteEP = new IPEndPoint(IPAddress.Parse(serverIPAddr), _portNumber);
                    // Each attempt starts from a clear event and flag. Resetting them only once before the loop let the
                    // primary's failed callback leave _connectDone set, so the backup was judged before it could connect.
                    lock (_connectLock)
                    {
                        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _isConnectionTimeOut = true;
                        _connectDone.Reset();
                    }
                    //Set IOControl
                    const uint dummy = 0;
                    var inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                    //set keep alive on
                    BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                    //interval time between last operation on socket and first checking. example:5000ms=5s
                    BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
                    //after first checking, socket will check several times by 1000ms.
                    BitConverter.GetBytes((uint)1000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        _client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                    _client.BeginConnect(_remoteEP, ConnectCallback, _client);
                    _connectDone.WaitOne(_connectionTimeOut * 1000, false);
                    if (!(_isConnectionTimeOut))
                    {
                        isGetClient = true;
                        break;
                    }
                    Log.Fatal($"Socket Poll Failed, Server EndPoint [{_remoteEP.ToString()}]");
                    // A timed-out socket was left open with its connect still pending (about 21 s on Windows, 2 min
                    // on Linux), and callers call DisconnectToServer only on "00". Closing it also ends that connect.
                    _client.Close();
                }

                if (!isGetClient)
                {
                    Log.Fatal($"GetClientService Error, Server EndPoint [{_remoteEP.ToString()}]");
                    return Encoding.ASCII.GetBytes("02");
                }

                return Encoding.ASCII.GetBytes("00");
            }
            catch (Exception ex1)
            {
                Log.Fatal(ex1, $"Exception : {ex1.Message}");
                DisconnectToServer();
                return Encoding.ASCII.GetBytes("03");
            }
        }


        /// <summary>
        /// Send Message To Server And Receive Message From Server
        /// </summary>
        /// <param name="inputRequest">Input Request Data</param>
        /// <returns>
        /// Return Message From Server
        /// </returns>
        [Obsolete]
        public byte[] Process(byte[] inputRequest)
        {
            byte[] retnBytes;
            try
            {
                retnBytes = GetClientService();
                if (!Encoding.ASCII.GetString(retnBytes).Equals("00"))
                {
                    return retnBytes;
                }

                _isSendTimeOut = true;
                _isReceiveTimeOut = true;
                _response = null;
                _sendDone.Reset();
                _receiveDone.Reset();
                // Send test data to the remote device.
                Send(_client, inputRequest);
                _sendDone.WaitOne(_sendTimeOut * 1000, false);
                if (_isSendTimeOut)
                {
                    DisconnectToServer();
                    throw new TimeoutException("SendTimeOut Exception");
                }
                // Receive the response from the remote device.
                Receive(_client);
                _receiveDone.WaitOne(_receiveTimeOut * 1000, false);
                if (_isReceiveTimeOut)
                {
                    DisconnectToServer();
                    throw new TimeoutException("ReceiveTimeOut Exception");
                }
                retnBytes = new byte[_response.Length];
                Buffer.BlockCopy(_response, 0, retnBytes, 0, retnBytes.Length);
                Log.Info($"Receive Data From Server Data Length[{retnBytes.Length}]");
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Process Error",
                          ex.Message);
                retnBytes = Encoding.ASCII.GetBytes("10");
            }
            return (retnBytes);
        }

        /// <summary>
        /// Send Message To Server And Receive Message From Server By Request
        /// </summary>
        /// <param name="inputRequest">Input Request Data</param>
        /// <returns>
        /// Return Message From Server
        /// </returns>
        public byte[] ProcessByRequest(byte[] inputRequest)
        {
            byte[] retnBytes;
            try
            {
                retnBytes = GetClientService();
                if (!Encoding.ASCII.GetString(retnBytes).Equals("00"))
                {
                    return retnBytes;
                }
                _isSendTimeOut = true;
                _isReceiveTimeOut = true;
                _response = null;
                _sendDone.Reset();
                _receiveDone.Reset();
                // Send test data to the remote device.
                Send(_client, inputRequest);
                _sendDone.WaitOne(_sendTimeOut * 1000, false);
                if (_isSendTimeOut)
                {
                    DisconnectToServer();
                    throw new TimeoutException("SendTimeOut Exception");
                }
                // Receive the response from the remote device.
                Receive(_client);
                _receiveDone.WaitOne(_receiveTimeOut * 1000, false);
                if (_isReceiveTimeOut)
                {
                    DisconnectToServer();
                    throw new TimeoutException("ReceiveTimeOut Exception");
                }
                retnBytes = new byte[_response.Length];
                Buffer.BlockCopy(_response, 0, retnBytes, 0, retnBytes.Length);
                Log.Info($"Receive Data From Server Data Length[{retnBytes.Length}]");
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                         new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                         "ProcessByRequest",
                         ex.Message);
                retnBytes = Encoding.ASCII.GetBytes("20");
            }
            return (retnBytes);
        }

        /// <summary>
        /// Send Message To Server And Receive Message From Server By Connection
        /// </summary>
        /// <param name="inputRequest">Input Request Data</param>
        /// <returns>
        /// Return Message From Server
        /// </returns>
        public byte[] ProcessByConnection(byte[] inputRequest)
        {
            byte[] retnBytes;
            try
            {
                if (_client == null)
                {
                    Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                         new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                         "Process ProcessByRequest Error",
                         "Client Socket Error");
                    retnBytes = Encoding.ASCII.GetBytes("30");
                    return retnBytes;
                }
                _isSendTimeOut = true;
                _isReceiveTimeOut = true;
                _response = null;
                _sendDone.Reset();
                _receiveDone.Reset();
                // Send test data to the remote device.
                Send(_client, inputRequest);
                _sendDone.WaitOne(_sendTimeOut * 1000, false);
                if (_isSendTimeOut)
                {
                    throw new TimeoutException("SendTimeOut Exception");
                }
                // Receive the response from the remote device.
                Receive(_client);
                _receiveDone.WaitOne(_receiveTimeOut * 1000, false);
                if (_isReceiveTimeOut)
                {
                    throw new TimeoutException("ReceiveTimeOut Exception");
                }
                retnBytes = new byte[_response.Length];
                Buffer.BlockCopy(_response, 0, retnBytes, 0, retnBytes.Length);
                Log.Info($"Receive Data From Server Data Length[{retnBytes.Length}]");
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ProcessByConnection",
                          ex.Message);
                retnBytes = Encoding.ASCII.GetBytes("31");
            }
            return (retnBytes);
        }

        /// <summary>
        /// Disconnect Client And Server
        /// </summary>

        public void DisconnectToServer()
        {
            // DisConnect From A Remote Server
            if (_client == null) return;
            try
            {
                if (_client.Connected)
                {
                    _client.Shutdown(SocketShutdown.Both);
                    _client.Disconnect(false);
                }
                _client.Close();
                Log.Info("Disconnect To Server");
            }
            catch (ObjectDisposedException ex1)
            {
                Log.Info("DisconnectToServer ObjectDisposed Exception:" + ex1.Message);
            }
            catch (Exception ex2)
            {
                Log.Warn("DisconnectToServer Exception:" + ex2.Message);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var client = (Socket)ar.AsyncState;

                lock (_connectLock)
                {
                    // A late callback of an attempt that already timed out (its socket is closed) must not mark the
                    // current attempt as connected or wake its wait.
                    if (!ReferenceEquals(client, _client))
                    {
                        return;
                    }

                    // Complete the connection.
                    if (client.Connected)
                    {
                        client.EndConnect(ar);
                        _isConnectionTimeOut = false;
                        // Signal that the connection has been made.
                        Log.Info("Socket Connect To [{0}]", client.RemoteEndPoint);
                    }

                    _connectDone.Set();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ConnectCallback Error",
                          ex.Message);

                _connectDone.Set();
                //throw new ArgumentException("ConnectCallback Error");
            }
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                var state = new StateObject { WorkSocket = client };

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.ReceiveBuffer, 0, StateObject.BufferSize, 0, ReceiveCallback, state);
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "Receive Error",
                          ex.Message);
                throw new ArgumentException("Receive Error");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.
                var state = (StateObject)ar.AsyncState;
                Socket client = state.WorkSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);
                state.ResponseStringBuilder.Append(Encoding.ASCII.GetString(state.ReceiveBuffer, 0, bytesRead));
                _response = new byte[bytesRead];
                Buffer.BlockCopy(state.ReceiveBuffer, 0, _response, 0, bytesRead);
                _isReceiveTimeOut = false;
                _receiveDone.Set();
            }
            catch (SocketException sockex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ReceiveCallback Socket Error Code : " + sockex.SocketErrorCode,
                          sockex.Message);
                _receiveDone.Set();
                //throw new ArgumentException("ReceiveCallback Error");
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "ReceiveCallback Error",
                          ex.Message);

                _receiveDone.Set();
                //throw new ArgumentException("ReceiveCallback Error");
            }
        }

        private void Send(Socket client, byte[] byteData)
        {
            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
            Log.Info($"Send Data To HServer[{client.RemoteEndPoint}] -- Send Data Length[{byteData.Length}]");
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                client.EndSend(ar);
                // Signal that all bytes have been send.
                _isSendTimeOut = false;
                _sendDone.Set();
            }
            catch (SocketException sockex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "SendCallback Socket Error Code : " + sockex.SocketErrorCode,
                          sockex.Message);
                _sendDone.Set();
                //throw new ArgumentException("SendCallback Error");
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                          "SendCallback Error",
                          ex.Message);
                _sendDone.Set();
                //throw new ArgumentException("SendCallback Error");
            }
        }
    }
}

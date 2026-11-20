using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.Test.WpfEAITCPClient
{
    public class SocketAsyncClient
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static readonly ManualResetEvent clientDone = new ManualResetEvent(false);

        private static byte[]? _receiveBytes = null;

        private static byte[]? _sendBytes = null;

        public SocketAsyncClient(string serverName, int serverListenPort, byte[] sendBytes)
        {
            try
            {
                var serverIPAddr = CommonUtils.GetIPAddressStringByHostName(serverName, AddressFamily.InterNetwork);
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverIPAddr), serverListenPort);
                _sendBytes = new byte[sendBytes.Length];
                Array.Copy(sendBytes, 0, _sendBytes, 0, sendBytes.Length);
                // Port number of server
                var socketEventArg = new SocketAsyncEventArgs();
                // Create a socket and connect to the server
                var sock = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(SocketEventArg_Completed);
                socketEventArg.RemoteEndPoint = remoteEndPoint;
                socketEventArg.UserToken = sock;
                sock.ConnectAsync(socketEventArg);
                clientDone.WaitOne();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SocketAsyncClient Exception");
            }
        }

        /// <summary>
        /// A single callback is used for all socket operations. This method forwards execution on to the correct handler 
        /// based on the type of completed operation
        /// </summary>
        static void SocketEventArg_Completed(object? sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new Exception("Invalid operation completed");
            }
        }

        /// <summary>
        /// Called when a ConnectAsync operation completes
        /// </summary>
        private static void ProcessConnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Log.Info("Successfully connected to the server");

                // Send data to the server
                if (_sendBytes == null)
                {
                    throw new InvalidOperationException("_sendBytes is null.");
                }
                e.SetBuffer(_sendBytes, 0, _sendBytes.Length);
                var willRaiseEvent = e.UserToken is Socket sock && sock.SendAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessSend(e);
                }
            }
            else
            {
                throw new SocketException((int)e.SocketError);
            }
        }

        /// <summary>
        /// Called when a ReceiveAsync operation completes
        /// </summary>
        private static void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // Check if e.Buffer is null to avoid CS8604
                if (e.Buffer == null)
                {
                    Log.Error("SocketAsyncEventArgs.Buffer is null, unable to decode received data");
                }
                else
                {
                    Log.Info("Received from server: {0}", Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred));
                    _receiveBytes = new byte[e.BytesTransferred];
                }

                // Data has now been sent and received from the server. Disconnect from the server
                if (e.UserToken is Socket sock)
                {
                    sock.Shutdown(SocketShutdown.Send);
                    sock.Close();
                }

                clientDone.Set();
            }
            else
            {
                throw new SocketException((int)e.SocketError);
            }
        }


        /// <summary>
        /// Called when a SendAsync operation completes
        /// </summary>
        private static void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Log.Info("Sent 'Hello World' to the server");

                //Read data sent from the server
                bool willRaiseEvent = e.UserToken is Socket sock && sock.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                throw new SocketException((int)e.SocketError);
            }
        }
    }
}

using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.SMS;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using NLog;
using System;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfFiscMaintSimulator
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public class ServerToken : IDisposable
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
        internal ServerToken(Socket connection, int bufferSize)
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
                Log.Info($"Receive: [{BitConverter.ToString(receiveBytes).Replace("-", "")}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has read {_currentIndex} bytes.");
                //Process FISC 0300 Message
                //Convert Bytes array To Structure
                byte[] processBytes = new byte[receiveBytes.Length - 2];
                Buffer.BlockCopy(receiveBytes, 2, processBytes, 0, processBytes.Length);
                //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                //Encoding ascii  = Encoding.ASCII;
                //Encoding ebcdic = Encoding.GetEncoding("IBM037");
                //byte[] convertedByte = Encoding.Convert(ebcdic, ascii, processBytes);
                //string convertedString = Encoding.ASCII.GetString(convertedByte);
                string convertedString = Encoding.ASCII.GetString(processBytes);
                Log.Info($"Receive ASCII: [{convertedString}]");
                int offset = 0;
                int dataLength = 4;
                string isoMessageType = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 1;
                string isoRecType = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 1;
                string isoUpdCode = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 16;
                string isoCardNbr = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 3;
                string isoProdType = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 2;
                string isoActionCode = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 1;
                string isoFileName = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 13;
                string isoCRBRegion = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 12;
                string isoVIPAmt = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 4;
                string isoPurgeDte = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 8;
                string isoSendDte = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 8;
                string isoSendTime = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 2;
                string isoResp = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 6;
                string isoTraceNbr = convertedString.Substring(offset, dataLength);
                offset += dataLength;
                dataLength = 54;
                string isoAddData = convertedString.Substring(offset, dataLength);
                //Create Response 0310 Message
                string outputString = String.Format("{0,-4}{1,-1}{2,-1}{3,-16}{4,-3}{5,-2}{6,-1}{7,-13}{8,-12}{9,-4}{10,-8}{11,-8}{12,-2}{13,-6}{14,-54}",
                                                    "0310",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    " ",
                                                    "00",
                                                    isoTraceNbr,
                                                    " ");
                //var ebcdicByteArray = Encoding.GetEncoding("IBM037").GetBytes(outputString);
                var asciiByteArray = Encoding.ASCII.GetBytes(outputString);
                var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(asciiByteArray);
                args.SetBuffer(sendBytes, 0, sendBytes.Length);
                Log.Info($"Send: [{BitConverter.ToString(sendBytes).Replace("-", "")}]. The server has send {sendBytes.Length} bytes.");
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

            // No unmanaged resources to release

            _disposed = true;
        }

        #endregion
    }
}

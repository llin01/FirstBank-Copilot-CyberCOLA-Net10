namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;
    using System.Net;
    using System.Text;

    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Utils;
    using Com.Cybersoft4u.Utils;

    #endregion

    public class FiscLoggerHandler : ChannelDuplexHandler
    {
        private readonly LogLevel logLevel;

        private readonly Logger logger;

        public FiscLoggerHandler(string name, LogLevel logLevel)
        {
            this.logger = LoggerAdapter.GetLogger($"FiscLoggerHandler[{name}]");
            this.logLevel = logLevel;
        }

        public FiscLoggerHandler(LogLevel logLevel)
            : this("FiscLoggerHandler", logLevel)
        {
        }

        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            byte[] bytes = null;

            if (msg is ByteBuf)
            {
                bytes = ((ByteBuf)msg).Val();
            }
            else
                if (msg is byte[])
                {
                    bytes = (byte[])msg;
                }
            try
            {
                var length = bytes == null ? 0 : bytes.Length;
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                var remoteEndPoint = ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A";

                this.WriteLog(
                    () =>
                    string.Format(
                        "WRITE [{0} => {1}]\nLength: {2}B\n{3}",
                        localEndPoint,
                        remoteEndPoint,
                        length,
                        PrintBytesMap(bytes)));
                this.WriteLog(
                    () =>
                    string.Format("[{0}]",
                        PrintBytesHex(bytes)));
            }
            catch (Exception)
            {
                // ignored
            }
            base.Write(ctx, msg);
        }

        public override void Flush(ChannelHandlerContext ctx)
        {
            try
            {
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                var remoteEndPoint = ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A";
                this.WriteLog(() => string.Format("FLUSH [{0} => {1}]", localEndPoint, remoteEndPoint));
            }
            catch (Exception)
            {
                // ignored
            }

            base.Flush(ctx);
        }

        public override void Bind(ChannelHandlerContext ctx, EndPoint localAddress)
        {
            this.WriteLog("BIND");
            base.Bind(ctx, localAddress);
        }

        public override void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint)
        {
            this.WriteLog(() => string.Format("CONNECT [{0}]", remoteEndPoint));
            base.Connect(ctx, remoteEndPoint);
        }

        public override void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            this.WriteLog(() => string.Format("CONNECT [{0}] to [{1}]", localEndPoint, remoteEndPoint));
            base.Connect(ctx, remoteEndPoint, localEndPoint);
        }

        public override void Disconnect(ChannelHandlerContext ctx)
        {
            try
            {
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                var remoteEndPoint = ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A";

                this.WriteLog(() => string.Format("DISCONNECT [{0} => {1}]", localEndPoint, remoteEndPoint));
            }
            catch (Exception)
            {
                // ignored
            }

            base.Disconnect(ctx);
        }

        public override void Close(ChannelHandlerContext ctx)
        {
            try
            {
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                this.WriteLog(() => string.Format("CLOSE [{0}]", localEndPoint));
            }
            catch (Exception)
            {
                // ignored
            }

            base.Close(ctx);
        }

        public override void ChannelAdded(ChannelHandlerContext ctx)
        {
            this.WriteLog("CHANNELADDED");
            base.ChannelAdded(ctx);
        }

        public override void ChannelRemoved(ChannelHandlerContext ctx)
        {
            this.WriteLog("CHANNELREMOVED");
            base.ChannelRemoved(ctx);
        }

        public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            this.WriteLog("EXCEPTIONCAUGHT");
            base.ExceptionCaught(ctx, cause);
        }

        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            try
            {
                this.WriteLog(
                    () =>
                    ctx?.Channel is IServerSocket
                        ? string.Format("CHANNELACTIVED [{0}]", ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A")
                        : string.Format("CHANNELACTIVED [{0} => {1}]", ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A", ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A"));
            }
            catch (Exception)
            {
                // ignored
            }

            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            try
            {
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                var remoteEndPoint = ctx?.Channel is IServerSocket ? string.Empty : (ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A");

                this.WriteLog(
                    () =>
                    ctx?.Channel is IServerSocket
                        ? string.Format("CHANNELINACTIVED [{0}]", localEndPoint)
                        : string.Format("CHANNELINACTIVED [{0} => {1}]", localEndPoint, remoteEndPoint));
            }
            catch (Exception)
            {
                // ignored
            }
            base.ChannelInActived(ctx);
        }

        public override void ChannelRegistered(ChannelHandlerContext ctx)
        {
            this.WriteLog("CHANNELREGISTERED");
            base.ChannelRegistered(ctx);
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            this.WriteLog("CHANNELUNREGISTERED");
            base.ChannelUnRegistered(ctx);
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            byte[] bytes = null;

            if (msg is ByteBuf)
            {
                bytes = ((ByteBuf)msg).Val();
            }
            else
                if (msg is byte[])
                {
                    bytes = (byte[])msg;
                }
            try
            {
                var length = bytes == null ? 0 : bytes.Length;
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                var remoteEndPoint = ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A";

                this.WriteLog(
                    () =>
                    string.Format(
                        "CHANNELREAD [{0} => {1}]\nLength: {2}B\n{3}",
                        remoteEndPoint,
                        localEndPoint,
                        length,
                        PrintBytesMap(bytes)));
                this.WriteLog(
                    () =>
                    string.Format("[{0}]",
                        PrintBytesHex(bytes)));
                //Write Raw Data To Unpack String
                this.WriteLog(() => string.Format("CHANNELREAD Raw Data Unpack String[{0}]", BitConverter.ToString(bytes).Replace("-", "")));
                //check dummy record
                if ((length == 2) && ((bytes[0] == 0x00) && (bytes[1] == 0x00)))
                {
                    this.WriteLog(() => string.Format("Receive dummy data no process"));
                    return;
                }
                //check data for reformat


            }
            catch (Exception)
            {
                // ignored
            }

            base.ChannelRead(ctx, msg);
        }

        public override void ChannelReadCompleted(ChannelHandlerContext ctx)
        {
            try
            {
                var localEndPoint = ctx?.Channel?.LocalEndPoint()?.ToString() ?? "N/A";
                var remoteEndPoint = ctx?.Channel?.RemoteEndPoint()?.ToString() ?? "N/A";

                this.WriteLog(() => string.Format("CHANNELREADCOMPLETED [{0} => {1}]", remoteEndPoint, localEndPoint));
            }
            catch (Exception)
            {
                // ignored
            }
            base.ChannelReadCompleted(ctx);
        }

        private static string PrintBytesMap(byte[] bytes)
        {
            var dataOut = new StringBuilder();
            dataOut.Append(
                "         +-------------------------------------------------+\n         |  0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f |\n+--------+-------------------------------------------------+----------------+");
            var length = bytes.Length;
            var index = 0;
            var byteContent = string.Empty;
            var dataContent = string.Empty;
            foreach (var b in bytes)
            {
                if (index % 16 == 0)
                {
                    dataOut.Append(string.Format("\n|{0}| ", (index / 16).ToString().PadLeft(8, '0')));
                    byteContent = string.Empty;
                    dataContent = string.Empty;
                }

                //byteContent += $"{b:X2} ";
                //dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding ascii = Encoding.ASCII;
                Encoding ebcdic = Encoding.GetEncoding("IBM037");
                byte[] bytesArray = { b };
                byte[] convertedByte = Encoding.Convert(ebcdic, ascii, bytesArray);
                byteContent += $"{b:X2} ";
                if (index <= 1)
                {
                    dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);
                }
                else
                {
                    dataContent += convertedByte[0] < 32 || convertedByte[0] == 94 || convertedByte[0] > 126 ? '.' : Convert.ToChar(convertedByte[0]);
                }

                if (index % 16 == 15 | index + 1 == length)
                {
                    dataOut.AppendFormat("{0}|{1}|", byteContent.PadRight(48, ' '), dataContent.PadRight(16, ' '));
                }

                index++;
            }

            dataOut.Append("\n+--------+-------------------------------------------------+----------------+");
            return dataOut.ToString();
        }

        private static string PrintBytesHex(byte[] bytes)
        {
            var dataOut = new StringBuilder();

            foreach (var b in bytes)
            {
                dataOut.Append(string.Format("{0:X2}", b));
            }

            return dataOut.ToString();
        }

        private static string PrintBytesData(byte[] bytes)
        {
            var dataOut = new StringBuilder();

            foreach (var b in bytes)
            {
                dataOut.Append(b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b));
            }

            return dataOut.ToString();
        }

        private void WriteLog(Logger.LogTask logTask)
        {
            switch (this.logLevel)
            {
                case LogLevel.Debug:
                    this.logger.Debug(logTask);
                    break;
                case LogLevel.Info:
                    this.logger.Info(logTask);
                    break;
                case LogLevel.Warn:
                    this.logger.Warn(logTask);
                    break;
                case LogLevel.Error:
                    this.logger.Error(logTask);
                    break;
                case LogLevel.Fatal:
                    this.logger.Fatal(logTask);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void WriteLog(string message)
        {
            switch (this.logLevel)
            {
                case LogLevel.Debug:
                    this.logger.Debug(message);
                    break;
                case LogLevel.Info:
                    this.logger.Info(message);
                    break;
                case LogLevel.Warn:
                    this.logger.Warn(message);
                    break;
                case LogLevel.Error:
                    this.logger.Error(message);
                    break;
                case LogLevel.Fatal:
                    this.logger.Fatal(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

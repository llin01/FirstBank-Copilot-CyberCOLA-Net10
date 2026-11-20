// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServiceProviderLoggerHandlerFISC.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServiceProviderLoggerHandlerFISC type.
//   have any cola session must be update ChannelActived routine
//   ***********************************************************
//   ChannelActived & ChannelInActived
//   CyberGWFISCResource.FiscColaService?.AddChannel(ctx, dateTimeNow);
//   CyberGWFISCIDVerifyResource.FiscIDVerifyColaService?.AddChannel(ctx, dateTimeNow);
//   ***********************************************************
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers
{
    public class ServiceProviderLoggerHandlerFISC : LoggerHandler
    {
        private readonly Logger debugLogger;
        private readonly string serviceName;

        public ServiceProviderLoggerHandlerFISC(string name)
            : base(name, LogLevel.Info)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            this.debugLogger = LoggerAdapter.GetLogger(name);
            this.serviceName = name;
        }

        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            byte[] bytes = null;

            if (msg is ByteBuf)
            {
                bytes = ((ByteBuf)msg).Val();
            }
            try
            {
                var length = bytes?.Length ?? 0;
                IPEndPoint localEndPoint;
                IPEndPoint remoteEndPoint;
                if (ctx.Channel.LocalEndPoint() == null)
                {
                    localEndPoint = ctx.Channel.Carrier<LocalEndPointKey, IPEndPoint>();
                    if (localEndPoint != null)
                        ctx.Channel.Carrier<LocalEndPointKey>(localEndPoint);
                }
                else
                {
                    localEndPoint = (IPEndPoint)ctx.Channel.LocalEndPoint();
                }

                if (ctx.Channel.RemoteEndPoint() == null)
                {
                    remoteEndPoint = ctx.Channel.Carrier<RemoteEndPointKey, IPEndPoint>();
                    if (remoteEndPoint != null)
                        ctx.Channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                }
                else
                {
                    remoteEndPoint = (IPEndPoint)ctx.Channel.RemoteEndPoint();
                }


                var relationId = string.Empty;
                var responseCode = string.Empty;
                var mti = string.Empty;
                try
                {
                    if (bytes != null)
                    {
                        var dataBytes = ColaCommUtility.GetSubBytes(bytes, 2, bytes.Length - 2);
                        var requestMessage = new TM.FISC.FISC(dataBytes);
                        relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                   $"{requestMessage.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                   $"{requestMessage.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                   $"{requestMessage.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                   $"{requestMessage.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                   );
                        mti = $"{requestMessage.Mti()}";
                        responseCode = $"{requestMessage.Core().Field(FIELD.F_39) ?? $"{string.Empty}"}";
                    }
                }
                catch (Exception ex)
                {
                    debugLogger.Error("ServiceProviderLoggerHandlerFISC Write.", ex);
                }
                debugLogger.Info($"WRITE [{localEndPoint} => {remoteEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], MTI[{mti}], RelationId[{relationId}], ResponseCode[{responseCode}], Length: {length}B");
                // The hex dump is Debug only: at Info it was most of the session log volume under load.
                if (debugLogger.IsDebugEnabled && bytes != null)
                {
                    debugLogger.Debug($"WRITE [{localEndPoint} => {remoteEndPoint}] Length: {length}B\n{PrintBytesMap(bytes)}");
                }
            }
            catch (Exception)
            {
                // ignored
            }

            ctx.Write(msg);
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            byte[] bytes = null;

            if (msg is ByteBuf)
            {
                bytes = ((ByteBuf)msg).Val();
            }
            try
            {
                var length = bytes?.Length ?? 0;
                var localEndPoint = ctx.Channel.LocalEndPoint().ToString();
                var remoteEndPoint = ctx.Channel.RemoteEndPoint().ToString();
                ctx.Channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                var relationId = string.Empty;
                var responseCode = string.Empty;
                var mti = string.Empty;
                try
                {
                    if (bytes != null)
                    {
                        if (bytes.Length >= 2)
                        {
                            int receiveDataLength = Convert.ToInt16(bytes[0]) * 256 + Convert.ToInt16(bytes[1]);
                            debugLogger.Info($"CHANNELREAD [{remoteEndPoint}] => [{localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Receive Data Length[{receiveDataLength}], Real Receive Data Length[{bytes.Length - 2}]");
                        }
                        var dataBytes = ColaCommUtility.GetSubBytes(bytes, 2, bytes.Length - 2);
                        try
                        {
                            var message = new TM.FISC.FISC(dataBytes);
                            relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{message.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{message.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{message.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{message.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                       );
                            mti = $"{message.Mti()}";
                            responseCode = $"{message.Core().Field(FIELD.F_39) ?? $"{string.Empty}"}";
                        }
                        catch (Exception ex)
                        {
                            debugLogger.Error(() => $"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], MTI[{mti}], RelationId[{relationId}], ResponseCode[{responseCode}], Exception[{ex.Message}]\nLength: {length}B\n{PrintBytesMap(bytes)}");
                            throw new InvalidDataException(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    debugLogger.Error($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] ServiceProviderLoggerHandlerFISC ChannelRead.", ex);
                }
                debugLogger.Info($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], MTI[{mti}], RelationId[{relationId}], ResponseCode[{responseCode}], Length: {length}B");
                // The hex dump is Debug only: at Info it was most of the session log volume under load.
                if (debugLogger.IsDebugEnabled && bytes != null)
                {
                    debugLogger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Length: {length}B\n{PrintBytesMap(bytes)}");
                }
            }
            catch (Exception ex)
            {
                debugLogger.Error($"CHANNELREAD [{ctx.Channel.RemoteEndPoint()} => {ctx.Channel.LocalEndPoint()}] ServiceProviderLoggerHandlerFISC ChannelRead.", ex);
            }

            ctx.FireChannelRead(msg);
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
                    dataOut.Append($"\n|{(index / 16).ToString().PadLeft(8, '0')}| ");
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

        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            bool isServerSocket = ctx.Channel is IServerSocket;
            if (!isServerSocket)
            {
                if (serviceName.Equals("FiscColaSession"))
                {
                    var dateTimeNow = DateTime.Now;
                    CyberGWFISCResource.FiscColaService?.AddChannel(ctx, dateTimeNow);
                }
                else if (serviceName.Equals("FiscIDVerifyColaSession"))
                {
                    var dateTimeNow = DateTime.Now;
                    CyberGWFISCIDVerifyResource.FiscIDVerifyColaService?.AddChannel(ctx, dateTimeNow);
                }
            }
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            bool isServerSocket = ctx.Channel is IServerSocket;
            if (!isServerSocket)
            {
                if (serviceName.Equals("FiscColaSession"))
                    CyberGWFISCResource.FiscColaService?.RemoveChannel(ctx);
                else if (serviceName.Equals("FiscIDVerifyColaSession"))
                    CyberGWFISCIDVerifyResource.FiscIDVerifyColaService?.RemoveChannel(ctx);
            }
            base.ChannelInActived(ctx);
        }
    }
}

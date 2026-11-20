// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServiceProviderLoggerHandlerUseBufferUseBuffer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServiceProviderLoggerHandlerUseBuffer type.
//   have any cola session must be update ChannelActived routine
//   ***********************************************************
//   ChannelActived & ChannelInActived
//   CyberGWNCCCResource.NcccColaService?.AddChannel(ctx, dateTimeNow);
//   ***********************************************************
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.MCOLA;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers
{
    public class ServiceProviderLoggerHandlerUseBuffer : LoggerHandler
    {
        private readonly Logger debugLogger;
        private readonly string serviceName;
        private readonly byte[] receiveBuffer;
        private int receiveBufferPointer = 0;
        private static readonly object LockObject = new object();

        public ServiceProviderLoggerHandlerUseBuffer(string name)
            : base(name, LogLevel.Info)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            debugLogger = LoggerAdapter.GetLogger(name);
            serviceName = name;
            receiveBuffer = new byte[20000];
            receiveBufferPointer = 0;
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
                //var localEndPoint = ctx.Channel.LocalEndPoint().ToString();
                //var remoteEndPoint = ctx.Channel.RemoteEndPoint().ToString();
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
                        byte[] dataBytes = null;
                        if (Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(bytes, 2, 3)).Equals("ISO"))
                        {
                            dataBytes = ColaCommUtility.GetSubBytes(bytes, 2, bytes.Length - 2);
                            var requestMessage = new NcccIso8583(dataBytes);
                            relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{requestMessage.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{requestMessage.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{requestMessage.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{requestMessage.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                                       );
                            mti = $"{requestMessage.Mti()}";
                            responseCode = $"{requestMessage.Core().Field(FIELD.F_39) ?? $"{string.Empty}"}";
                        }
                        else
                        {
                            dataBytes = ColaCommUtility.GetSubBytes(bytes, 2, bytes.Length - 2);
                            var requestMessage = new MCOLA(dataBytes);
                            relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{requestMessage.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{requestMessage.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{requestMessage.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{requestMessage.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                                       );
                            mti = $"{requestMessage.Mti()}";
                            responseCode = $"{requestMessage.Core().Field(FIELD.F_39) ?? $"{string.Empty}"}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    debugLogger.Error("ServiceProviderLoggerHandlerUseBuffer Write.", ex);
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
                    var listReceiveBytes = GetProcessListBytes(bytes, remoteEndPoint, localEndPoint);
                    if (listReceiveBytes.Count <= 0)
                    {
                        debugLogger.Warn($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], ChannelRead No Process");
                        return;
                    }
                    else
                    {
                        debugLogger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], ChannelRead Process List Count[{listReceiveBytes.Count}]");
                    }
                    foreach (var dataBytes in listReceiveBytes)
                    {
                        try
                        {
                            var message = new NcccIso8583(ColaCommUtility.GetSubBytes(dataBytes, 2, dataBytes.Length - 2));
                            relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{message.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{message.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{message.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{message.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                                       );
                            mti = $"{message.Mti()}";
                            responseCode = $"{message.Core().Field(FIELD.F_39) ?? $"{string.Empty}"}";
                            debugLogger.Info($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], MTI[{mti}], RelationId[{relationId}], ResponseCode[{responseCode}]");
                            var newMsg = new ByteBuf(dataBytes);
                            ctx.FireChannelRead(newMsg);
                        }
                        catch (Exception ex)
                        {
                            debugLogger.Error($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}], Exception[{ex.Message}]\nLength: {dataBytes.Length}B\n{PrintBytesMap(dataBytes)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    debugLogger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{ex.Message}]");
                }
            }
            catch (Exception ex)
            {
                debugLogger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Exception : [{ex.Message}]");
            }
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

                byteContent += string.Format("{0:X2} ", b);
                dataContent += b < 32 || b == 94 || b > 126 ? '.' : Convert.ToChar(b);

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
                if (serviceName.Equals("NcccColaSession"))
                {
                    var dateTimeNow = DateTime.Now;
                    CyberGWNCCCResource.NcccColaService?.AddChannel(ctx, dateTimeNow);
                }
            }
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            bool isServerSocket = ctx.Channel is IServerSocket;
            if (!isServerSocket)
            {
                if (serviceName.Equals("NcccColaSession"))
                    CyberGWNCCCResource.NcccColaService?.RemoveChannel(ctx);
            }
            base.ChannelInActived(ctx);
        }

        private List<byte[]> GetProcessListBytes(byte[] receiveBytes, string remoteEndPoint, string localEndPoint)
        {
            try
            {
                lock (LockObject)
                {
                    if (receiveBytes != null)
                    {
                        debugLogger.Debug($"Before CHANNELREAD [{remoteEndPoint} => {localEndPoint}], GetProcessListBytes Byte Length[{receiveBytes.Length}], ReceiveBufferPointer[{receiveBufferPointer}]");
                        //if (receiveBytes.Length >= 2)
                        //{
                        //    int receiveDataLength = ((Convert.ToInt16(receiveBytes[0])) * 256) + (Convert.ToInt16(receiveBytes[1]));
                        //    debugLogger.Info($"CHANNELREAD [{remoteEndPoint}] => [{localEndPoint}] Time[{DateTime.Now:yyyyMMdd HHmmssfffff}],First 2 Bytes Data Length[{receiveDataLength}], Real Receive Data Length[{receiveBytes.Length - 2}]");
                        //}

                        Buffer.BlockCopy(receiveBytes, 0, receiveBuffer, receiveBufferPointer, receiveBytes.Length);
                        receiveBufferPointer = receiveBufferPointer + receiveBytes.Length;
                        debugLogger.Debug($"After CHANNELREAD [{remoteEndPoint} => {localEndPoint}], GetProcessListBytes Byte Length[{receiveBytes.Length}], ReceiveBufferPointer[{receiveBufferPointer}]");
                        var listReceiveBytes = new List<byte[]>();
                        var startIndexPoint = 0;
                        while ((startIndexPoint + 2) <= receiveBufferPointer)
                        {
                            int processDataLength = Convert.ToInt16(receiveBuffer[startIndexPoint]) * 256 + Convert.ToInt16(receiveBuffer[startIndexPoint + 1]);
                            debugLogger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], Binary data length[{processDataLength}]");
                            if (receiveBufferPointer < (startIndexPoint + 2 + processDataLength))
                            {
                                debugLogger.Warn($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], Buffer data length is not enough, Buffer Pointer[{receiveBufferPointer}], StartIndexPoint[{startIndexPoint}], ProcessDataLength[{processDataLength}], ReceiveBytesLength[{receiveBytes.Length}]");
                                debugLogger.Warn($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], Formula [{receiveBufferPointer}] must be large than [{startIndexPoint + 2 + processDataLength}]");
                                break;
                            }
                            var processBytes = new byte[2 + processDataLength];
                            Buffer.BlockCopy(receiveBuffer, startIndexPoint, processBytes, 0, 2 + processDataLength);
                            listReceiveBytes.Add(processBytes);
                            if (debugLogger.IsDebugEnabled)
                            {
                                debugLogger.Debug($"ListReceiveBytes CHANNELREAD [{remoteEndPoint} => {localEndPoint}] , StartIndexPoint[{startIndexPoint}], ProcessDataLength[{processDataLength}]\nLength: {processBytes.Length}B\n{PrintBytesMap(processBytes)}");
                            }
                            startIndexPoint += (2 + processDataLength);
                        }

                        if (listReceiveBytes.Count > 0)
                        {
                            byte[] tempBytes = new byte[receiveBufferPointer - startIndexPoint];
                            Buffer.BlockCopy(receiveBuffer, startIndexPoint, tempBytes, 0, receiveBufferPointer - startIndexPoint);
                            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                            Buffer.BlockCopy(tempBytes, 0, receiveBuffer, 0, tempBytes.Length);
                            receiveBufferPointer = tempBytes.Length;
                            debugLogger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], Before ReceiveBufferPointer[{receiveBufferPointer} + {startIndexPoint}], After ReceiveBufferPointer[{receiveBufferPointer}]");
                        }

                        debugLogger.Debug($"CHANNELREAD [{remoteEndPoint} => {localEndPoint}], ReceiveBufferPointer[{receiveBufferPointer}]");
                        return listReceiveBytes;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                debugLogger.Error($"Call {new StackTrace().GetFrame(0)?.GetMethod()?.Name} Process ChangeKey Exception : [{ex.Message}]");
                return null;
            }
        }
    }
}

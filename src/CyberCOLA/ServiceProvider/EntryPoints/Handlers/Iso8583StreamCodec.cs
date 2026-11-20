// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Iso8583StreamCodec.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Iso8583StreamCode type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers
{
    #region DEPENDENCY
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Utils;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.Yetty.Utils;
    #endregion
    public class Iso8583StreamCode<TIso8583> : ReplayingDecoder<Iso8583StreamCode<TIso8583>.DecodeState>,
                                                   IChannelOutboundHandler
        where TIso8583 : Iso8583, new()
    {
        public delegate void ErrorTask(ChannelHandlerContext ctx, Exception cause, object decodedContent);

        public enum DecodeState
        {
            Length,

            Content
        }

        private readonly long maxDecodeTime;

        private byte[] decodedContent;

        private int decodeMessageLength;

        private long decodeTime;

        public Iso8583StreamCode(
            long maxDecodeTimeInMs = 30000,
            ErrorTask inboundErrorTask = null,
            ErrorTask outboundErrorTask = null)
            : base(DecodeState.Length)
        {
            this.maxDecodeTime = maxDecodeTimeInMs;
            this.InboundErrorTask = inboundErrorTask;
            this.OutboundErrorTask = outboundErrorTask;
            this.Logger = LoggerAdapter.GetLogger($"{this.GetType().Name}[{typeof(TIso8583).Name}]");
        }

        protected ErrorTask InboundErrorTask { get; set; }

        protected ErrorTask OutboundErrorTask { get; set; }

        public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            this.Logger.Error(cause);
            if (this.InboundErrorTask == null)
            {
                this.InboundErrorTask = (context, exception, content) => { context.Channel.Disconnect(); };
            }

            this.InboundErrorTask(ctx, cause, this.decodedContent);
        }

        public void Read(ChannelHandlerContext ctx)
        {
            ctx.Read();
        }

        public void Write(ChannelHandlerContext ctx, object msg)
        {
            try
            {
                if (msg == null)
                {
                    throw new InvalidTypeException("msg == null");
                }

                //check object msg is byte[] for fisc 0300 fixed iso message
                if (msg is System.Byte[])
                {
                    ctx.Write((byte[])msg);
                    this.Logger.Info($"Write Byte Data[{BitConverter.ToString((byte[])msg)?.Replace("-", "")}, Length[{((byte[])msg)?.Length}]");
                    return;
                }

                TIso8583 val;
                if (msg is PayloadContext)
                {
                    try
                    {
                        val = (TIso8583)((PayloadContext)msg).Val;
                    }
                    catch (Exception)
                    {
                        throw new InvalidTypeException("val is not ISO8583");
                    }

                    if (msg is Recyclable)
                    {
                        Recycler.Recycle(msg as Recyclable);
                    }
                }
                else if (msg is TIso8583)
                {
                    val = (TIso8583)msg;
                }
                else
                {
                    throw new InvalidTypeException("msg is not PayloadContext");
                }

                this.Logger.Debug("Encode State");
                ctx.Write(this.Encode(val.Encode().Val()));
            }
            catch (Exception cause)
            {
                this.EncodeExceptionCaught(ctx, cause, msg);
            }
        }

        public void Flush(ChannelHandlerContext ctx)
        {
            ctx.Flush();
        }

        public void Bind(ChannelHandlerContext ctx, EndPoint localAddress)
        {
            ctx.Bind(localAddress);
        }

        public void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint)
        {
            ctx.Connect(remoteEndPoint);
        }

        public void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            ctx.Connect(remoteEndPoint, localEndPoint);
        }

        public void Disconnect(ChannelHandlerContext ctx)
        {
            ctx.Disconnect();
        }

        public void Close(ChannelHandlerContext ctx)
        {
            ctx.Close();
        }

        protected void EncodeExceptionCaught(ChannelHandlerContext ctx, Exception cause, object writeObject)
        {
            this.Logger.Error(cause);
            if (this.OutboundErrorTask == null)
            {
                this.OutboundErrorTask = (context, exception, content) => { context.Disconnect(); };
            }

            this.OutboundErrorTask(ctx, cause, writeObject);
        }

        protected override void Decode(ChannelHandlerContext ctx, ByteBuf dataIn, List<object> dataOut)
        {
            while (true)
            {
                switch (this.State())
                {
                    case DecodeState.Length:
                        this.Logger.Debug("Decode Length State");
                        this.DecodeLength(dataIn);
                        this.Logger.Debug(() => string.Format("Decode Data Length:{0}", this.decodeMessageLength));
                        continue;
                    case DecodeState.Content:
                        this.Logger.Debug("Decode Content State");
                        dataOut.Add(new PayloadContext(this.DecodeContent(dataIn)));
                        if (dataIn.ReadRemainLength() > 0)
                        {
                            continue;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            }
        }

        protected ByteBuf Encode(byte[] data)
        {
            return new ByteBuf().WriteUnsignedShort((ushort)data.Length).WriteBytes(data);
        }

        private static TIso8583 GetIso8583Instance()
        {
            return new TIso8583();
        }

        private void DecodeLength(ByteBuf dataIn)
        {
            this.decodeMessageLength = dataIn.ReadUnsignedShort();
            this.Checkpoint(DecodeState.Content);
            this.decodeTime = TimeUtils.CurrentMillisecond();
        }

        private object DecodeContent(ByteBuf dataIn)
        {
            var current = TimeUtils.CurrentMillisecond();
            if (current - this.decodeTime > this.maxDecodeTime)
            {
                throw new DecodeTimeoutException();
            }

            try
            {
                this.decodedContent = dataIn.ReadBytes(this.decodeMessageLength);
                var masterCard = GetIso8583Instance().Decode(this.decodedContent);
                this.Checkpoint(DecodeState.Length);
                return masterCard;
            }
            catch (DataNotEnoughException)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.Logger.Error(exception);
                this.Logger.Error("Content decode failed, discard all remain buffer.");
                this.Discard(dataIn);
                this.Checkpoint(DecodeState.Length);
                throw;
            }
        }
    }
}

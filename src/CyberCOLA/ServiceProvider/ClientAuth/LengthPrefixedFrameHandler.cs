using System;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.ServiceProvider.ClientAuth
{
    /// <summary>
    /// Splits the TCP stream from the GW's COLA port into 2-byte length-prefixed messages (header kept) and
    /// keeps any incomplete tail for the next read. The Auth InboundHandlers treat each read as one message,
    /// so without this a read holding several messages, or only part of one, was processed incorrectly.
    /// Must sit before InboundMultiWorkerHandler: that handler runs each read on its own task, so a split
    /// message can only be reassembled while reads are still in order. Create one instance per channel.
    /// </summary>
    public class LengthPrefixedFrameHandler : ChannelInboundHandler
    {
        private byte[] _pending = Array.Empty<byte>();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            if (!(msg is ByteBuf buf))
            {
                base.ChannelRead(ctx, msg);
                return;
            }

            var incoming = buf.Val();
            var data = new byte[_pending.Length + incoming.Length];
            Buffer.BlockCopy(_pending, 0, data, 0, _pending.Length);
            Buffer.BlockCopy(incoming, 0, data, _pending.Length, incoming.Length);

            var offset = 0;
            while (data.Length - offset >= 2)
            {
                var frameLength = 2 + ((data[offset] << 8) | data[offset + 1]);
                if (data.Length - offset < frameLength)
                {
                    break;
                }

                var frame = new byte[frameLength];
                Buffer.BlockCopy(data, offset, frame, 0, frameLength);
                offset += frameLength;
                ctx.FireChannelRead(new ByteBuf(frame));
            }

            _pending = data[offset..];
            if (_pending.Length > 0)
            {
                Logger.Debug($"CHANNELREAD [{ctx.Channel.RemoteEndPoint()} => {ctx.Channel.LocalEndPoint()}], waiting for the rest of a split message, Buffered[{_pending.Length}]B");
            }
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            _pending = Array.Empty<byte>();
            base.ChannelInActived(ctx);
        }
    }
}

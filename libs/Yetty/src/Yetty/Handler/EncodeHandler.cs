namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    public abstract class EncodeHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            if (msg is ByteBuf)
            {
                ctx.Write(this.Encode(ctx, (ByteBuf)msg));
            }
            else
            {
                ctx.Write(msg);
            }
        }

        protected abstract ByteBuf Encode(ChannelHandlerContext ctx, ByteBuf msg);
    }
}

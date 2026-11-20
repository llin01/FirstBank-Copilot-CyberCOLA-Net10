namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System.Collections.Generic;

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    public abstract class DecodeHandler : ChannelInboundHandler
    {
        private readonly List<object> outList = new List<object>();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            if (msg is ByteBuf)
            {
                this.StartDecode(ctx, (ByteBuf)msg, this.outList);
                foreach (var data in this.outList)
                {
                    ctx.FireChannelRead(data);
                }

                this.outList.Clear();
            }
            else
            {
                ctx.FireChannelRead(msg);
            }
        }

        protected virtual void StartDecode(ChannelHandlerContext ctx, ByteBuf dataIn, List<object> dataOut)
        {
            this.Decode(ctx, dataIn, dataOut);
        }

        protected abstract void Decode(ChannelHandlerContext ctx, ByteBuf dataIn, List<object> dataOut);
    }
}

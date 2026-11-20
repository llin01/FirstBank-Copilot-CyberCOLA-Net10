namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System;
    using System.Net;
    using Com.Cybersoft4u.Yetty.Context;

    #endregion

    public class ConsolePrintHandler : ChannelDuplexHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            Console.Write("WRITE\n");
            base.Write(ctx, msg);
        }

        public override void Flush(ChannelHandlerContext ctx)
        {
            Console.Write("FLUSH\n");
            base.Flush(ctx);
        }

        public override void Bind(ChannelHandlerContext ctx, EndPoint localAddress)
        {
            Console.Write("BIND\n");
            base.Bind(ctx, localAddress);
        }

        public override void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint)
        {
            Console.Write("CONNECT\n");
            base.Connect(ctx, remoteEndPoint);
        }

        public override void Disconnect(ChannelHandlerContext ctx)
        {
            Console.Write("DISCONNECT\n");
            base.Disconnect(ctx);
        }

        public override void Close(ChannelHandlerContext ctx)
        {
            Console.Write("CLOSE\n");
            base.Close(ctx);
        }

        public override void ChannelAdded(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELADDED\n");
            base.ChannelAdded(ctx);
        }

        public override void ChannelRemoved(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELREMOVED\n");
            base.ChannelRemoved(ctx);
        }

        public override void ExceptionCaught(ChannelHandlerContext ctx, Exception cause)
        {
            Console.Write("EXCEPTIONCAUGHT\n");
            base.ExceptionCaught(ctx, cause);
        }

        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELACTIVED\n");
            base.ChannelActived(ctx);
        }

        public override void ChannelInActived(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELINACTIVED\n");
            base.ChannelInActived(ctx);
        }

        public override void ChannelRegistered(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELREGISTERED\n");
            base.ChannelRegistered(ctx);
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELUNREGISTERED\n");
            base.ChannelUnRegistered(ctx);
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            Console.Write("CHANNELREAD\n");
            base.ChannelRead(ctx, msg);
        }

        public override void ChannelReadCompleted(ChannelHandlerContext ctx)
        {
            Console.Write("CHANNELREADCOMPLETED\n");
            base.ChannelReadCompleted(ctx);
        }
    }
}

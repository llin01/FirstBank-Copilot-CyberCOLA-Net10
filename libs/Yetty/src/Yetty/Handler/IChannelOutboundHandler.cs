namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System.Net;

    using Com.Cybersoft4u.Yetty.Context;

    #endregion

    public interface IChannelOutboundHandler : IChannelHandler
    {
        void Read(ChannelHandlerContext ctx);

        void Write(ChannelHandlerContext ctx, object msg);

        void Flush(ChannelHandlerContext ctx);

        void Bind(ChannelHandlerContext ctx, EndPoint localAddress);

        void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint);

        void Connect(ChannelHandlerContext ctx, EndPoint remoteEndPoint, EndPoint localEndPoint);

        void Disconnect(ChannelHandlerContext ctx);

        void Close(ChannelHandlerContext ctx);
    }
}

namespace Com.Cybersoft4u.Yetty.Channel
{
    using Com.Cybersoft4u.Yetty.Handler;

    public abstract class ServerSocketChannel : SocketChannel, IServerSocket
    {
        protected internal IChannelHandler ChildHandler { get; set; }
    }
}

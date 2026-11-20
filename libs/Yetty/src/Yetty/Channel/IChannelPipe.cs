namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Net;

    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public interface IChannelPipe
    {
        IChannel Channel();

        void FireChannelActived();

        void FireChannelInActived();

        void FireChannelRegistered();

        void FireChannelUnRegistered();

        void FireChannelRead(object msg);

        void FireChannelReadCompleted();

        void Read();

        void Write(object msg);

        void Flush();

        void WriteAndFlush(object msg);

        void Bind(EndPoint localEndPoint);

        void Connect(EndPoint remoteEndPoint);

        void Connect(EndPoint remoteEndPoint, EndPoint localEndPoint);

        void Disconnect();

        void Close();

        IChannelPipe AddFirst(IChannelHandler channelHandler, string name = null);

        IChannelPipe AddLast(IChannelHandler channelHandler, string name = null);

        IChannelPipe Remove(IChannelHandler channelHandler);

        IChannelPipe Remove(string name);

        IChannelPipe RemoveFirst();

        IChannelPipe RemoveLast();
    }
}

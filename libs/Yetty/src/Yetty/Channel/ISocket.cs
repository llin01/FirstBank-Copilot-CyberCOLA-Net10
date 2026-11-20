namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Net;

    #endregion

    public interface ISocket
    {
        AsyncFuture Connect(EndPoint remoteEndPoint);
        AsyncFuture Connect(EndPoint remoteEndPoint, EndPoint localEndPoint);
        void Disconnect();
    }
}

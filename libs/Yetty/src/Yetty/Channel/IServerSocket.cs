namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Net;

    #endregion

    public interface IServerSocket
    {
        AsyncFuture Bind(EndPoint localEndPoint);

        void Close();
    }
}

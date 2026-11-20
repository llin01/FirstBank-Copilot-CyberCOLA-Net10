namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Net.Sockets;

    #endregion

    public enum ChannelOptionLevel
    {
        IP = SocketOptionLevel.IP,

        IPv6 = SocketOptionLevel.IPv6,

        Socket = SocketOptionLevel.Socket,

        Tcp = SocketOptionLevel.Tcp,

        Udp = SocketOptionLevel.Udp
    }
}

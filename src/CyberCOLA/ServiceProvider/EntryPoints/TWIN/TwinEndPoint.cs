// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinEndPoint.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinEndPoint type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN
{
    public class TwinEndPoint
    {
        public TwinEndPoint(string remoteIp, int remotePort, string localIp, int localPort)
        {
            this.RemoteIp = remoteIp;
            this.RemotePort = remotePort;
            this.LocalIp = localIp;
            this.LocalPort = localPort;
        }

        public string RemoteIp { get; set; }

        public int RemotePort { get; set; }

        public string LocalIp { get; set; }

        public int LocalPort { get; set; }
    }
}

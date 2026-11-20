// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberAuthNCCCResource.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the Global Resource For NCCC Client Authorization Service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.ServiceProvider.QRCodeServerService;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY


    #endregion

    public static class CyberAuthNCCCResource
    {
        public static CyberAuthNCCC CyberAuthNCCC { get; set; }
        public static ClientAuthClientMonitorService ClientAuthClientMonitorService { get; set; }
        public static ClientAuthRouterService ClientAuthRouterService { get; set; }
        public static SocketListener CyberAuthSocketListener { get; set; }
        public static CyberAuthNCCCQRCode CyberAuthNcccQRCode { get; set; }
    }
}

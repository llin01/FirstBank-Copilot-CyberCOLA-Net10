// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberAuthFISCResource.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the Global Resource For FISC Client Authorization Service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.ServiceProvider.IDVerifyServerService;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY


    #endregion

    public static class CyberAuthFISCResource
    {
        public static CyberAuthFISC CyberAuthFISC { get; set; }
        public static ClientAuthClientMonitorService ClientAuthClientMonitorService { get; set; }
        public static ClientAuthRouterService ClientAuthRouterService { get; set; }
        public static SocketListener CyberAuthSocketListener { get; set; }
    }
}

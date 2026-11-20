// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberAuthFISCIDVerifyResource.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the Global Resource For FISC ID Verify Client Authorization Service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY


    #endregion

    public static class CyberAuthFISCIDVerifyResource
    {
        public static CyberAuthFISCIDVerify CyberAuthFISCIDVerify { get; set; }
        public static ClientAuthClientMonitorService ClientAuthClientMonitorService { get; set; }
        public static ClientAuthRouterService ClientAuthRouterService { get; set; }
    }
}

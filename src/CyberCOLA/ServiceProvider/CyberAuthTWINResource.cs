// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberAuthTWINResource.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the Global Resource For TWIN Client Authorization Service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY


    #endregion

    public static class CyberAuthTWINResource
    {
        public static CyberAuthTWIN CyberAuthTWIN { get; set; }
        public static ClientAuthClientMonitorService ClientAuthClientMonitorService { get; set; }
        public static ClientAuthRouterService ClientAuthRouterService { get; set; }
    }
}

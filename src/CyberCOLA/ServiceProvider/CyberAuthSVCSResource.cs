// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberAuthSVCSResource.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the Global Resource For SVCS Client Authorization Service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY


    #endregion

    public static class CyberAuthSVCSResource
    {
        public static CyberAuthSVCS CyberAuthSVCS { get; set; }
        public static ClientAuthClientMonitorService ClientAuthClientMonitorService { get; set; }
        public static ClientAuthRouterService ClientAuthRouterService { get; set; }
    }
}

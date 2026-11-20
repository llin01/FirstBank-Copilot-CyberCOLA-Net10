// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberGWNCCCResource.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the Global Resource For NCCC Gateway Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
    #endregion

    /// <summary>
    /// Set GatewayResource Class.
    /// </summary>
    public static class CyberGWNCCCResource
    {
        /// <summary>
        /// Gets or sets the NCCC service.
        /// </summary>
        /// <value>The NCCC service.</value>
        public static CyberGWNCCC CyberGWNCCC { get; set; }
        /// <summary>
        /// Gets or sets the cola service.
        /// </summary>
        /// <value>The cola service.</value>
        public static NcccColaService NcccColaService { get; set; }
        /// <summary>
        /// Gets or sets the gateway client monitor service.
        /// </summary>
        /// <value>The gateway client monitor service.</value>
        public static GatewayClientMonitorService GatewayClientMonitorService { get; set; }
    }
}

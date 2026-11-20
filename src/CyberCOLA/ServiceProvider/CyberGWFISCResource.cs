// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberGWFISCResource.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the Global Resource For FISC Gateway Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
    #endregion

    /// <summary>
    /// Set GatewayResource Class.
    /// </summary>
    public static class CyberGWFISCResource
    {
        /// <summary>
        /// Gets or sets the FISC service.
        /// </summary>
        /// <value>The FISC service.</value>
        public static CyberGWFISC CyberGWFISC { get; set; }
        /// <summary>
        /// Gets or sets the cola service.
        /// </summary>
        /// <value>The cola service.</value>
        public static FiscColaService FiscColaService { get; set; }
        /// <summary>
        /// Gets or sets the gateway client monitor service.
        /// </summary>
        /// <value>The gateway client monitor service.</value>
        public static GatewayClientMonitorService GatewayClientMonitorService { get; set; }
    }
}

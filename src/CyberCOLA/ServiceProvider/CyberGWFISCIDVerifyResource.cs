// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberGWFISCIDVerifyResource.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the Global Resource For FISC ID Verify Gateway Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.IDVerify;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
    #endregion

    /// <summary>
    /// Set GatewayResource Class.
    /// </summary>
    public static class CyberGWFISCIDVerifyResource
    {
        /// <summary>
        /// Gets or sets the FISC service.
        /// </summary>
        /// <value>The FISC ID Verify service.</value>
        public static CyberGWFISCIDVerify CyberGWFISCIDVerify { get; set; }
        /// <summary>
        /// Gets or sets the cola service.
        /// </summary>
        /// <value>The cola service.</value>
        public static FiscIDVerifyColaService FiscIDVerifyColaService { get; set; }
        /// <summary>
        /// Gets or sets the gateway client monitor service.
        /// </summary>
        /// <value>The gateway client monitor service.</value>
        public static GatewayClientMonitorService GatewayClientMonitorService { get; set; }
    }
}

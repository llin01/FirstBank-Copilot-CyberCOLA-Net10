// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberGWTWINResource.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the Global Resource For TWIN Gateway Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
    #endregion

    /// <summary>
    /// Set GatewayResource Class.
    /// </summary>
    public static class CyberGWTWINResource
    {
        /// <summary>
        /// Gets or sets the TWIN service.
        /// </summary>
        /// <value>The TWIN service.</value>
        public static CyberGWTWIN CyberGWTWIN { get; set; }
        /// <summary>
        /// Gets or sets the cola service.
        /// </summary>
        /// <value>The cola service.</value>
        public static TwinColaService TwinColaService { get; set; }
        /// <summary>
        /// Gets or sets the gateway client monitor service.
        /// </summary>
        /// <value>The gateway client monitor service.</value>
        public static GatewayClientMonitorService GatewayClientMonitorService { get; set; }
    }
}

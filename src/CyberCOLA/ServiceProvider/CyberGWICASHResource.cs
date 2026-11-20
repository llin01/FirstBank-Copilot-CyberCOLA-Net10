// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberGWICASHResource.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the Global Resource For ICASH Server Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
    #endregion

    /// <summary>
    /// Set GatewayResource Class.
    /// </summary>
    public static class CyberGWICASHResource
    {
        /// <summary>
        /// Gets or sets the NCCC service.
        /// </summary>
        /// <value>The NCCC service.</value>
        public static CyberGWICASH CyberGWICASH { get; set; }
        /// <summary>
        /// Gets or sets the cola service.
        /// </summary>
        /// <value>The cola service.</value>
        public static IcashColaService IcashColaService { get; set; }
        /// <summary>
        /// Gets or sets the gateway client monitor service.
        /// </summary>
        /// <value>The gateway client monitor service.</value>
        public static GatewayClientMonitorService GatewayClientMonitorService { get; set; }
    }
}

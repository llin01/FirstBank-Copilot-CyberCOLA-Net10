// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CyberGWNCCCQRCodeResource.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the CyberGWNCCC Resource For Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
    #endregion

    /// <summary>
    /// Set CyberGWNCCCQRCodeResource Class.
    /// </summary>
    public static class CyberGWNCCCQRCodeResource
    {
        /// <summary>
        /// Gets or sets the NCCC service.
        /// </summary>
        /// <value>The NCCC service.</value>
        public static CyberGWNCCCQRCode CyberGWNCCCQRCode { get; set; }
        /// <summary>
        /// Gets or sets the cola service.
        /// </summary>
        /// <value>The cola service.</value>
        public static NcccQRCodeColaService NcccQRCodeColaService { get; set; }
        /// <summary>
        /// Gets or sets the gateway client monitor service.
        /// </summary>
        /// <value>The gateway client monitor service.</value>
        public static GatewayClientMonitorService GatewayClientMonitorService { get; set; }

        //public static readonly string NCCCRelationIdPrefix = "Nccc";
        //public static readonly string COLARelationIdPrefix = "Cola";
    }
}

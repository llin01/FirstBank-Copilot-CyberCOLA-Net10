// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="RouterServiceResource.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the Global Resource For Router Service Provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.ServiceRouter;

namespace Com.Cybersoft4u.ServiceProvider
{
    #region DEPENDENCY

    #endregion

    /// <summary>
    /// Set GatewayResource Class.
    /// </summary>
    public static class RouterServiceResource
    {
        /// <summary>
        /// Gets or sets the ServerRouterService service For CyberGWNCCC Use
        /// </summary>
        /// <value>The ServerRouterService service.</value>
        public static ServerRouterService ServerRouterService { get; set; }
        /// <summary>
        /// Gets or sets the ClientRouterService service For  CyberAuthNCCC Use
        /// </summary>
        /// <value>The ClientRouterService service.</value>
        public static ClientRouterService ClientRouterService { get; set; }
    }
}

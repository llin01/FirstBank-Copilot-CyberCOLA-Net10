// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerColaChannelInitializer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerColaChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerIPASS("IpassColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly IpassColaInboundMessageTypeConvertHandler ipassColaInboundMessageTypeConvertHandler = new IpassColaInboundMessageTypeConvertHandler();

        private readonly IpassColaNetworkHandler ipassColaNetworkHandler = new IpassColaNetworkHandler();

        private readonly IpassColaFileMaintHandler ipassColaFileMaintHandler = new IpassColaFileMaintHandler();

        private readonly IpassColaSendRouteHandler ipassColaSendRouteHandler = new IpassColaSendRouteHandler();

        private readonly IpassColaPostDispatchHandler ipassColaPostDispatchHandler = new IpassColaPostDispatchHandler();

        private readonly IpassColaService ipassColaService;

        public IpassColaChannelInitializer(IpassColaService ipassColaService)
        {
            this.ipassColaService = ipassColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.ipassColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.ISO8583.IPASS.IPASS>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<TM.ISO8583.IPASS.IPASS>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.ipassColaNetworkHandler)
                   .AddLast(this.ipassColaFileMaintHandler)
                   .AddLast(this.ipassColaInboundMessageTypeConvertHandler)
                   .AddLast(this.ipassColaSendRouteHandler)
                   .AddLast(this.ipassColaPostDispatchHandler)
                   .AddLast(new IpassColaChannelStatusHandler())
                   ;
        }
    }
}

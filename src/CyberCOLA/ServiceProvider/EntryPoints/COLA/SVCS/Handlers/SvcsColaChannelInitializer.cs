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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerSVCS("SvcsColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly SvcsColaInboundMessageTypeConvertHandler svcsColaInboundMessageTypeConvertHandler = new SvcsColaInboundMessageTypeConvertHandler();

        private readonly SvcsColaNetworkHandler svcsColaNetworkHandler = new SvcsColaNetworkHandler();

        private readonly SvcsColaFileMaintHandler svcsColaFileMaintHandler = new SvcsColaFileMaintHandler();

        private readonly SvcsColaSendRouteHandler svcsColaSendRouteHandler = new SvcsColaSendRouteHandler();

        private readonly SvcsColaPostDispatchHandler svcsColaPostDispatchHandler = new SvcsColaPostDispatchHandler();

        private readonly SvcsColaService svcsColaService;

        public SvcsColaChannelInitializer(SvcsColaService svcsColaService)
        {
            this.svcsColaService = svcsColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.svcsColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.ISO8583.SVCS.SVCS>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<TM.ISO8583.SVCS.SVCS>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.svcsColaNetworkHandler)
                   .AddLast(this.svcsColaFileMaintHandler)
                   .AddLast(this.svcsColaInboundMessageTypeConvertHandler)
                   .AddLast(this.svcsColaSendRouteHandler)
                   .AddLast(this.svcsColaPostDispatchHandler)
                   .AddLast(new SvcsColaChannelStatusHandler())
                   ;
        }
    }
}

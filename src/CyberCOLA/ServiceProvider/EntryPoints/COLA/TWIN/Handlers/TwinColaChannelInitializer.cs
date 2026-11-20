// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinColaChannelInitializer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinColaChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.TWIN.Handlers
{
    #region DEPENDENCY

    #endregion

    public class TwinColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandler("TwinColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly TwinColaInboundMessageTypeConvertHandler twinColaInboundMessageTypeConvertHandler = new TwinColaInboundMessageTypeConvertHandler();

        private readonly TwinColaFileMaintHandler twinColaFileMaintHandler = new TwinColaFileMaintHandler();

        private readonly TwinColaSendRouteHandler twinColaSendRouteHandler = new TwinColaSendRouteHandler();

        private readonly TwinColaPostDispatchHandler twinColaPostDispatchHandler = new TwinColaPostDispatchHandler();

        private readonly TwinColaService twinColaService;

        public TwinColaChannelInitializer(TwinColaService twinColaService)
        {
            this.twinColaService = twinColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.twinColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TWINIso8583>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<TWINIso8583>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.twinColaNetworkHandler)
                   .AddLast(this.twinColaFileMaintHandler)
                   .AddLast(this.twinColaInboundMessageTypeConvertHandler)
                   //.AddLast(this.echoHandler)
                   //.AddLast(this.signOnOffHandler)
                   .AddLast(this.twinColaSendRouteHandler)
                   .AddLast(this.twinColaPostDispatchHandler)
                   .AddLast(new TwinColaChannelStatusHandler())
                   ;
        }
    }
}

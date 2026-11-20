// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ColaChannelInitializer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ColaChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandler("NcccColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly NcccColaInboundMessageTypeConvertHandler ncccColaInboundMessageTypeConvertHandler = new NcccColaInboundMessageTypeConvertHandler();

        private readonly NcccColaFileMaintHandler ncccColaFileMaintHandler = new NcccColaFileMaintHandler();

        private readonly NcccColaSendRouteHandler ncccColaSendRouteHandler = new NcccColaSendRouteHandler();

        private readonly NcccColaPostDispatchHandler ncccColaPostDispatchHandler = new NcccColaPostDispatchHandler();

        private readonly NcccColaService ncccColaService;

        public NcccColaChannelInitializer(NcccColaService ncccColaService)
        {
            this.ncccColaService = ncccColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.ncccColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, NcccIso8583>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<NcccIso8583>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.ncccColaNetworkHandler)
                   .AddLast(this.ncccColaFileMaintHandler)
                   .AddLast(this.ncccColaInboundMessageTypeConvertHandler)
                   //.AddLast(this.echoHandler)
                   //.AddLast(this.signOnOffHandler)
                   .AddLast(this.ncccColaSendRouteHandler)
                   .AddLast(this.ncccColaPostDispatchHandler)
                   .AddLast(new NcccColaChannelStatusHandler())
                   ;
        }
    }
}

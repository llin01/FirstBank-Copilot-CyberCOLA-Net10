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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.QRCode.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccQRCodeColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandler("NcccQRCodeColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly NcccQRCodeColaInboundMessageTypeConvertHandler ncccQRCodeColaInboundMessageTypeConvertHandler = new NcccQRCodeColaInboundMessageTypeConvertHandler();

        private readonly NcccQRCodeColaNetworkHandler ncccQRCodeColaNetworkHandler = new NcccQRCodeColaNetworkHandler();

        private readonly NcccQRCodeColaSendRouteHandler ncccQRCodeColaSendRouteHandler = new NcccQRCodeColaSendRouteHandler();

        private readonly NcccQRCodeColaPostDispatchHandler ncccQRCodeColaPostDispatchHandler = new NcccQRCodeColaPostDispatchHandler();

        private readonly NcccQRCodeColaService ncccQRCodeColaService;

        public NcccQRCodeColaChannelInitializer(NcccQRCodeColaService NcccQRCodeColaService)
        {
            this.ncccQRCodeColaService = NcccQRCodeColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();

            channel.Carrier<ServiceKey>(this.ncccQRCodeColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, NcccIso8583>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                .AddLast(this.loggerHandler)
                .AddLast(new Iso8583StreamCode<NcccIso8583>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.ncccQRCodeColaNetworkHandler)
                .AddLast(this.ncccQRCodeColaInboundMessageTypeConvertHandler)
                .AddLast(this.ncccQRCodeColaSendRouteHandler)
                .AddLast(this.ncccQRCodeColaPostDispatchHandler)
                .AddLast(new NcccQRCodeColaChannelStatusHandler())
                ;
        }
    }
}

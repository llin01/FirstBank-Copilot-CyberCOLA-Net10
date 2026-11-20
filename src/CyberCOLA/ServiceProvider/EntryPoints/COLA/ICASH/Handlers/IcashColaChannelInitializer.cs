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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerICASH("IcashColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly IcashColaInboundMessageTypeConvertHandler icashColaInboundMessageTypeConvertHandler = new IcashColaInboundMessageTypeConvertHandler();

        private readonly IcashColaNetworkHandler icashColaNetworkHandler = new IcashColaNetworkHandler();

        private readonly IcashColaFileMaintHandler icashColaFileMaintHandler = new IcashColaFileMaintHandler();

        private readonly IcashColaSendRouteHandler icashColaSendRouteHandler = new IcashColaSendRouteHandler();

        private readonly IcashColaPostDispatchHandler icashColaPostDispatchHandler = new IcashColaPostDispatchHandler();

        private readonly IcashColaService icashColaService;

        public IcashColaChannelInitializer(IcashColaService icashColaService)
        {
            this.icashColaService = icashColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.icashColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.ISO8583.ICASH.ICASH>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<TM.ISO8583.ICASH.ICASH>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.icashColaNetworkHandler)
                   .AddLast(this.icashColaFileMaintHandler)
                   .AddLast(this.icashColaInboundMessageTypeConvertHandler)
                   .AddLast(this.icashColaSendRouteHandler)
                   .AddLast(this.icashColaPostDispatchHandler)
                   .AddLast(new IcashColaChannelStatusHandler())
                   ;
        }
    }
}

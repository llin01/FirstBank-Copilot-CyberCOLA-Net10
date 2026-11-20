// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscColaChannelInitializer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ColaChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.CommonLib.Resource;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.FISC.Handlers
{
    #region DEPENDENCY

    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Handler;


    #endregion

    public class FiscColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerFISC("FiscColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly FiscColaInboundMessageTypeConvertHandler fiscColaInboundMessageTypeConvertHandler = new FiscColaInboundMessageTypeConvertHandler();

        private readonly FiscColaFileMaint0302Handler fiscColaFileMaint0302Handler = new FiscColaFileMaint0302Handler();

        private readonly FiscColaFileMaint0300Handler fiscColaFileMaint0300Handler = new FiscColaFileMaint0300Handler();

        private readonly FiscColaSendRouteHandler fiscColaSendRouteHandler = new FiscColaSendRouteHandler();

        private readonly FiscColaPostDispatchHandler fiscColaPostDispatchHandler = new FiscColaPostDispatchHandler();

        private readonly FiscColaService fiscColaService;

        public FiscColaChannelInitializer(FiscColaService fiscColaService)
        {
            this.fiscColaService = fiscColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.fiscColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.FISC.FISC>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<TM.FISC.FISC>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.fiscColaNetworkHandler)
                   .AddLast(this.fiscColaFileMaint0300Handler)
                   .AddLast(this.fiscColaFileMaint0302Handler)
                   .AddLast(this.fiscColaInboundMessageTypeConvertHandler)
                   //.AddLast(this.echoHandler)
                   //.AddLast(this.signOnOffHandler)
                   .AddLast(this.fiscColaSendRouteHandler)
                   .AddLast(this.fiscColaPostDispatchHandler)
                   .AddLast(new FiscColaChannelStatusHandler())
                   ;
        }
    }
}

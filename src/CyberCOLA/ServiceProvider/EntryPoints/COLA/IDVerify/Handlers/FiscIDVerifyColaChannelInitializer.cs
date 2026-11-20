// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyColaChannelInitializer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyColaChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.CommonLib.Resource;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IDVerify.Handlers
{
    #region DEPENDENCY

    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Handler;


    #endregion

    public class FiscIDVerifyColaChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerFISC("FiscIDVerifyColaSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly FiscIDVerifyColaInboundMessageTypeConvertHandler fiscIDVerifyColaInboundMessageTypeConvertHandler = new FiscIDVerifyColaInboundMessageTypeConvertHandler();

        private readonly FiscIDVerifyColaFileMaint0302Handler fiscIDVerifyColaFileMaint0302Handler = new FiscIDVerifyColaFileMaint0302Handler();

        private readonly FiscIDVerifyColaFileMaint0300Handler fiscIDVerifyColaFileMaint0300Handler = new FiscIDVerifyColaFileMaint0300Handler();

        private readonly FiscIDVerifyColaSendRouteHandler fiscIDVerifyColaSendRouteHandler = new FiscIDVerifyColaSendRouteHandler();

        private readonly FiscIDVerifyColaPostDispatchHandler fiscIDVerifyColaPostDispatchHandler = new FiscIDVerifyColaPostDispatchHandler();

        private readonly FiscIDVerifyColaService fiscIDVerifyColaService;

        public FiscIDVerifyColaChannelInitializer(FiscIDVerifyColaService fiscIDVerifyColaService)
        {
            this.fiscIDVerifyColaService = fiscIDVerifyColaService;
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.fiscIDVerifyColaService);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.FISC.FISC>());
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                   .AddLast(this.loggerHandler)
                   .AddLast(new Iso8583StreamCode<TM.FISC.FISC>())
                   .AddLast(this.inboundMultiWorkerHandler)
                   //.AddLast(this.fiscIDVerifyColaNetworkHandler)
                   .AddLast(this.fiscIDVerifyColaFileMaint0300Handler)
                   .AddLast(this.fiscIDVerifyColaFileMaint0302Handler)
                   .AddLast(this.fiscIDVerifyColaInboundMessageTypeConvertHandler)
                   //.AddLast(this.echoHandler)
                   //.AddLast(this.signOnOffHandler)
                   .AddLast(this.fiscIDVerifyColaSendRouteHandler)
                   .AddLast(this.fiscIDVerifyColaPostDispatchHandler)
                   .AddLast(new FiscIDVerifyColaChannelStatusHandler())
                   ;
        }
    }
}

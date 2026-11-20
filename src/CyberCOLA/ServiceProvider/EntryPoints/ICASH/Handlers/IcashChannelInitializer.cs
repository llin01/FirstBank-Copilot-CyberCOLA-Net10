// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerChannelInitializer.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerICASH("CyberGWICASHSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly IcashInboundMessageProcessHandler icashInboundMessageTypeConvertHandler = new IcashInboundMessageProcessHandler();

        private readonly IcashAutoSignOnHandler icashAutoSignOnHandler = new IcashAutoSignOnHandler();

        private readonly IcashEchoTestHandler icashEchoTestHandler = new IcashEchoTestHandler();

        private readonly IcashRegisterKeyHandler icashRegisterKeyHandler = new IcashRegisterKeyHandler();

        private readonly IcashPostDispatchHandler icashPostDispatchHandler = new IcashPostDispatchHandler();

        private readonly IcashChannelReviveHandler icashChannelReviveHandler = new IcashChannelReviveHandler();

        private readonly CyberGWICASH cyberGWICASH;

        public IcashChannelInitializer(CyberGWICASH cyberGWICASH)
        {
            this.cyberGWICASH = cyberGWICASH;
        }

        public override void InitChannel(IChannel channel)
        {
            //var configurationLoader = new ConfigurationLoader();
            //channel.Carrier<ServiceKey>(this.cyberGWNCCC);
            //channel.Carrier<RemoteEndPointKey>(this.remoteEndPoint);
            //channel.Carrier<LocalEndPointKey>(this.localEndPoint);
            //channel.Carrier<BootstrapKey>(this.connectBootstrap);
            //channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, ICASH>());
            //channel.Carrier<SignOnKey>(false);
            //channel.Carrier<AutoSignOnKey>(configurationLoader.Get("nccc-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            //channel.Carrier<DisconnectKey>(true);
            //channel.Carrier<AutoEchoKey>(configurationLoader.Get("nccc-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            //var maintenanceEchoFlag = true;
            //if (configurationLoader.Get("nccc-service-maintenance-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"))
            //{
            //    if (!this.maintenanceLocalPortFlag)
            //        maintenanceEchoFlag = false;
            //
            //}
            //channel.Carrier<MaintAutoEchoKey>(maintenanceEchoFlag);
            //channel.Pipe()
            //    .AddLast(this.loggerHandler)
            //    //.AddLast(this.ncccAutoSignOnHandler)
            //    .AddLast(new Iso8583StreamCode<ICASH>())
            //    .AddLast(this.inboundMultiWorkerHandler)
            //    .AddLast(this.ncccInboundMessageTypeConvertHandler)
            //    //.AddLast(new NcccEchoTestHandler())
            //    .AddLast(this.ncccEchoTestHandler)
            //    .AddLast(this.ncccAutoSignOnHandler)
            //    .AddLast(this.ncccRegisterKeyHandler)  //This Handler For self test no transfer to NcccColaService
            //                                           //.AddLast(new SignOnOffHandler());
            //                                           //.AddLast(this.visaSendMcolaHandler)
            //                                           //.AddLast(this.ncccResponseFillHandler)
            //    .AddLast(this.ncccPostDispatchHandler)
            //    .AddLast(this.ncccChannelReviveHandler);

            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.cyberGWICASH);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.ISO8583.ICASH.ICASH>());
            channel.Carrier<SignOnKey>(false);
            channel.Carrier<AutoSignOnKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                .AddLast(this.loggerHandler)
                .AddLast(new Iso8583StreamCode<TM.ISO8583.ICASH.ICASH>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.icashInboundMessageTypeConvertHandler)
                .AddLast(this.icashEchoTestHandler)
                .AddLast(this.icashAutoSignOnHandler)
                .AddLast(this.icashRegisterKeyHandler)
                .AddLast(this.icashPostDispatchHandler)
                .AddLast(this.icashChannelReviveHandler);

        }
    }
}

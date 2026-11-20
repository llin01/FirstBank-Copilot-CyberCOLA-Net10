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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassChannelInitializer : ChannelInitializer
    {
        private readonly LoggerHandler loggerHandler = new ServiceProviderLoggerHandlerIPASS("CyberGWIPASSSession");

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly IpassInboundMessageProcessHandler ipassInboundMessageTypeConvertHandler = new IpassInboundMessageProcessHandler();

        private readonly IpassAutoSignOnHandler ipassAutoSignOnHandler = new IpassAutoSignOnHandler();

        private readonly IpassEchoTestHandler ipassEchoTestHandler = new IpassEchoTestHandler();

        private readonly IpassRegisterKeyHandler ipassRegisterKeyHandler = new IpassRegisterKeyHandler();

        private readonly IpassPostDispatchHandler ipassPostDispatchHandler = new IpassPostDispatchHandler();

        private readonly IpassChannelReviveHandler ipassChannelReviveHandler = new IpassChannelReviveHandler();

        private readonly CyberGWIPASS cyberGWIPASS;

        public IpassChannelInitializer(CyberGWIPASS cyberGWIPASS)
        {
            this.cyberGWIPASS = cyberGWIPASS;
        }

        public override void InitChannel(IChannel channel)
        {
            //var configurationLoader = new ConfigurationLoader();
            //channel.Carrier<ServiceKey>(this.cyberGWNCCC);
            //channel.Carrier<RemoteEndPointKey>(this.remoteEndPoint);
            //channel.Carrier<LocalEndPointKey>(this.localEndPoint);
            //channel.Carrier<BootstrapKey>(this.connectBootstrap);
            //channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, IPASS>());
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
            //    .AddLast(new Iso8583StreamCode<IPASS>())
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
            channel.Carrier<ServiceKey>(this.cyberGWIPASS);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.ISO8583.IPASS.IPASS>());
            channel.Carrier<SignOnKey>(false);
            channel.Carrier<AutoSignOnKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));

            channel.Pipe()
                .AddLast(this.loggerHandler)
                .AddLast(new Iso8583StreamCode<TM.ISO8583.IPASS.IPASS>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.ipassInboundMessageTypeConvertHandler)
                .AddLast(this.ipassEchoTestHandler)
                .AddLast(this.ipassAutoSignOnHandler)
                .AddLast(this.ipassRegisterKeyHandler)
                .AddLast(this.ipassPostDispatchHandler)
                .AddLast(this.ipassChannelReviveHandler);

        }
    }
}

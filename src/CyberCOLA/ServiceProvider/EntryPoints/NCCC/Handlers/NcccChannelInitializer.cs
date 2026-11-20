// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NcccChannelInitializer.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC.Handlers
{
    #region DEPENDENCY
    using System.Collections.Concurrent;
    using System.Net;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class NcccChannelInitializer : ChannelInitializer
    {
        private readonly NcccChannelReviveHandler ncccChannelReviveHandler;

        private readonly Bootstrap connectBootstrap;

        private readonly IPEndPoint remoteEndPoint;

        private readonly IPEndPoint localEndPoint;

        private readonly bool maintenanceLocalPortFlag;

        private readonly NcccAutoSignOnHandler ncccAutoSignOnHandler = new NcccAutoSignOnHandler();

        private readonly NcccEchoTestHandler ncccEchoTestHandler = new NcccEchoTestHandler();

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly LoggerHandler loggerHandler;

        private readonly NcccInboundMessageProcessHandler ncccInboundMessageTypeConvertHandler = new NcccInboundMessageProcessHandler();

        private readonly NcccRegisterKeyHandler ncccRegisterKeyHandler = new NcccRegisterKeyHandler();

        //private readonly NcccResponseFillHandler ncccResponseFillHandler = new NcccResponseFillHandler();

        private readonly NcccPostDispatchHandler ncccPostDispatchHandler = new NcccPostDispatchHandler();

        //private readonly NcccSendColaHandler ncccSendColaHandler = new NcccSendColaHandler();

        private readonly CyberGWNCCC cyberGWNCCC;

        public NcccChannelInitializer(CyberGWNCCC cyberGWNCCC, Bootstrap connectBootstrap, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, bool maintenanceLocalPortFlag = false, int reconnectIntervalMilliseconds = 10000)
        {
            this.ncccChannelReviveHandler = new NcccChannelReviveHandler(reconnectIntervalMilliseconds);
            this.cyberGWNCCC = cyberGWNCCC;
            this.connectBootstrap = connectBootstrap;
            this.remoteEndPoint = remoteEndPoint;
            this.localEndPoint = localEndPoint;
            this.maintenanceLocalPortFlag = maintenanceLocalPortFlag;
            this.loggerHandler = new ServiceProviderLoggerHandler("NcccSession");
            //this.loggerHandler = new ServiceProviderLoggerHandlerUseBuffer("NcccSession");
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.cyberGWNCCC);
            channel.Carrier<RemoteEndPointKey>(this.remoteEndPoint);
            channel.Carrier<LocalEndPointKey>(this.localEndPoint);
            channel.Carrier<BootstrapKey>(this.connectBootstrap);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, NcccIso8583>());
            channel.Carrier<SignOnKey>(false);
            channel.Carrier<AutoSignOnKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Carrier<DisconnectKey>(true);
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            var maintenanceEchoFlag = true;
            if (configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-maintenance-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"))
            {
                if (!this.maintenanceLocalPortFlag)
                    maintenanceEchoFlag = false;

            }
            channel.Carrier<MaintAutoEchoKey>(maintenanceEchoFlag);
            channel.Pipe()
                .AddLast(this.loggerHandler)
                //.AddLast(this.ncccAutoSignOnHandler)
                .AddLast(new Iso8583StreamCode<NcccIso8583>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.ncccInboundMessageTypeConvertHandler)
                //.AddLast(new NcccEchoTestHandler())
                .AddLast(this.ncccEchoTestHandler)
                .AddLast(this.ncccAutoSignOnHandler)
                .AddLast(this.ncccRegisterKeyHandler)  //This Handler For self test no transfer to NcccColaService
                                                       //.AddLast(new SignOnOffHandler());
                                                       //.AddLast(this.visaSendMcolaHandler)
                                                       //.AddLast(this.ncccResponseFillHandler)
                .AddLast(this.ncccPostDispatchHandler)
                .AddLast(this.ncccChannelReviveHandler);
        }
    }
}

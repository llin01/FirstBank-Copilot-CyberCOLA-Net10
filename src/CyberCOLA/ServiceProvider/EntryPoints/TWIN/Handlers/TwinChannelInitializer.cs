// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwinChannelInitializer.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.CommonLib.Resource;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY
    using System.Collections.Concurrent;
    using System.Net;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583.TWIN;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class TwinChannelInitializer : ChannelInitializer
    {
        private readonly TwinChannelReviveHandler twinChannelReviveHandler;

        private readonly Bootstrap connectBootstrap;

        private readonly IPEndPoint remoteEndPoint;

        private readonly IPEndPoint localEndPoint;

        private readonly bool maintenanceLocalPortFlag;

        private readonly TwinAutoSignOnHandler twinAutoSignOnHandler = new TwinAutoSignOnHandler();

        private readonly TwinEchoTestHandler twinEchoTestHandler = new TwinEchoTestHandler();

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly LoggerHandler loggerHandler;

        private readonly TwinInboundMessageProcessHandler twinInboundMessageTypeConvertHandler = new TwinInboundMessageProcessHandler();

        private readonly TwinRegisterKeyHandler twinRegisterKeyHandler = new TwinRegisterKeyHandler();

        //private readonly TwinResponseFillHandler twinResponseFillHandler = new TwinResponseFillHandler();

        private readonly TwinPostDispatchHandler twinPostDispatchHandler = new TwinPostDispatchHandler();

        //private readonly TwinSendColaHandler twinSendColaHandler = new TwinSendColaHandler();

        private readonly CyberGWTWIN cyberGWTWIN;

        public TwinChannelInitializer(CyberGWTWIN cyberGWTWIN, Bootstrap connectBootstrap, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, bool maintenanceLocalPortFlag = false, int reconnectIntervalMilliseconds = 10000)
        {
            this.twinChannelReviveHandler = new TwinChannelReviveHandler(reconnectIntervalMilliseconds);
            this.cyberGWTWIN = cyberGWTWIN;
            this.connectBootstrap = connectBootstrap;
            this.remoteEndPoint = remoteEndPoint;
            this.localEndPoint = localEndPoint;
            this.maintenanceLocalPortFlag = maintenanceLocalPortFlag;
            this.loggerHandler = new ServiceProviderLoggerHandler("TwinSession");
            //this.loggerHandler = new ServiceProviderLoggerHandlerUseBuffer("TwinSession");
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.cyberGWTWIN);
            channel.Carrier<RemoteEndPointKey>(this.remoteEndPoint);
            channel.Carrier<LocalEndPointKey>(this.localEndPoint);
            channel.Carrier<BootstrapKey>(this.connectBootstrap);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TWINIso8583>());
            channel.Carrier<SignOnKey>(false);
            channel.Carrier<AutoSignOnKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Carrier<DisconnectKey>(true);
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            var maintenanceEchoFlag = true;
            if (configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-maintenance-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"))
            {
                if (!this.maintenanceLocalPortFlag)
                    maintenanceEchoFlag = false;

            }
            channel.Carrier<MaintAutoEchoKey>(maintenanceEchoFlag);
            channel.Pipe()
                .AddLast(this.loggerHandler)
                //.AddLast(this.twinAutoSignOnHandler)
                .AddLast(new Iso8583StreamCode<TWINIso8583>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.twinInboundMessageTypeConvertHandler)
                //.AddLast(new TwinEchoTestHandler())
                .AddLast(this.twinEchoTestHandler)
                .AddLast(this.twinAutoSignOnHandler)
                .AddLast(this.twinRegisterKeyHandler)  //This Handler For self test no transfer to TwinColaService
                                                       //.AddLast(new SignOnOffHandler());
                                                       //.AddLast(this.twinResponseFillHandler)
                .AddLast(this.twinPostDispatchHandler)
                .AddLast(this.twinChannelReviveHandler);
        }
    }
}

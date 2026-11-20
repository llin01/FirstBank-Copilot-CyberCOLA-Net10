// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FiscChannelInitializer.cs" company="Cybersoft">
// </copyright>
// <summary>
//   Defines the FiscChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.Handlers
{
    #region DEPENDENCY
    using System.Collections.Concurrent;
    using System.Net;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class FiscChannelInitializer : ChannelInitializer
    {
        private readonly FiscChannelReviveHandler fiscChannelReviveHandler;

        private readonly Bootstrap connectBootstrap;

        private readonly IPEndPoint remoteEndPoint;

        private readonly IPEndPoint localEndPoint;

        private readonly bool maintenanceLocalPortFlag;

        private readonly FiscAutoSignOnHandler fiscAutoSignOnHandler = new FiscAutoSignOnHandler();

        private readonly FiscEchoTestHandler fiscEchoTestHandler = new FiscEchoTestHandler();

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly FiscGWLoggerHandler fiscGWLoggerHandler;

        private readonly FiscInboundMessageProcessHandler fiscInboundMessageTypeConvertHandler = new FiscInboundMessageProcessHandler();

        private readonly FiscRegisterKeyHandler fiscRegisterKeyHandler = new FiscRegisterKeyHandler();

        private readonly FiscPostDispatchHandler fiscPostDispatchHandler = new FiscPostDispatchHandler();

        private readonly CyberGWFISC cyberGWFISC;

        public FiscChannelInitializer(CyberGWFISC cyberGWFISC, Bootstrap connectBootstrap, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, bool maintenanceLocalPortFlag = false, int reconnectIntervalMilliseconds = 10000)
        {
            this.fiscChannelReviveHandler = new FiscChannelReviveHandler(reconnectIntervalMilliseconds);
            this.cyberGWFISC = cyberGWFISC;
            this.connectBootstrap = connectBootstrap;
            this.remoteEndPoint = remoteEndPoint;
            this.localEndPoint = localEndPoint;
            this.maintenanceLocalPortFlag = maintenanceLocalPortFlag;
            this.fiscGWLoggerHandler = new FiscGWLoggerHandler("FiscSession", LogLevel.Debug);
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.cyberGWFISC);
            channel.Carrier<RemoteEndPointKey>(this.remoteEndPoint);
            channel.Carrier<LocalEndPointKey>(this.localEndPoint);
            channel.Carrier<BootstrapKey>(this.connectBootstrap);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, TM.FISC.FISC>());
            channel.Carrier<SignOnKey>(false);
            channel.Carrier<AutoSignOnKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.FiscServiceConfigSectionName, "fisc-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Carrier<DisconnectKey>(true);
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.FiscServiceConfigSectionName, "fisc-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            var maintenanceEchoFlag = true;
            if (configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.FiscServiceConfigSectionName, "fisc-service-maintenance-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"))
            {
                if (!this.maintenanceLocalPortFlag)
                    maintenanceEchoFlag = false;

            }
            channel.Carrier<MaintAutoEchoKey>(maintenanceEchoFlag);
            channel.Pipe()
                .AddLast(this.fiscGWLoggerHandler)
                //.AddLast(this.fiscAutoSignOnHandler)
                .AddLast(new Iso8583StreamCode<TM.FISC.FISC>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.fiscInboundMessageTypeConvertHandler)
                //.AddLast(new FiscEchoTestHandler())
                .AddLast(this.fiscEchoTestHandler)
                .AddLast(this.fiscAutoSignOnHandler)
                .AddLast(this.fiscRegisterKeyHandler)  //This Handler For self test no transfer to FiscColaService
                                                       //.AddLast(new SignOnOffHandler());
                                                       //.AddLast(this.visaSendMcolaHandler)
                                                       //.AddLast(this.fiscResponseFillHandler)
                .AddLast(this.fiscPostDispatchHandler)
                .AddLast(this.fiscChannelReviveHandler);
        }
    }
}

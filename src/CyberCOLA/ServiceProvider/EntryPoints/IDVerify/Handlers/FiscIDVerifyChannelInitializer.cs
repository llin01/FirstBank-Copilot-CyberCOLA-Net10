// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FiscIDVerifyChannelInitializer.cs" company="Cybersoft">
// </copyright>
// <summary>
//   Defines the FiscIDVerifyChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.Utils;
#region DEPENDENCY
using System.Collections.Concurrent;
using System.Net;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IDVerify.Handlers
{
    #endregion

    public class FiscIDVerifyChannelInitializer : ChannelInitializer
    {
        private readonly FiscIDVerifyChannelReviveHandler fiscIDVerifyChannelReviveHandler;

        private readonly Bootstrap connectBootstrap;

        private readonly IPEndPoint remoteEndPoint;

        private readonly IPEndPoint localEndPoint;

        private readonly bool maintenanceLocalPortFlag;

        private readonly FiscIDVerifyAutoSignOnHandler fiscIDVerifyAutoSignOnHandler = new FiscIDVerifyAutoSignOnHandler();

        private readonly FiscIDVerifyEchoTestHandler fiscIDVerifyEchoTestHandler = new FiscIDVerifyEchoTestHandler();

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly FiscGWLoggerHandler fiscGWLoggerHandler;

        private readonly FiscIDVerifyInboundMessageProcessHandler fiscIDVerifyInboundMessageTypeConvertHandler = new FiscIDVerifyInboundMessageProcessHandler();

        private readonly FiscIDVerifyRegisterKeyHandler fiscIDVerifyRegisterKeyHandler = new FiscIDVerifyRegisterKeyHandler();

        private readonly FiscIDVerifyPostDispatchHandler fiscIDVerifyPostDispatchHandler = new FiscIDVerifyPostDispatchHandler();

        private readonly CyberGWFISCIDVerify cyberGWFISCIDVerify;

        public FiscIDVerifyChannelInitializer(CyberGWFISCIDVerify cyberGWFISCIDVerify, Bootstrap connectBootstrap, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, bool maintenanceLocalPortFlag = false, int reconnectIntervalMilliseconds = 10000)
        {
            this.fiscIDVerifyChannelReviveHandler = new FiscIDVerifyChannelReviveHandler(reconnectIntervalMilliseconds);
            this.cyberGWFISCIDVerify = cyberGWFISCIDVerify;
            this.connectBootstrap = connectBootstrap;
            this.remoteEndPoint = remoteEndPoint;
            this.localEndPoint = localEndPoint;
            this.maintenanceLocalPortFlag = maintenanceLocalPortFlag;
            this.fiscGWLoggerHandler = new FiscGWLoggerHandler("FiscSession", LogLevel.Debug);
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.cyberGWFISCIDVerify);
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
                .AddLast(this.fiscIDVerifyInboundMessageTypeConvertHandler)
                //.AddLast(new FiscEchoTestHandler())
                .AddLast(this.fiscIDVerifyEchoTestHandler)
                .AddLast(this.fiscIDVerifyAutoSignOnHandler)
                .AddLast(this.fiscIDVerifyRegisterKeyHandler)  //This Handler For self test no transfer to FiscColaService
                                                               //.AddLast(new SignOnOffHandler());
                                                               //.AddLast(this.visaSendMcolaHandler)
                                                               //.AddLast(this.fiscResponseFillHandler)
                .AddLast(this.fiscIDVerifyPostDispatchHandler)
                .AddLast(this.fiscIDVerifyChannelReviveHandler);
        }
    }
}

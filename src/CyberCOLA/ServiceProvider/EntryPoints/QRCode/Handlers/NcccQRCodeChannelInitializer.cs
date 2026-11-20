// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NcccQRCodeChannelInitializer.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccQRCodeChannelInitializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers
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

    public class NcccQRCodeChannelInitializer : ChannelInitializer
    {
        private readonly NcccQRCodeChannelReviveHandler ncccQRCodeChannelReviveHandler;

        private readonly Bootstrap connectBootstrap;

        private readonly IPEndPoint remoteEndPoint;

        private readonly IPEndPoint localEndPoint;

        private readonly NcccQRCodeAutoSignOnHandler ncccQRCodeAutoSignOnHandler = new NcccQRCodeAutoSignOnHandler();

        private readonly NcccQRCodeEchoTestHandler ncccQRCodeEchoTestHandler = new NcccQRCodeEchoTestHandler();

        private readonly InboundMultiWorkerHandler inboundMultiWorkerHandler = new InboundMultiWorkerHandler();

        private readonly LoggerHandler loggerHandler;

        private readonly QRCode.Handlers.NcccQRCodeInboundMessageProcessHandler ncccQRCodeInboundMessageTypeConvertHandler = new QRCode.Handlers.NcccQRCodeInboundMessageProcessHandler();

        private readonly NcccQRCodeRegisterKeyHandler ncccQRCodeRegisterKeyHandler = new NcccQRCodeRegisterKeyHandler();

        //private readonly NcccQRCodeResponseFillHandler ncccQRCodeResponseFillHandler = new NcccQRCodeResponseFillHandler();

        private readonly NcccQRCodePostDispatchHandler ncccQRCodePostDispatchHandler = new NcccQRCodePostDispatchHandler();

        //private readonly NcccQRCodeSendColaHandler ncccQRCodeSendColaHandler = new NcccQRCodeSendColaHandler();

        private readonly CyberGWNCCCQRCode cyberGWNCCCQRCode;

        public NcccQRCodeChannelInitializer(CyberGWNCCCQRCode cyberGWNCCCQRCode, Bootstrap connectBootstrap, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, int reconnectIntervalMilliseconds = 10000)
        {
            this.ncccQRCodeChannelReviveHandler = new NcccQRCodeChannelReviveHandler(reconnectIntervalMilliseconds);
            this.cyberGWNCCCQRCode = cyberGWNCCCQRCode;
            this.connectBootstrap = connectBootstrap;
            this.remoteEndPoint = remoteEndPoint;
            this.localEndPoint = localEndPoint;
            this.loggerHandler = new ServiceProviderLoggerHandler("NcccSession");
            //this.loggerHandler = new ServiceProviderLoggerHandlerUseBuffer("NcccSession");
        }

        public override void InitChannel(IChannel channel)
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            channel.Carrier<ServiceKey>(this.cyberGWNCCCQRCode);
            channel.Carrier<RemoteEndPointKey>(this.remoteEndPoint);
            channel.Carrier<LocalEndPointKey>(this.localEndPoint);
            channel.Carrier<BootstrapKey>(this.connectBootstrap);
            channel.Carrier<RequestMapKey>(new ConcurrentDictionary<string, NcccIso8583>());
            channel.Carrier<SignOnKey>(false);
            channel.Carrier<AutoSignOnKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-auto-signon-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Carrier<DisconnectKey>(true);
            channel.Carrier<AutoEchoKey>(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-auto-echo-flag").Default("FALSE").ToUpper().Equals("TRUE"));
            channel.Pipe()
                .AddLast(this.loggerHandler)
                //.AddLast(this.ncccAutoSignOnHandler)
                .AddLast(new Iso8583StreamCode<NcccIso8583>())
                .AddLast(this.inboundMultiWorkerHandler)
                .AddLast(this.ncccQRCodeInboundMessageTypeConvertHandler)
                //.AddLast(new NcccQRCodeEchoTestHandler())
                .AddLast(this.ncccQRCodeEchoTestHandler)
                .AddLast(this.ncccQRCodeAutoSignOnHandler)
                .AddLast(this.ncccQRCodeRegisterKeyHandler)  //This Handler For self test no transfer to colaservice
                                                             //.AddLast(new SignOnOffHandler());
                                                             //.AddLast(this.visaSendMcolaHandler)
                                                             //.AddLast(this.ncccResponseFillHandler)
                .AddLast(this.ncccQRCodePostDispatchHandler)
                .AddLast(this.ncccQRCodeChannelReviveHandler);
        }
    }
}

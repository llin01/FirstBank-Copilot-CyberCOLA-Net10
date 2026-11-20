// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NetworkHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NetworkHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaNetworkHandler : ChannelInboundHandler
    {
        private static readonly ConfigurationLoaderRoot configurationLoaderRoot = new ConfigurationLoaderRoot();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var icash = payload.Val as TM.ISO8583.ICASH.ICASH;
            if (icash != null && icash.Mti().Equals("0800"))
            {
                var informationCode = icash.Val(FIELD.F_70);
                try
                {
                    switch (informationCode)
                    {
                        case IcashTransactionConst.NetworkManagementInformationCode.SignOn:
                            CyberGWICASHResource.CyberGWICASH.SignOn(payload);
                            break;
                        case IcashTransactionConst.NetworkManagementInformationCode.SignOff:
                            CyberGWICASHResource.CyberGWICASH.SignOff(payload);
                            break;
                        case IcashTransactionConst.NetworkManagementInformationCode.EchoTest:
                            CyberGWICASHResource.CyberGWICASH.EchoTest(payload);
                            break;
                        default:
                            ctx.Write(msg);
                            break;
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error("NetworkHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

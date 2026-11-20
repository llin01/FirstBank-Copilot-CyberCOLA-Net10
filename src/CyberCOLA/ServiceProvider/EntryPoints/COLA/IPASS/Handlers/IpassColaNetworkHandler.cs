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
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassColaNetworkHandler : ChannelInboundHandler
    {
        private static readonly ConfigurationLoaderRoot configurationLoaderRoot = new ConfigurationLoaderRoot();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var ipass = payload.Val as TM.ISO8583.IPASS.IPASS;
            if (ipass != null && ipass.Mti().Equals("0800"))
            {
                var informationCode = ipass.Val(FIELD.F_70);
                try
                {
                    switch (informationCode)
                    {
                        case IpassTransactionConst.NetworkManagementInformationCode.SignOn:
                            CyberGWIPASSResource.CyberGWIPASS.SignOn(payload);
                            break;
                        case IpassTransactionConst.NetworkManagementInformationCode.SignOff:
                            CyberGWIPASSResource.CyberGWIPASS.SignOff(payload);
                            break;
                        case IpassTransactionConst.NetworkManagementInformationCode.EchoTest:
                            CyberGWIPASSResource.CyberGWIPASS.EchoTest(payload);
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

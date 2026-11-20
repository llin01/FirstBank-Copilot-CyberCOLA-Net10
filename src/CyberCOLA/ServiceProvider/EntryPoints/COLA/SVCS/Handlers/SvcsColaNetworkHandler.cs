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
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaNetworkHandler : ChannelInboundHandler
    {
        private static readonly ConfigurationLoaderRoot configurationLoaderRoot = new ConfigurationLoaderRoot();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = payload.Val as TM.ISO8583.SVCS.SVCS;
            if (svcs != null && svcs.Mti().Equals("0800"))
            {
                var informationCode = svcs.Val(FIELD.F_70);
                try
                {
                    switch (informationCode)
                    {
                        case SvcsTransactionConst.NetworkManagementInformationCode.SignOn:
                            CyberGWSVCSResource.CyberGWSVCS.SignOn(payload);
                            break;
                        case SvcsTransactionConst.NetworkManagementInformationCode.SignOff:
                            CyberGWSVCSResource.CyberGWSVCS.SignOff(payload);
                            break;
                        case SvcsTransactionConst.NetworkManagementInformationCode.EchoTest:
                            CyberGWSVCSResource.CyberGWSVCS.EchoTest(payload);
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

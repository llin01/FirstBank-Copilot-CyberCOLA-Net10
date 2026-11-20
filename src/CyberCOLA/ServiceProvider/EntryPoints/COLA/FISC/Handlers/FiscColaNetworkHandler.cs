// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscColaNetworkHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscColaNetworkHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.FISC.Handlers
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.TM.FISC;

    #endregion

    public class FiscColaNetworkHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            if (payload.Val is FISC fisc && fisc.Mti().Equals("0800"))
            {
                var informationCode = fisc.Val(FIELD.F_70);
                try
                {
                    switch (informationCode)
                    {
                        case FISCTransactionConst.NetworkManagementInformationCode.SignOn:
                            CyberGWFISCResource.CyberGWFISC.SignOn(payload);
                            break;
                        case FISCTransactionConst.NetworkManagementInformationCode.SignOff:
                            CyberGWFISCResource.CyberGWFISC.SignOff(payload);
                            break;
                        case FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest:
                            CyberGWFISCResource.CyberGWFISC.ChangeKey(payload);
                            break;
                        case FISCTransactionConst.NetworkManagementInformationCode.EchoTest:
                            CyberGWFISCResource.CyberGWFISC.EchoTest(payload);
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

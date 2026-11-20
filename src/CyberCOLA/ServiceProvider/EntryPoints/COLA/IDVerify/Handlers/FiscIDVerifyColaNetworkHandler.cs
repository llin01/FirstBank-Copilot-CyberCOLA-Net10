// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyColaNetworkHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyColaNetworkHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IDVerify.Handlers
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.TM.FISC;

    #endregion

    public class FiscIDVerifyColaNetworkHandler : ChannelInboundHandler
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
                            CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.SignOn(payload);
                            break;
                        case FISCTransactionConst.NetworkManagementInformationCode.SignOff:
                            CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.SignOff(payload);
                            break;
                        case FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest:
                            CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.ChangeKey(payload);
                            break;
                        case FISCTransactionConst.NetworkManagementInformationCode.EchoTest:
                            CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.EchoTest(payload);
                            break;
                        default:
                            ctx.Write(msg);
                            break;
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error("FiscIDVerifyColaNetworkHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

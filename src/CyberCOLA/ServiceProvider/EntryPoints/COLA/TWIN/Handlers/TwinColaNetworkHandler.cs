// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinNetworkHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinNetworkHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.TWIN.Handlers
{
    #region DEPENDENCY

    #endregion

    public class TwinColaNetworkHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = payload.Val as TWINIso8583;
            if (twin != null && twin.Mti().Equals("0800"))
            {
                var informationCode = twin.Val(FIELD.F_70);
                try
                {
                    switch (informationCode)
                    {
                        case TwinTransactionConst.FieldConstant.F70.SignOn:
                            CyberGWTWINResource.CyberGWTWIN.SignOn(payload);
                            break;
                        case TwinTransactionConst.FieldConstant.F70.SignOff:
                            CyberGWTWINResource.CyberGWTWIN.SignOff(payload);
                            break;
                        case TwinTransactionConst.FieldConstant.F70.ChangeKey:
                            CyberGWTWINResource.CyberGWTWIN.ChangeKey(payload);
                            break;
                        case TwinTransactionConst.FieldConstant.F70.RepeatKey:
                            CyberGWTWINResource.CyberGWTWIN.RepeatKey(payload);
                            break;
                        case TwinTransactionConst.FieldConstant.F70.VerifyKey:
                            CyberGWTWINResource.CyberGWTWIN.VerifyKey(payload);
                            break;
                        case TwinTransactionConst.FieldConstant.F70.EchoTest:
                            CyberGWTWINResource.CyberGWTWIN.EchoTest(payload);
                            break;
                        default:
                            ctx.Write(msg);
                            break;
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error("TwinNetworkHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

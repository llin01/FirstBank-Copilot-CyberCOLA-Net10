// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccQRCodeNetworkHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccQRCodeNetworkHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.QRCode.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccQRCodeColaNetworkHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = payload.Val as NcccIso8583;
            if (nccc != null && nccc.Mti().Equals("0800"))
            {
                var informationCode = nccc.Val(FIELD.F_70);
                try
                {
                    switch (informationCode)
                    {
                        case NcccTransactionConst.FieldConstant.F70.SignOn:
                            CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode.SignOn(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.SignOff:
                            CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode.SignOff(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.ChangeKey:
                            CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode.ChangeKey(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.RepeatKey:
                            CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode.RepeatKey(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.VerifyKey:
                            CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode.VerifyKey(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.EchoTest:
                            CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode.EchoTest(payload);
                            break;
                        default:
                            ctx.Write(msg);
                            break;
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error("NcccQRCodeNetworkHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

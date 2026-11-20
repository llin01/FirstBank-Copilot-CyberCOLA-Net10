// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccNetworkHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccNetworkHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccColaNetworkHandler : ChannelInboundHandler
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
                            CyberGWNCCCResource.CyberGWNCCC.SignOn(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.SignOff:
                            CyberGWNCCCResource.CyberGWNCCC.SignOff(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.ChangeKey:
                            CyberGWNCCCResource.CyberGWNCCC.ChangeKey(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.RepeatKey:
                            CyberGWNCCCResource.CyberGWNCCC.RepeatKey(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.VerifyKey:
                            CyberGWNCCCResource.CyberGWNCCC.VerifyKey(payload);
                            break;
                        case NcccTransactionConst.FieldConstant.F70.EchoTest:
                            CyberGWNCCCResource.CyberGWNCCC.EchoTest(payload);
                            break;
                        default:
                            ctx.Write(msg);
                            break;
                    }
                }
                catch (Exception e)
                {
                    this.Logger.Error("NcccNetworkHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

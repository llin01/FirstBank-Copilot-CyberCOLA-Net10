// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccQRCodeRegisterKeyHandler.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the NcccQRCodeRegisterKeyHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class NcccQRCodeRegisterKeyHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (NcccIso8583)payload.Val;
            var relationId = string.Format(SystemConstantData.CommonData.GatewayQRCodeIDVerifyRelationIdStringFormat,
                                           $"{nccc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{nccc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{nccc.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                           );
            //This Message no send ColaService
            if (nccc.Mti().Equals("0810"))
            {
                if (!ObjectUtils.IsNull(nccc.RawVal(FIELD.F_70)) &&
                    (nccc.Core().Field(FIELD.F_70).ToString().Equals(NcccTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     nccc.Core().Field(FIELD.F_70).ToString().Equals(NcccTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    if (!ObjectUtils.IsNull(nccc.RawVal(FIELD.F_11)) && nccc.Core().Field(FIELD.F_11).ToString().Substring(0, 1).Equals("9"))
                    {
                        this.Logger.Info($"This received message is the original message sent by the gateway , MTI[{nccc.Mti()}], F_11[{nccc.Core().Field(FIELD.F_11)}], F_39[{nccc.Core().Field(FIELD.F_39) ?? ""}], F_70[{nccc.Core().Field(FIELD.F_70) ?? ""}]");
                        return;
                    }
                }
            }
            nccc.Extend<RelationIdKey>(relationId);
            ctx.Channel.Carrier<ServiceKey, CyberGWNCCCQRCode>().RegisterRelation(relationId, ctx.Channel);
            if (CyberGWNCCCQRCodeResource.NcccQRCodeColaService == null)
            {
                Logger.Fatal("NcccQRCodeColaService No Start");
                return;
            }
            CyberGWNCCCQRCodeResource.NcccQRCodeColaService.Put(nccc);
            this.Logger.Debug($"Go Hander: {this.GetType().FullName} Put:MTI[{nccc.Mti()}], Item RelationId[{relationId}]");
        }
    }
}

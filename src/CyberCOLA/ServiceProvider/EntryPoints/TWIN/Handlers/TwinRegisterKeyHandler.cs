// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinRegisterKeyHandler.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the TwinRegisterKeyHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.TWIN;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class TwinRegisterKeyHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = (TWINIso8583)payload.Val;
            var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{twin.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{twin.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{twin.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{twin.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                           );
            //This Message no send ColaService
            if (twin.Mti().Equals("0810"))
            {
                if (!ObjectUtils.IsNull(twin.RawVal(FIELD.F_70)) &&
                    (twin.Core().Field(FIELD.F_70).ToString()!.Equals(TwinTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     twin.Core().Field(FIELD.F_70).ToString()!.Equals(TwinTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    if (!ObjectUtils.IsNull(twin.RawVal(FIELD.F_11)) && twin.Core().Field(FIELD.F_11).ToString()!.Substring(0, 1).Equals("9"))
                    {
                        this.Logger.Info($"This received message is the original message sent by the gateway , MTI[{twin.Mti()}], F_11[{twin.Core().Field(FIELD.F_11)}], F_39[{twin.Core().Field(FIELD.F_39) ?? ""}], F_70[{twin.Core().Field(FIELD.F_70) ?? ""}]");
                        return;
                    }
                }
            }
            twin.Extend<RelationIdKey>(relationId);
            ctx.Channel.Carrier<ServiceKey, CyberGWTWIN>().RegisterRelation(relationId, ctx.Channel);
            if (CyberGWTWINResource.TwinColaService == null)
            {
                Logger.Fatal("TwinColaService No Start");
                return;
            }
            CyberGWTWINResource.TwinColaService.Put(twin);
            this.Logger.Debug($"Go Hander: {this.GetType().FullName} Put:MTI[{twin.Mti()}], Item RelationId[{relationId}]");
        }
    }
}

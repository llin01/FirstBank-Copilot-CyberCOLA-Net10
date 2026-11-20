// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscRegisterKeyHandler.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the FiscRegisterKeyHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.FISC;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    #endregion

    public class FiscRegisterKeyHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{fisc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );
            //This Message no send ColaService
            if (fisc.Mti().Equals("0810"))
            {
                if (!ObjectUtils.IsNull(fisc.RawVal(FIELD.F_70)) &&
                    (fisc.Core().Field(FIELD.F_70).ToString().Equals(FISCTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     fisc.Core().Field(FIELD.F_70).ToString().Equals(FISCTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    if (!ObjectUtils.IsNull(fisc.RawVal(FIELD.F_11)) && fisc.Core().Field(FIELD.F_11).ToString().Substring(0, 1).Equals("9"))
                    {
                        this.Logger.Info($"This received message is the original message sent by the gateway , MTI[{fisc.Mti()}], F_11[{fisc.Core().Field(FIELD.F_11)}], F_39[{fisc.Core().Field(FIELD.F_39) ?? ""}], F_70[{fisc.Core().Field(FIELD.F_70) ?? ""}]");
                        return;
                    }
                }
            }
            fisc.Extend<RelationIdKey>(relationId);
            ctx.Channel.Carrier<ServiceKey, CyberGWFISC>().RegisterRelation(relationId, ctx.Channel);
            if (CyberGWFISCResource.FiscColaService == null)
            {
                Logger.Fatal("FiscColaService No Start");
                return;
            }
            CyberGWFISCResource.FiscColaService.Put(fisc);
            this.Logger.Debug($"Go Hander: {this.GetType().FullName} Put:MTI[{fisc.Mti()}], Item RelationId[{relationId}]");
        }
    }
}

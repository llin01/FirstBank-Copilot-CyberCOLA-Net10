// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerRegisterKeyHandler.cs">
//   Author: 
// </copyright>
// <summary>
//   Defines the ServerRegisterKeyHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsRegisterKeyHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{svcs.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{svcs.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{svcs.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{svcs.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );
            //This Message no send ColaService
            if (svcs.Mti().Equals("0810"))
            {
                if (!ObjectUtils.IsNull(svcs.RawVal(FIELD.F_70)) &&
                    (svcs.Core().Field(FIELD.F_70).ToString().Equals(SvcsTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     svcs.Core().Field(FIELD.F_70).ToString().Equals(SvcsTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    if (!ObjectUtils.IsNull(svcs.RawVal(FIELD.F_11)) && svcs.Core().Field(FIELD.F_11).ToString().Substring(0, 1).Equals("9"))
                    {
                        this.Logger.Info($"This received message is the original message sent by the gateway , MTI[{svcs.Mti()}], F_11[{svcs.Core().Field(FIELD.F_11)}], F_39[{svcs.Core().Field(FIELD.F_39) ?? ""}], F_70[{svcs.Core().Field(FIELD.F_70) ?? ""}]");
                        return;
                    }
                }
            }
            svcs.Extend<RelationIdKey>(relationId);
            ctx.Channel.Carrier<ServiceKey, CyberGWSVCS>().RegisterRelation(relationId, ctx.Channel);
            if (CyberGWSVCSResource.SvcsColaService == null)
            {
                Logger.Fatal("SvcsColaService No Start");
                return;
            }
            CyberGWSVCSResource.SvcsColaService.Put(svcs);
            this.Logger.Debug($"Go Hander: {this.GetType().FullName} Put:MTI[{svcs.Mti()}], Item RelationId[{relationId}]");
        }
    }
}

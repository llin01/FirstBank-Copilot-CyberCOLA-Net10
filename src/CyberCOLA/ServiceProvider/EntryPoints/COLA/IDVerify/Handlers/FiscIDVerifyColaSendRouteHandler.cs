// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyColaSendRouteHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyColaSendRouteHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IDVerify.Handlers
{
    #region DEPENDENCY

    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.TM.FISC.Transaction;

    #endregion

    public class FiscIDVerifyColaSendRouteHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            TM.FISC.FISC request;
            string relationId;
            if (fisc is FiscRequest)
            {
                this.Logger.Debug("FiscRequest Type Process");
                relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{fisc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );

                // Reserve Channel if Request
                ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Take(ctx.Channel);

                // Register Channel and RelationID for find back route
                ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().RegisterRelation(relationId, ctx.Channel);

                // Set RelationID and message relation
                fisc.Extend<RelationIdKey>(relationId);
                fisc.Core().Extend<RelationIdKey>(relationId);
                fisc.Core().Extend<BeforeConvertKey>(fisc);

                // Dispatch Message according to Destination
                CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.Put(fisc);
                this.Logger.Debug($"FiscRequest Type Process Put:MTI[{fisc.Mti()}], Item RelationId[{relationId}]");
                return;
            }
            if (fisc is FiscResponse)
            {
                this.Logger.Debug("FiscResponse Type Process");
                if (!ObjectUtils.IsNull(fisc))
                {
                    relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{fisc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.FISC.FISC>>()
                        .TryRemove(relationId, out request);
                    this.Logger.Debug($"FiscResponse Original Request Message :MTI[{request?.Mti()}], F-11[{request?.Core().Field(FIELD.F_11) ?? $"{0:D6}"}]");
                    fisc.Extend<RelationIdKey>(relationId);
                    CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.Put(fisc);
                    this.Logger.Debug($"FiscResponse Type Process Put:MTI[{fisc.Mti()}], Item RelationId[{relationId}]");
                }
                ctx.Channel.Carrier<ServiceKey, FiscIDVerifyColaService>().Release(ctx.Channel);
            }
            else
            {
                this.Logger.Info($"Transaction Is not Request and Response type Field 11[{fisc.Val(FIELD.F_11)}]");
            }
        }
    }
}

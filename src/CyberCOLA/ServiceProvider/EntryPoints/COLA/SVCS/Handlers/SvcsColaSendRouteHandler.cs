// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ColaSendRouteHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ColaSendRouteHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaSendRouteHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            // The monitor's STOP command clears the gateway; a message arriving afterwards is dropped, but a
            // response must still free its COLA channel, or that channel stays reserved for good.
            var gateway = CyberGWSVCSResource.CyberGWSVCS;
            if (ObjectUtils.IsNull(gateway))
            {
                this.Logger.Warn($"CyberGWSVCS is stopped, drop message: MTI[{svcs.Mti()}]");
                if (svcs is SvcsResponse)
                {
                    ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Release(ctx.Channel);
                }

                return;
            }
            TM.ISO8583.SVCS.SVCS request;
            string relationId;
            if (svcs is SvcsRequest)
            {
                this.Logger.Debug("SvcsRequest Type Process");
                relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{svcs.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{svcs.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{svcs.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{svcs.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );

                // Reserve Channel if Request
                ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Take(ctx.Channel);

                // Register Channel and RelationID for find back route
                ctx.Channel.Carrier<ServiceKey, SvcsColaService>().RegisterRelation(relationId, ctx.Channel);

                // Set RelationID and message relation
                svcs.Extend<RelationIdKey>(relationId);
                svcs.Core().Extend<RelationIdKey>(relationId);
                svcs.Core().Extend<BeforeConvertKey>(svcs);

                // Dispatch Message according to Destination
                gateway.Put(svcs);
                this.Logger.Debug($"SvcsRequest Type Process Put:MTI[{svcs.Mti()}], Item RelationId[{relationId}]");
                return;
            }
            if (svcs is SvcsResponse)
            {
                this.Logger.Debug("SvcsResponse Type Process");
                if (!ObjectUtils.IsNull(svcs))
                {
                    relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{svcs.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{svcs.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{svcs.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{svcs.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.SVCS.SVCS>>()
                               .TryRemove(relationId, out request);
                    this.Logger.Debug($"SvcsResponse Original Request Message :MTI[{request?.Mti()}], F-11[{request?.Core().Field(FIELD.F_11) ?? $"{0:D6}"}]");
                    svcs.Extend<RelationIdKey>(relationId);
                    gateway.Put(svcs);
                    this.Logger.Debug($"SvcsResponse Type Process Put:MTI[{svcs.Mti()}], Item RelationId[{relationId}]");
                }
                ctx.Channel.Carrier<ServiceKey, SvcsColaService>().Release(ctx.Channel);
            }
            else
            {
                this.Logger.Info($"Transaction Is not Request and Response type Field 11[{svcs.Val(FIELD.F_11)}]");
            }
        }
    }
}

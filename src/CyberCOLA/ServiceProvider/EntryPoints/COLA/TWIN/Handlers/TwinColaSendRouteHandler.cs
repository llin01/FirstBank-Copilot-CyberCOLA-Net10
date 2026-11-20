// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinColaSendRouteHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinColaSendRouteHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.TWIN.Handlers
{
    #region DEPENDENCY

    #endregion

    public class TwinColaSendRouteHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = (TWINIso8583)payload.Val;
            // The monitor's STOP command clears the gateway; a message arriving afterwards is dropped, but a
            // response must still free its COLA channel, or that channel stays reserved for good.
            var gateway = CyberGWTWINResource.CyberGWTWIN;
            if (ObjectUtils.IsNull(gateway))
            {
                this.Logger.Warn($"CyberGWTWIN is stopped, drop message: MTI[{twin.Mti()}]");
                if (twin is TwinResponse)
                {
                    ctx.Channel.Carrier<ServiceKey, TwinColaService>().Release(ctx.Channel);
                }

                return;
            }
            TWINIso8583 request;
            string relationId;
            if (twin is TwinRequest)
            {
                this.Logger.Debug("TwinRequest Type Process");
                relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{twin.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{twin.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{twin.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{twin.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                           );

                // Reserve Channel if Request
                ctx.Channel.Carrier<ServiceKey, TwinColaService>().Take(ctx.Channel);

                // Register Channel and RelationID for find back route
                ctx.Channel.Carrier<ServiceKey, TwinColaService>().RegisterRelation(relationId, ctx.Channel);

                // Set RelationID and message relation
                twin.Extend<RelationIdKey>(relationId);
                twin.Core().Extend<RelationIdKey>(relationId);
                twin.Core().Extend<BeforeConvertKey>(twin);

                // Dispatch Message according to Destination
                gateway.Put(twin);
                this.Logger.Debug($"TwinRequest Type Process Put:MTI[{twin.Mti()}], Item RelationId[{relationId}]");
                return;
            }
            if (twin is TwinResponse)
            {
                this.Logger.Debug("TwinResponse Type Process");
                if (!ObjectUtils.IsNull(twin))
                {
                    relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{twin.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{twin.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{twin.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{twin.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                               );

                    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TWINIso8583>>()
                        .TryRemove(relationId, out request);
                    this.Logger.Debug($"TwinResponse Original Request Message :MTI[{request?.Mti()}], F-11[{request?.Core().Field(FIELD.F_11) ?? $"{0:D6}"}]");
                    twin.Extend<RelationIdKey>(relationId);
                    gateway.Put(twin);
                    this.Logger.Debug($"TwinResponse Type Process Put:MTI[{twin.Mti()}], Item RelationId[{relationId}]");
                }
                ctx.Channel.Carrier<ServiceKey, TwinColaService>().Release(ctx.Channel);
            }
            else
            {
                this.Logger.Info($"Transaction Is not Request and Response type Field 11[{twin.Val(FIELD.F_11)}]");
            }
            //relationId = $"Nccc{nccc.Val(FIELD.F_11)}";
            //ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, NcccIso8583>>().TryRemove(relationId, out request);
            //nccc.Extend<RelationIdKey>(relationId);
            //GatewayResource.NcccService.Put(nccc);
            //ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);

            //// Get Request Message if exist
            //if (nccc is NcccResponse)
            //{
            //    //var relationId = string.Format("{0}{1}", nccc.Dst, nccc.Val(FIELD.F_11));
            //    var relationId = string.Format("{0}", nccc.Val(FIELD.F_11));
            //    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, NcccIso8583>>()
            //        .TryRemove(relationId, out request);
            //}
            ////iso8583 = Request2ResponseConverter.Default.ToOrigin(nccc);

            //if (nccc is NcccRequest)
            //{
            //    this.Logger.Debug("McccRequest Type Process");
            //    var relationId = string.Format("cola{0}", nccc.Val(FIELD.F_11));

            //    // Reserve Channel if Request
            //    ctx.Channel.Carrier<ServiceKey, NcccColaService>().Take(ctx.Channel);

            //    // Register Channel and RelationID for find back route
            //    ctx.Channel.Carrier<ServiceKey, NcccColaService>().RegisterRelation(relationId, ctx.Channel);

            //    // Set RelationID and message relation
            //    nccc.Extend<RelationIdKey>(relationId);
            //    iso8583.Core().Extend<RelationIdKey>(relationId);
            //    iso8583.Core().Extend<BeforeConvertKey>(nccc);

            //    // Dispatch Message according to Destination
            //    //ResourceAllocator.CyberGWNCCC.Put((NcccIso8583)iso8583);
            //    CyberGWNCCCResource.CyberGWNCCC.Put((NcccIso8583)iso8583);
            //    return;
            //}
            //if (nccc is NcccResponse)
            //{
            //    this.Logger.Debug("NcccResponse Type Process");
            //    if (!ObjectUtils.IsNull(request))
            //    {
            //        var relationId = request.Extend<RelationIdKey, string>();
            //        nccc.Extend<RelationIdKey>(relationId);
            //        nccc.Extend<RequestKey>(request);
            //        iso8583.Core().Extend<RelationIdKey>(relationId);
            //        iso8583.Core().Extend<BeforeConvertKey>(nccc);
            //        iso8583.Core().Extend<RequestKey>(request.Extend<BeforeConvertKey>());
            //        CyberGWNCCCResource.CyberGWNCCC.Put((NcccIso8583)iso8583);
            //    }

            //    ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);
            //}
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccColaSendRouteHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccColaSendRouteHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccColaSendRouteHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (NcccIso8583)payload.Val;
            // The monitor's STOP command clears the gateway; a message arriving afterwards is dropped, but a
            // response must still free its COLA channel, or that channel stays reserved for good.
            var gateway = CyberGWNCCCResource.CyberGWNCCC;
            if (ObjectUtils.IsNull(gateway))
            {
                this.Logger.Warn($"CyberGWNCCC is stopped, drop message: MTI[{nccc.Mti()}]");
                if (nccc is NcccResponse)
                {
                    ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);
                }

                return;
            }
            NcccIso8583 request;
            string relationId;
            if (nccc is NcccRequest)
            {
                this.Logger.Debug("NcccRequest Type Process");
                relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{nccc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{nccc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{nccc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{nccc.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                           );

                // Reserve Channel if Request
                ctx.Channel.Carrier<ServiceKey, NcccColaService>().Take(ctx.Channel);

                // Register Channel and RelationID for find back route
                ctx.Channel.Carrier<ServiceKey, NcccColaService>().RegisterRelation(relationId, ctx.Channel);

                // Set RelationID and message relation
                nccc.Extend<RelationIdKey>(relationId);
                nccc.Core().Extend<RelationIdKey>(relationId);
                nccc.Core().Extend<BeforeConvertKey>(nccc);

                // Dispatch Message according to Destination
                gateway.Put(nccc);
                this.Logger.Debug($"NcccRequest Type Process Put:MTI[{nccc.Mti()}], Item RelationId[{relationId}]");
                return;
            }
            if (nccc is NcccResponse)
            {
                this.Logger.Debug("NcccResponse Type Process");
                if (!ObjectUtils.IsNull(nccc))
                {
                    relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{nccc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{nccc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{nccc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{nccc.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                               );

                    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, NcccIso8583>>()
                       .TryRemove(relationId, out request);
                    this.Logger.Debug($"NcccResponse Original Request Message :MTI[{request?.Mti()}], F-11[{request?.Core().Field(FIELD.F_11) ?? $"{0:D6}"}]");
                    nccc.Extend<RelationIdKey>(relationId);
                    gateway.Put(nccc);
                    this.Logger.Debug($"NcccResponse Type Process Put:MTI[{nccc.Mti()}], Item RelationId[{relationId}]");
                }
                ctx.Channel.Carrier<ServiceKey, NcccColaService>().Release(ctx.Channel);
            }
            else
            {
                this.Logger.Info($"Transaction Is not Request and Response type Field 11[{nccc.Val(FIELD.F_11)}]");
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

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
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaSendRouteHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            // The monitor's STOP command clears the gateway; a message arriving afterwards is dropped, but a
            // response must still free its COLA channel, or that channel stays reserved for good.
            var gateway = CyberGWICASHResource.CyberGWICASH;
            if (ObjectUtils.IsNull(gateway))
            {
                this.Logger.Warn($"CyberGWICASH is stopped, drop message: MTI[{icash.Mti()}]");
                if (icash is IcashResponse)
                {
                    ctx.Channel.Carrier<ServiceKey, IcashColaService>().Release(ctx.Channel);
                }

                return;
            }
            TM.ISO8583.ICASH.ICASH request;
            string relationId;
            if (icash is IcashRequest)
            {
                this.Logger.Debug("IcashRequest Type Process");
                relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{icash.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{icash.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{icash.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{icash.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );

                // Reserve Channel if Request
                ctx.Channel.Carrier<ServiceKey, IcashColaService>().Take(ctx.Channel);

                // Register Channel and RelationID for find back route
                ctx.Channel.Carrier<ServiceKey, IcashColaService>().RegisterRelation(relationId, ctx.Channel);

                // Set RelationID and message relation
                icash.Extend<RelationIdKey>(relationId);
                icash.Core().Extend<RelationIdKey>(relationId);
                icash.Core().Extend<BeforeConvertKey>(icash);

                // Dispatch Message according to Destination
                gateway.Put(icash);
                this.Logger.Debug($"IcashRequest Type Process Put:MTI[{icash.Mti()}], Item RelationId[{relationId}]");
                return;
            }
            if (icash is IcashResponse)
            {
                this.Logger.Debug("IcashResponse Type Process");
                if (!ObjectUtils.IsNull(icash))
                {
                    relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{icash.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{icash.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{icash.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{icash.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.ICASH.ICASH>>()
                               .TryRemove(relationId, out request);
                    this.Logger.Debug($"IcashResponse Original Request Message :MTI[{request?.Mti()}], F-11[{request?.Core().Field(FIELD.F_11) ?? $"{0:D6}"}]");
                    icash.Extend<RelationIdKey>(relationId);
                    gateway.Put(icash);
                    this.Logger.Debug($"IcashResponse Type Process Put:MTI[{icash.Mti()}], Item RelationId[{relationId}]");
                }
                ctx.Channel.Carrier<ServiceKey, IcashColaService>().Release(ctx.Channel);
            }
            else
            {
                this.Logger.Info($"Transaction Is not Request and Response type Field 11[{icash.Val(FIELD.F_11)}]");
            }
        }
    }
}

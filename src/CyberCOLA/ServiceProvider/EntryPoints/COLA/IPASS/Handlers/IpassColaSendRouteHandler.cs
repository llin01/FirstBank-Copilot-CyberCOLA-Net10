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
using Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassColaSendRouteHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var ipass = (TM.ISO8583.IPASS.IPASS)payload.Val;
            // The monitor's STOP command clears the gateway; a message arriving afterwards is dropped, but a
            // response must still free its COLA channel, or that channel stays reserved for good.
            var gateway = CyberGWIPASSResource.CyberGWIPASS;
            if (ObjectUtils.IsNull(gateway))
            {
                this.Logger.Warn($"CyberGWIPASS is stopped, drop message: MTI[{ipass.Mti()}]");
                if (ipass is IpassResponse)
                {
                    ctx.Channel.Carrier<ServiceKey, IpassColaService>().Release(ctx.Channel);
                }

                return;
            }
            TM.ISO8583.IPASS.IPASS request;
            string relationId;
            if (ipass is IpassRequest)
            {
                this.Logger.Debug("IpassRequest Type Process");
                relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                              //    $"{ipass.Core().Field(FIELD.F_7)  ?? $"{0:D10}"}",
                                              ipass.Mti().Substring(0, 2).Equals("08") ? $"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                           $"{ipass.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{ipass.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                           $"{ipass.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );

                // Reserve Channel if Request
                ctx.Channel.Carrier<ServiceKey, IpassColaService>().Take(ctx.Channel);

                // Register Channel and RelationID for find back route
                ctx.Channel.Carrier<ServiceKey, IpassColaService>().RegisterRelation(relationId, ctx.Channel);

                // Set RelationID and message relation
                ipass.Extend<RelationIdKey>(relationId);
                ipass.Core().Extend<RelationIdKey>(relationId);
                ipass.Core().Extend<BeforeConvertKey>(ipass);

                // Dispatch Message according to Destination
                gateway.Put(ipass);
                this.Logger.Debug($"IpassRequest Type Process Put:MTI[{ipass.Mti()}], Item RelationId[{relationId}]");
                return;
            }
            if (ipass is IpassResponse)
            {
                this.Logger.Debug("IpassResponse Type Process");
                if (!ObjectUtils.IsNull(ipass))
                {
                    relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                              // $"{ipass.Core().Field(FIELD.F_7)  ?? $"{0:D10}"}",
                                              ipass.Mti().Substring(0, 2).Equals("08") ? $"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                               $"{ipass.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{ipass.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                               $"{ipass.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                    ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.IPASS.IPASS>>()
                               .TryRemove(relationId, out request);
                    this.Logger.Debug($"IpassResponse Original Request Message :MTI[{request?.Mti()}], F-11[{request?.Core().Field(FIELD.F_11) ?? $"{0:D6}"}]");
                    ipass.Extend<RelationIdKey>(relationId);
                    gateway.Put(ipass);
                    this.Logger.Debug($"IpassResponse Type Process Put:MTI[{ipass.Mti()}], Item RelationId[{relationId}]");
                }
                ctx.Channel.Carrier<ServiceKey, IpassColaService>().Release(ctx.Channel);
            }
            else
            {
                this.Logger.Info($"Transaction Is not Request and Response type Field 11[{ipass.Val(FIELD.F_11)}]");
            }
        }
    }
}

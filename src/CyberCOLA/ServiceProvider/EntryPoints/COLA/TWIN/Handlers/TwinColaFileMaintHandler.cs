// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinFileMaintHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinFileMaintHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.TWIN.Handlers
{
    #region DEPENDENCY

    #endregion

    public class TwinColaFileMaintHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = payload.Val as TWINIso8583;
            if (twin != null && twin.Mti().Equals("0300"))
            {
                try
                {
                    this.Logger.Debug("TwinRequest Type Process");
                    var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
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
                    CyberGWTWINResource.CyberGWTWIN.ChannelWriteMaintenance(payload);
                }
                catch (Exception e)
                {
                    this.Logger.Error("TwinColaFileMaintHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

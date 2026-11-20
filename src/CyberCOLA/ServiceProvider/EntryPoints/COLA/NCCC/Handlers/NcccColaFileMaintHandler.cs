// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccFileMaintHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccFileMaintHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccColaFileMaintHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = payload.Val as NcccIso8583;
            if (nccc != null && nccc.Mti().Equals("0300"))
            {
                try
                {
                    this.Logger.Debug("NcccRequest Type Process");
                    var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
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
                    CyberGWNCCCResource.CyberGWNCCC.ChannelWriteMaintenance(payload);
                }
                catch (Exception e)
                {
                    this.Logger.Error("NcccColaFileMaintHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

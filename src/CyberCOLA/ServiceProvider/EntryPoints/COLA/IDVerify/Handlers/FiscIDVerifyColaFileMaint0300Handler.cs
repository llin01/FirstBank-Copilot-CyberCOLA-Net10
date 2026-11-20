// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyColaFileMaint0300Handler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyColaFileMaint0300Handler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IDVerify.Handlers
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class FiscIDVerifyColaFileMaint0300Handler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = payload.Val as TM.FISC.FISC;
            if (fisc != null && fisc.Mti().Equals("0300"))
            {
                try
                {
                    this.Logger.Debug("FicsRequest Type Process");
                    var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
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
                    CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify.ChannelWriteMaintenance300(payload);
                }
                catch (Exception e)
                {
                    this.Logger.Error("FiscIDVerifyColaFileMaint0300Handler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

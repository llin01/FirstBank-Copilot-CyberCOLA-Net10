// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IpassColaFileMaintHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassColaFileMaintHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassColaFileMaintHandler : ChannelInboundHandler
    {
        private static readonly ConfigurationLoaderRoot configurationLoaderRoot = new ConfigurationLoaderRoot();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            if (payload.Val is TM.ISO8583.IPASS.IPASS ipass && ipass.Mti().Equals("0302"))
            {
                try
                {
                    this.Logger.Debug("IpassRequest 0302 Type Process");
                    var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                      // $"{ipass.Core().Field(FIELD.F_7)  ?? $"{0:D10}"}",
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
                    CyberGWIPASSResource.CyberGWIPASS.ChannelWriteMaintenance(payload);
                }
                catch (Exception e)
                {
                    this.Logger.Error("IpassColaFileMaintHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

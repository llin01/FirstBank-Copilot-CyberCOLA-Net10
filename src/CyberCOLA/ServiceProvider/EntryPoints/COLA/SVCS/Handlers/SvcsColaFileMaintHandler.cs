// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SvcsColaFileMaintHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SvcsColaFileMaintHandler type.
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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaFileMaintHandler : ChannelInboundHandler
    {
        private static readonly ConfigurationLoaderRoot configurationLoaderRoot = new ConfigurationLoaderRoot();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            if (payload.Val is TM.ISO8583.SVCS.SVCS svcs && svcs.Mti().Equals("0302"))
            {
                try
                {
                    this.Logger.Debug("SvcsRequest 0302 Type Process");
                    var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
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
                    CyberGWSVCSResource.CyberGWSVCS.ChannelWriteMaintenance(payload);
                }
                catch (Exception e)
                {
                    this.Logger.Error("SvcsColaFileMaintHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

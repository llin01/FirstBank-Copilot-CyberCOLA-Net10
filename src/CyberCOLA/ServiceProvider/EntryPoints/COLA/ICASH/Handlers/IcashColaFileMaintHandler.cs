// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IcashColaFileMaintHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashColaFileMaintHandler type.
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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaFileMaintHandler : ChannelInboundHandler
    {
        private static readonly ConfigurationLoaderRoot configurationLoaderRoot = new ConfigurationLoaderRoot();

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            if (payload.Val is TM.ISO8583.ICASH.ICASH icash && icash.Mti().Equals("0302"))
            {
                try
                {
                    this.Logger.Debug("IcashRequest 0302 Type Process");
                    var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
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
                    CyberGWICASHResource.CyberGWICASH.ChannelWriteMaintenance(payload);
                }
                catch (Exception e)
                {
                    this.Logger.Error("IcashColaFileMaintHandler Write Failed.");
                    this.Logger.Error(e);
                }
                return;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}

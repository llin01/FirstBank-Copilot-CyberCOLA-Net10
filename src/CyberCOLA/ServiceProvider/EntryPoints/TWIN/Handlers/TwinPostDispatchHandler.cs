// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.TM.ISO8583.TWIN;
    using Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class TwinPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = (TWINIso8583)payload.Val;
            if (!(twin is TwinRequest) && !(twin is TwinResponse))
            {
                if (Convert.ToUInt16(twin.Mti()[2]) % 2 == 0)
                {
                    twin = (TWINIso8583)TwinTransCreator.TwinRequest().Core(twin.Core());
                }
                else
                {
                    twin = (TWINIso8583)TwinTransCreator.TwinResponse().Core(twin.Core());
                }

                payload.Val = twin;
            }

            if (twin is TwinRequest)
            {
                var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{twin.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{twin.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{twin.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{twin.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                               );

                ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TWINIso8583>>()[relationId] = twin;
            }

            if (twin is TwinResponse)
            {
            }

            base.Write(ctx, msg);
        }
    }
}

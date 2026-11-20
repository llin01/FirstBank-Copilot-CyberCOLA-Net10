// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class NcccPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (NcccIso8583)payload.Val;
            if (!(nccc is NcccRequest) && !(nccc is NcccResponse))
            {
                if (Convert.ToUInt16(nccc.Mti()[2]) % 2 == 0)
                {
                    nccc = (NcccIso8583)NcccTransCreator.NcccRequest().Core(nccc.Core());
                }
                else
                {
                    nccc = (NcccIso8583)NcccTransCreator.NcccResponse().Core(nccc.Core());
                }

                payload.Val = nccc;
            }

            if (nccc is NcccRequest)
            {
                var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{nccc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{nccc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{nccc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{nccc.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                               );

                ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, NcccIso8583>>()[relationId] = nccc;
            }

            if (nccc is NcccResponse)
            {
            }

            base.Write(ctx, msg);
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            if (!(svcs is SvcsRequest) && !(svcs is SvcsResponse))

            {
                if (Convert.ToUInt16(svcs.Mti()[2]) % 2 == 0)
                {
                    //svcs = (TM.ISO8583.SVCS.SVCS)SvcsTransCreator.SvcsRequest().Core(svcs.Core());
                    var header = $"{svcs.Header().DestID()}{svcs.Header().SrcID()}";
                    svcs = SvcsTransCreator.SvcsRequest().Core(svcs.Core());
                    svcs.Header().Decode(header);
                }
                else
                {
                    //svcs = (TM.ISO8583.SVCS.SVCS)SvcsTransCreator.SvcsResponse().Core(svcs.Core());
                    var header = $"{svcs.Header().DestID()}{svcs.Header().SrcID()}";
                    svcs = SvcsTransCreator.SvcsResponse().Core(svcs.Core());
                    svcs.Header().Decode(header);
                }

                payload.Val = svcs;
            }

            if (svcs is SvcsRequest)
            {
                var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{svcs.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{svcs.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{svcs.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{svcs.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.SVCS.SVCS>>()[relationId] = svcs;
            }

            if (svcs is SvcsResponse)
            {
            }

            base.Write(ctx, msg);
        }
    }
}

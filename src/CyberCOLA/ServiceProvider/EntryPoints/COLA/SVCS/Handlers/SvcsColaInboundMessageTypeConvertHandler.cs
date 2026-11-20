// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerColaInboundMessageTypeConvertHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerColaInboundMessageTypeConvertHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaInboundMessageTypeConvertHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            switch (svcs.Mti())
            {
                case "0800":
                    if ("001".Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = new SvcsSignOnRequest().Core(svcs.Core());
                    }
                    else if ("002".Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = new SvcsSignOffRequest().Core(svcs.Core());
                    }
                    else if ("301".Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = new SvcsEchoTestRequest().Core(svcs.Core());
                    }
                    else
                    {
                        var header = svcs.Header().Encode();
                        svcs = new SvcsRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    break;
                case "0810":
                    if ("001".Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = new SvcsSignOnResponse().Core(svcs.Core());
                    }
                    else if ("002".Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = new SvcsSignOffResponse().Core(svcs.Core());
                    }
                    else if ("301".Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = new SvcsEchoTestResponse().Core(svcs.Core());
                    }
                    else
                    {
                        var header = svcs.Header().Encode();
                        svcs = new SvcsResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    break;
                default:
                    if (Convert.ToUInt16(svcs.Mti()[2]) % 2 == 0)
                    {
                        var header = svcs.Header().Encode();
                        svcs = new SvcsRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                    }
                    else
                    {
                        var header = svcs.Header().Encode();
                        svcs = new SvcsResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
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
                    payload.Val = svcs;
                    break;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

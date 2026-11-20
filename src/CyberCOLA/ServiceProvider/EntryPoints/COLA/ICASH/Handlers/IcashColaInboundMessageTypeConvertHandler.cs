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
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaInboundMessageTypeConvertHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            switch (icash.Mti())
            {
                case "0800":
                    if ("001".Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = new IcashSignOnRequest().Core(icash.Core());
                    }
                    else if ("002".Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = new IcashSignOffRequest().Core(icash.Core());
                    }
                    else if ("301".Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = new IcashEchoTestRequest().Core(icash.Core());
                    }
                    else
                    {
                        var header = icash.Header().Encode();
                        icash = new IcashRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    break;
                case "0810":
                    if ("001".Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = new IcashSignOnResponse().Core(icash.Core());
                    }
                    else if ("002".Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = new IcashSignOffResponse().Core(icash.Core());
                    }
                    else if ("301".Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = new IcashEchoTestResponse().Core(icash.Core());
                    }
                    else
                    {
                        var header = icash.Header().Encode();
                        icash = new IcashResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    break;
                default:
                    if (Convert.ToUInt16(icash.Mti()[2]) % 2 == 0)
                    {
                        var header = icash.Header().Encode();
                        icash = new IcashRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                    }
                    else
                    {
                        var header = icash.Header().Encode();
                        icash = new IcashResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
                    if (icash is IcashRequest)
                    {
                        var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{icash.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{icash.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{icash.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{icash.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                       );

                        ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.ICASH.ICASH>>()[relationId] = icash;
                    }
                    payload.Val = icash;
                    break;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

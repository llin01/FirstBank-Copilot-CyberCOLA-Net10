// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinInboundMessageTypeConvertHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinInboundMessageTypeConvertHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.TWIN.Handlers
{
    #region DEPENDENCY

    #endregion

    public class TwinColaInboundMessageTypeConvertHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = (TWINIso8583)payload.Val;
            switch (twin.Mti())
            {
                case "0800":
                    if ("001".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinSignOnRequest().Core(twin.Core());
                    }
                    else if ("002".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinSignOffRequest().Core(twin.Core());
                    }
                    else if ("301".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinEchoTestRequest().Core(twin.Core());
                    }
                    else if ("161".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinChangeKeyRequest().Core(twin.Core());
                    }
                    else if ("162".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinNewKeyRequest().Core(twin.Core());
                    }
                    else
                    {
                        var header = twin.Header().Encode();
                        twin = new TwinRequest().Core(twin.Core());
                        twin.Header().Decode(header);
                        payload.Val = twin;
                    }
                    break;
                case "0810":
                    if ("001".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinSignOnResponse().Core(twin.Core());
                    }
                    else if ("002".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinSignOffResponse().Core(twin.Core());
                    }
                    else if ("301".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinEchoTestResponse().Core(twin.Core());
                    }
                    else if ("161".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinChangeKeyResponse().Core(twin.Core());
                    }
                    else if ("162".Equals(twin.Val(FIELD.F_70)))
                    {
                        payload.Val = new TwinNewKeyResponse().Core(twin.Core());
                    }
                    else
                    {
                        var header = twin.Header().Encode();
                        twin = new TwinResponse().Core(twin.Core());
                        twin.Header().Decode(header);
                        payload.Val = twin;
                    }
                    break;
                default:
                    if (Convert.ToUInt16(twin.Mti()[2]) % 2 == 0)
                    {
                        var header = twin.Header().Encode();
                        twin = new TwinRequest().Core(twin.Core());
                        twin.Header().Decode(header);
                    }
                    else
                    {
                        var header = twin.Header().Encode();
                        twin = new TwinResponse().Core(twin.Core());
                        twin.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
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
                    payload.Val = twin;
                    break;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

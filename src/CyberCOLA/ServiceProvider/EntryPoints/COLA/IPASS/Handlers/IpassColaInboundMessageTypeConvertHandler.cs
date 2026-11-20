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
using Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassColaInboundMessageTypeConvertHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var ipass = (TM.ISO8583.IPASS.IPASS)payload.Val;
            switch (ipass.Mti())
            {
                case "0800":
                    if ("001".Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = new IpassSignOnRequest().Core(ipass.Core());
                    }
                    else if ("002".Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = new IpassSignOffRequest().Core(ipass.Core());
                    }
                    else if ("301".Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = new IpassEchoTestRequest().Core(ipass.Core());
                    }
                    else
                    {
                        var header = ipass.Header().Encode();
                        ipass = new IpassRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    break;
                case "0810":
                    if ("001".Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = new IpassSignOnResponse().Core(ipass.Core());
                    }
                    else if ("002".Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = new IpassSignOffResponse().Core(ipass.Core());
                    }
                    else if ("301".Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = new IpassEchoTestResponse().Core(ipass.Core());
                    }
                    else
                    {
                        var header = ipass.Header().Encode();
                        ipass = new IpassResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    break;
                default:
                    if (Convert.ToUInt16(ipass.Mti()[2]) % 2 == 0)
                    {
                        var header = ipass.Header().Encode();
                        ipass = new IpassRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                    }
                    else
                    {
                        var header = ipass.Header().Encode();
                        ipass = new IpassResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
                    if (ipass is IpassRequest)
                    {
                        var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                          //  $"{ipass.Core().Field(FIELD.F_7)  ?? $"{0:D10}"}",
                                                          ipass.Mti().Substring(0, 2).Equals("08") ? $"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                                       $"{ipass.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{ipass.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                                       $"{ipass.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                       );

                        ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.IPASS.IPASS>>()[relationId] = ipass;
                    }
                    payload.Val = ipass;
                    break;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

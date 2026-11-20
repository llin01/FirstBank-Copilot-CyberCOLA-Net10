// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccInboundMessageTypeConvertHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccInboundMessageTypeConvertHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.NCCC.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccColaInboundMessageTypeConvertHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (NcccIso8583)payload.Val;
            switch (nccc.Mti())
            {
                case "0800":
                    if ("001".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new SignOnRequest().Core(nccc.Core());
                    }
                    else if ("002".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new SignOffRequest().Core(nccc.Core());
                    }
                    else if ("301".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new EchoTestRequest().Core(nccc.Core());
                    }
                    else if ("161".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new ChangeKeyRequest().Core(nccc.Core());
                    }
                    else if ("162".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new NewKeyRequest().Core(nccc.Core());
                    }
                    else
                    {
                        var header = nccc.Header().Encode();
                        nccc = new NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                        payload.Val = nccc;
                    }
                    break;
                case "0810":
                    if ("001".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new SignOnResponse().Core(nccc.Core());
                    }
                    else if ("002".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new SignOffResponse().Core(nccc.Core());
                    }
                    else if ("301".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new EchoTestResponse().Core(nccc.Core());
                    }
                    else if ("161".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new ChangeKeyResponse().Core(nccc.Core());
                    }
                    else if ("162".Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = new NewKeyResponse().Core(nccc.Core());
                    }
                    else
                    {
                        var header = nccc.Header().Encode();
                        nccc = new NcccResponse().Core(nccc.Core());
                        nccc.Header().Decode(header);
                        payload.Val = nccc;
                    }
                    break;
                default:
                    if (Convert.ToUInt16(nccc.Mti()[2]) % 2 == 0)
                    {
                        var header = nccc.Header().Encode();
                        nccc = new NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                    }
                    else
                    {
                        var header = nccc.Header().Encode();
                        nccc = new NcccResponse().Core(nccc.Core());
                        nccc.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
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
                    payload.Val = nccc;
                    break;
            }

            base.ChannelRead(ctx, msg);
        }
    }
}

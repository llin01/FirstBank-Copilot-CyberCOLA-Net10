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
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            if (!(icash is IcashRequest) && !(icash is IcashResponse))

            {
                if (Convert.ToUInt16(icash.Mti()[2]) % 2 == 0)
                {
                    //icash = (TM.ISO8583.ICASH.ICASH)IcashTransCreator.IcashRequest().Core(icash.Core());
                    var header = $"{icash.Header().SrcID()}{icash.Header().DestID()}";
                    icash = IcashTransCreator.IcashRequest().Core(icash.Core());
                    icash.Header().Decode(header);
                }
                else
                {
                    //icash = (TM.ISO8583.ICASH.ICASH)IcashTransCreator.IcashResponse().Core(icash.Core());
                    var header = $"{icash.Header().SrcID()}{icash.Header().DestID()}";
                    icash = IcashTransCreator.IcashResponse().Core(icash.Core());
                    icash.Header().Decode(header);
                }

                payload.Val = icash;
            }

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

            if (icash is IcashResponse)
            {
            }

            base.Write(ctx, msg);
        }
    }
}

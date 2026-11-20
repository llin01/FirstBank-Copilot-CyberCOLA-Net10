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
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var ipass = (TM.ISO8583.IPASS.IPASS)payload.Val;
            if (!(ipass is IpassRequest) && !(ipass is IpassResponse))

            {
                if (Convert.ToUInt16(ipass.Mti()[2]) % 2 == 0)
                {
                    //ipass = (TM.ISO8583.IPASS.IPASS)IpassTransCreator.IpassRequest().Core(ipass.Core());
                    var header = $"{ipass.Header().DestID()}{ipass.Header().SrcID()}";
                    ipass = IpassTransCreator.IpassRequest().Core(ipass.Core());
                    ipass.Header().Decode(header);
                }
                else
                {
                    //ipass = (TM.ISO8583.IPASS.IPASS)IpassTransCreator.IpassResponse().Core(ipass.Core());
                    var header = $"{ipass.Header().DestID()}{ipass.Header().SrcID()}";
                    ipass = IpassTransCreator.IpassResponse().Core(ipass.Core());
                    ipass.Header().Decode(header);
                }

                payload.Val = ipass;
            }

            if (ipass is IpassRequest)
            {
                var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               //$"{ipass.Core().Field(FIELD.F_7)  ?? $"{0:D10}"}",
                                               ipass.Mti().Substring(0, 2).Equals("08") ? $"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                               $"{ipass.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{ipass.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                               $"{ipass.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.IPASS.IPASS>>()[relationId] = ipass;
            }

            if (ipass is IpassResponse)
            {
            }

            base.Write(ctx, msg);
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.FISC.Transaction;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    using Com.Cybersoft4u.CommonLib.AuthUtils;

    #endregion

    public class FiscPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            if (!(fisc is FiscRequest) && !(fisc is FiscResponse))
            {
                if (Convert.ToUInt16(fisc.Mti()[2]) % 2 == 0)
                {
                    fisc = (TM.FISC.FISC)FISCTransCreator.FiscRequest().Core(fisc.Core());
                }
                else
                {
                    fisc = (TM.FISC.FISC)FISCTransCreator.FiscResponse().Core(fisc.Core());
                }

                payload.Val = fisc;
            }

            if (fisc is FiscRequest)
            {
                var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{fisc.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{fisc.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{fisc.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{fisc.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                               );

                ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.FISC.FISC>>()[relationId] = fisc;
            }

            if (fisc is FiscResponse)
            {
            }
            //this for fisc 0300 fixed iso message
            if (fisc.Mti().Equals("0300"))
            {
                byte[] dataBytes = ColaCommUtility.ConvertFisc0300ToIsoFixed(fisc);
                var sendBytes = ColaCommUtility.AddBinaryLength2BytesArray(dataBytes);
                this.Logger.Debug($"Write Fixed 0300 ISO Data Length[{sendBytes.Length}]");
                base.Write(ctx, sendBytes);
            }
            else
            {
                base.Write(ctx, msg);
            }
        }
    }
}

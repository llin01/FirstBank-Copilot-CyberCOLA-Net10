// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccQRCodePostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccQRCodePostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers
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

    public class NcccQRCodePostDispatchHandler : ChannelOutboundHandler
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
                    //if (nccc.Mti().Equals("0800"))
                    //{
                    //    nccc = (NcccIso8583)NcccTransCreator.NCCCQRCodeSignOnRequest().Core(nccc.Core());
                    //}
                    //else
                    //{
                    //    nccc = (NcccIso8583)NcccTransCreator.NcccRequest().Core(nccc.Core());
                    //}
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
                string relationId = string.Format(SystemConstantData.CommonData.GatewayQRCodeIDVerifyRelationIdStringFormat,
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

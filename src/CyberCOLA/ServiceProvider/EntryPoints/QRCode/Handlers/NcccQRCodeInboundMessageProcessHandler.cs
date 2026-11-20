// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccQRCodeInboundMessageProcessHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccQRCodeInboundMessageTypeConvertHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.Resources;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583.Nccc;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion
    public class NcccQRCodeInboundMessageProcessHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {

            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (NcccIso8583)payload.Val;
            //Update NcccChannelInfo
            var ncccChannelInfo = ctx.Channel.Carrier<NcccChannelInfoKey, ChannelSessionInfo>();
            ncccChannelInfo?.Update(nccc.Encode().Val());
            ctx.Channel.Carrier<NcccChannelInfoKey>(ncccChannelInfo);
            //
            switch (nccc.Mti())
            {
                case "0800":
                    if (NcccTransactionConst.FieldConstant.F70.SignOn.Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = NcccTransCreator.SignOnRequest().Core(nccc.Core());
                    }
                    else if (NcccTransactionConst.FieldConstant.F70.SignOff.Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = NcccTransCreator.SignOffRequest().Core(nccc.Core());
                    }
                    else if (NcccTransactionConst.FieldConstant.F70.EchoTest.Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = NcccTransCreator.EchoTestRequest().Core(nccc.Core());
                    }

                    break;
                case "0810":
                    if (NcccTransactionConst.FieldConstant.F70.SignOn.Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = NcccTransCreator.SignOnResponse().Core(nccc.Core());
                    }
                    else if (NcccTransactionConst.FieldConstant.F70.SignOff.Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = NcccTransCreator.SignOffResponse().Core(nccc.Core());
                    }
                    else if (NcccTransactionConst.FieldConstant.F70.EchoTest.Equals(nccc.Val(FIELD.F_70)))
                    {
                        payload.Val = NcccTransCreator.EchoTestResponse().Core(nccc.Core());
                    }
                    //return : no process this event 20170412(Gateway send message), comment return : process this event
                    //return;
                    break;
                ////Test for Bug
                //case "0310":
                //    return;
                //    break;
                default:
                    //Add For Header Bug
                    if (Convert.ToUInt16(nccc.Mti()[2]) % 2 == 0)
                    {
                        var header = nccc.Header().Encode();
                        nccc = (NcccIso8583)NcccTransCreator.NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                    }
                    else
                    {
                        var header = nccc.Header().Encode();
                        nccc = (NcccIso8583)NcccTransCreator.NcccResponse().Core(nccc.Core());
                        nccc.Header().Decode(header);
                    }

                    payload.Val = nccc;
                    break;
            }
            //var cyberGWNCCCQRCode = ctx.Channel.Carrier<ServiceKey, CyberGWNCCCQRCode>();
            //GlobalResource.ColaService.Put(nccc);
            base.ChannelRead(ctx, msg);
        }
    }
}


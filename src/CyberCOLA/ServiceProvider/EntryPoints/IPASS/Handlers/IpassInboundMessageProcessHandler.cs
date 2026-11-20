// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerInboundMessageProcessHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerInboundMessageTypeConvertHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.ServiceProvider.Resources;
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
    public class IpassInboundMessageProcessHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {

            this.Logger.Debug($"Go Hander: {this.GetType().FullName}, Timestamp[{ctx.Channel.Carrier<ReadTransactionTimeKey, DateTime>()}]");
            var payload = (PayloadContext)msg;
            var ipass = (TM.ISO8583.IPASS.IPASS)payload.Val;
            //Update IpassChannelInfo
            var ipassChannelInfo = ctx.Channel.Carrier<IpassChannelInfoKey, ChannelSessionInfo>();
            ipassChannelInfo?.Update(ipass.Encode().Val());
            ctx.Channel.Carrier<IpassChannelInfoKey>(ipassChannelInfo);
            //
            switch (ipass.Mti())
            {
                case "0800":
                    if (IpassTransactionConst.NetworkManagementInformationCode.SignOn.Equals(ipass.Val(FIELD.F_70)))
                    {
                        payload.Val = IpassTransCreator.IpassSignOnRequest().Core(ipass.Core());
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassSignOnRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    else if (IpassTransactionConst.NetworkManagementInformationCode.SignOff.Equals(ipass.Val(FIELD.F_70)))
                    {
                        //payload.Val = IpassTransCreator.IpassSignOffRequest().Core(ipass.Core());
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassSignOffRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    else if (IpassTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(ipass.Val(FIELD.F_70)))
                    {
                        //payload.Val = IpassTransCreator.IpassEchoTestRequest().Core(ipass.Core());
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassEchoTestRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    else
                    {
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    break;
                case "0810":
                    if (IpassTransactionConst.NetworkManagementInformationCode.SignOn.Equals(ipass.Val(FIELD.F_70)))
                    {
                        //payload.Val = IpassTransCreator.IpassSignOnResponse().Core(ipass.Core());
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassSignOnResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    else if (IpassTransactionConst.NetworkManagementInformationCode.SignOff.Equals(ipass.Val(FIELD.F_70)))
                    {
                        //payload.Val = IpassTransCreator.IpassSignOffResponse().Core(ipass.Core());
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassSignOffResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    else if (IpassTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(ipass.Val(FIELD.F_70)))
                    {
                        //payload.Val = IpassTransCreator.IpassEchoTestResponse().Core(ipass.Core());
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassEchoTestResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    else
                    {
                        var header = $"{ipass.Header().DestID()}{ipass.Header().SrcID()}";
                        ipass = IpassTransCreator.IpassResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                        payload.Val = ipass;
                    }
                    break;
                default:
                    //Add For Header Bug
                    if (Convert.ToUInt16(ipass.Mti()[2]) % 2 == 0)
                    {
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassRequest().Core(ipass.Core());
                        ipass.Header().Decode(header);
                    }
                    else
                    {
                        var header = ipass.Header().Encode();
                        ipass = IpassTransCreator.IpassResponse().Core(ipass.Core());
                        ipass.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
                    if (ipass is IpassRequest)
                    {
                        var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       //$"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       ipass.Mti().Substring(0, 2).Equals("08") ? $"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                                       $"{ipass.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{ipass.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                                       $"{ipass.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
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


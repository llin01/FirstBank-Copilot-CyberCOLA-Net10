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
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion
    public class IcashInboundMessageProcessHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {

            this.Logger.Debug($"Go Hander: {this.GetType().FullName}, Timestamp[{ctx.Channel.Carrier<ReadTransactionTimeKey, DateTime>()}]");
            var payload = (PayloadContext)msg;
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            //Update IcashChannelInfo
            var icashChannelInfo = ctx.Channel.Carrier<IcashChannelInfoKey, ChannelSessionInfo>();
            icashChannelInfo?.Update(icash.Encode().Val());
            ctx.Channel.Carrier<IcashChannelInfoKey>(icashChannelInfo);
            //
            switch (icash.Mti())
            {
                case "0800":
                    if (IcashTransactionConst.NetworkManagementInformationCode.SignOn.Equals(icash.Val(FIELD.F_70)))
                    {
                        payload.Val = IcashTransCreator.IcashSignOnRequest().Core(icash.Core());
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashSignOnRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    else if (IcashTransactionConst.NetworkManagementInformationCode.SignOff.Equals(icash.Val(FIELD.F_70)))
                    {
                        //payload.Val = IcashTransCreator.IcashSignOffRequest().Core(icash.Core());
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashSignOffRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    else if (IcashTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(icash.Val(FIELD.F_70)))
                    {
                        //payload.Val = IcashTransCreator.IcashEchoTestRequest().Core(icash.Core());
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashEchoTestRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    else
                    {
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    break;
                case "0810":
                    if (IcashTransactionConst.NetworkManagementInformationCode.SignOn.Equals(icash.Val(FIELD.F_70)))
                    {
                        //payload.Val = IcashTransCreator.IcashSignOnResponse().Core(icash.Core());
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashSignOnResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    else if (IcashTransactionConst.NetworkManagementInformationCode.SignOff.Equals(icash.Val(FIELD.F_70)))
                    {
                        //payload.Val = IcashTransCreator.IcashSignOffResponse().Core(icash.Core());
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashSignOffResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    else if (IcashTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(icash.Val(FIELD.F_70)))
                    {
                        //payload.Val = IcashTransCreator.IcashEchoTestResponse().Core(icash.Core());
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashEchoTestResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    else
                    {
                        var header = $"{icash.Header().DestID()}{icash.Header().SrcID()}";
                        icash = IcashTransCreator.IcashResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                        payload.Val = icash;
                    }
                    break;
                default:
                    //Add For Header Bug
                    if (Convert.ToUInt16(icash.Mti()[2]) % 2 == 0)
                    {
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashRequest().Core(icash.Core());
                        icash.Header().Decode(header);
                    }
                    else
                    {
                        var header = icash.Header().Encode();
                        icash = IcashTransCreator.IcashResponse().Core(icash.Core());
                        icash.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
                    if (icash is IcashRequest)
                    {
                        var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{icash.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{icash.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{icash.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{icash.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
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


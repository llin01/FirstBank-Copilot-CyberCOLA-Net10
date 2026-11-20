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
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion
    public class SvcsInboundMessageProcessHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {

            this.Logger.Debug($"Go Hander: {this.GetType().FullName}, Timestamp[{ctx.Channel.Carrier<ReadTransactionTimeKey, DateTime>()}]");
            var payload = (PayloadContext)msg;
            var svcs = (TM.ISO8583.SVCS.SVCS)payload.Val;
            //Update SvcsChannelInfo
            var svcsChannelInfo = ctx.Channel.Carrier<SvcsChannelInfoKey, ChannelSessionInfo>();
            svcsChannelInfo?.Update(svcs.Encode().Val());
            ctx.Channel.Carrier<SvcsChannelInfoKey>(svcsChannelInfo);
            //
            switch (svcs.Mti())
            {
                case "0800":
                    if (SvcsTransactionConst.NetworkManagementInformationCode.SignOn.Equals(svcs.Val(FIELD.F_70)))
                    {
                        payload.Val = SvcsTransCreator.SvcsSignOnRequest().Core(svcs.Core());
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsSignOnRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    else if (SvcsTransactionConst.NetworkManagementInformationCode.SignOff.Equals(svcs.Val(FIELD.F_70)))
                    {
                        //payload.Val = SvcsTransCreator.SvcsSignOffRequest().Core(svcs.Core());
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsSignOffRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    else if (SvcsTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(svcs.Val(FIELD.F_70)))
                    {
                        //payload.Val = SvcsTransCreator.SvcsEchoTestRequest().Core(svcs.Core());
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsEchoTestRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    else
                    {
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    break;
                case "0810":
                    if (SvcsTransactionConst.NetworkManagementInformationCode.SignOn.Equals(svcs.Val(FIELD.F_70)))
                    {
                        //payload.Val = SvcsTransCreator.SvcsSignOnResponse().Core(svcs.Core());
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsSignOnResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    else if (SvcsTransactionConst.NetworkManagementInformationCode.SignOff.Equals(svcs.Val(FIELD.F_70)))
                    {
                        //payload.Val = SvcsTransCreator.SvcsSignOffResponse().Core(svcs.Core());
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsSignOffResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    else if (SvcsTransactionConst.NetworkManagementInformationCode.EchoTest.Equals(svcs.Val(FIELD.F_70)))
                    {
                        //payload.Val = SvcsTransCreator.SvcsEchoTestResponse().Core(svcs.Core());
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsEchoTestResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    else
                    {
                        var header = $"{svcs.Header().DestID()}{svcs.Header().SrcID()}";
                        svcs = SvcsTransCreator.SvcsResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                        payload.Val = svcs;
                    }
                    break;
                default:
                    //Add For Header Bug
                    if (Convert.ToUInt16(svcs.Mti()[2]) % 2 == 0)
                    {
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsRequest().Core(svcs.Core());
                        svcs.Header().Decode(header);
                    }
                    else
                    {
                        var header = svcs.Header().Encode();
                        svcs = SvcsTransCreator.SvcsResponse().Core(svcs.Core());
                        svcs.Header().Decode(header);
                    }
                    //C.F Add For Test Process Response
                    if (svcs is SvcsRequest)
                    {
                        var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{svcs.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{svcs.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{svcs.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{svcs.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                                       );

                        ctx.Channel.Carrier<RequestMapKey, ConcurrentDictionary<string, TM.ISO8583.SVCS.SVCS>>()[relationId] = svcs;
                    }
                    payload.Val = svcs;
                    break;
            }
            base.ChannelRead(ctx, msg);
        }
    }
}


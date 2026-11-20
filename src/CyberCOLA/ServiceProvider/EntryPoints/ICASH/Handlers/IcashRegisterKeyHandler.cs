// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerRegisterKeyHandler.cs">
//   Author:
// </copyright>
// <summary>
//   Defines the ServerRegisterKeyHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashRegisterKeyHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
            var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                           $"{icash.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                           $"{icash.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{icash.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                           $"{icash.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );
            //This Message no send ColaService
            if (icash.Mti().Equals("0810"))
            {
                if (!ObjectUtils.IsNull(icash.RawVal(FIELD.F_70)) &&
                    (icash.Core().Field(FIELD.F_70).ToString()!.Equals(IcashTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     icash.Core().Field(FIELD.F_70).ToString()!.Equals(IcashTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    if (!ObjectUtils.IsNull(icash.RawVal(FIELD.F_11)) && icash.Core().Field(FIELD.F_11).ToString()!.Substring(0, 1).Equals("9"))
                    {
                        this.Logger.Info($"This received message is the original message sent by the gateway , MTI[{icash.Mti()}], F_11[{icash.Core().Field(FIELD.F_11)}], F_39[{icash.Core().Field(FIELD.F_39) ?? ""}], F_70[{icash.Core().Field(FIELD.F_70) ?? ""}]");
                        return;
                    }
                }
            }
            icash.Extend<RelationIdKey>(relationId);
            ctx.Channel.Carrier<ServiceKey, CyberGWICASH>().RegisterRelation(relationId, ctx.Channel);
            if (CyberGWICASHResource.IcashColaService == null)
            {
                Logger.Fatal("IcashColaService No Start");
                return;
            }
            CyberGWICASHResource.IcashColaService.Put(icash);
            this.Logger.Debug($"Go Hander: {this.GetType().FullName} Put:MTI[{icash.Mti()}], Item RelationId[{relationId}]");
        }
    }
}

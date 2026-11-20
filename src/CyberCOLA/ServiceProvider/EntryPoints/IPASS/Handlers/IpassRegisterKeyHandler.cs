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
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassRegisterKeyHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var ipass = (TM.ISO8583.IPASS.IPASS)payload.Val;
            var relationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                             //  $"{ipass.Core().Field(FIELD.F_7)  ?? $"{0:D10}"}",
                                             ipass.Mti().Substring(0, 2).Equals("08") ? $"{ipass.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                           $"{ipass.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                           $"{ipass.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                           $"{ipass.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                           );
            //This Message no send ColaService
            if (ipass.Mti().Equals("0810"))
            {
                if (!ObjectUtils.IsNull(ipass.RawVal(FIELD.F_70)) &&
                    (ipass.Core().Field(FIELD.F_70).ToString()!.Equals(IpassTransactionConst.NetworkManagementInformationCode.SignOn) ||
                     ipass.Core().Field(FIELD.F_70).ToString()!.Equals(IpassTransactionConst.NetworkManagementInformationCode.EchoTest)))
                {
                    if (!ObjectUtils.IsNull(ipass.RawVal(FIELD.F_11)) && ipass.Core().Field(FIELD.F_11).ToString()!.Substring(0, 1).Equals("9"))
                    {
                        this.Logger.Info($"This received message is the original message sent by the gateway , MTI[{ipass.Mti()}], F_11[{ipass.Core().Field(FIELD.F_11)}], F_39[{ipass.Core().Field(FIELD.F_39) ?? ""}], F_70[{ipass.Core().Field(FIELD.F_70) ?? ""}]");
                        return;
                    }
                }
            }
            ipass.Extend<RelationIdKey>(relationId);
            ctx.Channel.Carrier<ServiceKey, CyberGWIPASS>().RegisterRelation(relationId, ctx.Channel);
            if (CyberGWIPASSResource.IpassColaService == null)
            {
                Logger.Fatal("IpassColaService No Start");
                return;
            }
            CyberGWIPASSResource.IpassColaService.Put(ipass);
            this.Logger.Debug($"Go Hander: {this.GetType().FullName} Put:MTI[{ipass.Mti()}], Item RelationId[{relationId}]");
        }
    }
}

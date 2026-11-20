// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccQRCodeAutoSignOHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccQRCopeAutoSignOnHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers
{
    #region DEPENDENCY
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    #endregion

    public class NcccQRCodeAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                {
                    var nccc = NcccTransCreator.SignOnGWRequest();
                    if (!ObjectUtils.IsNull(nccc.RawVal(FIELD.F_48)))
                    {
                        nccc.Core().Field(FIELD.F_48, nccc.Core().Field(FIELD.F_48).ToString().Remove(10, 1).Insert(10, "N"));
                    }
                    ctx.Channel.WriteAndFlush(new PayloadContext(nccc));
                }
                else
                    this.Logger.Info($"NcccQRCodeAutoSignOnHandler ChannelActived(No Connected), LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
            }
        }
    }
}

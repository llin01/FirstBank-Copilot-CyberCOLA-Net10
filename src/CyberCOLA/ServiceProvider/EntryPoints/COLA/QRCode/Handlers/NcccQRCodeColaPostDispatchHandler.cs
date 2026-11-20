// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccQRCodeColaPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccQRCodeColaPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.QRCode.Handlers
{
    #region DEPENDENCY

    #endregion

    public class NcccQRCodeColaPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (NcccIso8583)payload.Val;
            base.Write(ctx, msg);
        }
    }
}

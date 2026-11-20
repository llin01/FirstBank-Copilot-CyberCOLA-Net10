// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinColaPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinColaPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.TWIN.Handlers
{
    #region DEPENDENCY

    #endregion

    public class TwinColaPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var twin = (TWINIso8583)payload.Val;
            base.Write(ctx, msg);
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ColaPostDispatchHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ColaPostDispatchHandler type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashColaPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (TM.ISO8583.ICASH.ICASH)payload.Val;
            base.Write(ctx, msg);
        }
    }
}

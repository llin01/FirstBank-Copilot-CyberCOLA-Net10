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

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsColaPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var nccc = (TM.ISO8583.SVCS.SVCS)payload.Val;
            base.Write(ctx, msg);
        }
    }
}

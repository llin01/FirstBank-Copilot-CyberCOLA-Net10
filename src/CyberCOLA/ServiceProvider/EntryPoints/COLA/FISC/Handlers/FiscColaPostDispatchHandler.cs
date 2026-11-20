// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscColaPostDispatchHandle.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscColaPostDispatchHandle type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.FISC.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class FiscColaPostDispatchHandler : ChannelOutboundHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            this.Logger.Debug($"Go Hander: {this.GetType().FullName}");
            var payload = (PayloadContext)msg;
            var fisc = (TM.FISC.FISC)payload.Val;
            base.Write(ctx, msg);
        }
    }
}

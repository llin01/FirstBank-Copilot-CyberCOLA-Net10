// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscAutoSignOHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscAutoSignOnHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.FISC;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.Handlers
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    #endregion

    public class FiscAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"FiscAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to FISC, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    //ctx.Channel.WriteAndFlush(new PayloadContext(new SignOnRequest().Prepare()));
                    ctx.Channel.WriteAndFlush(new PayloadContext(FISCTransCreator.FiscSignOnGWRequest()));
                }
                else
                    this.Logger.Info($"FiscAutoSignOnHandler ChannelActived(No Connected), LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
            }
        }
    }
}

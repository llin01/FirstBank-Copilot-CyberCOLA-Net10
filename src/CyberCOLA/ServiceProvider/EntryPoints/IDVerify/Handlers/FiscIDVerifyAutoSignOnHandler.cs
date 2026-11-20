// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscIDVerifyAutoSignOnHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscIDVerifyAutoSignOnHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.TM.FISC;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IDVerify.Handlers
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    #endregion

    public class FiscIDVerifyAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"FiscIDVerifyAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to FISC IDVerify, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    //ctx.Channel.WriteAndFlush(new PayloadContext(new SignOnRequest().Prepare()));
                    ctx.Channel.WriteAndFlush(new PayloadContext(FISCTransCreator.FiscSignOnGWRequest()));
                }
                else
                    this.Logger.Info($"FiscIDVerifyAutoSignOnHandler ChannelActived(No Connected), LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
            }
        }
    }
}

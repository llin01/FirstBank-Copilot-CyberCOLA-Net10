// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinAutoSignOnHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinAutoSignOnHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583.TWIN;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    #endregion

    public class TwinAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"TwinAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to TWIN, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    //ctx.Channel.WriteAndFlush(new PayloadContext(new SignOnRequest().Prepare()));
                    ctx.Channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOnGWRequest()));
                }
                else
                    this.Logger.Info($"TwinAutoSignOnHandler ChannelActived(No Connected), LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
            }
        }
    }
}

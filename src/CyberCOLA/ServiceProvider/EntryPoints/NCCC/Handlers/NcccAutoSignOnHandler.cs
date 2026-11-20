// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccAutoSignOHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccAutoSignOnHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC.Handlers
{
    #region DEPENDENCY
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;
    #endregion

    public class NcccAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"NcccAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to NCCC, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    //ctx.Channel.WriteAndFlush(new PayloadContext(new SignOnRequest().Prepare()));
                    ctx.Channel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOnGWRequest()));
                }
                else
                    this.Logger.Info($"NcccAutoSignOnHandler ChannelActived(No Connected), LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
            }
        }
    }
}

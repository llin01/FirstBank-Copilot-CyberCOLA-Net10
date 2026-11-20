// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ServerAutoSignOHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerAutoSignOnHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                //if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                if (!ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"SvcsAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to SVCS, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    ctx.Channel.WriteAndFlush(new PayloadContext(SvcsTransCreator.SvcsSignOnGWRequest()));
                }
                else
                    this.Logger.Info($" SvcsAutoSignOnHandler ChannelActived(No Closed), RemoteEndPoint[{ctx.Channel.RemoteEndPoint()}]");
            }
        }
    }
}

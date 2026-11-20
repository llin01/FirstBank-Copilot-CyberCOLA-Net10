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
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IpassAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                //if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                if (!ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"IpassAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to IPASS, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    ctx.Channel.WriteAndFlush(new PayloadContext(IpassTransCreator.IpassSignOnGWRequest()));
                }
                else
                    this.Logger.Info($" IpassAutoSignOnHandler ChannelActived(No Closed), RemoteEndPoint[{ctx.Channel.RemoteEndPoint()}]");
            }
        }
    }
}

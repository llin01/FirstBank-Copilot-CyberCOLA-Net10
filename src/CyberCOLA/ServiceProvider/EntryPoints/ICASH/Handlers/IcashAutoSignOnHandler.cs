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
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers
{
    #region DEPENDENCY

    #endregion

    public class IcashAutoSignOnHandler : ChannelInboundHandler
    {
        public override void ChannelActived(ChannelHandlerContext ctx)
        {
            if (ctx.Channel.Carrier<AutoSignOnKey, bool>())
            {
                //if (ctx.Channel.IsConnected() && !ctx.Channel.IsClosed())
                if (!ctx.Channel.IsClosed())
                {
                    this.Logger.Info($"IcashAutoSignOnHandler ChannelActived(Connected) Write SignOn Message to ICASH, LocalEndPoint[{ctx.Channel.LocalEndPoint()}]");
                    ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOnGWRequest()));
                }
                else
                    this.Logger.Info($" IcashAutoSignOnHandler ChannelActived(No Closed), RemoteEndPoint[{ctx.Channel.RemoteEndPoint()}]");
            }
        }
    }
}

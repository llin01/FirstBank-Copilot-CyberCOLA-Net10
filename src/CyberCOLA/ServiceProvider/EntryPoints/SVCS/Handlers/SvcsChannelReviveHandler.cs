// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file=" ServerChannelReviveHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ServerChannelReviveHandler type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using System;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS.Handlers
{
    #region DEPENDENCY

    #endregion

    public class SvcsChannelReviveHandler : ChannelInboundHandler
    {
        private readonly int interval;

        public SvcsChannelReviveHandler(int interval = 10000)
        {
            this.interval = interval;
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            try
            {
                if (ctx != null)
                {
                    base.ChannelUnRegistered(ctx);
                    if (ctx.Channel?.RemoteEndPoint() != null)
                        this.Logger.Info($"Remote Address {ctx.Channel.RemoteEndPoint()} Remove");
                    else
                        this.Logger.Info($"Remote Address invalid");
                }
                else
                {
                    this.Logger.Fatal($"ChannelUnRegistere of ctx data invalid");
                }
            }
            catch (Exception ex)
            {
                this.Logger.Fatal($"ChannelUnRegistere Exception: {ex.Message}");
            }
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinChannelReviveHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinChannelReviveHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.Resources;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Net;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class TwinChannelReviveHandler : ChannelInboundHandler
    {
        private readonly int interval;

        public TwinChannelReviveHandler(int interval = 10000)
        {
            this.interval = interval;
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            base.ChannelUnRegistered(ctx);
            var service = ctx.Channel.Carrier<ServiceKey, CyberGWTWIN>();
            var bootstrap = ctx.Channel.Carrier<BootstrapKey, Bootstrap>();
            var remoteEndPoint = ctx.Channel.Carrier<RemoteEndPointKey, IPEndPoint>();
            var localEndPoint = ctx.Channel.Carrier<LocalEndPointKey, IPEndPoint>();
            service.Remove(ctx.Channel);
            ctx.Channel.RemoveCarrier(remoteEndPoint);
            ctx.Channel.RemoveCarrier(localEndPoint);
            // A stopped gateway (resource cleared) is not revived: return now rather than after the interval,
            // which would hold up its Disconnect() for the whole interval.
            if (ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
            {
                return;
            }

            TimeUtils.Sleep(this.interval);
            if (ObjectUtils.IsNull(CyberGWTWINResource.CyberGWTWIN))
            {
                return;
            }

            if (!ctx.Channel.Carrier<DisconnectKey, bool>())
            {
                this.Logger.Fatal("Connection Disconnected without Permission, retrying...");
            }
            // Reconnect on this session's original endpoint pair when its local port is free (see GetFreeTwinEndPoint).
            var freeTwinEndPoint = service.GetFreeTwinEndPoint(localEndPoint);
            if (freeTwinEndPoint == null)
            {
                this.Logger.Debug("Get Free TwinEndPoint Is Null");
                return;
            }
            this.Logger.Debug($"Free TwinEndPoint Remote[{freeTwinEndPoint.RemoteIp}:{freeTwinEndPoint.RemotePort}], Local[{freeTwinEndPoint.LocalIp}:{freeTwinEndPoint.LocalPort}]");
            this.Logger.Debug($"ChannelUnRegistered Remote[{remoteEndPoint}],Local[{localEndPoint}]");
            //var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(freeTwinEndPoint.RemoteIp), freeTwinEndPoint.RemotePort);
            localEndPoint = new IPEndPoint(IPAddress.Parse(freeTwinEndPoint.LocalIp), freeTwinEndPoint.LocalPort);
            //var channel = bootstrap.Connect(remoteEndPoint, freeLocalEndPoint).Sync().Channel();
            var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            if (channel.IsConnected() && !channel.IsClosed())
            {
                channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                // Record the endpoint actually bound (it used to be a second, unrelated random pick).
                channel.Carrier<LocalEndPointKey>(localEndPoint);
                channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                channel.Carrier<TwinChannelInfoKey>(new ChannelSessionInfo(localEndPoint.ToString(), remoteEndPoint.ToString()));
                service.Release(channel);
            }
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscChannelReviveHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscChannelReviveHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.ServiceProvider.Resources;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Net;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class FiscChannelReviveHandler : ChannelInboundHandler
    {
        private readonly int interval;

        public FiscChannelReviveHandler(int interval = 10000)
        {
            this.interval = interval;
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            base.ChannelUnRegistered(ctx);
            var service = ctx.Channel.Carrier<ServiceKey, CyberGWFISC>();
            var bootstrap = ctx.Channel.Carrier<BootstrapKey, Bootstrap>();
            var remoteEndPoint = ctx.Channel.Carrier<RemoteEndPointKey, IPEndPoint>();
            var localEndPoint = ctx.Channel.Carrier<LocalEndPointKey, IPEndPoint>();
            service.Remove(ctx.Channel);
            ctx.Channel.RemoveCarrier(remoteEndPoint);
            ctx.Channel.RemoveCarrier(localEndPoint);
            // A stopped gateway (resource cleared) is not revived: return now rather than after the interval,
            // which would hold up its Disconnect() for the whole interval.
            if (ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
            {
                return;
            }

            TimeUtils.Sleep(this.interval);
            if (ObjectUtils.IsNull(CyberGWFISCResource.CyberGWFISC))
            {
                return;
            }

            if (!ctx.Channel.Carrier<DisconnectKey, bool>())
            {
                this.Logger.Fatal("Connection Disconnected without Permission, retrying...");
            }
            // Reconnect on this session's original endpoint pair when its local port is free (see GetFreeFiscEndPoint).
            var freeFiscEndPoint = service.GetFreeFiscEndPoint(localEndPoint);
            if (freeFiscEndPoint == null)
            {
                this.Logger.Debug("Get Free FiscEndPoint Is Null");
                return;
            }
            this.Logger.Debug($"Free FiscEndPoint Remote[{freeFiscEndPoint.RemoteIp}:{freeFiscEndPoint.RemotePort}], Local[{freeFiscEndPoint.LocalIp}:{freeFiscEndPoint.LocalPort}]");
            this.Logger.Debug($"ChannelUnRegistered Remote[{remoteEndPoint}],Local[{localEndPoint}]");
            //var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(freeFiscEndPoint.RemoteIp), freeFiscEndPoint.RemotePort);
            localEndPoint = new IPEndPoint(IPAddress.Parse(freeFiscEndPoint.LocalIp), freeFiscEndPoint.LocalPort);
            //var channel = bootstrap.Connect(remoteEndPoint, freeLocalEndPoint).Sync().Channel();
            var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            if (channel.IsConnected() && !channel.IsClosed())
            {
                channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                // Record the endpoint actually bound (it used to be a second, unrelated random pick).
                channel.Carrier<LocalEndPointKey>(localEndPoint);
                channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                channel.Carrier<FiscChannelInfoKey>(new ChannelSessionInfo(localEndPoint.ToString(), remoteEndPoint.ToString()));
                
                // Mark channel as revived and initialize its state with warm-up period
                var channelState = new ChannelStateInfo 
                { 
                    RevivalTime = DateTime.Now,
                    LastReleaseTime = DateTime.Now,
                    ReleaseCount = 0,
                    IsRevived = true
                };
                // Store state info for later verification
                channel.Carrier<ChannelStateKey>(channelState);
                
                this.Logger.Info($"Channel revived successfully: Remote[{remoteEndPoint}], Local[{localEndPoint}], entering 1s warm-up period");
                service.Release(channel);
                this.Logger.Info($"Channel released back to idle pool after revival");
            }
            else
            {
                this.Logger.Error($"Failed to establish revived channel: Connected={channel?.IsConnected()}, Closed={channel?.IsClosed()}");
            }
        }
    }
}

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

            // P0 Fix: Implement reconnection limit (max 3 attempts) with exponential backoff
            var reconnectAttempts = service.GetChannelReconnectAttempts().AddOrUpdate(ctx.Channel, 1, (key, oldValue) => oldValue + 1);
            
            if (reconnectAttempts > 3)
            {
                this.Logger.Fatal($"Channel[{localEndPoint}] exceeded maximum reconnection attempts ({reconnectAttempts}). Stopping reconnection.");
                service.GetChannelReconnectAttempts().TryRemove(ctx.Channel, out _);
                return;
            }

            // For first reconnection attempt, use the configured interval.
            // For subsequent retries (2+), apply exponential backoff: 5s, 10s, 30s
            int delayMs;
            if (reconnectAttempts == 1)
            {
                delayMs = this.interval;
            }
            else
            {
                delayMs = reconnectAttempts switch
                {
                    2 => 5000,   // 5 seconds for 2nd attempt
                    3 => 10000,  // 10 seconds for 3rd attempt
                    _ => 30000   // 30 seconds for beyond 3rd
                };
            }

            this.Logger.Info($"Channel[{localEndPoint}] reconnection attempt {reconnectAttempts}/3, waiting {delayMs}ms before retry");
            TimeUtils.Sleep(delayMs);

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
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(freeFiscEndPoint.RemoteIp), freeFiscEndPoint.RemotePort);
            localEndPoint = new IPEndPoint(IPAddress.Parse(freeFiscEndPoint.LocalIp), freeFiscEndPoint.LocalPort);
            var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            if (channel.IsConnected() && !channel.IsClosed())
            {
                channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                // Record the endpoint actually bound (it used to be a second, unrelated random pick).
                channel.Carrier<LocalEndPointKey>(localEndPoint);
                channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                channel.Carrier<FiscChannelInfoKey>(new ChannelSessionInfo(localEndPoint.ToString(), remoteEndPoint.ToString()));
                service.Release(channel);
                // Reset reconnection counter on successful connection
                service.GetChannelReconnectAttempts().TryRemove(ctx.Channel, out _);
                this.Logger.Info($"Connection established for channel[{channel.LocalEndPoint()}], reset reconnection counter");
            }
        }
    }
}

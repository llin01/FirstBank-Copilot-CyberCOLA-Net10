// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ChannelReviveHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ChannelReviveHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Net;
    using Com.Cybersoft4u.ServiceProvider.Resources;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class NcccChannelReviveHandler : ChannelInboundHandler
    {
        private readonly int interval;

        public NcccChannelReviveHandler(int interval = 10000)
        {
            this.interval = interval;
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            base.ChannelUnRegistered(ctx);
            var service = ctx.Channel.Carrier<ServiceKey, CyberGWNCCC>();
            var bootstrap = ctx.Channel.Carrier<BootstrapKey, Bootstrap>();
            var remoteEndPoint = ctx.Channel.Carrier<RemoteEndPointKey, IPEndPoint>();
            var localEndPoint = ctx.Channel.Carrier<LocalEndPointKey, IPEndPoint>();
            service.Remove(ctx.Channel);
            ctx.Channel.RemoveCarrier(remoteEndPoint);
            ctx.Channel.RemoveCarrier(localEndPoint);
            // A stopped gateway (resource cleared) is not revived: return now rather than after the interval,
            // which would hold up its Disconnect() for the whole interval.
            if (ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
            {
                return;
            }

            TimeUtils.Sleep(this.interval);
            if (ObjectUtils.IsNull(CyberGWNCCCResource.CyberGWNCCC))
            {
                return;
            }

            if (!ctx.Channel.Carrier<DisconnectKey, bool>())
            {
                this.Logger.Fatal("Connection Disconnected without Permission, retrying...");
            }
            // Reconnect on this session's original endpoint pair when its local port is free (see GetFreeNcccEndPoint).
            var freeNcccEndPoint = service.GetFreeNcccEndPoint(localEndPoint);
            if (freeNcccEndPoint == null)
            {
                this.Logger.Debug("Get Free NcccEndPoint Is Null");
                return;
            }
            this.Logger.Debug($"Free NcccEndPoint Remote[{freeNcccEndPoint.RemoteIp}:{freeNcccEndPoint.RemotePort}], Local[{freeNcccEndPoint.LocalIp}:{freeNcccEndPoint.LocalPort}]");
            this.Logger.Debug($"ChannelUnRegistered Remote[{remoteEndPoint}],Local[{localEndPoint}]");
            //var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(freeNcccEndPoint.RemoteIp), freeNcccEndPoint.RemotePort);
            localEndPoint = new IPEndPoint(IPAddress.Parse(freeNcccEndPoint.LocalIp), freeNcccEndPoint.LocalPort);
            //var channel = bootstrap.Connect(remoteEndPoint, freeLocalEndPoint).Sync().Channel();
            var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
            if (channel.IsConnected() && !channel.IsClosed())
            {
                channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                // Record the endpoint actually bound (it used to be a second, unrelated random pick).
                channel.Carrier<LocalEndPointKey>(localEndPoint);
                channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                channel.Carrier<NcccChannelInfoKey>(new ChannelSessionInfo(localEndPoint.ToString(), remoteEndPoint.ToString()));
                service.Release(channel);
            }
        }
    }
}

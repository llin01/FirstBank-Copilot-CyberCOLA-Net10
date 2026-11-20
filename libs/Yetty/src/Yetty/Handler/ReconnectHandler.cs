namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using System.Net;
    using System.Threading;

    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Context;

    #endregion

    public class ReconnectHandler : ChannelInboundHandler
    {
        private readonly Bootstrap bootstrap;

        private readonly EndPoint endPoint;

        private readonly int interval;

        public ReconnectHandler(Bootstrap bootstrap, EndPoint endPoint, int interval = 5000)
        {
            this.bootstrap = bootstrap;
            this.endPoint = endPoint;
            this.interval = interval;
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            Thread.Sleep(this.interval);
            this.bootstrap.Connect(this.endPoint);
        }
    }
}

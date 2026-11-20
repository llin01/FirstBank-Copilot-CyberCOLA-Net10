// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="InboundMultiWorkerHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the InboundMultiWorkerHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers
{
    #region DEPENDENCY

    using System;
    using System.Threading;

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class InboundMultiWorkerHandler : ChannelInboundHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            ThreadPool.QueueUserWorkItem(
                state =>
                    {
                        try
                        {
                            base.ChannelRead(ctx, msg);
                        }
                        catch (Exception exception)
                        {
                            this.Logger.Fatal(exception);
                        }
                    });
        }

        public override void ChannelReadCompleted(ChannelHandlerContext ctx)
        {
            ThreadPool.QueueUserWorkItem(
                state =>
                    {
                        try
                        {
                            base.ChannelReadCompleted(ctx);
                        }
                        catch (Exception exception)
                        {
                            this.Logger.Fatal(exception);
                        }
                    });
        }
    }
}

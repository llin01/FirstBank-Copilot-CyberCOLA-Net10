// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnWallHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnWallHandler type.
//   Block all read or write operation if channel isn't sign on.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class SignOnWallHandler : ChannelDuplexHandler
    {
        public override void Write(ChannelHandlerContext ctx, object msg)
        {
            var message = ((PayloadContext)msg).Val;

            if (message is SignOn || ctx.Channel.Carrier<SignOnKey, bool>())
            {
                base.Write(ctx, msg);
            }
            else
            {
                this.Logger.Warn("No Permission, StinSignOn required!");
            }
        }

        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            var message = ((PayloadContext)msg).Val;

            if (message is SignOn || ctx.Channel.Carrier<SignOnKey, bool>())
            {
                base.ChannelRead(ctx, msg);
            }
            else
            {
                this.Logger.Warn("No Permission, StinSignOn required!");
            }
        }
    }
}

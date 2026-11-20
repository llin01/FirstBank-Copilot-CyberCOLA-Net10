// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnOffHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnOffHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Action;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583.TWIN;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class SignOnOffHandler : ChannelDuplexHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            var twin = (TWINIso8583)((PayloadContext)msg).Val;
            if (twin is SignOn && twin is Response)
            {
                if ("00".Equals(twin.Val(FIELD.F_39)))
                {
                    ctx.Channel.Carrier<SignOnKey>(true);
                }
            }
            else if (twin is SignOff && twin is Response)
            {
                if ("00".Equals(twin.Val(FIELD.F_39)))
                {
                    ctx.Channel.Carrier<SignOnKey>(false);
                }
            }
            else
            {
                base.ChannelRead(ctx, msg);
            }
        }

        public override void ChannelUnRegistered(ChannelHandlerContext ctx)
        {
            ctx.Channel.Carrier<SignOnKey>(false);
            base.ChannelUnRegistered(ctx);
        }
    }
}

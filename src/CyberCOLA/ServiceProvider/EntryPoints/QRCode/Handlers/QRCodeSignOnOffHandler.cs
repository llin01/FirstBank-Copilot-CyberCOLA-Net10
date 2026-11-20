// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="QRCodeSignOnOffHandler.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the QRCodeSignOnOffHandler type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Action;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    public class QRCodeSignOnOffHandler : ChannelDuplexHandler
    {
        public override void ChannelRead(ChannelHandlerContext ctx, object msg)
        {
            var nccc = (NcccIso8583)((PayloadContext)msg).Val;
            if (nccc is SignOn && nccc is Response)
            {
                if ("00".Equals(nccc.Val(FIELD.F_39)))
                {
                    ctx.Channel.Carrier<SignOnKey>(true);
                }
            }
            else if (nccc is SignOff && nccc is Response)
            {
                if ("00".Equals(nccc.Val(FIELD.F_39)))
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

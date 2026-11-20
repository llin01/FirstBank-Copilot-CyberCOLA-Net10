namespace Com.Cybersoft4u.Yetty.Handler
{
    #region DEPENDENCY

    using Com.Cybersoft4u.Yetty.Context;
    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    public class TransactionHandler : ChannelDuplexHandler
    {
        protected static readonly Cache Cache = Cache.Instance();

        protected PayloadContext Payload(object msg)
        {
            return msg as PayloadContext;
        }
    }
}

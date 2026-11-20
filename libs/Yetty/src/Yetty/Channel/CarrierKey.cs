namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    public class CarrierKey : Key
    {
        public static new TCarrierKey Get<TCarrierKey>() where TCarrierKey : CarrierKey
        {
            return Key.Get<TCarrierKey>();
        }
    }
}

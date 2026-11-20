namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SE43 : MasterCardDE48SE
    {
        public MasterCardDE48SE43()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.Value;
        }

        public override MasterCardDE48SE ValueDecode()
        {
            return this;
        }
    }
}

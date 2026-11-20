namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SE77 : MasterCardDE48SE
    {
        public MasterCardDE48SE77()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.FundingPaymentTransactionTypeIndicator)
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

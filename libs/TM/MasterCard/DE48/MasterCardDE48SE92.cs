namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SE92 : MasterCardDE48SE
    {

        public string Cvc2 = null;

        public MasterCardDE48SE92()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.Cvc2TagId)
        {
        }

        public override string ValueEncode()
        {
            return this.Cvc2;
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.Cvc2 = this.Value;
            return this;
        }
    }
}

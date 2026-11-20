namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SE87 : MasterCardDE48SE
    {

        public string Cvv2Response = null;

        public MasterCardDE48SE87()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResultTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.Cvv2Response;
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.Cvv2Response = this.Value;
            return this;
        }
    }
}

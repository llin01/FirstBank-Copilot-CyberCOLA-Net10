namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    #endregion

    public class FISCDE48SE87 : FISCDE48SE
    {

        public string Cvv2Response = null;

        public FISCDE48SE87()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.CardValidationCodeResultTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.Cvv2Response;
        }

        public override FISCDE48SE ValueDecode()
        {
            this.Cvv2Response = this.Value;
            return this;
        }
    }
}

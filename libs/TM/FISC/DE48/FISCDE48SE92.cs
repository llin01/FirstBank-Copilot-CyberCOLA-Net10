namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY


    #endregion

    public class FISCDE48SE92 : FISCDE48SE
    {

        public string Cvc2 = null;

        public FISCDE48SE92()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.Cvc2TagId)
        {
        }

        public override string ValueEncode()
        {
            return this.Cvc2;
        }

        public override FISCDE48SE ValueDecode()
        {
            this.Cvc2 = this.Value;
            return this;
        }
    }
}

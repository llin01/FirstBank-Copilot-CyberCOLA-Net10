namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    #endregion

    public class FISCDE48SE43 : FISCDE48SE
    {
        public FISCDE48SE43()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.UniversalCardholderAuthenticationFieldTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.Value;
        }

        public override FISCDE48SE ValueDecode()
        {
            return this;
        }
    }
}

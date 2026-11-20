namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    #endregion

    public class FISCDE48SE66 : FISCDE48SE
    {
        public FISCDE48SE66()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.FiscDE48TagI66)
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

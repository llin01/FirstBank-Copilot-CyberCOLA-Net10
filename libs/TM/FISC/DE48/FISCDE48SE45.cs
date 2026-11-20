namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    #endregion

    public class FISCDE48SE45 : FISCDE48SE
    {

        public string CAVVResultCode = null;

        public FISCDE48SE45()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.CAVVResultCodeTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.CAVVResultCode;
        }

        public override FISCDE48SE ValueDecode()
        {
            this.CAVVResultCode = this.Value;
            return this;
        }
    }
}

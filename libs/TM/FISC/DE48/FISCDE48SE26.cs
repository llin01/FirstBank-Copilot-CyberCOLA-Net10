namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    #endregion

    public class FISCDE48SE26 : FISCDE48SE
    {

        public string WalletIdentifier = null;

        public FISCDE48SE26()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.WalletProgramDataTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.WalletIdentifier;
        }

        public override FISCDE48SE ValueDecode()
        {
            this.WalletIdentifier = this.Value;
            return this;
        }
    }
}

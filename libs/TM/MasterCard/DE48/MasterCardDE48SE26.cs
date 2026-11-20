namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SE26 : MasterCardDE48SE
    {

        public string WalletIdentifier = null;

        public MasterCardDE48SE26()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.WalletProgramDataTagId)
        {
        }

        public override string ValueEncode()
        {
            return this.WalletIdentifier;
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.WalletIdentifier = this.Value;
            return this;
        }
    }
}

namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SE63 : MasterCardDE48SE
    {
        public MasterCardDE48SE63()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.TraceId)
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

namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class MasterCardDE48SE32 : MasterCardDE48SE
    {
        //// Subfields
        public string MastercardAssignedId = null;

        public MasterCardDE48SE32() : base(MasterCardTransactionConst.AdditionalDataPrivateUse.MastercardAssignedTagId)
        {
        }

        public override String ValueEncode()
        {
            return this.MastercardAssignedId;
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.MastercardAssignedId = this.Value.IndexSubstring(0, 6);

            return this;
        }
    }
}

namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class MasterCardDE48SE61 : MasterCardDE48SE
    {
        //// Subfields
        public string PartialApprovalTerminalSupportIndicator = null;
        public string PurchaseAmountOnlyTerminalSupportIndicator = null;
        public string RealtimeSubstantiationIndicator = null;
        public string MerchantTransactionFraudScoringIndicator = null;
        public string FinalAuthorizationIndicator = null;

        public MasterCardDE48SE61() : base(MasterCardTransactionConst.AdditionalDataPrivateUse.PosDataExtendedConditionCodesTagId)
        {
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.PartialApprovalTerminalSupportIndicator);
            stringBuilder.Append(this.PurchaseAmountOnlyTerminalSupportIndicator);
            stringBuilder.Append(this.RealtimeSubstantiationIndicator);
            stringBuilder.Append(this.MerchantTransactionFraudScoringIndicator);
            stringBuilder.Append(this.FinalAuthorizationIndicator);

            return stringBuilder.ToString();
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.PartialApprovalTerminalSupportIndicator = this.Value.IndexSubstring(0, 1);
            this.PurchaseAmountOnlyTerminalSupportIndicator = this.Value.IndexSubstring(1, 2);
            this.RealtimeSubstantiationIndicator = this.Value.IndexSubstring(2, 3);
            this.MerchantTransactionFraudScoringIndicator = this.Value.IndexSubstring(3, 4);
            this.FinalAuthorizationIndicator = this.Value.IndexSubstring(4, 5);

            return this;
        }
    }
}

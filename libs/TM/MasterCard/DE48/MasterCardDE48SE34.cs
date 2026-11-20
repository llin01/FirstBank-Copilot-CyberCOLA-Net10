namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;
    using System.Text;

    #endregion

    public class MasterCardDE48SE34 : MasterCardDE48SE
    {
        //// Subfields
        public string ATC = null;
        public string ATCDiscrepancyValue = null;
        public string ATCDiscrepancyIndicator = null;

        public MasterCardDE48SE34() : base(MasterCardTransactionConst.AdditionalDataPrivateUse.ATCInformation)
        {
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ATC);
            stringBuilder.Append(this.ATCDiscrepancyValue);
            stringBuilder.Append(this.ATCDiscrepancyIndicator);

            return stringBuilder.ToString();
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.ATC = this.Value.Substring(0, 5);
            this.ATCDiscrepancyValue = this.Value.Substring(5, 5);
            this.ATCDiscrepancyIndicator = this.Value.Substring(10, 1);

            return this;
        }
    }
}

namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class MasterCardDE48SE11Double : MasterCardDE48SE
    {
        //// Subfields
        public string KeyClassId = null;
        public string KeyIndexNumber = null;
        public string KeyCycleNumber = null;
        public string PinEncryptionKey = null;
        public string KeyCheckValue = null;

        public MasterCardDE48SE11Double()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.KeyExchangeBlockData)
        {
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.KeyClassId);
            stringBuilder.Append(this.KeyIndexNumber);
            stringBuilder.Append(this.KeyCycleNumber);
            stringBuilder.Append(this.PinEncryptionKey);
            stringBuilder.Append(this.KeyCheckValue);

            return stringBuilder.ToString();
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.KeyClassId = this.Value.IndexSubstring(0, 2);
            this.KeyIndexNumber = this.Value.IndexSubstring(2, 4);
            this.KeyCycleNumber = this.Value.IndexSubstring(4, 6);
            this.PinEncryptionKey = this.Value.IndexSubstring(6, 38);
            this.KeyCheckValue = this.Value.IndexSubstring(38, 54);

            return this;
        }
    }
}

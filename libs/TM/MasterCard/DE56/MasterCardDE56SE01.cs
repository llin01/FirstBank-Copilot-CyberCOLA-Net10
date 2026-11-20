namespace Com.Cybersoft4u.TM.MasterCard.DE56
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class MasterCardDE56SE01 : MasterCardDE56SE
    {
        //// Subfields
        public MasterCardDE56SESF PAR = new MasterCardDE56SESF("01", 29, DEType.LL);

        //// Position
        public string BINControllerIdentifier = null;
        public string BINControllerRandomNO = null;

        public MasterCardDE56SE01()
            : base(MasterCardTransactionConst.PaymentAccountData.PaymentAccountReference.PARId)
        {
            this.Subfields[PAR.Id] = PAR;
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.BINControllerIdentifier);
            stringBuilder.Append(this.BINControllerRandomNO);
            this.PAR.Value = stringBuilder.ToString();

            return base.ValueEncode();
        }

        public override MasterCardDE56SE ValueDecode()
        {
            base.ValueDecode();

            this.BINControllerIdentifier = this.PAR.Value.IndexSubstring(0, 4);
            this.BINControllerRandomNO = this.PAR.Value.IndexSubstring(4, 29);

            return this;
        }
    }
}

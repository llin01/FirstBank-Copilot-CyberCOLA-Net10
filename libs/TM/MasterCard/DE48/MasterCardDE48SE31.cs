namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;
    using System.Text;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class MasterCardDE48SE31 : MasterCardDE48SE
    {
        //// Subfields
        public MasterCardDE48SESF OriginalProcessingCode = new MasterCardDE48SESF("01", 2, DEType.LL);
        public MasterCardDE48SESF OriginalMessageTypeIdentifier = new MasterCardDE48SESF("02", 4, DEType.LL);

        public MasterCardDE48SE31()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.OriginalTransactionDataTagId)
        {
            this.Subfields[OriginalProcessingCode.Id] = OriginalProcessingCode;
            this.Subfields[OriginalMessageTypeIdentifier.Id] = OriginalMessageTypeIdentifier;
        }
        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.OriginalProcessingCode);
            stringBuilder.Append(this.OriginalMessageTypeIdentifier);

            this.Value = stringBuilder.ToString();
            return base.ValueEncode();
        }

        //public override MasterCardDE48SE ValueDecode()
        //{
        //    base.ValueDecode();

        //    return this;
        //}

    }
}

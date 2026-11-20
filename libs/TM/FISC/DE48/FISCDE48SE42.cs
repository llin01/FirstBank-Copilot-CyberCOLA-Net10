namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class FISCDE48SE42 : FISCDE48SE
    {
        //// Subfields
        public FISCDE48SESF ElectronicCommerceIndicators = new FISCDE48SESF("01", 3, DEType.LL);
        public FISCDE48SESF ElectronicCommerceIndicators2 = new FISCDE48SESF("02", 3, DEType.LL);
        public FISCDE48SESF ReasonUCAFCollectionIndDowngrade = new FISCDE48SESF("03", 1, DEType.LL);

        //// Position
        public string SecurityLevelIndicator = null;
        public string UcafCollectionIndicator = null;
        public string SecurityLevelIndicator2 = null;
        public string UcafCollectionIndicator2 = null;
        public string ReasonUCAFCollectionIndDowngradeValues = null;

        public FISCDE48SE42()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceIndicatorsTagId)
        {
            this.Subfields[ElectronicCommerceIndicators.Id] = ElectronicCommerceIndicators;
            this.Subfields[ElectronicCommerceIndicators2.Id] = ElectronicCommerceIndicators2;
            this.Subfields[ReasonUCAFCollectionIndDowngrade.Id] = ReasonUCAFCollectionIndDowngrade;
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.SecurityLevelIndicator);
            stringBuilder.Append(this.UcafCollectionIndicator);
            this.ElectronicCommerceIndicators.Value = stringBuilder.ToString();

            var stringBuilder2 = new StringBuilder();

            stringBuilder2.Append(this.SecurityLevelIndicator2);
            stringBuilder2.Append(this.UcafCollectionIndicator2);
            this.ElectronicCommerceIndicators2.Value = stringBuilder2.ToString();

            var stringBuilder3 = new StringBuilder();

            stringBuilder3.Append(this.ReasonUCAFCollectionIndDowngradeValues);
            this.ReasonUCAFCollectionIndDowngrade.Value = stringBuilder3.ToString();

            var stringBuilder_encode = new StringBuilder();

            foreach (var pair in this.Subfields)
            {
                var sf = pair.Value;

                if (sf.Value == "")
                {
                    continue;
                }

                stringBuilder_encode.Append(sf.Encode());
            }

            return stringBuilder_encode.ToString();

            //return base.ValueEncode();
        }

        public override FISCDE48SE ValueDecode()
        {
            base.ValueDecode();

            this.SecurityLevelIndicator = this.ElectronicCommerceIndicators.Value.IndexSubstring(0, 2);
            this.UcafCollectionIndicator = this.ElectronicCommerceIndicators.Value.IndexSubstring(2, 3);
            if (this.ElectronicCommerceIndicators2.Value != null)
            {
                this.SecurityLevelIndicator2 = this.ElectronicCommerceIndicators2.Value.IndexSubstring(0, 2);
                this.UcafCollectionIndicator2 = this.ElectronicCommerceIndicators2.Value.IndexSubstring(2, 3);
            }
            if (this.ReasonUCAFCollectionIndDowngrade.Value != null)
            {
                this.ReasonUCAFCollectionIndDowngradeValues = this.ReasonUCAFCollectionIndDowngrade.Value.IndexSubstring(0, 1);
            }

            return this;
        }
    }
}

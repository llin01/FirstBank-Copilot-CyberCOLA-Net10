namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class AdditionalProcessingInformation
    {
        //// Subfields
        public string ProcessingIndicator = null;
        public string ProcessingInformation = null;

        public const int Length = 3;
    }

    public class FISCDE48SE74 : FISCDE48SE
    {
        public List<AdditionalProcessingInformation> infos = new List<AdditionalProcessingInformation>();

        public FISCDE48SE74()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.AdditionalProcessingInformationTagId)
        {
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            foreach (var additionalProcessingInformation in this.infos)
            {
                stringBuilder.Append(additionalProcessingInformation.ProcessingIndicator);
                stringBuilder.Append(additionalProcessingInformation.ProcessingInformation);
            }

            return stringBuilder.ToString();
        }

        public override FISCDE48SE ValueDecode()
        {
            this.infos = new List<AdditionalProcessingInformation>();
            var val = this.Value.Copy();

            while (val.Length > 0)
            {
                var v = val.IndexSubstring(0, AdditionalProcessingInformation.Length);
                var additionalProcessingInformation = new AdditionalProcessingInformation();

                additionalProcessingInformation.ProcessingIndicator = v.IndexSubstring(0, 2);
                additionalProcessingInformation.ProcessingInformation = v.IndexSubstring(2, 3);

                this.infos.Add(additionalProcessingInformation);

                val = val.Substring(AdditionalProcessingInformation.Length);
            }

            return this;
        }
    }
}

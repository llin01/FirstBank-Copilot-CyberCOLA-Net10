namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class ChipCvrTvrBitErrorResult
    {
        //// Subfields
        public string CvrTvrIdentifier = null;
        public string ByteId = null;
        public string ByteIdentifier = null;
        public string ValueOfBitInError = null;

        public const int Length = 5;
    }

    public class MasterCardDE48SE79 : MasterCardDE48SE
    {
        public List<ChipCvrTvrBitErrorResult> results = new List<ChipCvrTvrBitErrorResult>();

        public MasterCardDE48SE79()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.ChipCvrTvrBitErrorResultTagId)
        {
        }

        public override String ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            foreach (var chipCvrTvrBitErrorResult in this.results)
            {
                stringBuilder.Append(chipCvrTvrBitErrorResult.CvrTvrIdentifier);
                stringBuilder.Append(chipCvrTvrBitErrorResult.ByteId);
                stringBuilder.Append(chipCvrTvrBitErrorResult.ByteIdentifier);
                stringBuilder.Append(chipCvrTvrBitErrorResult.ValueOfBitInError);
            }

            return stringBuilder.ToString();
        }

        public override MasterCardDE48SE ValueDecode()
        {
            this.results = new List<ChipCvrTvrBitErrorResult>();
            var val = this.Value.Copy();

            while (val.Length > 0)
            {
                var v = val.IndexSubstring(0, ChipCvrTvrBitErrorResult.Length);
                var chipCvrTvrBitErrorResult = new ChipCvrTvrBitErrorResult();

                chipCvrTvrBitErrorResult.CvrTvrIdentifier = v.IndexSubstring(0, 1);
                chipCvrTvrBitErrorResult.ByteId = v.IndexSubstring(1, 3);
                chipCvrTvrBitErrorResult.ByteIdentifier = v.IndexSubstring(3, 4);
                chipCvrTvrBitErrorResult.ValueOfBitInError = v.IndexSubstring(4, 5);

                this.results.Add(chipCvrTvrBitErrorResult);

                val = val.Substring(ChipCvrTvrBitErrorResult.Length);
            }

            return this;
        }
    }
}

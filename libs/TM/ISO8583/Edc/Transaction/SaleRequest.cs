namespace Com.Cybersoft4u.TM.ISO8583.Edc.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0200", "000000")]
    public class SaleRequest : EdcIso8583
    {
        internal SaleRequest()
        {
        }

        internal SaleRequest(byte[] message)
            : base(message)
        {
        }

        public override Iso8583 Prepare()
        {
            try
            {
                this.PurgeFields();
                this.ConstructProcess();
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                this.Val(FIELD.F_24, this.Core().NetworkInternationalIdentifier);
                this.Val(FIELD.F_25, this.Core().PointOfServiceConditionMode);
                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_35, this.Track2Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);

                //// Val(FIELD.F_38, Core().ApproveCode);
                this.Val(FIELD.F_41, this.Core().TerminalId);
                this.Val(FIELD.F_42, this.Core().MerchantId.PadRight(15, ' '));

                //// Val(FIELD.F_48, F48Encode());
                //// Val(FIELD.F_52, Core().Pinblock);
                this.Val(FIELD.F_55, this.F55EmvChipEncode());

                //// Val(FIELD.F_57, F57TSAMEncode());
                this.Val(FIELD.F_59, this.F59TableEncode());
                this.Val(FIELD.F_60, (string)this.Core().GetOthersValByKey("F_60"));
                this.Val(FIELD.F_62, (string)this.Core().GetOthersValByKey("F_62"));
                this.Val(FIELD.F_64, this.Core().MessageAuthenticationCode);
            }
            catch (Exception e)
            {
                // TODO
                // Log.e(t);
                throw new TransactionPrepareException(e);
            }

            return base.Prepare();
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti(EdcTransactionConst.MTI.SaleRequest);
            this.Core().ProcessingCode = EdcTransactionConst.ProcessingCode.Sale;
        }
    }
}

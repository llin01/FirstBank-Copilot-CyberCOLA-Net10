namespace Com.Cybersoft4u.TM.ISO8583.Edc.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0220", "250000")]
    public class TcUploadRequest : EdcIso8583
    {
        internal TcUploadRequest()
        {
        }

        internal TcUploadRequest(byte[] message)
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
                this.Val(FIELD.F_12, this.Core().LocalTransactionTime);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);
                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                this.Val(FIELD.F_24, this.Core().NetworkInternationalIdentifier);
                this.Val(FIELD.F_25, this.Core().PointOfServiceConditionMode);
                this.Val(FIELD.F_35, this.Track2Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_41, this.Core().TerminalId);
                this.Val(FIELD.F_42, this.Core().MerchantId.PadRight(15, ' '));

                //// Val(FIELD.F_48, F48Encode());
                //// Val(FIELD.F_52, Core().Pinblock);
                this.Val(FIELD.F_55, this.F55EmvChipEncode());

                //// Val(FIELD.F_57, F57TSAMEncode());
                //// Val(FIELD.F_59, F59TableEncode());
                this.Val(FIELD.F_60, (string)this.Core().GetOthersValByKey("F_60"));
                this.Val(FIELD.F_62, (string)this.Core().GetOthersValByKey("F_62"));

                //// Val(FIELD.F_64, Core().MessageAuthenticationCode);
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
            this.Mti(EdcTransactionConst.MTI.TcUploadRequest);
            this.Core().ProcessingCode = EdcTransactionConst.ProcessingCode.TcUpload;
        }
    }
}

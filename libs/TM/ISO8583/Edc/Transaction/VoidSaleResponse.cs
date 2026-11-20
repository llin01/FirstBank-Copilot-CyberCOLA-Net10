namespace Com.Cybersoft4u.TM.ISO8583.Edc.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0210", "020000")]
    public class VoidSaleResponse : EdcIso8583
    {
        public VoidSaleResponse()
        {
        }

        public VoidSaleResponse(byte[] message)
            : base(message)
        {
        }

        public override Iso8583 Prepare()
        {
            try
            {
                this.PurgeFields();
                this.ConstructProcess();
                this.Core().NetworkInternationalIdentifier = "0297";
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_12, this.Core().LocalTransactionTime);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);
                this.Val(FIELD.F_24, this.Core().NetworkInternationalIdentifier);
                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_41, this.Core().TerminalId);

                //// Val(FIELD.F_59, F59TableEncode());
                //// Val(FIELD.F_60, Core().GetOthersValByKey("F_60"));
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
            this.Mti(EdcTransactionConst.MTI.VoidSaleResponse);
            this.Core().ProcessingCode = EdcTransactionConst.ProcessingCode.VoidSale;
        }
    }
}

namespace Com.Cybersoft4u.TM.ISO8583.Edc.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0210", "000000")]
    public class SaleResponse : EdcIso8583
    {
        internal SaleResponse()
        {
        }

        internal SaleResponse(byte[] message)
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
                if (this.Core().ApproveCode != null)
                {
                    this.Val(FIELD.F_38, this.Core().ApproveCode);
                }

                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_41, this.Core().TerminalId);
                this.Val(FIELD.F_55, this.F55EmvChipEncode());
                this.Val(FIELD.F_59, this.F59TableEncode());
                this.Val(FIELD.F_60, (string)this.Core().GetOthersValByKey("F_60"));
            }
            catch (Exception e)
            {
                throw new TransactionPrepareException(e);
            }

            return base.Prepare();
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti(EdcTransactionConst.MTI.SaleResponse);
            this.Core().ProcessingCode = EdcTransactionConst.ProcessingCode.Sale;
        }
    }
}

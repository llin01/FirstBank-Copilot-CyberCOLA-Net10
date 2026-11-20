namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class SaleResponse : MCOLAResponse, Sale
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

                if (this.Core().NullSecondBitmap)
                {
                    this.Val(FIELD.F_1, "0000000000000000");
                }

                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);

                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_12, this.Core().LocalTransactionTime);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);
                this.Val(FIELD.F_15, this.Core().SettlementDate);

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_35, this.Track2Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                if (!string.IsNullOrEmpty(this.Core().TerminalId))
                {
                    var len = this.Spec(FIELD.F_41).MaxLength();
                    this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(len, ' '));
                }

                this.Val(FIELD.F_49, "901");

                this.Val(FIELD.F_54, this.F54Encode());

                this.Val(FIELD.F_63, this.F63TokenEncode());
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
            this.Mti(MCOLATransactionConst.MTI.SaleResponse);
        }
    }
}

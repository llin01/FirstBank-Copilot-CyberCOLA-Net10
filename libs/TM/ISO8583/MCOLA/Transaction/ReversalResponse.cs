namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class ReversalResponse : MCOLAResponse, Reversal
    {
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

                this.Val(FIELD.F_2, this.Core().Pan);
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_5, this.F5Encode());
                this.Val(FIELD.F_6, this.F6Encode());
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_9, this.F9Encode());

                this.Val(FIELD.F_10, this.F10Encode());
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_15, this.Core().SettlementDate);
                this.Val(FIELD.F_16, this.F16Encode());

                this.Val(FIELD.F_20, this.F20Encode());
                this.Val(FIELD.F_28, this.Core().TransactionFeeAmount);

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_33, this.F33Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                if (!string.IsNullOrEmpty(this.Core().TerminalId))
                {
                    var len = this.Spec(FIELD.F_41).MaxLength();
                    this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(len, ' '));
                }

                this.Val(FIELD.F_48, this.F48Encode());
                this.Val(FIELD.F_49, this.Core().TransactionCurrencyCode);

                this.Val(FIELD.F_50, this.F50Encode());
                this.Val(FIELD.F_51, this.F51Encode());
                this.Val(FIELD.F_54, this.F54Encode());

                this.Val(FIELD.F_63, this.F63TokenEncode());

                this.Val(FIELD.F_90, this.F90OriginalDataElementsEncode());
                this.Val(FIELD.F_95, this.Core().ReplacementAmount);

                this.Val(FIELD.F_124, this.F124Encode());
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
            this.Mti(MCOLATransactionConst.MTI.ReversalResponse);
        }
    }
}

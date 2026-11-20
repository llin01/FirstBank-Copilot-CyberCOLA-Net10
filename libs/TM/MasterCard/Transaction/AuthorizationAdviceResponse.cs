namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class AuthorizationAdviceResponse : MasterCardResponse, Advice
    {
        internal AuthorizationAdviceResponse()
        {
        }

        internal AuthorizationAdviceResponse(byte[] message)
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

                this.Val(FIELD.F_2, this.Core().Pan);
                this.Val(FIELD.F_3, this.F3ProcessingCodeEncode());
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_5, this.F5Encode());
                this.Val(FIELD.F_6, this.ExtendedCore().CardholderBillingAmount);
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_9, this.F9Encode());

                this.Val(FIELD.F_10, this.ExtendedCore().CardholderBillingConversionRate);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_15, this.Core().SettlementDate);
                this.Val(FIELD.F_16, this.ExtendedCore().ConversionDate);

                this.Val(FIELD.F_20, this.F20Encode());
                this.Val(FIELD.F_23, this.ExtendedCore().CardSequenceNumber);
                this.Val(FIELD.F_28, this.F28Encode());

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_33, this.ExtendedCore().ForwardingInstitutionIdCode);
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                if (!string.IsNullOrEmpty(this.Core().TerminalId))
                {
                    var len = this.Spec(FIELD.F_41).MaxLength();
                    this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(len, ' '));
                }

                this.Val(FIELD.F_44, this.F44Encode());
                //this.Val(FIELD.F_48, this.F48AdditionalDataPrivateUseEncode());
                this.Val(FIELD.F_49, this.Core().TransactionCurrencyCode);

                this.Val(FIELD.F_50, this.F50Encode());
                this.Val(FIELD.F_51, this.ExtendedCore().CardholderBillingCurrencyCode);
                this.Val(FIELD.F_54, this.F54Encode());

                this.Val(FIELD.F_62, this.F62Encode());
                this.Val(FIELD.F_63, this.F63NetworkDataEncode());

                this.Val(FIELD.F_127, this.F127Encode());
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
            this.Mti(MasterCardTransactionConst.MTI.AuthorizationAdviceResponse);
            this.F3ProcessingCodeDecode(MasterCardTransactionConst.ProcessingCode.Authorization);
        }
    }
}

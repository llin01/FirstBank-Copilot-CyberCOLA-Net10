namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class ReversalRequest : MasterCardRequest, Reversal
    {
        internal ReversalRequest()
        {
        }

        internal ReversalRequest(byte[] message)
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
                this.Val(FIELD.F_12, this.Core().LocalTransactionTime);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);
                this.Val(FIELD.F_14, this.Core().ExpireDate);
                this.Val(FIELD.F_18, this.Core().MerchantCategoryCode);

                this.Val(FIELD.F_20, this.F20Encode());
                this.Val(FIELD.F_22, this.F22PointOfServiceEntryModeEncode());
                this.Val(FIELD.F_23, this.ExtendedCore().CardSequenceNumber);
                this.Val(FIELD.F_28, this.F28Encode());

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_33, this.ExtendedCore().ForwardingInstitutionIdCode);
                this.Val(FIELD.F_35, this.F35Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                if (!string.IsNullOrEmpty(this.Core().TerminalId))
                {
                    var len = this.Spec(FIELD.F_41).MaxLength();
                    this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(len, ' '));
                }

                if (!string.IsNullOrEmpty(this.Core().MerchantId))
                {
                    var len = this.Spec(FIELD.F_42).MaxLength();
                    this.Val(FIELD.F_42, this.Core().MerchantId.PadRight(len, ' '));
                }

                this.Val(FIELD.F_43, this.F43CardAcceptorNameLocationEncode());
                this.Val(FIELD.F_45, this.F45Encode());
                this.Val(FIELD.F_48, this.F48AdditionalDataPrivateUseEncode());
                this.Val(FIELD.F_49, this.Core().TransactionCurrencyCode);

                this.Val(FIELD.F_51, this.ExtendedCore().CardholderBillingCurrencyCode);
                this.Val(FIELD.F_52, this.Core().Pinblock);
                this.Val(FIELD.F_54, this.F54Encode());
                this.Val(FIELD.F_55, this.F55EmvChipEncode());

                this.Val(FIELD.F_60, this.F60AdviceReasonCodeEncode());
                this.Val(FIELD.F_61, this.F61PointOfServiceDataEncode());

                this.Val(FIELD.F_90, this.F90OriginalDataElementsEncode());
                this.Val(FIELD.F_95, this.Core().ReplacementAmount);

                this.Val(FIELD.F_124, this.F124MemberDefinedDataEncode());
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
            this.Mti(MasterCardTransactionConst.MTI.ReversalRequest);
            this.F3ProcessingCodeDecode(MasterCardTransactionConst.ProcessingCode.Authorization);
        }
    }
}

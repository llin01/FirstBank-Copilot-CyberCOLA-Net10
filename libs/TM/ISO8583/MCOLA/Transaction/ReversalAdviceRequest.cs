namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class ReversalAdviceRequest : MCOLARequest, ReversalAdvice
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

                //this.Val(FIELD.F_2, this.Core().Pan);
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_5, this.F5Encode());
                this.Val(FIELD.F_6, this.F6Encode());
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_9, this.F9Encode());

                this.Val(FIELD.F_10, this.F10Encode());
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_12, this.Core().LocalTransactionTime);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);
                //this.Val(FIELD.F_14, this.Core().ExpireDate);
                this.Val(FIELD.F_18, this.Core().MerchantCategoryCode);

                this.Val(FIELD.F_20, this.F20Encode());
                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                //this.Val(FIELD.F_23, this.F23Encode());
                this.Val(FIELD.F_25, this.Core().PointOfServiceConditionMode);
                this.Val(FIELD.F_28, this.Core().TransactionFeeAmount);

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_33, this.F33Encode());
                this.Val(FIELD.F_35, this.Track2Encode());
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

                this.Val(FIELD.F_43, this.CardAcceptorEncode());
                this.Val(FIELD.F_48, this.F48Encode());
                this.Val(FIELD.F_49, this.Core().TransactionCurrencyCode);

                this.Val(FIELD.F_51, this.F51Encode());
                this.Val(FIELD.F_52, this.Core().Pinblock);
                this.Val(FIELD.F_54, this.F54Encode());

                this.Val(FIELD.F_63, this.F63TokenEncode());

                this.Val(FIELD.F_90, this.F90OriginalDataElementsEncode());
                this.Val(FIELD.F_95, this.Core().ReplacementAmount);

                this.Val(FIELD.F_120, this.F120Encode());
                this.Val(FIELD.F_124, this.F124Encode());
                this.Val(FIELD.F_127, this.F127PosUserDataEncode());
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
            this.Mti(MCOLATransactionConst.MTI.ReversalAdviceRequest);
        }
    }
}

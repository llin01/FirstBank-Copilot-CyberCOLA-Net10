namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class TransactionHistoryAdviceRequest : MasterCardRequest, AlternativeNetworkRouting
    {
        internal TransactionHistoryAdviceRequest()
        {
        }

        internal TransactionHistoryAdviceRequest(byte[] message)
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
                //this.Val(FIELD.F_3, this.F3ProcessingCodeEncode());
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);

                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);

                this.Val(FIELD.F_22, this.F22PointOfServiceEntryModeEncode());

                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                this.Val(FIELD.F_43, this.F43CardAcceptorNameLocationEncode());
                this.Val(FIELD.F_48, this.F48AdditionalDataPrivateUseEncode());
                this.Val(FIELD.F_49, this.Core().TransactionCurrencyCode);

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
            this.Mti(MasterCardTransactionConst.MTI.TransactionHistoryAdviceRequest);
        }
    }
}

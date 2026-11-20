namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class TwinReversal0430 : TwinRequest
    {
        public TwinReversal0430()
        {
        }

        public TwinReversal0430(byte[] message)
            : base(message)
        {
        }

        public override Iso8583 Prepare()
        {
            try
            {
                this.PurgeFields();
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
                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(8, ' '));
                this.Val(FIELD.F_42, Core().MerchantId.PadRight(15, ' '));
                this.Val(FIELD.F_43, this.CardAcceptorEncode());
                if (!string.IsNullOrEmpty(this.Core().NetworkInfoSubAcquirerID) &&
                    !string.IsNullOrEmpty(this.Core().NetworkInfoNetworkReferenceID))
                {
                    var tempString = $"{this.Core().NetworkInfoSubAcquirerID}{this.Core().NetworkInfoNetworkReferenceID}";
                    this.Val(FIELD.F_48, tempString);
                }
                this.Val(FIELD.F_49, "901");
            }
            catch (Exception e)
            {
                // TODO
                // Log.e(t);
                throw new TransactionPrepareException(e);
            }

            return base.Prepare();
        }
    }
}

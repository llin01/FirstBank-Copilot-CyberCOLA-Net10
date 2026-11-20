namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0220", "203000")]
    public class TwinRefundRequest : TwinRequest
    {
        public TwinRefundRequest(byte[] message = null)
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
                this.Val(FIELD.F_17, this.Core().CaptureDate);
                this.Val(FIELD.F_18, this.Core().MerchantCategoryCode);
                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                this.Val(FIELD.F_24, this.Core().FunctionCode);
                this.Val(FIELD.F_25, this.Core().PointOfServiceConditionMode);
                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_35, this.Track2Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(8, ' '));
                this.Val(FIELD.F_42, Core().MerchantId.PadRight(15, ' '));
                this.Val(FIELD.F_43, this.CardAcceptorEncode());
                this.Val(FIELD.F_42, Core().MerchantId.PadRight(15, ' '));
                this.Val(FIELD.F_43, this.CardAcceptorEncode());
                if (!string.IsNullOrEmpty(this.Core().NetworkInfoSubAcquirerID) &&
                    !string.IsNullOrEmpty(this.Core().NetworkInfoNetworkReferenceID))
                {
                    var tempString = $"{this.Core().NetworkInfoSubAcquirerID}{this.Core().NetworkInfoNetworkReferenceID}";
                    this.Val(FIELD.F_48, tempString);
                }
                this.Val(FIELD.F_49, (string)this.Core().GetOthersValByKey("F_49"));
                var field55Data = (TWINFIELD_55)this.Core().GetOthersValByKey("F_55");
                if (field55Data != null)
                {
                    this.Val(FIELD.F_55, field55Data.Encode());

                }
                this.Val(FIELD.F_61, (string)this.Core().GetOthersValByKey("F_61"));

                var field63Data = (TWINTLV)this.Core().GetOthersValByKey("F_63");
                if (field63Data != null)
                {
                    this.Val(FIELD.F_63, field63Data.Encode());

                }
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
            this.Mti(TwinTransactionConst.MTI.RefundRequest);
            this.Core().ProcessingCode = TwinTransactionConst.ProcessingCode.Refund;
        }
    }
}

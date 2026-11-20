namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class AlternativeNetworkRoutingResponse : MasterCardResponse, AlternativeNetworkRouting
    {
        internal AlternativeNetworkRoutingResponse()
        {
        }

        internal AlternativeNetworkRoutingResponse(byte[] message)
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
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);

                this.Val(FIELD.F_14, this.Core().ExpireDate);

                this.Val(FIELD.F_22, this.F22PointOfServiceEntryModeEncode());

                this.Val(FIELD.F_39, this.Core().ResponseCode);

                if (!string.IsNullOrEmpty(this.Core().MerchantId))
                {
                    var len = this.Spec(FIELD.F_42).MaxLength();
                    this.Val(FIELD.F_42, this.Core().MerchantId.PadRight(len, ' '));
                }

                this.Val(FIELD.F_44, this.F44Encode());
                this.Val(FIELD.F_48, this.F48AdditionalDataPrivateUseEncode());

                this.Val(FIELD.F_60, this.F60AdviceReasonCodeEncode());
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
            this.Mti(MasterCardTransactionConst.MTI.AlternativeNetworkRoutingResponse);
        }
    }
}

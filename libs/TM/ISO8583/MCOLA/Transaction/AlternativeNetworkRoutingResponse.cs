namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class AlternativeNetworkRoutingResponse : MCOLAResponse, AlternativeNetworkRouting
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

                //this.Val(FIELD.F_2, this.Core().Pan);
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);

                //this.Val(FIELD.F_14, this.Core().ExpireDate);

                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                this.Val(FIELD.F_35, this.Track2Encode());
                this.Val(FIELD.F_39, this.Core().ResponseCode);

                if (!string.IsNullOrEmpty(this.Core().MerchantId))
                {
                    var len = this.Spec(FIELD.F_42).MaxLength();
                    this.Val(FIELD.F_42, this.Core().MerchantId.PadRight(len, ' '));
                }

                this.Val(FIELD.F_44, this.F44Encode());
                this.Val(FIELD.F_48, this.F48Encode());

                this.Val(FIELD.F_63, this.F63TokenEncode());

                this.Val(FIELD.F_120, this.F120Encode());
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
            this.Mti(MCOLATransactionConst.MTI.AlternativeNetworkRoutingResponse);
        }
    }
}

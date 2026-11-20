namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class AlternativeNetworkRoutingRequest : MCOLARequest, AlternativeNetworkRouting
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

                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_14, this.Core().ExpireDate);

                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                this.Val(FIELD.F_23, this.F23Encode());

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_35, this.Track2Encode());

                if (!string.IsNullOrEmpty(this.Core().MerchantId))
                {
                    var len = this.Spec(FIELD.F_42).MaxLength();
                    this.Val(FIELD.F_42, this.Core().MerchantId.PadRight(len, ' '));
                }

                if (!string.IsNullOrEmpty(this.F45Encode()))
                {
                    var len = this.Spec(FIELD.F_45).MaxLength();
                    this.Val(FIELD.F_45, this.F45Encode().PadRight(len, ' '));
                }

                this.Val(FIELD.F_52, this.Core().Pinblock);
                this.Val(FIELD.F_53, this.F53Encode());

                this.Val(FIELD.F_63, this.F63TokenEncode());

                this.Val(FIELD.F_70, this.Core().NetworkManagementInformationCode);
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
            this.Mti(MCOLATransactionConst.MTI.AlternativeNetworkRoutingRequest);
        }
    }
}

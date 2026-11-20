namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class AdministrativeResponse : MasterCardResponse, Administrative
    {
        internal AdministrativeResponse()
        {
        }

        internal AdministrativeResponse(byte[] message)
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
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);

                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);

                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_33, this.ExtendedCore().ForwardingInstitutionIdCode);
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

                this.Val(FIELD.F_60, this.F60AdviceReasonCodeEncode());
                this.Val(FIELD.F_62, this.F62Encode());
                this.Val(FIELD.F_63, this.F63NetworkDataEncode());
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
            this.Mti(MasterCardTransactionConst.MTI.AdministrativeResponse);
        }
    }
}

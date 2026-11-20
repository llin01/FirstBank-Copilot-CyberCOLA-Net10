namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class PekExchangeRequest : MasterCardRequest, ChangeKey
    {
        internal PekExchangeRequest()
        {
        }

        internal PekExchangeRequest(byte[] message)
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

                this.Val(FIELD.F_33, this.ExtendedCore().ForwardingInstitutionIdCode);

                this.Val(FIELD.F_48, this.F48AdditionalDataPrivateUseEncode());

                this.Val(FIELD.F_70, this.Core().NetworkManagementInformationCode);

                this.Val(FIELD.F_94, this.F94ServiceIndicatorEncode());
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
            this.Mti(MasterCardTransactionConst.MTI.NetworkManagementRequest);
        }
    }
}

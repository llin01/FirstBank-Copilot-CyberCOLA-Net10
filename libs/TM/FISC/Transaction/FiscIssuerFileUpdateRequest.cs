namespace Com.Cybersoft4u.TM.FISC.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class FiscIssuerFileUpdateRequest : FISC, FileUpdate
    {
        internal FiscIssuerFileUpdateRequest()
        {
        }

        internal FiscIssuerFileUpdateRequest(byte[] message)
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

                this.Val(FIELD.F_91, this.ExtendedCore().IssuerFileUpdateCode);
                this.Val(FIELD.F_96, this.ExtendedCore().MessageSecurityCode);

                this.Val(FIELD.F_101, this.ExtendedCore().FileName);

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
            this.Mti(FISCTransactionConst.MTI.IssuerFileUpdateRequest);
        }
    }
}

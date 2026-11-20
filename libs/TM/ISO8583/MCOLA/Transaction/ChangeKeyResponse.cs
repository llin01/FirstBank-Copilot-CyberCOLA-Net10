namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class ChangeKeyResponse : MCOLAResponse, ChangeKey
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

                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);

                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);

                this.Val(FIELD.F_39, this.Core().ResponseCode);

                this.Val(FIELD.F_48, this.F48Encode());

                this.Val(FIELD.F_53, this.F53Encode());

                this.Val(FIELD.F_70, this.Core().NetworkManagementInformationCode);

                this.Val(FIELD.F_120, this.F120Encode());

                this.Val(FIELD.F_123, this.F123CryptographicServiceMessageEncode());
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
            this.Mti(MCOLATransactionConst.MTI.NetworkManagementResponse);
        }
    }
}

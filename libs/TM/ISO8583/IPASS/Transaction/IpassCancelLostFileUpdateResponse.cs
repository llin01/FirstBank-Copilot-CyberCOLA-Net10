namespace Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0312", "920000")]
    public class IpassCancelLostFileUpdateResponse : IpassResponse
    {
        public IpassCancelLostFileUpdateResponse(byte[] message = null)
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
                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_36, this.Core().Track3Data);
                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_63, this.Core().SystemId);
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
            this.Mti(IpassTransactionConst.MTI.AuthorizationResponse);
            this.Core().ProcessingCode = IpassTransactionConst.ProcessingCode.IPASSLostCancel;
        }
    }
}

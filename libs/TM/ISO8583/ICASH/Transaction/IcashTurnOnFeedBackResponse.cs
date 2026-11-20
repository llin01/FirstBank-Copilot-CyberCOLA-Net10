namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    [TransactionType("0150", "990173")]
    public class IcashTurnOnFeedBackResponse : IcashResponse
    {
        public IcashTurnOnFeedBackResponse(byte[] message = null)
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
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_41, this.Core().TerminalId);
                this.Val(FIELD.F_61, this.Core().F61ICASHChipRelatedData);
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
            this.Mti(IcashTransactionConst.MTI.AuthorizationResponse);
            this.Core().ProcessingCode = IcashTransactionConst.ProcessingCode.ICASHTurnOnFeedBack;
        }
    }
}

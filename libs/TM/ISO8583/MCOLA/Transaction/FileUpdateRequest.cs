namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using System;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class FileUpdateRequest : MCOLARequest, FileUpdate
    {
        internal FileUpdateRequest()
        {
        }

        internal FileUpdateRequest(byte[] message)
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

                this.Val(FIELD.F_33, this.F33Encode());

                this.Val(FIELD.F_91, this.F91FileUpdateCodeEncode());

                if (!string.IsNullOrEmpty(this.Core().TerminalId))
                {
                    var len = this.Spec(FIELD.F_96).MaxLength();
                    this.Val(FIELD.F_96, this.F96Encode().PadRight(len, ' '));
                }

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
            this.Mti(MCOLATransactionConst.MTI.FileUpdateRequest);
        }
    }
}

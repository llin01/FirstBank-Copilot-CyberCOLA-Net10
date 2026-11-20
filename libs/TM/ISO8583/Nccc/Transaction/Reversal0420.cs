namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class Reversal0420 : NcccRequest
    {
        public Reversal0420()
        {
        }

        public Reversal0420(byte[] message)
            : base(message)
        {
        }

        public override Iso8583 Prepare()
        {
            try
            {
                this.PurgeFields();
                if (this.Core().NullSecondBitmap)
                {
                    this.Val(FIELD.F_1, "0000000000000000");
                }

                this.Val(FIELD.F_3, this.Core().ProcessingCode);
                this.Val(FIELD.F_4, this.Core().TransactionAmount);
                this.Val(FIELD.F_7, this.Core().TransmissionDatetime);
                this.Val(FIELD.F_11, this.Core().SystemTraceAuditNumber);
                this.Val(FIELD.F_12, this.Core().LocalTransactionTime);
                this.Val(FIELD.F_13, this.Core().LocalTransactionDate);
                this.Val(FIELD.F_15, this.Core().SettlementDate);
                this.Val(FIELD.F_17, this.Core().CaptureDate);
                this.Val(FIELD.F_22, this.Core().PointOfServiceEntryMode);
                this.Val(FIELD.F_25, this.Core().PointOfServiceConditionMode);
                this.Val(FIELD.F_32, this.Core().AcquiringInstitutionIdentificationCode);
                this.Val(FIELD.F_35, this.Track2Encode());
                this.Val(FIELD.F_37, this.Core().RetrievalReferenceNumber);
                this.Val(FIELD.F_38, this.Core().ApproveCode);
                this.Val(FIELD.F_39, this.Core().ResponseCode);
                this.Val(FIELD.F_41, this.Core().TerminalId.PadRight(16, ' '));

                //// Val(FIELD.F_42, Core().MerchantId.PadRight(27, ' '));
                this.Val(FIELD.F_43, this.CardAcceptorEncode());
                this.Val(FIELD.F_48, this.Core().MerchantId.PadRight(27, ' '));
                this.Val(FIELD.F_49, "901");

                //// Val(FIELD.F_54, Core().AdditionalAmount);
                this.Val(FIELD.F_61, "019BK77PRO200000000000");

                //// Val(FIELD.F_62, "010" + Core().PostCode.PadRight(10, ' '));
                this.Val(FIELD.F_90, this.F90OriginalDataElementsEncode());

                //// Val(FIELD.F_95, Core().ReplacementAmount);
                //// Val(FIELD.F_127, "");
            }
            catch (Exception e)
            {
                // TODO
                // Log.e(t);
                throw new TransactionPrepareException(e);
            }

            return base.Prepare();
        }
    }
}

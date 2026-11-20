namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class GroupSignOnRequest : MasterCardRequest, SignOn
    {
        public GroupSignOnRequest()
        {
        }

        public GroupSignOnRequest(byte[] message)
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

                //// Primary Account Number (PAN) Country Code
                //// .RawVal(FIELD.F_20, )

                this.Val(FIELD.F_33, this.ExtendedCore().ForwardingInstitutionIdCode);
                this.Val(FIELD.F_70, this.Core().NetworkManagementInformationCode);
                this.Val(FIELD.F_94, this.F94ServiceIndicatorEncode());
                this.Val(FIELD.F_96, this.ExtendedCore().MessageSecurityCode);

                //// Private Data
                //// .RawVal(FIELD.F_127, )
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

            var masterCardGroup = string.Empty.PadRight(19, '0');
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            var customerId = string.Empty.PadRight(6, '0');
            var networkManagementInformationCode = MasterCardTransactionConst.NetworkManagementInformationCode.GroupSignOn;
            var serviceIndicator = (MasterCardTransactionConst.ServiceIndicator.ReservedForFutureUse.Default
                                    + MasterCardTransactionConst.ServiceIndicator.AcquirerIssuerIndicator.IssuerOnly
                                    + MasterCardTransactionConst.ServiceIndicator.AddressDataIndicator.AvsNotSupport)
                                    .PadRight(7, '0');
            var messageSecurityCode = string.Empty.PadRight(8, '0');

            this.RawVal(FIELD.F_2, masterCardGroup)
                .RawVal(FIELD.F_7, utcDateTime)
                .RawVal(FIELD.F_11, stan)
                //// Primary Account Number (PAN) Country Code
                //// .RawVal(FIELD.F_20, )
                .RawVal(FIELD.F_33, customerId)
                .RawVal(FIELD.F_70, networkManagementInformationCode)
                .RawVal(FIELD.F_94, serviceIndicator)
                .RawVal(FIELD.F_96, messageSecurityCode);
            //// Private Data
            //// .RawVal(FIELD.F_127, )
        }
    }
}

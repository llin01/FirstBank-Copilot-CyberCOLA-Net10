namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class SignOffRequest : MasterCardRequest, SignOff
    {
        public SignOffRequest()
        {
        }

        public SignOffRequest(byte[] message)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti(MasterCardTransactionConst.MTI.NetworkManagementRequest);

            var pan = string.Empty.PadRight(19, '0');
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            var customerId = string.Empty.PadRight(6, '0');
            var networkManagementInformationCode = MasterCardTransactionConst.NetworkManagementInformationCode.SignOff;
            var serviceIndicator = (MasterCardTransactionConst.ServiceIndicator.ReservedForFutureUse.Default
                                    + MasterCardTransactionConst.ServiceIndicator.AcquirerIssuerIndicator.IssuerOnly
                                    + MasterCardTransactionConst.ServiceIndicator.AddressDataIndicator.AvsNotSupport)
                                    .PadRight(7, '0');
            var messageSecurityCode = string.Empty.PadRight(8, '0');

            this.RawVal(FIELD.F_2, pan)
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

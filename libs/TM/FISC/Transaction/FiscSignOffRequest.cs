namespace Com.Cybersoft4u.TM.FISC.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class FiscSignOffRequest : FISC, SignOff
    {
        public FiscSignOffRequest()
        {
        }

        public FiscSignOffRequest(byte[] message)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti(FISCTransactionConst.MTI.NetworkManagementRequest);

            var pan = FISCTransactionConst.MemberDefinedData.FISCField2Data.FISCNetworkF2NCID;
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            var customerId = FISCTransactionConst.MemberDefinedData.FISCField2Data.FISCNetworkF2NCID;
            var networkdata = FISCTransactionConst.MemberDefinedData.FISCField2Data.FISCNetworkData;
            var networkManagementInformationCode = FISCTransactionConst.NetworkManagementInformationCode.SignOff;
            var serviceIndicator = (FISCTransactionConst.ServiceIndicator.ReservedForFutureUse.Default
                                       + FISCTransactionConst.ServiceIndicator.AcquirerIssuerIndicator.AcquirerOnly
                                       + FISCTransactionConst.ServiceIndicator.AddressDataIndicator.AvsNotSupport)
                                       .PadRight(7, ' ');
            var messageSecurityCode = string.Empty.PadRight(16, '0');

            this.RawVal(FIELD.F_2, pan)
                .RawVal(FIELD.F_7, utcDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_33, customerId)
                .RawVal(FIELD.F_63, networkdata)
                .RawVal(FIELD.F_70, networkManagementInformationCode)
                .RawVal(FIELD.F_94, serviceIndicator)
                .RawVal(FIELD.F_96, messageSecurityCode);
        }
    }
}

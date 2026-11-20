namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class MasterCardDE48SE48 : MasterCardDE48SE
    {
        //// Subfields
        public MasterCardDE48SESF RemotePaymentsProgramTypeIdentifier = new MasterCardDE48SESF("01", 1, DEType.LL);
        public MasterCardDE48SESF MasterCardMobileRemotePaymentTransactionType = new MasterCardDE48SESF("02", 1, DEType.LL);
        public MasterCardDE48SESF MobilePhoneNumber = new MasterCardDE48SESF("03", 15, DEType.LL);
        public MasterCardDE48SESF ConvenienceFee = new MasterCardDE48SESF("04", 40, DEType.LL);

        public MasterCardDE48SE48()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.MobileProgramIndicatorsTagId)
        {
            this.Subfields[RemotePaymentsProgramTypeIdentifier.Id] = RemotePaymentsProgramTypeIdentifier;
            this.Subfields[MasterCardMobileRemotePaymentTransactionType.Id] = MasterCardMobileRemotePaymentTransactionType;
            this.Subfields[MobilePhoneNumber.Id] = MobilePhoneNumber;
            this.Subfields[ConvenienceFee.Id] = ConvenienceFee;
        }
    }
}

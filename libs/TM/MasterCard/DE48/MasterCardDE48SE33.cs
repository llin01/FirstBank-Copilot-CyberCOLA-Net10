namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class MasterCardDE48SE33 : MasterCardDE48SE
    {
        //// Subfields
        public MasterCardDE48SESF AccountNumberIndicator = new MasterCardDE48SESF("01", 1, DEType.LL);
        public MasterCardDE48SESF AccountNumber = new MasterCardDE48SESF("02", 19, DEType.LL);
        public MasterCardDE48SESF ExpirationDate = new MasterCardDE48SESF("03", 4, DEType.LL);
        public MasterCardDE48SESF ProductCode = new MasterCardDE48SESF("04", 3, DEType.LL);
        public MasterCardDE48SESF TokenAssuranceLevel = new MasterCardDE48SESF("05", 2, DEType.LL);
        public MasterCardDE48SESF TokenRequestorId = new MasterCardDE48SESF("06", 11, DEType.LL);
        public MasterCardDE48SESF PrimaryAccountNumberAccountRange = new MasterCardDE48SESF("07", 19, DEType.LL);
        public MasterCardDE48SESF StorageTechnology = new MasterCardDE48SESF("08", 2, DEType.LL);

        public MasterCardDE48SE33()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId)
        {
            this.Subfields[AccountNumberIndicator.Id] = AccountNumberIndicator;
            this.Subfields[AccountNumber.Id] = AccountNumber;
            this.Subfields[ExpirationDate.Id] = ExpirationDate;
            this.Subfields[ProductCode.Id] = ProductCode;
            this.Subfields[TokenAssuranceLevel.Id] = TokenAssuranceLevel;
            this.Subfields[TokenRequestorId.Id] = TokenRequestorId;
            this.Subfields[PrimaryAccountNumberAccountRange.Id] = PrimaryAccountNumberAccountRange;
            this.Subfields[StorageTechnology.Id] = StorageTechnology;
        }
    }
}

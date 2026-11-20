namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class FISCDE48SE33 : FISCDE48SE
    {
        //// Subfields
        public FISCDE48SESF AccountNumberIndicator = new FISCDE48SESF("01", 1, DEType.LL);
        public FISCDE48SESF AccountNumber = new FISCDE48SESF("02", 19, DEType.LL);
        public FISCDE48SESF ExpirationDate = new FISCDE48SESF("03", 4, DEType.LL);
        public FISCDE48SESF ProductCode = new FISCDE48SESF("04", 3, DEType.LL);
        public FISCDE48SESF TokenAssuranceLevel = new FISCDE48SESF("05", 2, DEType.LL);
        public FISCDE48SESF TokenRequestorId = new FISCDE48SESF("06", 11, DEType.LL);
        public FISCDE48SESF PrimaryAccountNumberAccountRange = new FISCDE48SESF("07", 19, DEType.LL);
        public FISCDE48SESF StorageTechnology = new FISCDE48SESF("08", 2, DEType.LL);

        public FISCDE48SE33()
            : base(FISCTransactionConst.AdditionalDataPrivateUse.PanMappingFileInformationTagId)
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

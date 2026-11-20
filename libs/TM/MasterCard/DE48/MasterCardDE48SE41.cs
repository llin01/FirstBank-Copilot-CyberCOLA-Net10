namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class MasterCardDE48SE41 : MasterCardDE48SE
    {
        public MasterCardDE48SESF subfield1 = new MasterCardDE48SESF("01", 26, DEType.LL);
        public MasterCardDE48SESF subfield2 = new MasterCardDE48SESF("02", 6, DEType.LL);
        public MasterCardDE48SESF subfield3 = new MasterCardDE48SESF("03", 3, DEType.LL);
        public MasterCardDE48SESF subfield4 = new MasterCardDE48SESF("04", 22, DEType.LL);
        public MasterCardDE48SESF subfield5 = new MasterCardDE48SESF("05", 20, DEType.LL);
        public MasterCardDE48SESF subfield6 = new MasterCardDE48SESF("06", 13, DEType.LL);
        public MasterCardDE48SESF subfield7 = new MasterCardDE48SESF("07", 3, DEType.LL);
        public MasterCardDE48SESF subfield8 = new MasterCardDE48SESF("08", 10, DEType.LL);
        public MasterCardDE48SESF subfield9 = new MasterCardDE48SESF("09", 22, DEType.LL);
        public MasterCardDE48SESF subfield10 = new MasterCardDE48SESF("10", 9, DEType.LL);
        public MasterCardDE48SESF CitizenId = new MasterCardDE48SESF("11", 20, DEType.LL);
        public MasterCardDE48SESF subfield12 = new MasterCardDE48SESF("12", 20, DEType.LL);
        public MasterCardDE48SESF subfield13 = new MasterCardDE48SESF("13", 20, DEType.LL);
        public MasterCardDE48SESF subfield14 = new MasterCardDE48SESF("14", 20, DEType.LL);
        public MasterCardDE48SESF subfield15 = new MasterCardDE48SESF("15", 10, DEType.LL);
        public MasterCardDE48SESF subfield16 = new MasterCardDE48SESF("16", 2, DEType.LL);
        public MasterCardDE48SESF subfield17 = new MasterCardDE48SESF("17", 1, DEType.LL);
        public MasterCardDE48SESF subfield18 = new MasterCardDE48SESF("18", 20, DEType.LL);

        public MasterCardDE48SE41()
            : base(MasterCardTransactionConst.AdditionalDataPrivateUse.ElectronicCommerceCertificateQualifyingInformation)
        {
            this.Subfields[subfield1.Id] = subfield1;
            this.Subfields[subfield2.Id] = subfield2;
            this.Subfields[subfield3.Id] = subfield3;
            this.Subfields[subfield4.Id] = subfield4;
            this.Subfields[subfield5.Id] = subfield5;
            this.Subfields[subfield6.Id] = subfield6;
            this.Subfields[subfield7.Id] = subfield7;
            this.Subfields[subfield8.Id] = subfield8;
            this.Subfields[subfield9.Id] = subfield9;
            this.Subfields[subfield10.Id] = subfield10;
            this.Subfields[CitizenId.Id] = CitizenId;
            this.Subfields[subfield12.Id] = subfield12;
            this.Subfields[subfield13.Id] = subfield13;
            this.Subfields[subfield14.Id] = subfield14;
            this.Subfields[subfield15.Id] = subfield15;
            this.Subfields[subfield16.Id] = subfield16;
            this.Subfields[subfield17.Id] = subfield17;
            this.Subfields[subfield18.Id] = subfield18;
        }
    }
}

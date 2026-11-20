// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenQ9.cs">
//   Author: C.F
// </copyright>
// <summary>
//   Defines the TokenQ9 type.
//   Update:C.F 20190506
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenQ9 : Token
    {
        public TokenQ9()
            : base(NcccTransactionConst.PosAdditionalData.TokenQ9.Id)
        {
        }

        public string FIID { get; set; }

        public string TokenData { get; set; }
        public string VisaDevTyp { get; set; }
        public string VisaChipTxnInd { get; set; }
        public string VisaMsgRsnCde { get; set; }
        public string VisaPrtlAuthInd { get; set; }
        public string VisaStipCde { get; set; }
        public string VisaMrchVrfcnVal { get; set; }
        public string VisaBusApplId { get; set; }
        public string VisaTraceNum { get; set; }
        public string VisaRefNum { get; set; }
        public string VisaFiller { get; set; }
        public string MastercardDevTyp { get; set; }
        public string MastercardAdviceRsnCde { get; set; }
        public string MastercardAdvcDetlCde { get; set; }
        public string MastercardAuthAgentIdCde { get; set; }
        public string MastercardOnBehalf { get; set; }
        public string MastercardRsnAavDowngrade { get; set; }
        public string JcbievTyp { get; set; }
        public string Jcbiiller { get; set; }
        public string DfsFuncCde { get; set; }
        public string DfsStan { get; set; }
        public string DfsFiller { get; set; }

        public override string DataEncode()
        {
            return this.FIID + this.TokenData;
        }

        public override Token DataDecode(string data)
        {
            this.FIID = data.Substring(0, 4);
            this.TokenData = data.Substring(4, 46);
            return this;
        }
        public void SubDataDecode()
        {
            if (string.IsNullOrEmpty(FIID) || string.IsNullOrEmpty(TokenData))
                return;
            if (FIID.Length != 4 || TokenData.Length != 46)
                return;
            switch (FIID.Trim().ToUpper())
            {
                case "VISA":
                    VisaDevTyp = TokenData.Substring(0, 2);
                    VisaChipTxnInd = TokenData.Substring(2, 1);
                    VisaMsgRsnCde = TokenData.Substring(3, 4);
                    VisaPrtlAuthInd = TokenData.Substring(7, 1);
                    VisaStipCde = TokenData.Substring(8, 4);
                    VisaMrchVrfcnVal = TokenData.Substring(12, 10);
                    VisaBusApplId = TokenData.Substring(22, 2);
                    VisaTraceNum = TokenData.Substring(24, 6);
                    VisaRefNum = TokenData.Substring(30, 12);
                    VisaFiller = TokenData.Substring(42, 4);
                    break;
                case "BNET":
                    MastercardDevTyp = TokenData.Substring(0, 2);
                    MastercardAdviceRsnCde = TokenData.Substring(2, 3);
                    MastercardAdvcDetlCde = TokenData.Substring(5, 4);
                    MastercardAuthAgentIdCde = TokenData.Substring(9, 6);
                    MastercardOnBehalf = TokenData.Substring(15, 30);
                    MastercardRsnAavDowngrade = TokenData.Substring(45, 1);
                    break;
                case "JCBI":
                    JcbievTyp = TokenData.Substring(0, 1);
                    Jcbiiller = TokenData.Substring(1, 45);
                    break;
                case "DFS":
                    DfsFuncCde = TokenData.Substring(0, 3);
                    DfsStan = TokenData.Substring(3, 6);
                    DfsFiller = TokenData.Substring(9, 37);
                    break;
            }
        }
    }
}

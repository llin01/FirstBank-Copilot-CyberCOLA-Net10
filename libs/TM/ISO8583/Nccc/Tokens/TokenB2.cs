// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenB2.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenB2 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenB2 : Token
    {
        public TokenB2()
            : base(NcccTransactionConst.PosAdditionalData.TokenB2.Id)
        {
            this.Bitmap = string.Empty.PadRight(4, ' ');
            this.UserFld1 = string.Empty.PadRight(4, ' ');
            this.CryptoInfData = string.Empty.PadRight(2, '0');
            this.Tvr = string.Empty.PadRight(10, '0');
            this.Arqc = string.Empty.PadRight(16, '0');
            this.AmtAuth = string.Empty.PadRight(12, '0');
            this.AmtOther = string.Empty.PadRight(12, '0');
            this.Aip = string.Empty.PadRight(4, '0');
            this.Atc = string.Empty.PadRight(4, '0');
            this.TermCntryCde = string.Empty.PadRight(3, '0');
            this.TermCrncyCde = string.Empty.PadRight(3, '0');
            this.TranDat = string.Empty.PadRight(6, '0');
            this.TranType = string.Empty.PadRight(2, '0');
            this.UnpredictNum = string.Empty.PadRight(8, '0');
            this.IssApplDataLgth = string.Empty.PadRight(4, '0');
            this.IssApplData = string.Empty.PadRight(64, '0');
        }

        public string Bitmap { get; set; }

        public string UserFld1 { get; set; }

        public string CryptoInfData { get; set; }

        public string Tvr { get; set; }

        public string Arqc { get; set; }

        public string AmtAuth { get; set; }

        public string AmtOther { get; set; }

        public string Aip { get; set; }

        public string Atc { get; set; }

        public string TermCntryCde { get; set; }

        public string TermCrncyCde { get; set; }

        public string TranDat { get; set; }

        public string TranType { get; set; }

        public string UnpredictNum { get; set; }

        public string IssApplDataLgth { get; set; }

        public string IssApplData { get; set; }

        public override string DataEncode()
        {
            return this.Bitmap + this.UserFld1 + this.CryptoInfData + this.Tvr + this.Arqc + this.AmtAuth
                   + this.AmtOther + this.Aip + this.Atc + this.TermCntryCde + this.TermCrncyCde + this.TranDat
                   + this.TranType + this.UnpredictNum + this.IssApplDataLgth + this.IssApplData;
        }

        public override Token DataDecode(string data)
        {
            this.Bitmap = data.Substring(0, 4);
            this.UserFld1 = data.Substring(4, 4);
            this.CryptoInfData = data.Substring(8, 2);
            this.Tvr = data.Substring(10, 10);
            this.Arqc = data.Substring(20, 16);
            this.AmtAuth = data.Substring(36, 12);
            this.AmtOther = data.Substring(48, 12);
            this.Aip = data.Substring(60, 4);
            this.Atc = data.Substring(64, 4);
            this.TermCntryCde = data.Substring(68, 3);
            this.TermCrncyCde = data.Substring(71, 3);
            this.TranDat = data.Substring(74, 6);
            this.TranType = data.Substring(80, 2);
            this.UnpredictNum = data.Substring(82, 8);
            this.IssApplDataLgth = data.Substring(90, 4);
            this.IssApplData = data.Substring(94, 64);
            return this;
        }
    }
}

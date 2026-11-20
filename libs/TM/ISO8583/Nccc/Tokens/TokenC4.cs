// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenC4.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenC4 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenC4 : Token
    {
        public TokenC4()
            : base(NcccTransactionConst.PosAdditionalData.TokenC4.Id)
        {
            this.TermAttendInd = string.Empty.PadRight(1, ' ');
            this.TermLocInd = string.Empty.PadRight(1, ' ');
            this.ChPresentInd = string.Empty.PadRight(1, ' ');
            this.CrdPresentInd = string.Empty.PadRight(1, ' ');
            this.CrdCaptrInd = string.Empty.PadRight(1, ' ');
            this.TxnStatInd = string.Empty.PadRight(1, ' ');
            this.TxnSecInd = string.Empty.PadRight(1, ' ');
            this.TxnSecInd2 = "0";
            this.ChActvtInd = string.Empty.PadRight(1, ' ');
            this.TermInputCapInd = string.Empty.PadRight(1, ' ');
        }

        public string TermAttendInd { get; set; }

        public string TermLocInd { get; set; }

        public string ChPresentInd { get; set; }

        public string CrdPresentInd { get; set; }

        public string CrdCaptrInd { get; set; }

        public string TxnStatInd { get; set; }

        public string TxnSecInd { get; set; }

        public string TxnSecInd2 { get; set; }

        public string ChActvtInd { get; set; }

        public string TermInputCapInd { get; set; }

        public override string DataEncode()
        {
            return this.TermAttendInd + "0" + this.TermLocInd + this.ChPresentInd + this.CrdPresentInd
                   + this.CrdCaptrInd + this.TxnStatInd + this.TxnSecInd + this.TxnSecInd2 + this.ChActvtInd
                   + this.TermInputCapInd + " ";
        }

        public override Token DataDecode(string data)
        {
            this.TermAttendInd = data.Substring(0, 1);
            this.TermLocInd = data.Substring(2, 1);
            this.ChPresentInd = data.Substring(3, 1);
            this.CrdPresentInd = data.Substring(4, 1);
            this.CrdCaptrInd = data.Substring(5, 1);
            this.TxnStatInd = data.Substring(6, 1);
            this.TxnSecInd = data.Substring(7, 1);
            this.TxnSecInd2 = data.Substring(8, 1);
            this.ChActvtInd = data.Substring(9, 1);
            this.TermInputCapInd = data.Substring(10, 1);
            return this;
        }
    }
}

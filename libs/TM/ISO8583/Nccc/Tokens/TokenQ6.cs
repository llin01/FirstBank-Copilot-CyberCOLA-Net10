// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenQ6.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenQ6 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenQ6 : Token
    {
        public TokenQ6()
            : base("Q6")
        {
        }

        public string TxnChnlTyp { get; set; }

        public string TxnInitMthd { get; set; }

        public string ChnCustIdTyp { get; set; }

        public string ChnCustId { get; set; }

        public string TxnInd { get; set; }

        public string TxnSpcfData { get; set; }

        public override string DataEncode()
        {
            return this.TxnChnlTyp + this.TxnInitMthd + this.ChnCustIdTyp + this.ChnCustId + this.TxnInd
                   + this.TxnSpcfData;
        }

        public override Token DataDecode(string data)
        {
            this.TxnChnlTyp = data.Substring(0, 2);
            this.TxnInitMthd = data.Substring(2, 1);
            this.ChnCustIdTyp = data.Substring(3, 2);
            this.ChnCustId = data.Substring(5, 20);
            this.TxnInd = data.Substring(25, 1);
            this.TxnSpcfData = data.Substring(26);
            return this;
        }
    }
}

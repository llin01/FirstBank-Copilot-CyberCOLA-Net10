// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenU4.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenU4 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenU4 : Token
    {
        public TokenU4()
            : base("U4")
        {
        }

        public string TxnType { get; set; }

        public string Resp { get; set; }

        public string CashCanuse { get; set; }

        public string CashCanuseIns { get; set; }

        public override string DataEncode()
        {
            return this.TxnType + this.Resp + this.CashCanuse + this.CashCanuseIns;
        }

        public override Token DataDecode(string data)
        {
            this.TxnType = data.Substring(0, 1);
            this.Resp = data.Substring(1, 4);
            this.CashCanuse = data.Substring(5, 10);
            this.CashCanuseIns = data.Substring(15, 10);
            return this;
        }
    }
}

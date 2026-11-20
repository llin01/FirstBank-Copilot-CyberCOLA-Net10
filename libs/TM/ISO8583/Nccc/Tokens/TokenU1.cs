// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenU1.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenU1 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenU1 : Token
    {
        public TokenU1()
            : base("U1")
        {
        }

        public string CashRebateFlag { get; set; }

        public string RefNo { get; set; }

        public string RebatePoint { get; set; }

        public string RebateAmount { get; set; }

        public string RealTxnAmount { get; set; }

        public string PointBalance { get; set; }

        public override string DataEncode()
        {
            return this.CashRebateFlag + this.RefNo + this.RebatePoint + this.RebateAmount + this.RealTxnAmount
                   + this.PointBalance;
        }

        public override Token DataDecode(string data)
        {
            this.CashRebateFlag = data.Substring(0, 1);
            this.RefNo = data.Substring(1, 12);
            this.RebatePoint = data.Substring(13, 7);
            this.RebateAmount = data.Substring(20, 12);
            this.RealTxnAmount = data.Substring(32, 12);
            this.PointBalance = data.Substring(44, 7);
            return this;
        }
    }
}

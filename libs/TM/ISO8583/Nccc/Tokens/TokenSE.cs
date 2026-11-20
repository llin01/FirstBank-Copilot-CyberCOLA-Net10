// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenSE.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenSE type.
//   Update:C.F 20180831 
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenSE : Token
    {
        public TokenSE()
            : base("SE")
        {
        }

        public string NumAmts { get; set; }

        public string AmtTyp { get; set; }

        public string CrncyCde { get; set; }

        public string AmtSign { get; set; }

        public string Amt { get; set; }

        public string Filler { get; set; }

        public override string DataEncode()
        {
            return this.NumAmts + this.AmtTyp + this.CrncyCde + this.AmtSign + this.Amt + this.Filler;
        }

        public override Token DataDecode(string data)
        {
            this.NumAmts = data.Substring(0, 2);
            this.AmtTyp = data.Substring(2, 2);
            this.CrncyCde = data.Substring(4, 3);
            this.AmtSign = data.Substring(7, 1);
            this.Amt = data.Substring(8, 12);
            this.Filler = data.Substring(20, 2);

            return this;
        }
    }
}

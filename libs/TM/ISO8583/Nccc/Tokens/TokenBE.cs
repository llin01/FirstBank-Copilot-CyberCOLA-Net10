// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenBE.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenBE type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenBE : Token
    {
        public TokenBE()
            : base("BE")
        {
            this.Amount = string.Empty.PadRight(19, ' ');
            this.Filler1 = string.Empty.PadRight(19, ' ');
            this.OrigCurrencyCode = string.Empty.PadRight(3, ' ');
            this.ConvRate = string.Empty.PadRight(8, ' ');
            this.ConvDate = string.Empty.PadRight(4, ' ');
            this.ConvInd = string.Empty.PadRight(1, ' ');
            this.Filler2 = string.Empty.PadRight(8, ' ');
        }

        public string Amount { get; set; }
        public string Filler1 { get; set; }
        public string OrigCurrencyCode { get; set; }
        public string ConvRate { get; set; }
        public string ConvDate { get; set; }
        public string ConvInd { get; set; }
        public string Filler2 { get; set; }

        public override string DataEncode()
        {
            return this.Amount + this.Filler1 + OrigCurrencyCode +
                   this.ConvRate + this.ConvDate + ConvInd +
                   this.Filler2;
        }

        public override Token DataDecode(string data)
        {
            this.Amount = data.Substring(0, 19);
            this.Filler1 = data.Substring(19, 19);
            this.OrigCurrencyCode = data.Substring(38, 3);
            this.ConvRate = data.Substring(41, 8);
            this.ConvDate = data.Substring(49, 4);
            this.ConvInd = data.Substring(53, 1);
            this.Filler2 = data.Substring(54, 8);
            return this;
        }
    }
}

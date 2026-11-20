// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenCZ.cs">
//   Author: MTTso
// </copyright>
// <summary>
//   Defines the TokenCZ type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenCZ : Token
    {
        public TokenCZ()
            : base(NcccTransactionConst.PosAdditionalData.TokenCZ.Id)
        {
            this.ATC = string.Empty.PadRight(4, '0');
            this.FormFactorIndicator = string.Empty.PadRight(8, ' ');
            this.ATCDiscrepancyIndicator = string.Empty.PadRight(1, ' ');
            this.ATCDiscrepancyValue = string.Empty.PadRight(5, '0');
        }

        public string ATC { get; set; }

        public string FormFactorIndicator { get; set; }

        public string ATCDiscrepancyIndicator { get; set; }

        public string ATCDiscrepancyValue { get; set; }

        public override string DataEncode()
        {
            return this.ATC + FormFactorIndicator + this.ATCDiscrepancyIndicator + this.ATCDiscrepancyValue;
        }

        public override Token DataDecode(string data)
        {
            this.ATC = data.Substring(0, 4);
            this.ATCDiscrepancyIndicator = data.Substring(12, 1);
            this.ATCDiscrepancyValue = data.Substring(13, 5);
            return this;
        }
    }
}

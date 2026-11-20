// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenS8.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenS8 type.
//   Update:C.F 20180803 
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenS8 : Token
    {
        public TokenS8()
            : base("S8")
        {
            this.AcctNumInd = string.Empty.PadRight(1, ' ');
            this.AcctNum = string.Empty.PadRight(19, ' ');
            this.ExpDat = string.Empty.PadRight(4, ' ');
            this.AcVeryRslt = string.Empty.PadRight(2, ' ');
            this.FraudChkRslt = string.Empty.PadRight(2, ' ');
            this.Filler = string.Empty.PadRight(2, ' ');
        }

        public string AcctNumInd { get; set; }

        public string AcctNum { get; set; }

        public string ExpDat { get; set; }

        public string AcVeryRslt { get; set; }

        public string FraudChkRslt { get; set; }

        public string Filler { get; set; }

        public override string DataEncode()
        {
            return this.AcctNumInd + this.AcctNum + this.ExpDat + this.AcVeryRslt + this.FraudChkRslt + this.Filler;
        }

        public override Token DataDecode(string data)
        {
            this.AcctNumInd = data.Substring(0, 1);
            this.AcctNum = data.Substring(1, 19);
            this.ExpDat = data.Substring(20, 4);
            this.AcVeryRslt = data.Substring(24, 2);
            this.FraudChkRslt = data.Substring(26, 2);
            this.Filler = data.Substring(28, 2);

            return this;
        }
    }
}

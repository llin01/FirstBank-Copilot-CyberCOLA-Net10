// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenCH.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenCH type.
//   modified:C.F  20190717
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenCH : Token
    {
        public TokenCH()
            : base(NcccTransactionConst.PosAdditionalData.TokenCH.Id)
        {
            this.RespSrcRsnCde = string.Empty.PadRight(1, ' ');
            this.Filler1 = string.Empty.PadRight(18, ' ');
            this.RecurPmntInd = string.Empty.PadRight(1, ' ');
            this.Filler2 = string.Empty.PadRight(4, ' ');
            this.PmntTypInd = string.Empty.PadRight(3, ' ');
            this.Filler3 = string.Empty.PadRight(1, ' ');
            this.CrdhldrAuthnCapInd = string.Empty.PadRight(1, ' ');
            this.Filler4 = string.Empty.PadRight(4, ' ');
            this.RvslRsnInd = string.Empty.PadRight(1, ' ');
            this.Filler5 = string.Empty.PadRight(2, ' ');
            this.AuthMsgInd = string.Empty.PadRight(1, ' ');
            this.TermTyp = string.Empty.PadRight(1, ' ');
            this.Filler7 = string.Empty.PadRight(2, ' ');
        }

        public string RespSrcRsnCde;
        public string Filler1 { get; set; }
        public string RecurPmntInd { get; set; }
        public string Filler2 { get; set; }
        public string PmntTypInd { get; set; }
        public string Filler3 { get; set; }
        public string CrdhldrAuthnCapInd { get; set; }
        public string Filler4 { get; set; }
        public string RvslRsnInd { get; set; }
        public string Filler5 { get; set; }
        public string AuthMsgInd { get; set; }
        public string TermTyp { get; set; }
        public string Filler7 { get; set; }

        public override string DataEncode()
        {
            return this.RespSrcRsnCde + this.Filler1 + this.RecurPmntInd + this.Filler2 +
                   this.PmntTypInd + this.Filler3 + this.CrdhldrAuthnCapInd + this.Filler4 +
                   this.RvslRsnInd + this.Filler5 + this.AuthMsgInd + this.TermTyp + this.Filler7;
        }

        public override Token DataDecode(string data)
        {
            this.RespSrcRsnCde = data.Substring(0, 1);
            this.Filler1 = data.Substring(1, 18);
            this.RecurPmntInd = data.Substring(19, 1);
            this.Filler2 = data.Substring(20, 4);
            this.PmntTypInd = data.Substring(24, 3);
            this.Filler3 = data.Substring(27, 1);
            this.CrdhldrAuthnCapInd = data.Substring(28, 1);
            this.Filler4 = data.Substring(29, 4);
            this.RvslRsnInd = data.Substring(33, 1);
            this.Filler5 = data.Substring(34, 2);
            this.AuthMsgInd = data.Substring(36, 1);
            this.TermTyp = data.Substring(37, 1);
            this.Filler7 = data.Substring(38, 2);
            return this;
        }
    }
}

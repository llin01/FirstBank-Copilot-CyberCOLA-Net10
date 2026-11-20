// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Token04.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Token04 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class Token04 : Token
    {
        public Token04()
            : base("04")
        {
            this.ErrorFlg = string.Empty.PadRight(1, ' ');
            this.CrdVeryFlg = string.Empty.PadRight(1, ' ');
            this.CompleteTk2Data = string.Empty.PadRight(1, ' ');
        }

        public string ErrorFlg { get; set; }

        public string CrdVeryFlg { get; set; }

        public string CompleteTk2Data { get; set; }

        public override string DataEncode()
        {
            return this.ErrorFlg + " ".PadLeft(11, ' ') + this.CrdVeryFlg + " ".PadLeft(5, ' ') + this.CompleteTk2Data
                   + " ";
        }

        public override Token DataDecode(string data)
        {
            this.ErrorFlg = data.Substring(0, 1);
            this.CrdVeryFlg = data.Substring(12, 1);
            this.CompleteTk2Data = data.Substring(18, 1);
            return this;
        }
    }
}

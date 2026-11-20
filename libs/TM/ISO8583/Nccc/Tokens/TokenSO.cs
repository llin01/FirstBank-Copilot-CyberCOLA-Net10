// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenSO.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenSO type.
//   Update:C.F 20180903 
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenSO : Token
    {
        public TokenSO()
            : base("SO")
        {
        }

        public string PayAcctRef { get; set; }

        public string Filler { get; set; }

        public override string DataEncode()
        {
            return this.PayAcctRef + this.Filler;
        }

        public override Token DataDecode(string data)
        {
            this.PayAcctRef = data.Substring(0, 29);
            this.Filler = data.Substring(29, 11);

            return this;
        }
    }
}

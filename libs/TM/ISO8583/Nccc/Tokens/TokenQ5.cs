// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenQ5.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenQ5 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenQ5 : Token
    {
        public TokenQ5()
            : base("Q5")
        {
        }

        public string CardProdIdenInfo { get; set; }

        public override string DataEncode()
        {
            return this.CardProdIdenInfo;
        }

        public override Token DataDecode(string data)
        {
            this.CardProdIdenInfo = data;
            return this;
        }
    }
}

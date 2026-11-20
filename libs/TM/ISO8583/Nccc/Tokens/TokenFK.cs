// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenFK.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenFK type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenFK : Token
    {
        public TokenFK()
            : base("FK")
        {
        }

        public string DirectoryServerTransactionId { get; set; }

        public string Filler { get; set; }

        public override string DataEncode()
        {
            return this.DirectoryServerTransactionId + this.Filler;
        }

        public override Token DataDecode(string data)
        {
            this.DirectoryServerTransactionId = data.Substring(0, 36);
            this.Filler = data.Substring(36, 44);
            return this;
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Token17.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Token17 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class Token17 : Token
    {
        public Token17()
            : base("17")
        {
        }

        public string TranId { get; set; }

        public override string DataEncode()
        {
            return "0" + this.TranId + "        ";
        }

        public override Token DataDecode(string data)
        {
            this.TranId = data.Substring(1, 15);
            return this;
        }
    }
}

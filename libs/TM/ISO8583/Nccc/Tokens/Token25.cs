// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Token25.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Token25 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class Token25 : Token
    {
        public Token25()
            : base("25")
        {
        }

        public string TranFee { get; set; }

        public string OrigFee { get; set; }

        public override string DataEncode()
        {
            return this.TranFee + this.OrigFee + "                                ";
        }

        public override Token DataDecode(string data)
        {
            this.TranFee = data.Substring(0, 19);
            this.OrigFee = data.Substring(19, 19);
            return this;
        }
    }
}

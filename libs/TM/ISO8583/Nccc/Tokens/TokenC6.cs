// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenC6.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenC6 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenC6 : Token
    {
        public TokenC6()
            : base("C6")
        {
        }

        public string Xid { get; set; }

        public string TransStainCavv { get; set; }

        public override string DataEncode()
        {
            return this.Xid + this.TransStainCavv;
        }

        public override Token DataDecode(string data)
        {
            this.Xid = data.Substring(0, 40);
            this.TransStainCavv = data.Substring(40, 40);
            return this;
        }
    }
}

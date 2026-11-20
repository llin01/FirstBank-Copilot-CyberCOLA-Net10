// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Token20.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Token20 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class Token20 : Token
    {
        public Token20()
            : base("20")
        {
        }

        public string TraceId { get; set; }

        public override string DataEncode()
        {
            return " " + this.TraceId + "      ";
        }

        public override Token DataDecode(string data)
        {
            this.TraceId = data.Substring(1, 15);
            return this;
        }
    }
}

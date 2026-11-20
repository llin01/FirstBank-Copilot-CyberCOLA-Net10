// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenBJ.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenBJ type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenBJ : Token
    {
        public TokenBJ()
            : base("BJ")
        {
        }

        public string NumIssScriptRslts { get; set; }

        public string IssScriptRsltsData { get; set; }

        public override string DataEncode()
        {
            return this.NumIssScriptRslts + " " + this.IssScriptRsltsData;
        }

        public override Token DataDecode(string data)
        {
            this.NumIssScriptRslts = data.Substring(0, 1);
            this.IssScriptRsltsData = data.Substring(2);
            return this;
        }
    }
}

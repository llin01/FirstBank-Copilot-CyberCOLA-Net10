// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenB6.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenB6 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenB6 : Token
    {
        public TokenB6()
            : base(NcccTransactionConst.PosAdditionalData.TokenB6.Id)
        {
        }

        public string IssScriptData { get; set; }

        public override string DataEncode()
        {
            return this.IssScriptData.Length.ToString().PadLeft(4, '0') + this.IssScriptData;
        }

        public override Token DataDecode(string data)
        {
            this.IssScriptData = data.Substring(4);
            return this;
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenF4.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenF4 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenF4 : Token
    {
        public TokenF4()
            : base("F4")
        {
        }

        public string WalletIndFlg { get; set; }

        public string WalletIndData { get; set; }

        public override string DataEncode()
        {
            return this.WalletIndFlg + this.WalletIndData + string.Empty.PadLeft(9, ' ');
        }

        public override Token DataDecode(string data)
        {
            this.WalletIndFlg = data.Substring(0, 2);
            this.WalletIndData = data.Substring(2, 3);
            return this;
        }
    }
}

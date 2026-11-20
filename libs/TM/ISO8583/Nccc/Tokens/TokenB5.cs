// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenB5.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenB5 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenB5 : Token
    {
        public TokenB5()
            : base(NcccTransactionConst.PosAdditionalData.TokenB5.Id)
        {
            this.IssAuthDataLgth = string.Empty.PadRight(4, ' ');
            this.Arpc = string.Empty.PadRight(16, ' ');
            this.IssRespCde = string.Empty.PadRight(4, ' ');
            this.Info = string.Empty.PadRight(12, ' ');
            this.UserFld1 = string.Empty.PadRight(6, ' ');
        }

        public string IssAuthDataLgth { get; set; }

        public string Arpc { get; set; }

        public string IssRespCde { get; set; }

        public string Info { get; set; }

        public string UserFld1 { get; set; }

        public override string DataEncode()
        {
            return this.IssAuthDataLgth + this.Arpc + this.IssRespCde + this.Info + this.UserFld1;
        }

        public override Token DataDecode(string data)
        {
            this.IssAuthDataLgth = data.Substring(0, 4);
            this.Arpc = data.Substring(4, 16);
            this.IssRespCde = data.Substring(20, 4);
            this.Info = data.Substring(24, 12);
            this.UserFld1 = data.Substring(36, 6);
            return this;
        }
    }
}

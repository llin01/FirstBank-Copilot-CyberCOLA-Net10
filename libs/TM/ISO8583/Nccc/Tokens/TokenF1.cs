// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenF1.cs">
//   Author: MTTso
// </copyright>
// <summary>
//   Defines the TokenF1 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenF1 : Token
    {
        public TokenF1()
            : base(NcccTransactionConst.PosAdditionalData.TokenF1.Id)
        {
            this.AssignedId = string.Empty.PadRight(15, ' ');
            this.CardholderAuthenticationIndicator = string.Empty.PadRight(1, ' ');
            this.UserFld1 = string.Empty.PadRight(14, ' ');
        }

        public string AssignedId { get; set; }

        public string CardholderAuthenticationIndicator { get; set; }

        public string UserFld1 { get; set; }

        public override string DataEncode()
        {
            return this.AssignedId + CardholderAuthenticationIndicator + this.UserFld1;
        }

        public override Token DataDecode(string data)
        {
            this.AssignedId = data.Substring(0, 15);
            this.CardholderAuthenticationIndicator = data.Substring(15, 1);
            this.UserFld1 = data.Substring(16, 14);
            return this;
        }
    }
}

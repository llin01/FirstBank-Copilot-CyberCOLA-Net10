// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenU5.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenU5 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenU5 : Token
    {
        public TokenU5()
            : base("U5")
        {
        }

        public string ProcessingCode { get; set; }

        public string ResponseCode { get; set; }

        public string IdentityAuthorization { get; set; }

        public string CardholderId { get; set; }

        public string CardholderCellPhone { get; set; }

        public string CardholderBirthYear { get; set; }

        public string CardholderBirthMonthDate { get; set; }

        public string CardholderHomePhone { get; set; }

        public string CardholderCompanyPhone { get; set; }

        public string PaidPlatForm { get; set; }

        public string U5Filler { get; set; }

        public override string DataEncode()
        {
            return this.ProcessingCode + this.ResponseCode + this.IdentityAuthorization + this.CardholderId
                   + this.CardholderCellPhone + this.CardholderBirthYear + this.CardholderBirthMonthDate
                   + this.CardholderHomePhone + this.CardholderCompanyPhone + this.PaidPlatForm + this.U5Filler;
        }

        public override Token DataDecode(string data)
        {
            this.ProcessingCode = data.Substring(0, 10);
            this.ResponseCode = data.Substring(10, 2);
            this.IdentityAuthorization = data.Substring(12, 1);
            this.CardholderId = data.Substring(13, 11);
            this.CardholderCellPhone = data.Substring(24, 15);
            this.CardholderBirthYear = data.Substring(39, 4);
            this.CardholderBirthMonthDate = data.Substring(43, 4);
            this.CardholderHomePhone = data.Substring(47, 15);
            this.CardholderCompanyPhone = data.Substring(62, 15);
            this.PaidPlatForm = data.Substring(77, 1);
            this.U5Filler = data.Substring(78, 42);
            return this;
        }
    }
}

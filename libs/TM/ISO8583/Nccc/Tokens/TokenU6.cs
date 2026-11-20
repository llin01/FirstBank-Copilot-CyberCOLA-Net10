// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenU6.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenU6 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenU6 : Token
    {
        public TokenU6()
            : base("U6")
        {
        }

        public string CardNo { get; set; }

        public string EMail { get; set; }

        public string MobilePhone { get; set; }

        public string ProvisionColor { get; set; }

        public string Otp { get; set; }

        public string OtpFlag { get; set; }

        public string U6Filler { get; set; }

        public override string DataEncode()
        {
            return this.CardNo + this.EMail + this.MobilePhone + this.ProvisionColor + this.Otp + this.OtpFlag
                   + this.U6Filler;
        }

        public override Token DataDecode(string data)
        {
            this.CardNo = data.Substring(0, 19);
            this.EMail = data.Substring(19, 30);
            this.MobilePhone = data.Substring(49, 15);
            this.ProvisionColor = data.Substring(64, 2);
            this.Otp = data.Substring(66, 16);
            this.OtpFlag = data.Substring(82, 1);
            this.U6Filler = data.Substring(83, 17);
            return this;
        }
    }
}

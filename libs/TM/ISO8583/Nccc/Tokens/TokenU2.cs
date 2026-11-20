// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenU2.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenU2 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenU2 : Token
    {
        public TokenU2()
            : base("U2")
        {
        }

        public string BancsResp { get; set; }

        public string CardNo { get; set; }

        public string BlockFlag { get; set; }

        public string AuthCode { get; set; }

        public string AccntAbleBalance { get; set; }

        public string AbleBalance { get; set; }

        public string NcpsFlag { get; set; }

        public override string DataEncode()
        {
            return this.BancsResp + this.CardNo + this.BlockFlag + this.AuthCode + this.AccntAbleBalance
                   + this.AbleBalance + this.NcpsFlag;
        }

        public override Token DataDecode(string data)
        {
            this.BancsResp = data.Substring(0, 4);
            this.CardNo = data.Substring(4, 16);
            this.BlockFlag = data.Substring(20, 1);
            this.AuthCode = data.Substring(21, 6);
            this.AccntAbleBalance = data.Substring(27, 11);
            this.AbleBalance = data.Substring(38, 11);
            this.NcpsFlag = data.Substring(49, 1);
            return this;
        }
    }
}

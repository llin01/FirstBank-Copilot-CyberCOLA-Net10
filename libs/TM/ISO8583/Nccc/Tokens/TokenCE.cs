// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenCE.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenCE type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    #region DEPENDENCY

    using System;

    #endregion

    public class TokenCE : Token
    {
        public TokenCE()
            : base(NcccTransactionConst.PosAdditionalData.TokenCE.Id)
        {
            this.AuthIndFlg = string.Empty.PadRight(2, ' ');
            this.AuthIndData = string.Empty.PadRight(200, ' ');
        }

        public string AuthIndFlg { get; set; }

        public string AuthIndData { get; set; }

        public string PinVerification { get; set; }

        public string CardExpireDateVerification { get; set; }

        public string IdentificationCardVerification { get; set; }

        public string Cvn2Verification { get; set; }

        public string NameVerification { get; set; }

        public string MobilePhoneVerification { get; set; }

        public string DynamicVerificationCodeVerification { get; set; }

        public string OperCustData { get; set; }

        public override string DataEncode()
        {
            if (this.AuthIndFlg.Equals(string.Empty.PadRight(2, ' ')) ||
                this.AuthIndFlg.Equals("04"))
            {
                return this.AuthIndFlg + this.AuthIndData;
            }

            return this.AuthIndFlg
                   + (this.AuthIndFlg.Equals("01")
                          ? this.AuthIndData
                          : this.PinVerification + this.CardExpireDateVerification + this.IdentificationCardVerification
                            + "00" + this.Cvn2Verification + "0" + this.NameVerification + this.MobilePhoneVerification
                            + this.DynamicVerificationCodeVerification + "000000"
                            + this.OperCustData.Length.ToString().PadLeft(3, '0') + this.OperCustData
                            + (this.OperCustData.Length % 2 == 0 ? " " : string.Empty));
        }

        public override Token DataDecode(string data)
        {
            this.AuthIndFlg = data.Substring(0, 2);
            if (this.AuthIndFlg.Equals("01") ||
                this.AuthIndFlg.Equals("04") ||
                this.AuthIndFlg.Equals(string.Empty.PadRight(2, ' ')))
            {
                this.AuthIndData = data.Substring(2, (data.Length - 2));
                return this;
            }

            this.PinVerification = data.Substring(2, 1);
            this.CardExpireDateVerification = data.Substring(3, 1);
            this.IdentificationCardVerification = data.Substring(4, 1);
            this.Cvn2Verification = data.Substring(7, 1);
            this.NameVerification = data.Substring(9, 1);
            this.MobilePhoneVerification = data.Substring(10, 1);
            this.DynamicVerificationCodeVerification = data.Substring(11, 1);
            this.PinVerification = data.Substring(21, Convert.ToUInt16(data.Substring(18, 3)));
            return this;
        }
    }
}

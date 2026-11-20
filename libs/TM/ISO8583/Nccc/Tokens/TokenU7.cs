// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenU7.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenU7 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenU7 : Token
    {
        public TokenU7()
            : base(NcccTransactionConst.PosAdditionalData.TokenU7.Id)
        {
            this.WalletProviderRiskAssessment = string.Empty.PadRight(1, ' ');
            this.WalletProviderId = string.Empty.PadRight(11, ' ');
            this.ChipTransactionIndicator = string.Empty.PadRight(1, ' ');
            this.ActivationMethod = string.Empty.PadRight(60, ' ');
            this.Otp = string.Empty.PadRight(16, ' ');
            this.OriginalDe3TransactionType = string.Empty.PadRight(2, ' ');
            this.OriginalMti = string.Empty.PadRight(4, ' ');
            this.FinancialNetworkCode = string.Empty.PadRight(3, ' ');
            this.BanknetReferenceNumber = string.Empty.PadRight(9, ' ');
        }

        public string WalletProviderRiskAssessment { get; set; }

        public string WalletProviderId { get; set; }

        public string ChipTransactionIndicator { get; set; }

        public string ActivationMethod { get; set; }

        public string Otp { get; set; }

        public string OriginalDe3TransactionType { get; set; }

        public string OriginalMti { get; set; }

        public string FinancialNetworkCode { get; set; }

        public string BanknetReferenceNumber { get; set; }

        public override string DataEncode()
        {
            //return this.WalletProviderRiskAssessment + this.Otp + this.ActivationMethod + this.WalletProviderId
            //       + this.FinancialNetworkCode + this.BanknetReferenceNumber + string.Empty.PadLeft(38, ' ')
            //       + this.OriginalDe3TransactionType + this.OriginalMti + this.ChipTransactionIndicator;

            // for 20170302 for NCPS changed 

            string data = this.WalletProviderRiskAssessment + this.WalletProviderId + this.ChipTransactionIndicator
                        + this.ActivationMethod + this.Otp + this.OriginalDe3TransactionType + this.OriginalMti
                        + this.FinancialNetworkCode + this.BanknetReferenceNumber;

            return PadbyField(data);
        }

        public override Token DataDecode(string data)
        {
            //this.WalletProviderRiskAssessment = data.Substring(0, 1);
            //this.Otp = data.Substring(1, 16);
            //this.ActivationMethod = data.Substring(17, 60);
            //this.WalletProviderId = data.Substring(77, 11);
            //this.FinancialNetworkCode = data.Substring(88, 3);
            //this.BanknetReferenceNumber = data.Substring(91, 9);
            //this.OriginalDe3TransactionType = data.Substring(138, 2);
            //this.OriginalMti = data.Substring(140, 4);
            //this.ChipTransactionIndicator = data.Substring(144, 1);

            // for 20170302 for NCPS changed
            data = PadbyField(data);

            try
            {
                this.WalletProviderRiskAssessment = data.Substring(0, 1);
                this.WalletProviderId = data.Substring(1, 11);
                this.ChipTransactionIndicator = data.Substring(12, 1);
                this.ActivationMethod = data.Substring(13, 60);
                this.Otp = data.Substring(73, 16);
                this.OriginalDe3TransactionType = data.Substring(89, 2);
                this.OriginalMti = data.Substring(91, 4);
                this.FinancialNetworkCode = data.Substring(95, 3);
                this.BanknetReferenceNumber = data.Substring(98, 9);
            }
            catch (System.Exception)
            {
                // Ignore
            }
            return this;
        }

        public string PadbyField(string data)
        {
            //if (data.Length > 98)
            //{
            //    data = data.PadRight(107, ' ');
            //}
            //else if (data.Length > 95)
            //{
            //    data = data.PadRight(98, ' ');
            //}
            //else if (data.Length > 91)
            //{
            //    data = data.PadRight(95, ' ');
            //}
            //else if (data.Length > 89)
            //{
            //    data = data.PadRight(91, ' ');
            //}
            //else if (data.Length > 73)
            //{
            //    data = data.PadRight(89, ' ');
            //}
            //else if (data.Length > 13)
            //{
            //    data = data.PadRight(73, ' ');
            //}
            //else if (data.Length > 12)
            //{
            //    data = data.PadRight(13, ' ');
            //}
            //else if (data.Length > 1)
            //{
            //    data = data.PadRight(12, ' ');
            //}

            // for 20180608 changed for fix length 107; 
            return data.PadRight(107, ' ');
        }
    }
}

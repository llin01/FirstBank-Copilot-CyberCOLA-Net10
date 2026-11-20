// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenNCCCInstallmentC5.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenNCCCInstallmentC5 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenNCCCInstallmentC5 : Token
    {
        public TokenNCCCInstallmentC5()
            : base("C5")
        {
        }

        public string MerchantId { get; set; }

        public string StoreId { get; set; }

        public string TransactionId { get; set; }

        public string ReferenceId { get; set; }

        public string GoodsId { get; set; }

        public string Personal { get; set; }

        public string Pin { get; set; }

        public string EcFlag { get; set; }

        public string OrderNum { get; set; }

        public string InstallmentTxInd { get; set; }

        public string InstallmentTxnRespCde { get; set; }

        public string PeriodNumber { get; set; }

        public string DownPayment { get; set; }

        public string InstallmentPayments { get; set; }

        public string FormalityFee { get; set; }

        public string PointOfRedemption { get; set; }

        public string SignPointOfBalance { get; set; }

        public string PointOfBalance { get; set; }

        public string PaidCreditAmount { get; set; }

        public string OnusProductId { get; set; }

        public string OnusOrderNo { get; set; }

        public string OnusPeriodNumber { get; set; }

        public string OnusDownPayment { get; set; }

        public string OnusInstallmentPayments { get; set; }

        public string OnusFormalityFee { get; set; }

        public string OnusTxnVol { get; set; }

        public string OnusTxnType { get; set; }

        public string OnusResp { get; set; }

        public string OnusDownPaymentFee { get; set; }

        public string OnusInstallmentPaymentsFee { get; set; }


        public override string DataEncode()
        {
            if (InstallmentTxInd == "E" || InstallmentTxInd == "I")
            {
                return this.MerchantId + this.StoreId + this.TransactionId + this.ReferenceId + this.GoodsId + this.Personal
                       + this.Pin + this.EcFlag + this.OrderNum + this.InstallmentTxInd + this.InstallmentTxnRespCde
                       + string.Empty.PadLeft(27, ' ') + this.PeriodNumber + this.DownPayment + this.InstallmentPayments
                       + this.FormalityFee;
            }
            else
            {
                return this.MerchantId + this.StoreId + this.TransactionId + this.ReferenceId + this.GoodsId + this.Personal
                       + this.Pin + this.EcFlag + this.OrderNum + this.InstallmentTxInd
                       + this.PointOfRedemption + this.SignPointOfBalance
                       + this.PointOfBalance + this.PaidCreditAmount;
            }
        }

        public override Token DataDecode(string data)
        {
            this.MerchantId = data.Substring(0, 10);
            this.StoreId = data.Substring(10, 6);
            this.TransactionId = data.Substring(16, 2);
            this.ReferenceId = data.Substring(18, 11);
            this.GoodsId = data.Substring(29, 6);
            this.Personal = data.Substring(35, 10);
            this.Pin = data.Substring(45, 4);
            this.EcFlag = data.Substring(49, 1);
            this.OrderNum = data.Substring(50, 40);
            this.InstallmentTxInd = data.Substring(90, 1);
            if (InstallmentTxInd == "E" || InstallmentTxInd == "I")
            {
                try
                {
                    this.InstallmentTxnRespCde = data.Substring(91, 2);
                    this.PeriodNumber = data.Substring(120, 2);
                    this.DownPayment = data.Substring(122, 8);
                    this.InstallmentPayments = data.Substring(130, 8);
                    this.FormalityFee = data.Substring(138, 6);
                }
                catch (System.Exception)
                {
                    // Ignore
                }
            }
            else
            {
                try
                {
                    this.InstallmentTxnRespCde = data.Substring(91, 2);
                    this.PointOfRedemption = data.Substring(93, 8);
                    this.SignPointOfBalance = data.Substring(101, 1);
                    this.PointOfBalance = data.Substring(102, 8);
                    this.PaidCreditAmount = data.Substring(110, 10);
                }
                catch (System.Exception)
                {
                    // Ignore
                }
            }
            return this;
        }
    }
}

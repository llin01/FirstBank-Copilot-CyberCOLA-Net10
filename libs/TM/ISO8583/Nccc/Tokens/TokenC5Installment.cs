// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenC5Installment.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenC5Installment type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenC5Installment : Token
    {
        public TokenC5Installment()
            : base(NcccTransactionConst.PosAdditionalData.TokenC5.Id)
        {
            this.MerchantId = string.Empty.PadRight(10, ' ');
            this.StoreId = string.Empty.PadRight(6, ' ');
            this.TransactionId = string.Empty.PadRight(2, ' ');
            this.ReferenceId = string.Empty.PadRight(11, ' ');
            this.GoodsId = string.Empty.PadRight(6, ' ');
            this.Personal = string.Empty.PadRight(10, ' ');
            this.PIN = string.Empty.PadRight(4, ' ');
            this.ECFlag = string.Empty.PadRight(1, ' ');
            this.OrderNum = string.Empty.PadRight(40, ' ');
            this.InstallmentTxInd = string.Empty.PadRight(1, ' ');
            this.InstallmentTxnRespCode = string.Empty.PadRight(2, ' ');
            this.Filler = string.Empty.PadRight(27, ' ');
            this.PeriodNumber = string.Empty.PadRight(2, ' ');
            this.DownPayment = string.Empty.PadRight(8, ' ');
            this.InstallmentPayments = string.Empty.PadRight(8, ' ');
            this.FormalityFee = string.Empty.PadRight(6, ' ');
        }

        public string MerchantId { get; set; }

        public string StoreId { get; set; }

        public string TransactionId { get; set; }

        public string ReferenceId { get; set; }

        public string GoodsId { get; set; }

        public string Personal { get; set; }

        public string PIN { get; set; }

        public string ECFlag { get; set; }

        public string OrderNum { get; set; }

        public string InstallmentTxInd { get; set; }

        public string InstallmentTxnRespCode { get; set; }

        public string Filler { get; set; }

        public string PeriodNumber { get; set; }

        public string DownPayment { get; set; }

        public string InstallmentPayments { get; set; }

        public string FormalityFee { get; set; }

        public override string DataEncode()
        {
            return this.MerchantId + this.StoreId + this.TransactionId + this.ReferenceId + this.GoodsId
                   + this.Personal + this.PIN + this.ECFlag + this.OrderNum + this.InstallmentTxInd
                   + this.InstallmentTxnRespCode + this.Filler + this.PeriodNumber + this.DownPayment
                   + this.InstallmentPayments + this.FormalityFee;
        }

        public override Token DataDecode(string data)
        {
            this.MerchantId = data.Substring(0, 10);
            this.StoreId = data.Substring(10, 6);
            this.TransactionId = data.Substring(16, 2);
            this.ReferenceId = data.Substring(18, 11);
            this.GoodsId = data.Substring(29, 6);
            this.Personal = data.Substring(35, 10);
            this.PIN = data.Substring(45, 4);
            this.ECFlag = data.Substring(49, 1);
            this.OrderNum = data.Substring(50, 40);
            this.InstallmentTxInd = data.Substring(90, 1);
            this.InstallmentTxnRespCode = data.Substring(91, 2);
            this.Filler = data.Substring(93, 27);
            this.PeriodNumber = data.Substring(120, 2);
            this.DownPayment = data.Substring(122, 8);
            this.InstallmentPayments = data.Substring(130, 8);
            this.FormalityFee = data.Substring(138, 6);
            return this;
        }
    }
}

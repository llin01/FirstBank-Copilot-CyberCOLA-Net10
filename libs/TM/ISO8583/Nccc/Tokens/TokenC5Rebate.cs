// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenC5Rebate.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenC5Rebate type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenC5Rebate : Token
    {
        public TokenC5Rebate()
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
            this.SettlementFlag = string.Empty.PadRight(2, ' ');
            this.OrderNum = string.Empty.PadRight(38, ' ');
            this.LoyaltyInd = string.Empty.PadRight(1, ' ');
            this.LoyaltyTxRespCode = string.Empty.PadRight(2, ' ');
            this.PointsOfRedemption = string.Empty.PadRight(8, ' ');
            this.SignPointOfBalance = string.Empty.PadRight(1, ' ');
            this.PointOfBalance = string.Empty.PadRight(8, ' ');
            this.PaidCreditAmount = string.Empty.PadRight(10, ' ');
        }

        public string MerchantId { get; set; }

        public string StoreId { get; set; }

        public string TransactionId { get; set; }

        public string ReferenceId { get; set; }

        public string GoodsId { get; set; }

        public string Personal { get; set; }

        public string PIN { get; set; }

        public string ECFlag { get; set; }

        public string SettlementFlag { get; set; }

        public string OrderNum { get; set; }

        public string LoyaltyInd { get; set; }

        public string LoyaltyTxRespCode { get; set; }

        public string PointsOfRedemption { get; set; }

        public string SignPointOfBalance { get; set; }

        public string PointOfBalance { get; set; }

        public string PaidCreditAmount { get; set; }


        public override string DataEncode()
        {
            return this.MerchantId + this.StoreId + this.TransactionId + this.ReferenceId + this.GoodsId
                   + this.Personal + this.PIN + this.ECFlag + this.SettlementFlag + this.OrderNum
                   + this.LoyaltyInd + this.LoyaltyTxRespCode + this.PointsOfRedemption
                   + this.SignPointOfBalance + this.PointOfBalance + this.PaidCreditAmount;
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
            this.SettlementFlag = data.Substring(50, 2);
            this.OrderNum = data.Substring(52, 38);
            this.LoyaltyInd = data.Substring(90, 1);
            this.LoyaltyTxRespCode = data.Substring(91, 2);
            this.PointsOfRedemption = data.Substring(93, 8);
            this.SignPointOfBalance = data.Substring(101, 1);
            this.PointOfBalance = data.Substring(102, 8);
            this.PaidCreditAmount = data.Substring(110, 10);
            return this;
        }
    }
}

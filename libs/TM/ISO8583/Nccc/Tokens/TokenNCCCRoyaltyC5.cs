// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenNCCCRoyaltyC5.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenNCCCRoyaltyC5 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenNCCCRoyaltyC5 : Token
    {
        public TokenNCCCRoyaltyC5()
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

        public string SettlementFlag { get; set; }

        public string OrderNum { get; set; }

        public string LoyaltyTxInd { get; set; }

        public string LoyaltyTxnRespCde { get; set; }

        public string PointsOfRedemption { get; set; }

        public string SignPointOfBalance { get; set; }

        public string PointOfBalance { get; set; }

        public string PaidCreditAmount { get; set; }

        public override string DataEncode()
        {
            return this.MerchantId + this.StoreId + this.TransactionId + this.ReferenceId + this.GoodsId + this.Personal
                   + this.Pin + this.EcFlag + this.SettlementFlag + this.OrderNum + this.LoyaltyTxInd
                   + this.LoyaltyTxnRespCde + this.PointsOfRedemption + this.SignPointOfBalance + this.PointOfBalance
                   + this.PaidCreditAmount;
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
            this.SettlementFlag = data.Substring(50, 2);
            this.OrderNum = data.Substring(52, 38);
            this.LoyaltyTxInd = data.Substring(90, 1);
            this.LoyaltyTxnRespCde = data.Substring(91, 2);
            this.PointsOfRedemption = data.Substring(93, 8);
            this.SignPointOfBalance = data.Substring(101, 1);
            this.PointOfBalance = data.Substring(102, 8);
            this.PaidCreditAmount = data.Substring(110, 10);
            return this;
        }
    }
}

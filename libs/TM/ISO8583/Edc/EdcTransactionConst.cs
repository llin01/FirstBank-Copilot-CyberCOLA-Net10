namespace Com.Cybersoft4u.TM.ISO8583.Edc
{
    public class EdcTransactionConst
    {
        public class ProcessingCode
        {
            // Normal
            public static string Sale = "000000";

            public static string Refund = "200000";

            public static string VoidSale = "020000";

            public static string PreAuth = "300000";

            public static string PreAuthComplete = "400000";

            // Reversal
            public static string ReversalSale = "000000";

            public static string ReversalRefund = "200000";

            public static string ReversalPreAuth = "300000";

            public static string ReversalPreAuthComplete = "400000";

            // Other
            public static string TcUpload = "250000";
        }

        public class MTI
        {
            // Normal
            public static string SaleRequest = "0200";

            public static string SaleResponse = "0210";

            public static string RefundRequest = "0200";

            public static string RefundResponse = "0210";

            public static string VoidSaleRequest = "0200";

            public static string VoidSaleResponse = "0210";

            public static string PreAuthRequest = "0100";

            public static string PreAuthResponse = "0110";

            public static string PreAuthCompleteRequest = "0220";

            public static string PreAuthCompleteResponse = "0230";

            // Reversal
            public static string ReversalSaleRequest = "0400";

            public static string ReversalSaleResponse = "0410";

            public static string ReversalRefundRequest = "0400";

            public static string ReversalRefundResponse = "0410";

            public static string ReversalPreAuthRequest = "0400";

            public static string ReversalPreAuthResponse = "0410";

            public static string ReversalPreAuthCompleteRequest = "0400";

            public static string ReversalPreAuthCompleteResponse = "0410";

            // Other
            public static string TcUploadRequest = "0220";

            public static string TcUploadResponse = "0230";
        }

        public class ResponseCode
        {
            public static string Approved = "00";
        }
    }
}

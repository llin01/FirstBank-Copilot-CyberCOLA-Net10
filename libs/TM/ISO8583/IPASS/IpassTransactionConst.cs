namespace Com.Cybersoft4u.TM.ISO8583.IPASS
{
    public class IpassTransactionConst
    {
        public class MessageHeader
        {
            public static string IpassHostID = "9999";
            public static string IpassIssuerID = "0007";
        }

        public class ProcessingCode
        {
            public static string IPASSAutoload = "810000";
            public static string IPASSAuthCheck = "820000";
            public static string IPASSLost = "910000";
            public static string IPASSLostCancel = "920000";
        }

        public class MTI
        {
            // Normal
            public const string AuthorizationRequest = "0100";

            public const string AuthorizationResponse = "0110";

            public const string AuthorizationAdviceRequest = "0120";

            public const string AuthorizationAdviceResponse = "0130";

            public const string FinancialTransactionRequest = "0200";

            public const string FinancialTransactionResponse = "0210";

            // File
            public const string FileUpdateRequest = "0302";

            public const string FileUpdateResponse = "0312";

            // Reversal
            public const string ReversalAdviceRequest = "0420";

            public const string ReversalAdviceResponse = "0430";

            // Network Management

            public const string NetworkManagementRequest = "0800";

            public const string NetworkManagementResponse = "0810";

        }

        public class ResponseCode
        {
            public class AutoloadAuth
            {
                public const string Successful = "00";
                public const string CheckRange = "57";
                public const string NoSupportService = "92";
                public const string Timeout = "91";
            }
            public class FileUpdate
            {
                public const string AcceptAndUpdate = "00";
                public const string UpdateRepeat = "19";
                public const string IssuerCanNotCancel = "93";
            }
        }

        public class NetworkManagementInformationCode
        {
            public const string SignOn = "071";
            public const string SignOff = "072";
            public const string EchoTest = "301";

        }
    }
}

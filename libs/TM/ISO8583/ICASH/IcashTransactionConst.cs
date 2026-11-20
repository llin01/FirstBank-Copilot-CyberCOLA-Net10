namespace Com.Cybersoft4u.TM.ISO8583.ICASH
{
    public class IcashTransactionConst
    {
        public class MessageHeader
        {
            public static string IcashHostID = "8888";
            public static string IcashIssuerID = "0007";
        }

        public class ProcessingCode
        {
            public static string ICASHCheckCardStatus = "990172";
            public static string ICASHTurnOnFeedBack = "990173";
            public static string ICASHAutoAddValue = "990174";
            public static string ICASHStipAuth = "990175";
            public static string ICASHLostFileUpdate = "990176";
            public static string ICASHRejectStip = "990178";
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
                public const string OtherError = "01";
                public const string CardNotIssued = "03";
                public const string CardLost = "04";
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

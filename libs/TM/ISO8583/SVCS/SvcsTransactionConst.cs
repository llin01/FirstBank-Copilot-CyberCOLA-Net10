namespace Com.Cybersoft4u.TM.ISO8583.SVCS
{
    public class SvcsTransactionConst
    {
        public class MessageHeader
        {
            public static string SvcsHostID = "9990";
            public static string SvcsIssuerID = "0074";
        }

        //悠遊聯名卡自動加值			        8209
        //悠遊聯名卡自動加值功能開啟			8103
        //悠遊聯名卡授權狀態查詢			    8129
        //悠遊聯名卡掛失			            8903
        //悠遊聯名卡掛失取消			        8908
        //連線拒授權名單維護			        8909

        //悠遊Debit卡自動加值			        8207
        //悠遊Debit卡自動加值功能開啟			8107
        //悠遊Debit卡授權狀態查詢			    8127
        //悠遊Debit卡掛失			            8913
        //悠遊Debit卡掛失取消			        8918
        public class ProcessingCode
        {
            public static string SVCSAddValue = "820999";
            public static string SVCSAddValueOpen = "810399";
            public static string SVCSAuthStatus = "812999";
            public static string SVCSLost = "890399";
            public static string SVCSLostCancel = "890899";
            public static string SVCSMaintReject = "890999";

            public static string SVCSDebitAddValue = "820799";
            public static string SVCSDebitAddValueOpen = "810799";
            public static string SVCSDebitAuthStatus = "812799";
            public static string SVCSDebitLost = "891399";
            public static string SVCSDebitLostCancel = "891899";
        }

        public class MTI
        {
            // Normal
            public const string AuthorizationRequest = "0100";

            public const string AuthorizationResponse = "0110";

            public const string AuthorizationAdviceRequest = "0120";

            public const string AuthorizationAdviceResponse = "0130";

            // File
            public const string FileUpdateRequest = "0300";

            public const string FileUpdateResponse = "0310";

            public const string FileUpdateAdviceRequest = "0302";

            public const string FileUpdateAdviceResponse = "0312";

            // Reversal
            public const string ReversalAdviceRequest = "0420";

            public const string ReversalAdviceResponse = "0430";

            // Network Management

            public const string NetworkManagementRequest = "0800";

            public const string NetworkManagementResponse = "0810";

        }

        public class ResponseCode
        {
            public class FileUpdate
            {
                public const string AcceptAndUpdate = "00";
                public const string InvalidUpdateCode = "24";
                public const string DeleteNotFound = "25";
                public const string FileReadError = "N3";
                public const string FileAddExisted = "N4";
                public const string FileChangeNotExisted = "N5";
                public const string ConvertFormatError = "27";
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

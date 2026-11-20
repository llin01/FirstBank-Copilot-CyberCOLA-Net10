namespace Com.Cybersoft4u.TM.ISO8583.TWIN
{
    public class TwinTransactionConst
    {
        public class ProcessingCode
        {
            // Normal
            public static string Sale =
                CardholderTransactionTypeCode.Purchase +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;
            public static string Refund =
                CardholderTransactionTypeCode.Refund +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;
            public static string PreAuth =
                CardholderTransactionTypeCode.Purchase +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;
            public static string PreAuthComplete =
                CardholderTransactionTypeCode.Purchase +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;

            // Reversal
            public static string ReversalSale =
                CardholderTransactionTypeCode.Purchase +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;
            public static string ReversalRefund =
                CardholderTransactionTypeCode.RefundAdjustment +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;
            public static string ReversalPreAuth =
                CardholderTransactionTypeCode.Purchase +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;
            public static string ReversalPreAuthComplete =
                CardholderTransactionTypeCode.Purchase +
                CardholderFromAccountTypeCode.CreditAccount +
                CardholderToAccountTypeCode.DefaultAccount;

            public class CardholderTransactionTypeCode
            {
                public static string Purchase = "00";
                public static string CashAdvance = "01";
                public static string DebitAdjustment = "02";
                public static string RecurringBilling = "14";
                public static string Refund = "20";
                public static string RefundAdjustment = "22";
                public static string MailOrder = "80";
                public static string AccountVerification = "81";
            }

            public class CardholderFromAccountTypeCode
            {
                public static string DefaultAccount = "00";
                public static string SavingAccount = "10";
                public static string CreditAccount = "30";
            }

            public class CardholderToAccountTypeCode
            {
                public static string DefaultAccount = "00";
            }
        }

        public class MTI
        {
            // Normal
            public const string SaleRequest = "0200";

            public const string SaleResponse = "0210";

            public const string SaleAdviceRequest = "0220";

            public const string SaleAdviceResponse = "0230";

            public const string RefundRequest = "0220";

            public const string RefundResponse = "0230";

            public const string PreAuthRequest = "0100";

            public const string PreAuthResponse = "0110";

            public const string PreAuthCompleteRequest = "0220";

            public const string PreAuthCompleteResponse = "0230";

            // File
            public const string FileUpdateRequest = "0300";

            public const string FileUpdateResponse = "0310";

            // Reversal
            public const string ReversalRequest = "0400";

            public const string ReversalResponse = "0410";

            public const string ReversalAdviceRequest = "0420";

            public const string ReversalAdviceResponse = "0430";

            public const string ReversalSaleRequest = ReversalAdviceRequest;

            public const string ReversalSaleResponse = ReversalAdviceResponse;

            public const string ReversalRefundRequest = ReversalAdviceRequest;

            public const string ReversalRefundResponse = ReversalAdviceResponse;

            public const string ReversalPreAuthRequest = ReversalAdviceRequest;

            public const string ReversalPreAuthResponse = ReversalAdviceResponse;

            public const string ReversalPreAuthCompleteRequest = ReversalAdviceRequest;

            public const string ReversalPreAuthCompleteResponse = ReversalAdviceResponse;

            // Network Management

            public const string NetworkManagementRequest = "0800";

            public const string NetworkManagementResponse = "0810";

            public const string NetworkManagementAdviceRequest = "0820";
        }

        public class PointOfServiceConditionMode
        {
            public const string Present = "00";
            public const string NotPresent = "01";
            public const string PreAuthPreAuthCmpl = "06";
            public const string MailOrder = "08";
            public const string MasterCardEC = "15";
            public const string VisaAccountVerification = "51";
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

        public class SecurityRelatedControlInformation
        {
            public class KeyType
            {
                public const string PinKey = "00";
            }

            public class KeyDirection
            {
                public const string OutboundKeyOnly = "01";
                public const string InboundKeyOnly = "02";
                public const string CombinedKey = "03";
            }
        }

        public class PosAdditionalData
        {
            public class Token04
            {
                public const string Id = "04";

                public class ErrorFlg
                {
                    public const string CardVerificationValid = " ";
                    public const string CardVerificationFailed = "C";
                    public const string NewAccountInfoAvailable = "1";
                    public const string TryAgainLater = "2";
                    public const string DoNotYryAgain = "3";
                }

                public class CrdVrfyFlg
                {
                    public const string CvdInvalidErrFlgC = "D";
                    public const string SecurityDeviceError = "N";
                    public const string CvdValid = "Y";
                    public const string CvdInvalid = "C";
                    public const string CrdVrfyFlgE = "E";
                    public const string CrdVrfyFlgP = "P";
                }

                public class CompleteTk2Data
                {
                    public const string Completed = "Y";
                    public const string NotCompleted = "N";
                }
            }

            public class Token17
            {
                public const string Id = "17";
            }

            public class Token20
            {
                public const string Id = "20";
            }

            public class TokenB0
            {
                public const string Id = "B0";
            }

            public class TokenB2
            {
                public const string Id = "B2";
            }

            public class TokenB3
            {
                public const string Id = "B3";
            }

            public class TokenB4
            {
                public const string Id = "B4";

                public class LastEmvStat
                {
                    public const string FallbackOrEmv = "1";
                    public const string Others = "0";
                }

                public class DataSuspect
                {
                    public const string CamDataCorrect = "0";
                    public const string CamDataUnreliable = "1";
                }

                public class ArqcVerify
                {
                    public const string NoPerform = "0";
                    public const string Fail = "1";
                    public const string Pass = "2";
                    public const string Space = " ";
                }
            }

            public class TokenB5
            {
                public const string Id = "B5";
            }

            public class TokenB6
            {
                public const string Id = "B6";
            }

            public class TokenC0
            {
                public const string Id = "C0";

                public class MoToEcFlag
                {
                    public const string NotApplicable = "0";
                    public const string PartialShipment = " ";
                    public const string MailPhoneOrder = "1";
                    public const string RecurringTransaction = "2";
                    public const string ChannelEncryption = "5";
                    public const string NonAuthenticated = "7";
                    public const string NonSecureEc = "8";
                }

                public class CvdFldPresent
                {
                    public const string Cvv2NotProvided = "0";
                    public const string Cvv2Present = "1";
                    public const string Cvv2OnCardButIllegible = "2";
                }
            }

            public class TokenC2
            {
                public const string Id = "C2";
            }

            public class TokenC4
            {
                public const string Id = "C4";
            }

            public class TokenC5
            {
                public const string Id = "C5";
            }

            public class TokenC6
            {
                public const string Id = "C6";
            }

            public class TokenCE
            {
                public const string Id = "CE";

                public class AuthIndFlg
                {
                    public const string Ucaf = "01";
                }
            }

            public class TokenCH
            {
                public const string Id = "CH";

                public class RvslRsnInd
                {
                    public const string SuspectFraud = "1";
                }
            }

            public class TokenCZ
            {
                public const string Id = "CZ";
            }

            public class TokenF1
            {
                public const string Id = "F1";
            }
            public class TokenF4
            {
                public const string Id = "F4";
            }

            public class TokenQ8
            {
                public const string Id = "Q8";
            }
            public class TokenQ9
            {
                public const string Id = "Q9";
            }
            public class TokenQA
            {
                public const string Id = "QA";
            }
            public class TokenQR
            {
                public const string Id = "QR";
            }
            public class TokenS8
            {
                public const string Id = "S8";
            }
            public class TokenU1
            {
                public const string Id = "U1";
            }

            public class TokenU2
            {
                public const string Id = "U2";
            }

            public class TokenU3
            {
                public const string Id = "U3";
            }

            public class TokenU4
            {
                public const string Id = "U4";
            }

            public class TokenU5
            {
                public const string Id = "U5";
            }

            public class TokenU6
            {
                public const string Id = "U6";
            }

            public class TokenU7
            {
                public const string Id = "U7";
            }
        }

        public class NetworkManagementInformationCode
        {
            public const string SignOn = "001";
            public const string SignOff = "002";
            public const string ChangeKey = "161";
            public const string NewKey = "162";
            public const string RepeatKey = "163";
            public const string VerifyKey = "164";
            public const string Cutover = "201";
            public const string EchoTest = "301";

        }

        public class FileName
        {
            public const string MasterCard = "MC";
        }

        public class FieldConstant
        {
            public class F48
            {
                //public const string NetworkConstData = "60YNYY2122S009012";      //For TaishinBank
                //public const string NetworkConstData = "60YYNY2122S009012";      //For Other(YuantaBank)
                //public const string NetworkConstData = "60YYNY2122S009012";      //For Other(KGIBank)
                public const string NetworkConstData = "60YYNY2122S009012";       //For Other(FirstBank)
            }
            public class F70
            {
                public const string SignOn = "001";

                public const string SignOff = "002";

                public const string ChangeKey = "161";

                public const string NewKey = "162";

                public const string RepeatKey = "163";

                public const string VerifyKey = "164";

                public const string CutOver = "201";

                public const string EchoTest = "301";
            }

            public class F123
            {
                public const string BankID = "0000000048"; //For FirstBank

            }
        }
    }
}

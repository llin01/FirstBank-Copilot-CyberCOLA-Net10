// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MCOLA.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MCOLA type.
//   This is mainframe COLA type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Nccc;

    #endregion

    public class MCOLATransactionConst : NcccTransactionConst
    {
        public new class MTI : NcccTransactionConst.MTI
        {
            public const string AuthorizationResponseNegAck = "0190";

            public const string AdministrativeRequest = "0600";
            public const string AdministrativeResponse = "0610";
            public const string AdministrativeAdviceRequest = "0620";
            public const string AdministrativeAdviceResponse = "0630";

            // Token
            public const string AlternativeNetworkRoutingRequest = "0100";
            public const string AlternativeNetworkRoutingResponse = "0110";
            public const string TransactionHistoryAdviceRequest = "0120";
            public const string TransactionHistoryAdviceResponse = "0130";

            // Payment
            public const string PaymentTransactionRequest = "0200";
            public const string PaymentTransactionResponse = "0210";
        }

        public new class ProcessingCode : NcccTransactionConst.ProcessingCode
        {
            public new class CardholderTransactionTypeCode :
                NcccTransactionConst.ProcessingCode.CardholderTransactionTypeCode
            {
                public const string MasterCardTokenProc = "33";

                // Payment
                public const string MasterCardPaymentProc = "28";
                public const string VISAPaymentProc = "26";
            }
        }

        public new class SecurityRelatedControlInformation : NcccTransactionConst.SecurityRelatedControlInformation
        {
            public new class KeyType : NcccTransactionConst.SecurityRelatedControlInformation.KeyType
            {
                public const string Custom = "01";
            }
        }

        public new class NetworkManagementInformationCode : NcccTransactionConst.NetworkManagementInformationCode
        {
            public const string ANR = "104";
        }

        public new class PosAdditionalData : NcccTransactionConst.PosAdditionalData
        {
            public new class TokenU7 : NcccTransactionConst.PosAdditionalData.TokenU7
            {
                public class WalletProviderRiskAssessment
                {
                    public const string Approve = "G";
                    public const string ConditionallyApproved = "Y";
                    public const string Decline = "R";
                }
            }
        }

        public class RecordData
        {
            // MCC 102
            public const int StandInAccountFileLength = 92;
            // MCC 103
            public const int ElectronicWarningBulletinFileLength = 101;
            // MCC 106 PAN
            public const int PanMappingFilePanLength = 38;
            // MCC 106 Token
            public const int PanMappingFileTokenLength = 63;
            // MCC 106 A-D
            public const int PanMappingFileADLength = 96;

            public class StandInAccountFile
            {
                public const string ActCode = "MCC102         ";

                public class UpdateCode
                {
                    public const string AddOrUpdate = "1";
                    public const string Delete = "3";
                    public const string Inquiry = "5";
                }
            }

            public class ElectronicWarningBulletinFile
            {
                public const string ActCode = "MCC103         ";

                public class UpdateCode
                {
                    public const string AddOrUpdate = "1";
                    public const string Delete = "3";
                    public const string Inquiry = "5";
                }
            }

            public class PanMappingFilePan
            {
                public const string UpdatedMemo = "P";
                public const string ActCode = "MCC106         ";
            }

            public class PanMappingFileToken
            {
                public const string UpdatedMemo = "T";
                public const string ActCode = "MCC106         ";
            }

            public class PanMappingFileAD
            {
                public const string UpdatedMemo = " ";
                public const string ActCode = "MCC106         ";
            }
        }
    }
}

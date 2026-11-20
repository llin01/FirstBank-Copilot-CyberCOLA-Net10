namespace Com.Cybersoft4u.TM.FISC
{
    public class FISCTransactionConst
    {
        public class ProcessingCode
        {
            // Normal
            public const string Authorization =
                                CardholderTransactionTypeCode.Purchase +
                                CardholderFromAccountTypeCode.DefaultAccount +
                                CardholderToAccountTypeCode.DefaultAccount;

            public class CardholderTransactionTypeCode
            {
                public const string Purchase = "00";
                public const string Withdrawal = "01";
                public const string DebitAdjustment = "02";
                public const string PurchaseReturnRefund = "20";
                public const string CreditAdjustment = "22";
                public const string BalanceInquiry = "30";
                public const string PinChange = "92";
                public const string CashDisbursement = "17";
            }

            public class CardholderFromAccountTypeCode
            {
                public const string DefaultAccount = "00";
                public const string CreditCardAccount = "30";
            }

            public class CardholderToAccountTypeCode
            {
                public const string DefaultAccount = "00";
                public const string CreditCardAccount = "30";
            }
        }

        public class MTI
        {
            // Normal
            public const string AuthorizationRequest = "0100";
            public const string AuthorizationResponse = "0110";
            public const string AuthorizationAdviceRequest = "0120";
            public const string AuthorizationAdviceResponse = "0130";
            public const string IssuerFileUpdateRequest = "0302";
            public const string IssuerFileUpdateResponse = "0312";
            public const string ReversalRequest = "0420";
            public const string ReversalResponse = "0430";

            // Network Management
            public const string NetworkManagementRequest = "0800";
            public const string NetworkManagementResponse = "0810";
            public const string NetworkManagementAdviceRequest = "0820";

            // Token
            public const string AlternativeNetworkRoutingRequest = "0100";
            public const string AlternativeNetworkRoutingResponse = "0110";
            public const string TransactionHistoryAdviceRequest = "0120";
            public const string TransactionHistoryAdviceResponse = "0130";
        }

        public class PointOfServiceEntryMode
        {
            public class PosTerminalPanEntryMode
            {
                public const string Chip = "05";
                public const string Contactless = "07";
                public const string MagneticStripe = "90";
                public const string ContactlessMagneticStripe = "91";
            }
        }

        public class ResponseCode
        {
            public class IssuerFileUpdate
            {
                public const string CompletedSuccessfully = "00";
                public const string UnableLocateRecord = "25";
                public const string RecordNotActive = "26";
                public const string FieldEditError = "27";
                public const string RecordDeleted = "28";
                public const string FormatError = "30";
                public const string DuplicateAdd = "80";
                public const string SystemError = "96";
            }

            public class Reversal
            {
                public const string SuspectFraud = "34";
            }

            public class NetworkManagement
            {
                public const string Approved = "00";
            }
        }

        public class AdditionalDataPrivateUse
        {
            public class TransactionCategoryCode
            {
                public const string Space = " ";
                public const string CashDisbursement = "C";
                public const string RetailSale = "R";
            }

            public const string KeyExchangeBlockData = "11";
            public const string WalletProgramDataTagId = "26";
            public const string TokenTransactionIdentifierTagId = "30";
            public const string OriginalTransactionDataTagId = "31";
            public const string MastercardAssignedTagId = "32";
            public const string PanMappingFileInformationTagId = "33";
            public const string ATCInformation = "34";
            public const string ElectronicCommerceCertificateQualifyingInformation = "41";
            public const string ElectronicCommerceIndicatorsTagId = "42";
            public const string UniversalCardholderAuthenticationFieldTagId = "43";
            public const string ThreeDSecureElectronicCommerceTransactioTagId = "44";
            public const string CAVVResultCodeTagId = "45";
            public const string MobileProgramIndicatorsTagId = "48";
            public const string PosDataExtendedConditionCodesTagId = "61";
            public const string TraceId = "63";
            public const string FiscDE48TagI66 = "66";
            public const string OnbehalfServicesTagId = "71";
            public const string AdditionalProcessingInformationTagId = "74";
            public const string FundingPaymentTransactionTypeIndicator = "77";
            public const string ChipCvrTvrBitErrorResultTagId = "79";
            public const string AddressVerificationRequest = "82";
            public const string AddressVerificationResponse = "83";
            public const string CardValidationCodeResultTagId = "87";
            public const string Cvc2TagId = "92";

            public class ElectronicCommerceIndicators
            {
                public class SecurityLevelIndicator
                {
                    public const string ChannelEncryptCertNotUsed = "21";
                    public const string MasterpassGenerated = "22";
                    public const string DigitalSecureRemotePayment = "24";
                    public const string NoSecurityProtocolCertNotUsed = "91";
                }

                public class CardholderAuthentication
                {
                    public const string NotApplicable = "0";
                    public const string eCommerceSecureCode = "1";
                    public const string ProcessedThroughMasterpass = "2";
                    public const string DigitalSecureRemotePaymentUCAF = "4";
                }


                public class UCAFCollectionIndicator
                {
                    public const string UCAFDataCollectionNotSupport = "0";
                    public const string UCAFDataCollectionSupportAttemptAAV = "1";
                    public const string UCAFDataCollectionSupportFullyAAV = "2";
                    public const string UCAFDataCollectionSupportStaticAAV = "3";
                    public const string UCAFIssuerRiskBasedDecisioning = "5";
                    public const string UCAFMerchantRiskBasedDecisioning = "6";
                    public const string UCAFPartialShipmentRecurringpayment = "7";
                }
            }

            public class MobileProgramIndicators
            {
                public const string RemotePurchaseEC = "2";
                public const string RemotePurchaseMOTO = "3";
            }

            public class PosDataExtendedConditionCodes
            {
                public const string NormalAuthorizationUndefinedFinality = "0";
                public const string FinalAuthorization = "1";
            }

            public class AdditionalProcessingInformation
            {
                public class ProcessingIndicator
                {
                    public const string IssuerChipValidation = "50";
                }

                public class ProcessingInformation
                {
                    public const string ValidApplicationCryptogram = "A";
                    public const string ApplicationCryptogramInvalid = "I";
                }
            }

            public class CardValidationCodeResult
            {
                public const string Match = "M";
                public const string NoMatch = "N";
                public const string NotProcessed = "P";
                public const string InvalidCvc1 = "Y";
                public const string E = "E";
            }
        }

        public class PaymentAccountData
        {
            public class PaymentAccountReference
            {
                public const string PARId = "01";
            }

        }

        public class AdviceReasonCode
        {
            public class AdviceReasonCodeSubfield
            {
                public const string McDigitalEnableServiceAdviceIssuer = "141";
            }
            public class AdviceDetailCode
            {
                public const string ActivationCodeNotification = "0250";
                public const string TokenizationCompleteNotification = "0251";
                public const string TokenizationEventNotification = "0252";
            }
        }

        public class PointOfServiceData
        {
            public class PosCardholderPresence
            {
                public const string Present = "0";
            }
            public class PosTransactionStatus
            {
                public const string PreAuthorizedRequest = "4";
                public const string AtcUpdate = "6";
                public const string AccountStatusInquiryService = "8";
                public const string TokenizationRequestNotification = "9";
            }
        }

        public class NetworkManagementInformationCode
        {
            public const string SignOn = "061";
            public const string SignOff = "062";
            public const string EchoTest = "270";
            public const string EncryptionKeyExchangeRequest = "161";
            public const string EncryptionKeyExchangeOnDemandRequest = "162";
        }

        public class IssuerFileUpdateCode
        {
            public const string Add = "1";
            public const string Update = "2";
            public const string Delete = "3";
            public const string Inquiry = "5";
        }

        public class ServiceIndicator
        {
            public class ReservedForFutureUse
            {
                public const string Default = "C";
            }

            public class AcquirerIssuerIndicator
            {
                public const string AcquirerOnly = "A";
                public const string IssuerOnly = "I";
                public const string BothAcquirerIssuer = "B";
            }

            public class AddressDataIndicator
            {
                public const string AvsNotSupport = "0";
            }
        }

        public class FileName
        {
            public const string StandInAccountFile = "MCC102";
            public const string ElectronicWarningBulletinFile = "MCC103";
            public const string PanMappingFile = "MCC106";

            public const string PanMappingFilePan = PanMappingFile;
            public const string PanMappingFileToken = PanMappingFile;
        }

        public class MemberDefinedData
        {
            public class TokenizationAuthorization
            {
                public const string MessageType = "TA";
            }

            public class WalletServiceProviderTokenizationRecommendation
            {
                public const string Decline = "0";
                public const string Approve = "1";
                public const string RequireAdditionalAuthentication = "2";
            }

            public class AddressVerificationRequestAndAuthorization
            {
                public const string AVSAndAuth = "52";
            }

            public class AddressVerificationResponseAndAuthorization
            {
                public const string AVSRespLength = "01";
                public const string AVSRespCurrentlyNotSupport = "S";
            }
            public class FISCField2Data
            {
                public const string FISCNetworkF2NCID = "01272";
                public const string FISCNetworkData = "MCC 000004";
            }
        }
    }
}

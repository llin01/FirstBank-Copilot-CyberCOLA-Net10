namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.MasterCard.DE48;

    using Com.Cybersoft4u.TM.MasterCard.DE56;

    #endregion

    public class ProcessingCode
    {
        public string CardholderTransactionTypeCode = null;
        public string CardholderFromAccountTypeCode = null;
        public string CardholderToAccountTypeCode = null;
    }

    public class PointOfServiceEntryMode
    {
        public string PosTerminalPanEntryMode = null;
        public string PosTerminalPinEntryMode = null;
    }

    public class CardAcceptorNameLocation
    {
        public string CardAcceptorName = null;
        public string CardAcceptorCity = null;
        public string CardAcceptorStateCountryCode = null;
    }

    public class AdditionalDataPrivateUse
    {
        public string TransactionCategoryCode = null;
        public MasterCardDE48 Subelement = new MasterCardDE48();
    }

    public class PaymentAccountData
    {
        public MasterCardDE56 Subelement = new MasterCardDE56();
    }

    public class AdviceReasonCode
    {
        public string AdviceReasonCodeSubfield = null;
        public string AdviceDetailCode = null;
        public string AdviceDetailText = null;
    }

    public class PointOfServiceData
    {
        public string PosTerminalAttendance = null;
        public string Sub2ReservedForFutureUse = null;
        public string PosTerminalLocation = null;
        public string PosCardholderPresence = null;
        public string PosCardPresence = null;

        public string PosCardCaptureCapabilities = null;
        public string PosTransactionStatus = null;
        public string PosTransactionSecurity = null;
        public string Sub9ReservedForFutureUse = null;
        public string CardholderActivatedTerminalLevel = null;

        public string PosCardDataTerminalInputCapabilityIndicator = null;
        public string PosAuthorizationLifeCycle = null;
        public string PosCountryCode = null;
        public string PosPostalCode = null;
    }

    public class NetworkData
    {
        public string FinancialNetworkCode = null;
        public string BanknetReferenceNumber = null;
    }

    public class OriginalDataElements
    {
        public string OriginalMessageTypeIdentifier = null;
        public string OriginalSystemsTraceAuditNumber = null;
        public string OriginalTransmissionDateAndTime = null;
        public string OriginalAcquiringInstitutionIdCode = null;
        public string OriginalForwardingInstitutionIdCode = null;
    }

    public class ServiceIndicator
    {
        public string ReservedForFutureUse = null;
        public string AcquirerIssuerIndicator = null;
        public string AddressDataIndicator = null;
        public string Filler = null;
    }

    public class MemberDefinedData
    {
        public string MessageType = null;
        public string CorrelationId = null;

        //// Tokenization Authorization Request
        public string PrimaryAccountNumberSource = null;
        public string PaymentApplicationInstanceId = null;
        public string DeviceSourceIpAddress = null;
        public string WalletServiceProviderAccountIdHash = null;
        public string CardholderName = null;
        public string WalletServiceProviderTokenizationRecommendation = null;
        public string WalletServiceProviderTokenizationRecommendationStandardVersion = null;
        public string WalletServiceProviderDeviceScore = null;
        public string WalletServiceProviderAccountScore = null;
        public string NumberofActiveTokensforthePrimaryAccountNumber = null;
        public string WalletServiceProviderTokenizationRecommendationReasonCodes = null;
        public string DeviceLocation = null;
        public string MobileNumberLast4Digits = null;
        public string TokenType = null;

        //// Activation Code Notification
        public string ActivationCode = null;
        public string ActivationCodeExpirationDateTime = null;
        public string ConsumerActivationMethodPreference = null;
    }

    public class MasterCardExtendedCore
    {
        //// DE 3
        public ProcessingCode ProcessingCode = new ProcessingCode();
        //// DE 6
        public string CardholderBillingAmount = null;
        //// DE 10
        public string CardholderBillingConversionRate = null;
        //// DE 16
        public string ConversionDate = null;
        //// DE 22
        public PointOfServiceEntryMode PointOfServiceEntryMode = new PointOfServiceEntryMode();
        //// DE 23 
        public string CardSequenceNumber = null;
        //// DE 25
        public string PointOfServiceConditionCode = null;
        //// DE 33
        public string ForwardingInstitutionIdCode = null;
        //// DE 35
        public string Track2DataSeparator = "D";
        public string DiscretionaryData = null;
        //// DE 43
        public CardAcceptorNameLocation CardAcceptorNameLocation = new CardAcceptorNameLocation();
        //// DE 48
        public AdditionalDataPrivateUse AdditionalDataPrivateUse = new AdditionalDataPrivateUse();
        //// DE 51
        public string CardholderBillingCurrencyCode = null;
        //// DE 55
        public MasterCardTLV IntegratedCircuitCardSystemRelatedData = new MasterCardTLV();
        //// DE 56
        public PaymentAccountData PaymentAccountData = new PaymentAccountData();
        //// DE 60
        public AdviceReasonCode AdviceReasonCode = new AdviceReasonCode();
        //// DE 61
        public PointOfServiceData PointOfServiceData = new PointOfServiceData();
        //// DE 63
        public NetworkData NetworkData = new NetworkData();
        //// DE 90
        public OriginalDataElements OriginalDataElements = new OriginalDataElements();
        //// DE 91
        public string IssuerFileUpdateCode = null;
        //// DE 93
        public string ResponseIndicator = null;
        //// DE 94
        public ServiceIndicator ServiceIndicator = new ServiceIndicator();
        //// DE 96
        public string MessageSecurityCode = null;
        //// DE 101
        public string ReceivingInstitutionIdCode = null;
        //// DE 101
        public string FileName = null;
        //// DE 121
        public string AuthorizingAgentIdCode = null;
        //// DE 124
        public MemberDefinedData MemberDefinedData = new MemberDefinedData();
    }
}

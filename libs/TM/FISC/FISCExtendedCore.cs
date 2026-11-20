namespace Com.Cybersoft4u.TM.FISC
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using Com.Cybersoft4u.TM.FISC.DE48;

    //using Com.Cybersoft4u.TM.FISC.DE56;
    #endregion

    /// <summary>
    /// Represents the processing code (Field 3) in FISC protocol.
    /// </summary>
    public class ProcessingCode
    {
        /// <summary>Cardholder transaction type code.</summary>
        public string CardholderTransactionTypeCode = null;
        /// <summary>Cardholder from-account type code.</summary>
        public string CardholderFromAccountTypeCode = null;
        /// <summary>Cardholder to-account type code.</summary>
        public string CardholderToAccountTypeCode = null;
    }

    /// <summary>
    /// Represents the POS Entry Mode (Field 22) in FISC protocol.
    /// </summary>
    public class PointOfServiceEntryMode
    {
        /// <summary>POS terminal PAN entry mode.</summary>
        public string PosTerminalPanEntryMode = null;
        /// <summary>POS terminal PIN entry mode.</summary>
        public string PosTerminalPinEntryMode = null;
    }

    /// <summary>
    /// Represents the Card Acceptor Name/Location (Field 43) in FISC protocol.
    /// </summary>
    public class CardAcceptorNameLocation
    {
        /// <summary>Card acceptor name.</summary>
        public string CardAcceptorName = null;
        /// <summary>Card acceptor city.</summary>
        public string CardAcceptorCity = null;
        /// <summary>Card acceptor state/country code.</summary>
        public string CardAcceptorStateCountryCode = null;
    }

    /// <summary>
    /// Represents Additional Data - Private Use (Field 48) in FISC protocol.
    /// </summary>
    public class AdditionalDataPrivateUse
    {
        /// <summary>Transaction category code.</summary>
        public string TransactionCategoryCode = null;
        /// <summary>FISC Field 48 sub-elements.</summary>
        public FISCDE48 Subelement = new FISCDE48();
    }

    /// <summary>
    /// Represents Payment Account Data (Field 56) in FISC protocol.
    /// </summary>
    public class PaymentAccountData
    {
        //public MasterCardDE56 Subelement = new MasterCardDE56();
    }

    /// <summary>
    /// Represents the Advice Reason Code (Field 60) in FISC protocol.
    /// </summary>
    public class AdviceReasonCode
    {
        /// <summary>Advice reason code subfield.</summary>
        public string AdviceReasonCodeSubfield = null;
        /// <summary>Advice detail code.</summary>
        public string AdviceDetailCode = null;
        /// <summary>Advice detail text.</summary>
        public string AdviceDetailText = null;
    }

    /// <summary>
    /// Represents Point-of-Service (POS) Data (Field 61) in FISC protocol.
    /// </summary>
    public class PointOfServiceData
    {
        /// <summary>POS terminal attendance.</summary>
        public string PosTerminalAttendance = null;
        /// <summary>POS terminal operator.</summary>
        public string PosTerminalOperator = null;
        /// <summary>POS terminal location.</summary>
        public string PosTerminalLocation = null;
        /// <summary>POS customer presence.</summary>
        public string PosCustomerPresence = null;
        /// <summary>POS card presence.</summary>
        public string PosCardPresence = null;
        /// <summary>POS card retention.</summary>
        public string PosCardRetention = null;
        /// <summary>POS transaction status.</summary>
        public string PosTransactionStatus = null;
        /// <summary>POS transaction security.</summary>
        public string PosTransactionSecurity = null;
        /// <summary>POS transaction routing.</summary>
        public string PosTransactionRouting = null;
        /// <summary>POS terminal type.</summary>
        public string PosTerminalType = null;
        /// <summary>POS entry capability.</summary>
        public string PosEntryCapability = null;
        /// <summary>POS other data.</summary>
        public string PosOtherData = null;
    }

    /// <summary>
    /// Represents Network Data (Field 63) in FISC protocol.
    /// </summary>
    public class NetworkData
    {
        /// <summary>Financial network code.</summary>
        public string FinancialNetworkCode = null;
        /// <summary>Banknet reference number.</summary>
        public string BanknetReferenceNumber = null;
    }

    /// <summary>
    /// Represents Original Data Elements (Field 90) in FISC protocol.
    /// </summary>
    public class OriginalDataElements
    {
        /// <summary>Original message type identifier.</summary>
        public string OriginalMessageTypeIdentifier = null;
        /// <summary>Original system trace audit number.</summary>
        public string OriginalSystemsTraceAuditNumber = null;
        /// <summary>Original transmission date and time.</summary>
        public string OriginalTransmissionDateAndTime = null;
        /// <summary>Original acquiring institution ID code.</summary>
        public string OriginalAcquiringInstitutionIdCode = null;
        /// <summary>Original forwarding institution ID code.</summary>
        public string OriginalForwardingInstitutionIdCode = null;
    }

    /// <summary>
    /// Represents the Service Indicator (Field 94) in FISC protocol.
    /// </summary>
    public class ServiceIndicator
    {
        /// <summary>Reserved for future use.</summary>
        public string ReservedForFutureUse = null;
        /// <summary>Acquirer/issuer indicator.</summary>
        public string AcquirerIssuerIndicator = null;
        /// <summary>Address data indicator.</summary>
        public string AddressDataIndicator = null;
        /// <summary>Filler.</summary>
        public string Filler = null;
    }

    /// <summary>
    /// Represents the extended core data for a FISC transaction.
    /// </summary>
    public class FISCExtendedCore
    {
        /// <summary>DE 3: Processing Code.</summary>
        public ProcessingCode ProcessingCode = new ProcessingCode();
        /// <summary>DE 6: Cardholder Billing Amount.</summary>
        public string CardholderBillingAmount = null;
        /// <summary>DE 10: Cardholder Billing Conversion Rate.</summary>
        public string CardholderBillingConversionRate = null;
        /// <summary>DE 16: Conversion Date.</summary>
        public string ConversionDate = null;
        /// <summary>DE 22: Point of Service Entry Mode.</summary>
        public PointOfServiceEntryMode PointOfServiceEntryMode = new PointOfServiceEntryMode();
        /// <summary>DE 23: Card Sequence Number.</summary>
        public string CardSequenceNumber = null;
        /// <summary>DE 25: Point of Service Condition Code.</summary>
        public string PointOfServiceConditionCode = null;
        /// <summary>DE 33: Forwarding Institution ID Code.</summary>
        public string ForwardingInstitutionIdCode = null;
        /// <summary>DE 35: Track 2 Data Separator.</summary>
        public string Track2DataSeparator = "D";
        /// <summary>DE 35: Discretionary Data.</summary>
        public string DiscretionaryData = null;
        /// <summary>DE 43: Card Acceptor Name Location.</summary>
        public CardAcceptorNameLocation CardAcceptorNameLocation = new CardAcceptorNameLocation();
        /// <summary>DE 48: Additional Data Private Use.</summary>
        public AdditionalDataPrivateUse AdditionalDataPrivateUse = new AdditionalDataPrivateUse();
        /// <summary>DE 51: Cardholder Billing Currency Code.</summary>
        public string CardholderBillingCurrencyCode = null;
        /// <summary>DE 55: Integrated Circuit Card (ICC) System-Related Data.</summary>
        public FISCTLV IntegratedCircuitCardSystemRelatedData = new FISCTLV();
        /// <summary>DE 56: Payment Account Data.</summary>
        public PaymentAccountData PaymentAccountData = new PaymentAccountData();
        /// <summary>DE 58: FISC Additional Data Private Use.</summary>
        public Dictionary<string, string> FiscAdditionalDataPrivateUse = null;
        /// <summary>DE 60: Advice Reason Code.</summary>
        public AdviceReasonCode AdviceReasonCode = new AdviceReasonCode();
        /// <summary>DE 61: Point of Service Data.</summary>
        public PointOfServiceData PointOfServiceData = new PointOfServiceData();
        /// <summary>DE 63: Network Data.</summary>
        public NetworkData NetworkData = new NetworkData();
        /// <summary>DE 90: Original Data Elements.</summary>
        public OriginalDataElements OriginalDataElements = new OriginalDataElements();
        /// <summary>DE 91: Issuer File Update Code.</summary>
        public string IssuerFileUpdateCode = null;
        /// <summary>DE 93: Response Indicator.</summary>
        public string ResponseIndicator = null;
        /// <summary>DE 94: Service Indicator.</summary>
        public ServiceIndicator ServiceIndicator = new ServiceIndicator();
        /// <summary>DE 96: Message Security Code.</summary>
        public string MessageSecurityCode = null;
        /// <summary>DE 101: Receiving Institution ID Code.</summary>
        public string ReceivingInstitutionIdCode = null;
        /// <summary>DE 101: File Name.</summary>
        public string FileName = null;
        /// <summary>DE 121: Authorizing Agent ID Code.</summary>
        public string AuthorizingAgentIdCode = null;
    }
}

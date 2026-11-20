namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    #region DEPENDENCY

    using System.Linq;

    #endregion

    public class DefaultCore : CoreV2003
    {
        #region VARIABLES

        /// <summary>
        /// </summary>
        public object F_1_ContinuationBit;

        /// <summary>
        ///     A series of digits used to identify a customer account or relationship.
        /// </summary>
        public object F_2_primaryAccountNumber;

        /// <summary>
        ///     Code used to describe the effect of a transaction on the customer account and the accounts affected.
        /// </summary>
        public object F_3_ProcessingCode;

        /// <summary>
        ///     Code which indicates the type of transaction being undertaken. Used in conjunction with the Account type
        ///     code as part of the Processing code.
        /// </summary>
        public object F_3_ProcessingCode_TransactionTypeCode;

        /// <summary>
        ///     Code which identifies the type of account to be updated. Used in conjunction with the Transaction type
        ///     code as part of the Processing code.///
        /// </summary>
        public object F_3_ProcessingCode_From;

        /// <summary>
        ///     Code which identifies the type of account to be updated. Used in conjunction with the Transaction type
        ///     code as part of the Processing code.///
        /// </summary>
        public object F_3_ProcessingCode_To;

        /// <summary>
        ///     Funds requested by the cardholder in the local currency of the acquirer or source location of the
        ///     transaction, exclusive of Amounts fees.
        /// </summary>
        public CurrencyUnit F_4_AmountTransaction;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Amount transaction (in accordance with ISO 4217).
        // ///    
        // /// </summary>
        // public object F_4_AmountTransaction_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount transaction. Indicates the number of places the decimal point shall be
        // ///     moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_4_AmountTransaction_MinorUnit;
        // /// <summary> 
        // /// The transaction amount in the indicated currency. 
        // /// </summary>
        // public object F_4_AmountTransaction_Value;

        /// <summary>
        ///     Funds to be transferred between the acquirer and card issuer equal to the Amount transaction in the
        ///     currency of reconciliation.
        /// </summary>
        public CurrencyUnit F_5_AmountReconciliation;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Amount reconciliation (in accordance with ISO
        // ///     4217).
        // /// </summary>
        // public object F_5_AmountReconciliation_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount reconciliation. Indicates the number of places the decimal point shall be
        // ///     moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_5_AmountReconciliation_MinorUnit;
        // /// <summary> 
        // /// The reconciliation amount in the indicated currency. 
        // /// </summary>
        // public object F_5_AmountReconciliation_Value;

        /// <summary>
        ///     Amount billed to the cardholder in the currency of the cardholder account exclusive of cardholder billing
        ///     fees.
        /// </summary>
        public CurrencyUnit F_6_AmountCardholderBilling;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Amount cardholder billing (in accordance with ISO
        // ///     4217).
        // /// </summary>
        // public object F_6_AmountCardholderBilling_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount cardholder billing. Indicates the number of places the decimal point shall
        // ///     be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_6_AmountCardholderBilling_MinorUnit;
        // /// <summary> 
        // /// The cardholder billing amount in the indicated currency. 
        // /// </summary>
        // public object F_6_AmountCardholderBilling_Value;

        /// <summary>
        ///     Date and time the message initiator sends this message. To be expressed in Co-ordinated Universal Time
        ///     (UTC) (in accordance with ISO 8601), formerly known as Greenwich Mean Time (GMT).
        /// </summary>
        public object F_7_DateAndTimeTransmission;

        /// <summary>
        ///     Fee to be billed to the cardholder by the card issuing institution in the same currency as Amount
        ///     cardholder billing.
        /// </summary>
        public CurrencyUnit F_8_AmountCardholderBillingFee;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Amount cardholder billing fee (in accordance with
        // ///     ISO 4217).
        // /// </summary>
        // public object F_8_AmountCardholderBillingFee_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount cardholder billing fee. Indicates the number of places the decimal point
        // ///     shall be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_8_AmountCardholderBillingFee_MinorUnit;
        // /// <summary> 
        // /// The cardholder billing fee amount in the indicated currency. 
        // /// </summary>
        // public object F_8_AmountCardholderBillingFee_Value;

        /// <summary>
        ///     The factor used in the conversion from transaction to reconciliation amount. The Amount transaction is
        ///     multiplied by Conversion rate reconciliation to determine the Amount reconciliation.
        /// </summary>
        public object F_9_ConversionRateReconciliation;

        /// <summary>
        ///     The factor used in the conversion from transaction to cardholder billing amount. The Amount transaction
        ///     is multiplied by Conversion rate cardholder billing to determine Amount cardholder billing.
        /// </summary>
        public object F_10_ConversionRateCardholderBilling;

        /// <summary>
        ///     A number assigned by a transaction originator to assist in identifying a transaction uniquely. The trace
        ///     number remains unchanged for all messages within a two-message exchange, e.g. request/repeat and response.
        /// </summary>
        public object F_11_SystemsTraceAuditNumber;

        /// <summary>
        ///     The local year, month, day and time the transaction takes place at the card acceptor location in
        ///     authorization messages and financial presentment messages.
        /// </summary>
        public object F_12_DateTimeLocalTransaction;

        /// <summary>
        ///     The local year, month and day of the transaction.
        /// </summary>
        public object F_12_DateTimeLocalTransaction_Date;

        /// <summary>
        ///     The local time of the transaction.
        /// </summary>
        public object F_12_DateTimeLocalTransaction_Time;

        /// <summary>
        ///     The year and month in which the card becomes effective.
        /// </summary>
        public object F_13_DateEffective;

        /// <summary>
        ///     The year and month after which the card expires.
        /// </summary>
        public object F_14_DateExpiration;

        /// <summary>
        ///     The year, month and day for which funds shall be transferred between acquirer and card issuer.
        /// </summary>
        public object F_15_DateSettlement;

        /// <summary>
        ///     The month and day the conversion rate is effective to convert the transaction amount from the original to
        ///     reconciliation currency.
        /// </summary>
        public object F_16_DateConversion;

        /// <summary>
        ///     The month and day the acquirer processed the transaction data.
        /// </summary>
        public object F_17_DateCapture;

        /// <summary>
        ///     Identifies the location, severity and general description of data element errors in a message.
        /// </summary>
        public object F_18_MessageErrorIndicator;

        /// <summary>
        ///     Indicates whether or not the error condition caused the receiver to reject the message. Contained in
        ///     Message error indicator.
        /// </summary>
        public object F_18_MessageErrorIndicator_ErrorSeverityCode;

        /// <summary>
        ///     Code identifying a specific error condition. Contained in Message error indicator.
        /// </summary>
        public object F_18_MessageErrorIndicator_MessageErrorCode;

        /// <summary>
        ///     Identifies the data element number in error. Contained in Message error indicator.
        /// </summary>
        public object F_18_MessageErrorIndicator_DataElement;

        /// <summary>
        ///     Identifies the part within a constructed data element that is in error. Contained in Message error
        ///     indicator.
        /// </summary>
        public object F_18_MessageErrorIndicator_DataSubElement;

        /// <summary>
        ///     Identifies the dataset identifier within a composite data element that is in error. Contained in Message
        ///     error indicator.
        /// </summary>
        public object F_18_MessageErrorIndicator_DatasetIdentifier;

        /// <summary>
        ///     Identifies the bit or tag within a dataset identifier that is in error. Contained in Message error
        ///     indicator.
        /// </summary>
        public object F_18_MessageErrorIndicator_DatasetBitOrTag;

        /// <summary>
        ///     The code of the country where the acquiring institution is located (in accordance with ISO 3166).
        /// </summary>
        public object F_19_CountryCodeAcquiringInstitution;

        /// <summary>
        ///     The code of the country where the card issuing institution is located (in accordance with ISO 3166).
        /// </summary>
        public object F_20_CountryCodePrimaryAccountNumber;

        /// <summary>
        ///     A unique identifier used to match transactions throughout their life cycle (for example, authorization to
        ///     financial presentment, financial presentment to chargeback etc.) It shall contain the same value in all messages
        ///     throughout a transaction’s life cycle.
        /// </summary>
        public object F_21_TransactionLifeCycleIdentification;

        /// <summary>
        ///     Indicates the point in the transaction life cycle at which Life cycle trace identifier was assigned.
        /// </summary>
        public object F_21_TransactionLifeCycleIdentification_LifeCycleSupportIndicator;

        /// <summary>
        ///     Unique transaction identifier.
        /// </summary>
        public object F_21_TransactionLifeCycleIdentification_LifeCycleTraceIdentifier;

        /// <summary>
        ///     Number used with Life cycle trace identifier to uniquely identify when multiple financial presentments
        ///     are generated from a single authorization transaction.
        /// </summary>
        public object F_21_TransactionLifeCycleIdentification_LifeCycleTransactionSequenceNumber;

        /// <summary>
        ///     A code calculated using an algorithm against key transaction data elements that are common to both
        ///     authorization messages and financial presentment messages.
        /// </summary>
        public object F_21_TransactionLifeCycleIdentification_LifeCycleAuthenticationToken;

        /// <summary>
        ///     A series of codes intended to identify how a transaction completed at the POS.
        /// </summary>
        public object F_22_POSDataCode;

        /// <summary>
        ///     A series of code values that indicate which of the card reading capabilities of the POS were used for
        ///     this transaction.
        /// </summary>
        public object F_22_POSDataCode_CardReadingMethod;

        /// <summary>
        ///     A series of code values that indicate the POS capabilities of the POS actually used to identify the
        ///     cardholder.
        /// </summary>
        public object F_22_POSDataCode_CardholderVerificationMethod;

        /// <summary>
        ///     A series of code values that indicate if the card acceptor were present and if not the type of
        ///     environment in which the transaction took place.
        /// </summary>
        public object F_22_POSDataCode_POSEnvironment;

        /// <summary>
        ///     A series of codes that indicate the security characteristics applicable to a transaction.
        /// </summary>
        public object F_22_POSDataCode_SecurityCharacteristics;

        /// <summary>
        ///     A number distinguishing between separate cards with the same primary account number.
        /// </summary>
        public object F_23_CardSequenceNumber;

        /// <summary>
        ///     Code indicating the specific purpose of the message within its message class.
        /// </summary>
        public object F_24_FunctionCode;

        /// <summary>
        ///     Provides the receiver of a request, advice or notification message with the reason, or purpose, of that
        ///     message. For original authorizations and financial presentments, it identifies why the type of message was sent,
        ///     e.g. why an advice versus a request. For other messages it identifies states why this action was taken.///
        /// </summary>
        public object F_25_MessageReasonCode;

        /// <summary>
        ///     Code classifying the type of business being done by the card acceptor for this transaction (in accordance
        ///     with ISO 18245).
        /// </summary>
        public object F_26_MerchantCategoryCode;

        /// <summary>
        ///     A series of codes intended to identify the capabilities of the POS.
        /// </summary>
        public object F_27_POSCapability;

        /// <summary>
        ///     A series of code values which indicate the card reading capabilities of the POS.
        /// </summary>
        public object F_27_POSCapability_POSCardReadingCapability;

        /// <summary>
        ///     A series of code values that indicate the POS capabilities available to identify the cardholder.
        /// </summary>
        public object F_27_POSCapability_POSCardholderVerificationCapability;

        /// <summary>
        ///     Maximum length of the approval code which the acquirer may accommodate. The card issuer or agent shall
        ///     limit the approval code to this length.
        /// </summary>
        public object F_27_POSCapability_ApprovalCodeLength;

        /// <summary>
        ///     Number of characters of data from a card issuer that can be printed on a POS receipt for the cardholder.
        /// </summary>
        public object F_27_POSCapability_CardholderReceiptDataLength;

        /// <summary>
        ///     Number of characters of data from a card issuer that can be printed on a POS receipt for the card
        ///     acceptor.
        /// </summary>
        public object F_27_POSCapability_CardAcceptorReceiptDataLength;

        /// <summary>
        ///     Number of characters of data from a card issuer that can be displayed on the POS for the cardholder.
        /// </summary>
        public object F_27_POSCapability_CardholderDisplayDataLength;

        /// <summary>
        ///     Number of characters of data from a card issuer that can be displayed on the POS for the card acceptor.
        /// </summary>
        public object F_27_POSCapability_CardAcceptorDisplayDataLength;

        /// <summary>
        ///     Number of characters of data from a card issuer that can be returned to the ICC at the POS.
        /// </summary>
        public object F_27_POSCapability_ICCScriptsDataLength;

        /// <summary>
        ///     Indicates if the POS can rewrite Track 3.
        /// </summary>
        public object F_27_POSCapability_MagneticStripeTrack3RewriteCapability;

        /// <summary>
        ///     Indicates if the card can be captured.
        /// </summary>
        public object F_27_POSCapability_CardCaptureCapability;

        /// <summary>
        ///     The number of PIN digits that the point of service device can accept.
        /// </summary>
        public object F_27_POSCapability_PINInputLengthCapability;

        /// <summary>
        ///     The year, month and day for which financial totals are reconciled between the acquirer and the card
        ///     issuer.
        /// </summary>
        public object F_28_DateReconciliation;

        /// <summary>
        ///     A value used to allow reconciliation of time periods within a reconciliation date. The value is subject
        ///     to bilateral agreement.
        /// </summary>
        public object F_29_ReconciliationIndicator;

        /// <summary>
        ///     The amount data elements from the original transaction.
        /// </summary>
        public object F_30_AmountsOriginal;

        /// <summary>
        ///     The original amount of the transaction. Contained in Amounts original.
        /// </summary>
        public CurrencyUnit F_30_OriginalAmountTransaction;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Original amount transaction (in accordance with ISO
        // ///     4217).
        // /// </summary>
        // public object F_30_OriginalAmountTransaction_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Original amount transaction. Indicates the number of places the decimal point
        // ///     shall be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_30_OriginalAmountTransaction_MinorUnit;
        // /// <summary> 
        // /// The original transaction amount in the indicated currency. 
        // /// </summary>
        // public object F_30_OriginalAmountTransaction_Value;

        /// <summary>
        ///     The original amount of the transaction expressed in the reconciliation currency. Contained in Amounts
        ///     original.
        /// </summary>
        public CurrencyUnit F_30_OriginalAmountReconciliation;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Original amount reconciliation (in accordance with
        // ///     ISO 4217).
        // /// </summary>
        // public object F_30_OriginalAmountReconciliation_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Original amount reconciliation. Indicates the number of places the decimal point
        // ///     shall be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_30_OriginalAmountReconciliation_MinorUnit;
        // /// <summary> 
        // /// The original reconciliation amount in the indicated currency. 
        // /// </summary>
        // public object F_30_OriginalAmountReconciliation_Value;

        /// <summary>
        ///     Data supplied by an acquirer to assist in identifying a transaction, e.g. for researching retrievals and
        ///     chargebacks.
        /// </summary>
        public object F_31_AcquirerReferenceNumber;

        /// <summary>
        ///     Identifier applied by acquirer based on bilateral agreements.
        /// </summary>
        public object F_31_AcquirerReferenceNumber_UserFormatIdentifier;

        /// <summary>
        ///     First six digits of the Acquiring institution identification code.
        /// </summary>
        public object F_31_AcquirerReferenceNumber_AcquirerNumber;

        /// <summary>
        ///     Date acquirer processed the original transaction, e.g. first presentment.
        /// </summary>
        public object F_31_AcquirerReferenceNumber_JulianProcessingDate;

        /// <summary>
        ///     Data supplied by acquirer.
        /// </summary>
        public object F_31_AcquirerReferenceNumber_SequenceNumber;

        /// <summary>
        ///     "Check sum on the previous 22 digits of Acquirer reference number calculated in accordance with ISO
        ///     7812-1///
        /// </summary>
        public object F_31_AcquirerReferenceNumber_LuhnCheckDigit;

        /// <summary>
        ///     Code identifying the acquirer.
        /// </summary>
        public object F_32_AcquiringInstitutionIdentificationCode;

        /// <summary>
        ///     Code identifying the forwarding institution.
        /// </summary>
        public object F_33_ForwardingInstitutionIdentificationCode;

        /// <summary>
        ///     The data needed to support the processing and prove the identity of the requester (usually a cardholder)
        ///     using public key cryptography and/or digital certificates.
        /// </summary>
        public object F_34_ElectronicCommerceData;

        /// <summary>
        ///     A digital signature created by the private part of the private/public key pair supplied by a card issuer
        ///     to a cardholder and linked to the cardholder's account on which the card is issued.///
        /// </summary>
        public object F_34_71_ElectronicCommerceData_AccountBasedDigitalSignature;

        /// <summary>
        /// </summary>
        public object F_34_71_ElectronicCommerceData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_34_71_ElectronicCommerceData_MultipleTLVSubElements;

        /// <summary>
        ///     Value assigned to a cardholder certificate issued by the card issuer’s certificate authority.
        /// </summary>
        public object F_34_72_ElectronicCommerceData_CardholderCertificateSerialNumber;

        /// <summary>
        ///     Value assigned to a card acceptor certificate issued by the acquirer’s certificate authority.
        /// </summary>
        public object F_34_72_ElectronicCommerceData_CardAcceptorCertificateSerialNumber;

        /// <summary>
        ///     Value assigned to a transaction as a unique transaction identifier.
        /// </summary>
        public object F_34_72_ElectronicCommerceData_XID;

        /// <summary>
        ///     A hash value calculated by applying a secure hash algorithm to the XID and card secret (a secret defined
        ///     value known only to the cardholder and issuer of the cardholder certificate).
        /// </summary>
        public object F_34_72_ElectronicCommerceData_Transstain;

        /// <summary>
        /// </summary>
        public object F_34_72_ElectronicCommerceData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_34_72_ElectronicCommerceData_MultipleTLVSubElements;

        /// <summary>
        ///     Used in authentication of a party, e.g. a cardholder. If more than one authentication code is present
        ///     they shall be sent in the exact order received from the certificate processor.
        /// </summary>
        public object F_34_72_ElectronicCommerceData_AuthenticationCode;

        /// <summary>
        ///     The information encoded on track 2 of the magnetic stripe as specified in ISO 7813, excluding beginning
        ///     and ending sentinels and longitudinal redundancy check characters as defined therein.
        /// </summary>
        public object F_35_Track2Data;

        /// <summary>
        ///     The information encoded on track 3 of the magnetic stripe as specified in ISO 4909, including field
        ///     separators, but excluding beginning and ending sentinels and longitudinal redundancy check characters as defined
        ///     therein.
        /// </summary>
        public object F_36_Track3Data;

        /// <summary>
        ///     A reference supplied by the system retaining the original source information and used to assist in
        ///     locating that information or a copy thereof.
        /// </summary>
        public object F_37_RetrievalReferenceNumber;

        /// <summary>
        ///     Code assigned by the authorizing institution indicating approval.
        /// </summary>
        public object F_38_ApprovalCode;

        /// <summary>
        ///     A code which defines the action taken or to be taken as well as the reason for taking this action.
        /// </summary>
        public object F_39_ActionCode;

        /// <summary>
        ///     An identification of geographic/service availability (in accordance with ISO 7813).
        /// </summary>
        public object F_40_ServiceCode;

        /// <summary>
        ///     Unique code identifying a terminal at the card acceptor location.
        /// </summary>
        public object F_41_CardAcceptorTerminalIdentification;

        /// <summary>
        ///     Code identifying the card acceptor.
        /// </summary>
        public object F_42_CardAcceptorIdentificationCode;

        /// <summary>
        ///     The name and location of the card acceptor as known to the cardholder.
        /// </summary>
        public object F_43_CardAcceptorNameLocation;

        /// <summary>
        ///     The name of the card acceptor as known to the cardholder.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_name;

        /// <summary>
        ///     The address of the card acceptor, allocated by the acquirer, sufficient to allow the cardholder to
        ///     recognize the location from his statement.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_streetAddress;

        /// <summary>
        ///     The city of the card acceptor, allocated by the acquirer, sufficient to allow the cardholder to recognize
        ///     the location from his statement.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_city;

        /// <summary>
        ///     State, province, or region code allocated by the acquirer to indicate the physical location of the card
        ///     acceptor.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_state;

        /// <summary>
        ///     Code allocated by postal authority of the card acceptor to identify its physical location.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_postalCode;

        /// <summary>
        ///     "The code of the country, allocated by the acquirer, to indicate the country where the card acceptor is
        ///     located (in accordance with ISO 3166)///
        /// </summary>
        public object F_43_CardAcceptorNameLocation_countryCode;

        /// <summary>
        ///     Phone number of the card acceptor at the location of the original transaction.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_phoneNumber;

        /// <summary>
        ///     Phone number to be used to contact the card acceptor to resolve questions or disputes related to the
        ///     transaction.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_customerServicePhoneNumber;

        /// <summary>
        ///     Additional information used to facilitate contact with the card acceptor, e.g. sales agent name, dispute
        ///     manager name etc.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_additionalContactInformation;

        /// <summary>
        ///     The primary internet universal resource locator (URL) of the card acceptor, sufficient to allow the
        ///     cardholder to recognize the location from his statement.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_internetURL;

        /// <summary>
        ///     The electronic mail identifier of the card acceptor, sufficient to allow the cardholder to recognize the
        ///     location from his statement.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_eMailAddress;

        /// <summary>
        /// </summary>
        public object F_43_CardAcceptorNameLocation_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_43_CardAcceptorNameLocation_MultipleTLVSubElements;

        /// <summary>
        ///     Additional information used when card acceptor street address is insufficient.
        /// </summary>
        public object F_43_CardAcceptorNameLocation_additionalAddressInformation;

        /// <summary>
        ///     Other data required in a response, e.g. a telephone number, printer capabilities etc.
        /// </summary>
        public object F_44_AdditionalResponseData;

        /// <summary>
        ///     Card issuer data to be printed on the POS cardholder receipt.
        /// </summary>
        public object F_44_71_AdditionalResponseData_CardholderReceiptData;

        /// <summary>
        ///     Card issuer data to be printed on the POS card acceptor receipt.
        /// </summary>
        public object F_44_71_AdditionalResponseData_CardAcceptorReceiptData;

        /// <summary>
        ///     Card issuer data to be displayed on the POS for the cardholder.
        /// </summary>
        public object F_44_71_AdditionalResponseData_CardholderDisplayData;

        /// <summary>
        ///     Card issuer data to be displayed on the POS for the card acceptor.
        /// </summary>
        public object F_44_71_AdditionalResponseData_CardAcceptorDisplayData;

        /// <summary>
        ///     Contact telephone number for use by the acquirer and/or card acceptor if the card issuer wishes to make
        ///     voice contact
        /// </summary>
        public object F_44_71_AdditionalResponseData_CardIssuerTelephoneNumber;

        /// <summary>
        /// </summary>
        public object F_44_71_AdditionalResponseData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_44_71_AdditionalResponseData_MultipleTLVSubElements;

        /// <summary>
        ///     The information encoded on track 1 of the magnetic stripe as specified in ISO 7813, including field
        ///     separators but excluding beginning and ending sentinels and longitudinal redundancy check characters as defined
        ///     therein.
        /// </summary>
        public object F_45_Track1Data;

        /// <summary>
        ///     Fees associated with this transaction.
        /// </summary>
        public object F_46_AmountsFees;

        /// <summary>
        ///     Code indicating the type of fee. Contained in the data elements Amounts fees, Reconciliation fee amounts
        ///     credit and Reconciliation fee amounts debit.
        /// </summary>
        public object F_46_AmountsFees_FeeTypeCode;

        /// <summary>
        ///     Amount of the fee within the Fee type code. Contained in Amounts fees.
        /// </summary>
        public CurrencyUnit F_46_AmountFee;

        // /// <summary> 
        // /// Code defining the currency of the amount contained in Amount fee (in accordance with ISO 4217). 
        // /// </summary>
        // public object F_46_AmountFee_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount fee. Indicates the number of places the decimal point shall be moved to the
        // ///     left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_46_AmountFee_MinorUnit;
        // /// <summary> 
        // /// The fee amount in the indicated currency. 
        // /// </summary>
        // public object F_46_AmountFee_Value;

        /// <summary>
        ///     The factor used in the conversion from fee amount to reconciliation fee amount. The Amount fee is
        ///     multiplied by Conversion rate fee to determine the Amount reconciliation fee. Contained in Amounts fees.
        /// </summary>
        public object F_46_AmountsFees_ConversionRateFee;

        /// <summary>
        ///     Fee to be transferred between the acquirer and card issuer equal to the Amount fee in the currency of
        ///     reconciliation. Contained in the Amounts fees data element.
        /// </summary>
        public CurrencyUnit F_46_AmountReconciliationFee;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Reconciliation fee (in accordance with ISO 4217).
        // ///    
        // /// </summary>
        // public object F_46_AmountReconciliationFee_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount reconciliation fee. Indicates the number of places the decimal point shall
        // ///     be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_46_AmountReconciliationFee_MinorUnit;
        // /// <summary> 
        // /// The reconciliation fee amount in the indicated currency. 
        // /// </summary>
        // public object F_46_AmountReconciliationFee_Value;

        /// <summary>
        ///     Reserved for national organizations to define data unique to country applications. The use of this data
        ///     element is under the control of national bodies.
        /// </summary>
        public object F_47_AdditionalDataNational;

        /// <summary>
        ///     Reserved for private data. The use of this data element is determined by bilateral agreement.
        /// </summary>
        public object F_48_AdditionalDataPrivate;

        /// <summary>
        ///     Additional data required to support identification of the cardholder
        /// </summary>
        public object F_49_VerificationData;

        /// <summary>
        ///     A number that is only printed on the card which is not included in any other technology e.g. magnetic
        ///     stripe or ICC.
        /// </summary>
        public object F_49_71_VerificationData_CardVerificationData;

        /// <summary>
        ///     Street address of cardholder
        /// </summary>
        public object F_49_71_VerificationData_CardholderBillingStreetAddress;

        /// <summary>
        ///     Code allocated by postal authority
        /// </summary>
        public object F_49_71_VerificationData_CardholderBillingPostalCode;

        /// <summary>
        ///     Numeric and postcode elements only of the cardholder/delivery address
        /// </summary>
        public object F_49_71_VerificationData_CardholderBillingAddressCompressed;

        /// <summary>
        ///     Type of additional identification offered by cardholder.
        /// </summary>
        public object F_49_71_VerificationData_AdditionalIdentificationType;

        /// <summary>
        ///     Identification number on additional identification document.
        /// </summary>
        public object F_49_71_VerificationData_AdditionalIdentificationReferenceNumber;

        /// <summary>
        /// </summary>
        public object F_49_71_VerificationData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_49_71_VerificationData_MultipleTLVSubElements;

        /// <summary>
        ///     Code which defines the result from the address verification process
        /// </summary>
        public object F_49_72_VerificationData_AddressVerificationResultCode;

        /// <summary>
        /// </summary>
        public object F_49_72_VerificationData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_49_72_VerificationData_MultipleTLVSubElements;

        /// <summary>
        /// </summary>
        public object F_50_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_51_ReservedForISOUse;

        /// <summary>
        ///     Used to identify the cardholder at the point of service (in accordance with ISO 9564-1).
        /// </summary>
        public object F_52_PINData;

        /// <summary>
        ///     Identifies security management information used in the current transaction or specifies security
        ///     management information to be used in future transactions (in accordance with ISO 13492).
        /// </summary>
        public object F_53_SecurityRelatedControlInformation;

        /// <summary>
        ///     Information on up to six amounts and related account data for which specific data elements have not been
        ///     defined.
        /// </summary>
        public object F_54_AmountsAdditional;

        /// <summary>
        ///     Code used to describe the accounts affected. Contained in Amounts additional.
        /// </summary>
        public object F_54_AmountsAdditional_AccountTypeAdditionalAmounts;

        /// <summary>
        ///     Identifies the kind of amount contained in Amounts additional.
        /// </summary>
        public object F_54_AmountsAdditional_AmountTypeAdditionalAmounts;

        /// <summary>
        ///     Amount contained in Amounts additional.
        /// </summary>
        public CurrencyUnit F_54_AmountAdditionalAmounts;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Amount additional amounts (in accordance with ISO
        // ///     4217).
        // /// </summary>
        // public object F_54_AmountsAdditional_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount additional amounts. Indicates the number of places the decimal point shall
        // ///     be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_54_AmountsAdditional_MinorUnit;
        // /// <summary> 
        // /// The additional amounts in the indicated currency. 
        // /// </summary>
        // public object F_54_AmountsAdditional_Value;

        /// <summary>
        ///     Contains data related to integrated circuit card systems.
        /// </summary>
        public object F_55_ICCSystemRelatedData;

        /// <summary>
        ///     The data elements contained in the original message.
        /// </summary>
        public object F_56_OriginalDataElements;

        /// <summary>
        ///     The message type identifier of the original transaction. Contained in Original data elements.
        /// </summary>
        public object F_56_OriginalDataElements_OriginalMessageTypeIdentifier;

        /// <summary>
        ///     The system trace audit number of the original transaction. Contained in Original data elements.
        /// </summary>
        public object F_56_OriginalDataElements_OriginalSystemTraceAuditNumber;

        /// <summary>
        ///     The local date and time of the original transaction. Contained in Original data elements.
        /// </summary>
        public object F_56_OriginalDataElements_OriginalDateAndTimeLocalTransaction;

        /// <summary>
        ///     The acquiring institution identification code of the original financial presentment. Contained in
        ///     Original data elements.
        /// </summary>
        public object F_56_OriginalDataElements_OriginalAcquiringInstitutionIdentificationCode;

        /// <summary>
        ///     A value in calendar days, hours or minutes which defines the time period for which the acquirer is
        ///     requesting guarantee of funds, or that the card issuer shall guarantee funds for a financial presentment which may
        ///     follow.
        /// </summary>
        public object F_57_AuthorizationLifeCycleCode;

        /// <summary>
        ///     Code identifying the authorizing agent institution.
        /// </summary>
        public object F_58_AuthorizingAgentInstitutionIdentificationCode;

        /// <summary>
        ///     Contains data from the originator of the message that shall be returned unaltered in a response message.
        /// </summary>
        public object F_59_TransportData;

        /// <summary>
        /// </summary>
        public object F_60_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_61_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_62_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_63_ReservedForPrivateUse;

        /// <summary>
        ///     Used to validate the source and the text of the message between the sender and receiver (in accordance
        ///     with ISO 9807).
        /// </summary>
        public object F_64_MessageAuthenticationCode;

        /// <summary>
        /// </summary>
        public object F_65_ReservedForISOUse;

        /// <summary>
        ///     The original Amounts fees necessary to perform a partial reversal, partial chargeback or partial approval
        ///     or to replace or supplement a previously authorised transaction.
        /// </summary>
        public object F_66_AmountsOriginalFees;

        /// <summary>
        ///     Code used to indicate the type of fee. Contained in Amounts original.
        /// </summary>
        public object F_66_AmountsOriginalFees_OriginalFeeTypeCode;

        /// <summary>
        ///     The original amount of the fee in a reversal, chargeback or partial approval. Contained in Amounts
        ///     original fees.
        /// </summary>
        public CurrencyUnit F_66_OriginalAmountFee;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Original amount fee (in accordance with ISO 4217).
        // ///    
        // /// </summary>
        // public object F_66_OriginalAmountFee_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Original amount fee. Indicates the number of places the decimal point shall be
        // ///     moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_66_OriginalAmountFee_MinorUnit;
        // /// <summary> 
        // /// The original fee amount in the indicated currency. 
        // /// </summary>
        // public object F_66_OriginalAmountFee_Value;

        /// <summary>
        ///     The factor used in the conversion from transaction to reconciliation fee amount. The Original amount fee
        ///     is multiplied by Original conversion rate fee to determine Original amount reconciliation fee. Contained in Amounts
        ///     original fees.
        /// </summary>
        public object F_66_AmountsOriginalFees_OriginalConversionRateFee;

        /// <summary>
        ///     The original amount of the fee expressed in the reconciliation currency. Contained in Amounts original
        ///     fees.
        /// </summary>
        public CurrencyUnit F_66_OriginalAmountReconciliationFee;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Original amount reconciliation fee (in accordance
        // ///     with ISO 4217).
        // /// </summary>
        // public object F_66_OriginalAmountReconciliationFee_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Original amount reconciliation fee. Indicates the number of places the decimal
        // ///     point shall be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_66_OriginalAmountReconciliationFee_MinorUnit;
        // /// <summary> 
        // /// The original reconciliation fee amount in the indicated currency. 
        // /// </summary>
        // public object F_66_OriginalAmountReconciliationFee_Value;

        /// <summary>
        ///     Number of months that the cardholder prefers to pay for this item if permitted by the card issuer.
        /// </summary>
        public object F_67_ExtendedPaymentData;

        /// <summary>
        ///     Identifies the message number of the individual message and indicates whether an acknowledgement is
        ///     required. Used in the management of file and batch transfer.
        /// </summary>
        public object F_68_BatchFileTransferMessageControl;

        /// <summary>
        ///     Indicates whether an acknowledgement message is required for a notification or instruction message
        ///     submitted via batch or file transfer.
        /// </summary>
        public object F_68_BatchFileTransferMessageControl_BatchFileTransferAcknowledgMentCode;

        /// <summary>
        ///     Unique sequence number assigned to a notification or instruction message that is sent via batch or file
        ///     transfer.
        /// </summary>
        public object F_68_BatchFileTransferMessageControl_BatchFileTransferMessageSequenceNumber;

        /// <summary>
        ///     The name and number of messages in the current batch or file. Part of batch/file transfer.
        /// </summary>
        public object F_69_BatchFileTransferControlData;

        /// <summary>
        ///     The number of messages to be sent in the batch or file.
        /// </summary>
        public object F_69_BatchFileTransferControlData_BatchFileTransferMessageCount;

        /// <summary>
        ///     The unique identifier assigned to each batch or file in the current batch or file transfer.
        /// </summary>
        public object F_69_BatchFileTransferControlData_BatchFileTransferFileIdentification;

        /// <summary>
        ///     The total length and the number of elementary data records of the file, used in the management of file
        ///     transfer.
        /// </summary>
        public object F_70_FileTransferDescriptionData;

        /// <summary>
        ///     The total length in bytes of the file to be transferred.
        /// </summary>
        public object F_70_FileTransferDescriptionData_FileTransferFileSize;

        /// <summary>
        ///     The total number of elementary data records in the file to be transferred.
        /// </summary>
        public object F_70_FileTransferDescriptionData_FileTransferElementaryDataRecordCount;

        /// <summary>
        ///     The remaining number of elementary data records to be transmitted in the current file transfer.
        /// </summary>
        public object F_70_FileTransferDescriptionData_FileTransferRemainingElementaryDataRecordCount;

        /// <summary>
        /// </summary>
        public object F_71_ReservedForISOUse;

        /// <summary>
        ///     Other data required to be passed to support an administrative or file action message.
        /// </summary>
        public object F_72_DataRecord;

        /// <summary>
        ///     A date for a future action or a specific date such as a birth date.
        /// </summary>
        public object F_73_DateAction;

        /// <summary>
        ///     Number and amount values required to complete the reconciliation of financial totals.
        /// </summary>
        public object F_74_ReconciliationDataPrimary;

        /// <summary>
        ///     The sum amount of Amount transaction in all financial presentment transactions exclusive of any fees
        ///     where positions 1-2 of the Processing code in the financial presentment transaction indicated a credit (20-29).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_CreditsAmount;

        /// <summary>
        ///     The sum number of all financial presentment transactions where positions 1-2 of the Processing code in
        ///     the financial presentment transaction indicated a credit (20-29).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_CreditsNumber;

        /// <summary>
        ///     The sum amount of Amount transaction in all chargeback transactions exclusive of any fees where positions
        ///     1-2 of the Processing code in the chargeback transaction indicated a debit (00-19).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_CreditsChargebackAmount;

        /// <summary>
        ///     The sum number of all chargeback transactions where positions 1-2 of the Processing code in the
        ///     chargeback transaction indicated a debit (00-19).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_CreditsChargebackNumber;

        /// <summary>
        ///     The sum amount of Amount transaction of all reversal transactions exclusive of any fees where positions
        ///     1-2 of the Processing code in the reversal transaction indicated a debit (00-19).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_CreditsReversalAmount;

        /// <summary>
        ///     The sum number of all reversal transactions where positions 1-2 of the Processing code in the reversal
        ///     transaction indicated a credit (20-29).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_DebitsReversalNumber;

        /// <summary>
        ///     The sum amount of Amount transaction in all financial presentment transactions exclusive of any fees
        ///     where positions 1-2 of the Processing code in the financial presentment transaction indicated a debit (00-19).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_DebitsAmount;

        /// <summary>
        ///     The sum number of all financial presentment transactions where positions 1-2 of the Processing code in
        ///     the financial presentment transaction indicated a debit (00-19).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_DebitsNumber;

        /// <summary>
        ///     The sum amount of Amount transaction in all chargeback transactions exclusive of any fees where positions
        ///     1-2 of the Processing code in the chargeback transaction indicated a credit (20-29).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_DebitsChargebackAmount;

        /// <summary>
        ///     The sum number of all chargeback transactions where positions 1-2 of the Processing code in the
        ///     chargeback transaction indicated a credit (20-29).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_DebitsChargebackNumber;

        /// <summary>
        ///     The sum amount of Amount transaction of all reversal transactions exclusive of any fees where positions
        ///     1-2 of the Processing code in the reversal transaction indicated a credit (20-29).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_DebitsReversalAmount;

        /// <summary>
        ///     The sum number of all reversal transactions where positions 1-2 of the Processing code in the reversal
        ///     transaction indicated a debit (00-19).
        /// </summary>
        public object F_74_ReconciliationDataPrimary_CreditsReversalNumber;

        /// <summary>
        ///     Additional count which may be utilized for reconciliation.
        /// </summary>
        public object F_75_ReconciliationDataSecondary;

        /// <summary>
        ///     The sum number of all authorization transactions.
        /// </summary>
        public object F_75_ReconciliationDataSecondary_AuthorizationsNumber;

        /// <summary>
        ///     The sum number of all reversal transactions processed where the message type identifier in the original
        ///     data elements indicated an authorization transaction (1xx).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_AuthorizationsReversalNumber;

        /// <summary>
        ///     The sum number of all authorization transactions processed where positions 1-2 of the Processing code in
        ///     the authorization transaction indicated an inquiry (30-39).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_InquiriesNumber;

        /// <summary>
        ///     The sum number of all reversal transactions processed where positions 1-2 of the Processing code in the
        ///     reversal transaction indicated an inquiry (30-39).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_InquiriesReversalNumber;

        /// <summary>
        ///     The sum number of all fee collection transactions.
        /// </summary>
        public object F_75_ReconciliationDataSecondary_FeeCollectionsNumber;

        /// <summary>
        ///     The sum number of all financial presentment transactions processed where positions 1-2 of the Processing
        ///     code in the financial presentment transaction indicated a payment (50-59).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_PaymentsNumber;

        /// <summary>
        ///     The sum number of all reversal transactions processed where positions 1-2 of the Processing code in the
        ///     reversal transaction indicated a payment (50-59).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_PaymentsReversalNumber;

        /// <summary>
        ///     The sum number of all financial presentment transactions processed where positions 1-2 of the Processing
        ///     code in the financial presentment transaction indicated a transfer (40-49).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_TransfersNumber;

        /// <summary>
        ///     The sum number of all reversal transactions processed where positions 1-2 of the Processing code in the
        ///     reversal transaction indicated a transfer (40-49).
        /// </summary>
        public object F_75_ReconciliationDataSecondary_TransfersReversalNumber;

        /// <summary>
        /// </summary>
        public object F_76_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_77_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_78_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_79_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_80_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_81_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_82_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_83_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_84_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_85_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_86_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_87_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_88_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_89_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_90_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_91_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_92_ReservedForISOUse;

        /// <summary>
        ///     Code identifying the institution that is the transaction destination.
        /// </summary>
        public object F_93_TransactionDestinationInstitutionIdentificationCode;

        /// <summary>
        ///     Code identifying the institution that is the transaction originator.
        /// </summary>
        public object F_94_TransactionOriginatorInstitutionIdentificationCode;

        /// <summary>
        ///     Data supplied by a card issuer in an authorization response message, financial presentment response
        ///     message, or in a chargeback transaction that the acquirer may be required to be provided in subsequent
        ///     transactions.
        /// </summary>
        public object F_95_CardIssuerReferenceData;

        /// <summary>
        ///     Contains data related to key management (in accordance with ISO 13492).
        /// </summary>
        public object F_96_KeyManagementData;

        /// <summary>
        ///     The net value of all gross amounts.
        /// </summary>
        public CurrencyUnit F_97_AmountNetReconciliation;

        // /// <summary>
        // /// Code defining the currency of the amount contained in Amount net reconciliation (in accordance with ISO
        // ///     4217).
        // /// </summary>
        // public object F_97_AmountNetReconciliation_CurrencyCode;
        // /// <summary>
        // /// Currency minor unit of Amount net reconciliation. Indicates the number of places the decimal point shall
        // ///     be moved to the left, starting from the rightmost numeric digit.
        // /// </summary>
        // public object F_97_AmountNetReconciliation_MinorUnit;
        // /// <summary> 
        // /// The net reconciliation amount in the indicated currency. 
        // /// </summary>
        // public object F_97_AmountNetReconciliation_Value;

        /// <summary>
        ///     The third party beneficiary in a Financial Presentment where the Processing code indicates a payment
        ///     (50-59).
        /// </summary>
        public object F_98_Payee;

        /// <summary>
        ///     Code identifying the settlement institution or its agent.
        /// </summary>
        public object F_99_SettlementInstitutionIdentificationCode;

        /// <summary>
        ///     Code identifying the receiving institution.
        /// </summary>
        public object F_100_ReceivingInstitutionIdentificationCode;

        /// <summary>
        ///     The actual or abbreviated name of the file being accessed.
        /// </summary>
        public object F_101_FileName;

        /// <summary>
        ///     A series of digits and/or characters used to identify a customer account or relationship, e.g. for the
        ///     “from” account.///
        /// </summary>
        public object F_102_AccountIdentification1;

        /// <summary>
        ///     A series of digits and/or characters used to identify a customer account or relationship, e.g. for the
        ///     “to” account.///
        /// </summary>
        public object F_103_AccountIdentification2;

        /// <summary>
        ///     Non financial data captured as part of a transaction.
        /// </summary>
        public object F_104_TransactionSpecificData;

        /// <summary>
        ///     Transaction data related to new programs and services, content and format are based on bilateral
        ///     agreement.
        /// </summary>
        public object F_104_71_FreeFormDescriptionData_FreeFormDescriptionData;

        /// <summary>
        /// </summary>
        public object F_104_71_FreeFormDescriptionData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_71_FreeFormDescriptionData_MultipleTLVSubElements;

        /// <summary>
        ///     Indicator of whether tax is included.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceTaxIndicator;

        /// <summary>
        ///     Reference number given by cardholder.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardholderOrderReferenceNumber;

        /// <summary>
        ///     Tax reference number of cardholder’s company.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardholderTaxReferenceNumber;

        /// <summary>
        ///     Reference number on card acceptor invoice.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardAcceptorInvoiceReferenceNumber;

        /// <summary>
        ///     Registered tax reference number of card acceptor.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardAcceptorTaxReferenceNumber;

        /// <summary>
        ///     Total amount of tax on the transaction.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountTotalTax;

        /// <summary>
        ///     Discount amount on total purchase.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountBottomLineDiscount;

        /// <summary>
        ///     Postal code for delivery of goods.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceDestinationPostalCode;

        /// <summary>
        ///     Postal code from which goods are to be shipped.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceShipFromPostalCode;

        /// <summary>
        ///     "Country of destination for delivery of goods (in accordance with ISO 3166)
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceDestinationCountryCode;

        /// <summary>
        ///     Date when order was placed.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceOrderDate;

        /// <summary>
        ///     Amount charged for freight/delivery including any tax.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountFreightIncludingTax;

        /// <summary>
        ///     Cardholder's internal accounting cost centre code.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCostCentre;

        /// <summary>
        ///     Cardholder supplied reference number.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCustomerReference;

        /// <summary>
        ///     Order number allocated by the card acceptor.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardAcceptorOrderNumber;

        /// <summary>
        ///     Invoice total, net of tax.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceValueExcludingTax;

        /// <summary>
        ///     Invoice number of the original invoice. Used to identify the original invoice on a refund transaction.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceOriginalInvoiceNumber;

        /// <summary>
        ///     Card acceptors reference number for corporate customer.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardAcceptorCustomerReferenceNumber;

        /// <summary>
        ///     Rate of tax to be applied to Invoice freight amount excluding tax to calculate the tax on the freight
        ///     charges.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceFreightTaxRate;

        /// <summary>
        ///     Amount charged for freight/delivery excluding any tax.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountFreightExcludingTax;

        /// <summary>
        ///     A code allocated by the acquirer, describing various classifications of business ownership
        ///     characteristics, e.g. small business.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceCardAcceptorType;

        /// <summary>
        ///     Duty on the total purchase amount.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountDuty;

        /// <summary>
        ///     State or province within the country where the purchased goods will be delivered.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceDestinationStateProvinceCode;

        /// <summary>
        ///     Amount of the alternate tax on the purchase.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountAlternateTax;

        /// <summary>
        ///     Indicates whether alternate tax amount is included in the total purchase amount.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAmountAlternateTaxIndicator;

        /// <summary>
        ///     The card acceptors tax identification number for the Invoice amount alternate tax.
        /// </summary>
        public object F_104_72_InvoicingHeaderData_InvoiceAlternateTaxIdentifier;

        /// <summary>
        /// </summary>
        public object F_104_72_InvoicingHeaderData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_72_InvoicingHeaderData_MultipleTLVSubElements;

        /// <summary>
        ///     Identifies the sequence in which multiple datasets with the same dataset identifier should be processed.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemDatasetSequenceNumber;

        /// <summary>
        ///     Code of goods purchased. Defined by national tax authorities.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemCommodityCode;

        /// <summary>
        ///     Code which identifies a specific product as supplied by the card acceptor.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemProductCode;

        /// <summary>
        ///     Description of item purchased.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemDescriptor;

        /// <summary>
        ///     Unit of measure of quantity purchased as defined by card acceptor.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemUnitOfMeasure;

        /// <summary>
        ///     Price per unit net of tax.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemUnitPriceExcludingTax;

        /// <summary>
        ///     Quantity of goods purchased.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemProductQuantity;

        /// <summary>
        ///     Amount of discount per line if unit price is before discount.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemAmountDiscount;

        /// <summary>
        ///     Rate of tax to be applied to calculate the tax amount.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemTaxRate;

        /// <summary>
        ///     Total line amount excluding tax.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemValueExcludingTax;

        /// <summary>
        ///     Total line amount including tax.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemValueIncludingTax;

        /// <summary>
        ///     Tax amount for the line item.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemAmountTax;

        /// <summary>
        ///     Indicates the number of places the decimal point shall be moved to the left, starting from the rightmost
        ///     numeric digit of Line item product quantity.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemQuantityMinorUnit;

        /// <summary>
        ///     Indicates whether the line item amount is discounted.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemDiscountIndicator;

        /// <summary>
        ///     Code, defined by the acquirer, identifying the type of tax applied to the line item.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemTaxType;

        /// <summary>
        ///     Indicates whether the line item value is a debit or a credit.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemValueDebitCreditIndicator;

        /// <summary>
        ///     Discount rate for the line item.
        /// </summary>
        public object F_104_73_InvoicingLineItemData_LineItemDiscountRate;

        /// <summary>
        /// </summary>
        public object F_104_73_InvoicingLineItemData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_73_InvoicingLineItemData_MultipleTLVSubElements;

        /// <summary>
        ///     Ticket number as supplied by airline.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineTicketNumber;

        /// <summary>
        ///     Name of airline carrier as defined by ATPCO
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineCarrierName;

        /// <summary>
        ///     Code of travel agency which sold airline ticket as defined by ATPCO.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineTravelAgencyCode;

        /// <summary>
        ///     Name of ticket agency which sold the airline ticket.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineTicketAgencyName;

        /// <summary>
        ///     Plan number as supplied by airline.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlinePlanNumber;

        /// <summary>
        ///     Invoice number as supplied by airline.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineInvoiceNumber;

        /// <summary>
        ///     Currency code of the original transaction. Used to identify the original currency in a refund transaction
        ///     (in accordance with ISO 4217).
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineOriginalCurrencyCode;

        /// <summary>
        ///     Name of airline passenger.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlinePassengerName;

        /// <summary>
        ///     Cardholder supplied reference number.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineCustomerReference;

        /// <summary>
        ///     Amount of the original invoice. Used to identify the original amount on a refund transaction.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineAmountOriginalInvoice;

        /// <summary>
        ///     Address at which the airline ticket was issued.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineTicketIssueAddress;

        /// <summary>
        ///     Date on which the airline ticket was issued.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineDateTicketIssue;

        /// <summary>
        ///     The total airline ticket amount.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineAmountTotalFare;

        /// <summary>
        ///     The total fees associated with an airline ticket.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineAmountTotalFees;

        /// <summary>
        ///     The total taxes associated with an airline ticket.
        /// </summary>
        public object F_104_74_AirlineItineraryData_AirlineAmountTotalTaxes;

        /// <summary>
        /// </summary>
        public object F_104_74_AirlineItineraryData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_74_AirlineItineraryData_MultipleTLVSubElements;

        /// <summary>
        ///     Identifies the sequence in which multiple datasets with the same dataset identifier should be processed.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegDatasetSequenceNumber;

        /// <summary>
        ///     Number of leg or stage of airline journey of a multi-flight booking.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegNumber;

        /// <summary>
        ///     Airport from which journey/leg departs as defined by ATPCO.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegDepartureAirport;

        /// <summary>
        ///     Code of airline carrier as defined by ATPCO.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegCarrierCode;

        /// <summary>
        ///     Basis for calculation of airline fare as defined by ATPCO.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegFareBasisCode;

        /// <summary>
        ///     Class of airline journey as defined by ATPCO.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegClassOfTravel;

        /// <summary>
        ///     Code of stop over airport during an airline journey as defined by ATPCO.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegStopOverCode;

        /// <summary>
        ///     Code of destination airport as defined by ATPCO.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegDestinationCode;

        /// <summary>
        ///     Date of departure of airline journey.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegDateOfTravel;

        /// <summary>
        ///     Tax payable by airline passenger on departure.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegAmountDepartureTax;

        /// <summary>
        ///     The ticket that contains additional coupons on an itinerary that is more than four trip legs.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegConjunctionTicketNumber;

        /// <summary>
        ///     The original airline ticket number replaced by a new ticket number.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegExchangeTicketNumber;

        /// <summary>
        ///     Identifies the coupon associated with each trip leg.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegCouponNumber;

        /// <summary>
        ///     Number assigned to each trip leg by the operating or marketing air carrier.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegFlightNumber;

        /// <summary>
        ///     Time of departure for each trip leg.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegDepartureTime;

        /// <summary>
        ///     Indicates whether the departure time is in the a.m. or p.m.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegDepartureTimeSegmentCode;

        /// <summary>
        ///     Time of arrival for each trip leg.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegArrivalTime;

        /// <summary>
        ///     Indicates whether the arrival time is in the a.m. or the p.m.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegArrivalTimeSegmentCode;

        /// <summary>
        ///     The trip leg airline ticket amount.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegAmountFare;

        /// <summary>
        ///     The fees associated with each trip leg.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegAmountFees;

        /// <summary>
        ///     The total taxes associated with each trip leg.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegAmountTaxes;

        /// <summary>
        ///     Agency-added or government required notations, or restrictions such as non-refundable, that are
        ///     applicable to a trip leg.
        /// </summary>
        public object F_104_75_AirlineJourneyData_TripLegEndorsementsRestrictions;

        /// <summary>
        /// </summary>
        public object F_104_75_AirlineJourneyData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_75_AirlineJourneyData_MultipleTLVSubElements;

        /// <summary>
        ///     Address from where automobile was rented.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalAddress;

        /// <summary>
        ///     Date from which auto rental starts.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalDate;

        /// <summary>
        ///     Time from which auto rental period starts.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalTime;

        /// <summary>
        ///     Address to which the automobile was/will be returned.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnAddress;

        /// <summary>
        ///     Date on which automobile was/will be returned.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnDate;

        /// <summary>
        ///     Time by which automobile was/will be returned.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnTime;

        /// <summary>
        ///     Distance travelled during rental period (see Auto distance unit of measure).
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalDistance;

        /// <summary>
        ///     Unit of measure of distance travelled.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoDistanceUnitOfMeasure;

        /// <summary>
        ///     Reference number on auto rental agreement form.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalAgreementReference;

        /// <summary>
        ///     Registration number of rented or fleet automobile.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoVehicleRegistrationNumber;

        /// <summary>
        ///     Reading of total distance travelled by vehicle.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoOdometerReading;

        /// <summary>
        ///     Name of the person making the vehicle rental agreement.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRenterName;

        /// <summary>
        ///     City to which the automobile was/will be returned.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnCity;

        /// <summary>
        ///     State or province within the country to which the automobile was/will be returned.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnStateProvince;

        /// <summary>
        ///     Country to which the automobile was/will be returned (in accordance with ISO 3166).
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnCountry;

        /// <summary>
        ///     Agency code, phone number, or other abbreviation used to identify the location to which the automobile
        ///     was/will be returned.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoReturnLocationIdentifier;

        /// <summary>
        ///     Customer service number that the cardholder may call to resolve questions or disputes.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoCustomerServiceTollFreePhoneNumber;

        /// <summary>
        ///     Rental rate charged for the vehicle.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalRate;

        /// <summary>
        ///     Indicates the time period to which the auto rental rate applies, e.g. daily, weekly etc.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalRateTimePeriod;

        /// <summary>
        ///     The number of free miles or kilometres allowed to a customer for the duration of the vehicle rental
        ///     agreement.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoMaximumFreeMilesKilometres;

        /// <summary>
        ///     Amount of vehicle insurance purchased as part of the vehicle rental agreement.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoAmountVehicleInsurance;

        /// <summary>
        ///     Indicates whether the customer purchased vehicle insurance as part of the vehicle rental agreement.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoVehicleInsuranceIndicator;

        /// <summary>
        ///     Amount of miscellaneous charges incurred after the vehicle was rented, e.g. extra hours.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoAmountAdjusted;

        /// <summary>
        ///     Indicates the type of charges provided in Auto amount adjusted.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoAmountAdjustedIndicatorCode;

        /// <summary>
        ///     Code allocated by the acquirer that identifies special circumstances, e.g. frequent renter or “no show”.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoProgramCode;

        /// <summary>
        ///     City in which the automobile was rented.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalCity;

        /// <summary>
        ///     State or province within the country in which the automobile was rented.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalStateProvince;

        /// <summary>
        ///     Country in which the automobile was rented (in accordance with ISO 3166).
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalCountry;

        /// <summary>
        ///     Classification defined by the acquirer of the vehicle rented, e.g. midsize, luxury.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalClassIdentifier;

        /// <summary>
        ///     Agency code, phone number, or other abbreviation used to identify the location from which the automobile
        ///     was rented.
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_AutoRentalLocationIdentifier;

        /// <summary>
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_76_AutoRentalVehicleData_MultipleTLVSubElements;

        /// <summary>
        ///     Customer service number that the cardholder may call to resolve questions or disputes.
        /// </summary>
        public object F_104_77_LodgingData_LodgingCustomerServiceTollFreePhoneNumber;

        /// <summary>
        ///     Date on which the cardholder checked into the lodging facility.
        /// </summary>
        public object F_104_77_LodgingData_LodgingDateArrival;

        /// <summary>
        ///     Date on which the cardholder checked out of the lodging facility.
        /// </summary>
        public object F_104_77_LodgingData_LodgingDateDeparture;

        /// <summary>
        ///     Lodging facility’s internal invoice or billing identification reference number.
        /// </summary>
        public object F_104_77_LodgingData_LodgingFolioNumber;

        /// <summary>
        ///     Local phone number of the lodging facility at which the cardholder stayed.
        /// </summary>
        public object F_104_77_LodgingData_LodgingFacilityPhoneNumber;

        /// <summary>
        ///     Amount of any additional charges incurred after the cardholder’s departure from the lodging facility.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountBillingAdjustment;

        /// <summary>
        ///     Daily room charges exclusive of taxes and fees.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountRoomRate;

        /// <summary>
        ///     Daily room tax amount.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountRoomTax;

        /// <summary>
        ///     Code allocated by the acquirer that identifies special circumstances, e.g. frequent renter or “no show”.
        /// </summary>
        public object F_104_77_LodgingData_LodgingProgramCode;

        /// <summary>
        ///     Total amount of phone calls charged to room.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountPhoneCharges;

        /// <summary>
        ///     Total amount of restaurant and/or room service food charged to room.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountRestaurantRoomService;

        /// <summary>
        ///     Total amount of bar and in-room “mini-bar” items charged to room.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountBarMiniBar;

        /// <summary>
        ///     Total amount of laundry and dry cleaning items charged to room.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountLaundryDryCleaning;

        /// <summary>
        ///     The total amount of miscellaneous items/services charged to the room, not specified elsewhere.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountOtherServices;

        /// <summary>
        ///     Indicates the type of charges provided in Lodging amount other services. Values provided by acquirer.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountOtherServicesIndicator;

        /// <summary>
        ///     Total amount of gift shop and speciality shop items charged to room.
        /// </summary>
        public object F_104_77_LodgingData_LodgingAmountGiftShop;

        /// <summary>
        /// </summary>
        public object F_104_77_LodgingData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_77_LodgingData_MultipleTLVSubElements;

        /// <summary>
        ///     Acquirer’s abbreviation for the brand name of the card acceptor's oil company.
        /// </summary>
        public object F_104_78_FleetCardData_FleetMotorFuelOilCompanyBrandName;

        /// <summary>
        ///     Type of service received at the acceptor location.
        /// </summary>
        public object F_104_78_FleetCardData_FleetMotorFuelServiceTypeCode;

        /// <summary>
        ///     Code which identifies a specific product as supplied by the card acceptor.
        /// </summary>
        public object F_104_78_FleetCardData_FleetProductCode;

        /// <summary>
        ///     Price per unit net of tax.
        /// </summary>
        public object F_104_78_FleetCardData_FleetUnitPriceExcludingTax;

        /// <summary>
        ///     Price per unit inclusive of tax
        /// </summary>
        public object F_104_78_FleetCardData_FleetUnitPriceIncludingTax;

        /// <summary>
        ///     Unit of measure of quantity purchased as defined by card acceptor.
        /// </summary>
        public object F_104_78_FleetCardData_FleetUnitOfMeasure;

        /// <summary>
        ///     Quantity of goods purchased.
        /// </summary>
        public object F_104_78_FleetCardData_FleetProductQuantity;

        /// <summary>
        ///     Indicates the number of places the decimal point shall be moved to the left, starting from the rightmost
        ///     numeric digit of Fleet product quantity.
        /// </summary>
        public object F_104_78_FleetCardData_FleetQuantityMinorUnit;

        /// <summary>
        ///     Total line amount excluding tax.
        /// </summary>
        public object F_104_78_FleetCardData_FleetLineItemValueExcludingTax;

        /// <summary>
        ///     Total line amount including tax.
        /// </summary>
        public object F_104_78_FleetCardData_FleetLineItemValueIncludingTax;

        /// <summary>
        ///     Reading of total distance travelled by vehicle.
        /// </summary>
        public object F_104_78_FleetCardData_FleetOdometerReading;

        /// <summary>
        ///     Registration number of rented or fleet automobile.
        /// </summary>
        public object F_104_78_FleetCardData_FleetVehicleRegistrationNumber;

        /// <summary>
        ///     The number assigned to the driver by the employer for purposes of tracking fuel purchases.
        /// </summary>
        public object F_104_78_FleetCardData_FleetDriverNumberOtherIdentificationNumber;

        /// <summary>
        ///     Contains a code read from a card that indicates terminal prompts that occur at the point of service.
        /// </summary>
        public object F_104_78_FleetCardData_FleetMotorFuelPromptCode;

        /// <summary>
        ///     Discount amount on total purchase.
        /// </summary>
        public object F_104_78_FleetCardData_FleetAmountBottomLineDiscount;

        /// <summary>
        ///     Total amount of tax on the transaction.
        /// </summary>
        public object F_104_78_FleetCardData_FleetTotalTaxAmount;

        /// <summary>
        /// </summary>
        public object F_104_78_FleetCardData_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_104_78_FleetCardData_MultipleTLVSubElements;

        /// <summary>
        /// </summary>
        public object F_105_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_106_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_107_ReservedForISOUse;

        /// <summary>
        /// </summary>
        public object F_108_ReservedForISOUse;

        /// <summary>
        ///     The sum amount of Amount fee in all authorization, financial presentment, reversal and fee collection
        ///     messages where the Amount fee “x” indicated a credit “C”.
        /// </summary>
        public object F_109_ReconciliationFeeAmountsCredit;

        /// <summary>
        ///     Code indicating the type of fee. Contained in the data elements Amounts fees, Reconciliation fee amounts
        ///     credit and Reconciliation fee amounts debit.
        /// </summary>
        public object F_109_ReconciliationFeeAmountsCredit_FeeTypeCode;

        /// <summary>
        ///     The sum amount of all fees of the fee type specified. Contained in Reconciliation fee amounts credit and
        ///     Reconciliation fee amounts debit.
        /// </summary>
        public object F_109_ReconciliationFeeAmountsCredit_AmountFeeTotal;

        /// <summary>
        ///     The count of all fees of the fee type specified. Contained in the Reconciliation fee amounts credit and
        ///     Reconciliation fee amounts debit data elements.
        /// </summary>
        public object F_109_ReconciliationFeeAmountsCredit_NumberFeeTotal;

        /// <summary>
        ///     The sum amount of Amount fee in all authorization, financial presentment, reversal and fee collection
        ///     messages where the Amount fee “x” indicated a debit “D”.
        /// </summary>
        public object F_110_ReconciliationFeeAmountsDebit;

        /// <summary>
        ///     Code indicating the type of fee. Contained in the data elements Amounts fees, Reconciliation fee amounts
        ///     credit and Reconciliation fee amounts debit.
        /// </summary>
        public object F_110_ReconciliationFeeAmountsDebit_FeeTypeCode;

        /// <summary>
        ///     The sum amount of all fees of the fee type specified. Contained in Reconciliation fee amounts credit and
        ///     Reconciliation fee amounts debit.
        /// </summary>
        public object F_110_ReconciliationFeeAmountsDebit_AmountFeeTotal;

        /// <summary>
        ///     The count of all fees of the fee type specified. Contained in the Reconciliation fee amounts credit and
        ///     Reconciliation fee amounts debit data elements.
        /// </summary>
        public object F_110_ReconciliationFeeAmountsDebit_NumberFeeTotal;

        /// <summary>
        /// </summary>
        public object F_111_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_112_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_113_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_114_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_115_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_116_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_117_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_118_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_119_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_120_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_121_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_122_ReservedForNationUse;

        /// <summary>
        /// </summary>
        public object F_123_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_124_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_125_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_126_ReservedForPrivateUse;

        /// <summary>
        /// </summary>
        public object F_127_ReservedForPrivateUse;

        /// <summary>
        ///     Used to validate the source and the text of the message between the sender and receiver (in accordance
        ///     with ISO 9807).
        /// </summary>
        public object F_128_MessageAuthenticationCode;

        #endregion

        protected override void UpdateSecBitmap()
        {
            byte[] secMap = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (var pair in
                this.Fields().Where(
                    pair => FIELDMethods.Val(pair.Key) > 64 && FIELDMethods.Val(pair.Key) < 129 && pair.Value != null))
            {
                Mark(secMap, FIELDMethods.Val(pair.Key) - 64);
            }

            var result = secMap.Aggregate(0x00, (current, b) => current | b);

            if (result != 0x00)
            {
                this.FieldStorage[FIELD.F_1] = secMap;
            }
        }

        public override string Identity()
        {
            return this.MessageTypeIndicator + this.F_3_ProcessingCode + this.F_24_FunctionCode;
        }
    }
}

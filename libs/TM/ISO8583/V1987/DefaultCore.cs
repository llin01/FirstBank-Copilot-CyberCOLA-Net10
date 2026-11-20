namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    #region DEPENDENCY

    using System.Collections.Generic;

    using Com.Cybersoft4u.TM.ISO8583.Category.CardType;

    #endregion

    public class DefaultCore : Core
    {
        public bool NullSecondBitmap = false;

        public string CardNo = null;

        public CardType CardType = CardType.Default;

        //public CardType CardType = null;

        public string ExpireDate = null;

        public string AeEffectiveDate = null;

        public string AeDiscretionaryData = null;

        public string AeInterchangeDesignator = null;

        public string AeZero = null;

        public string AeLanguage = null;

        public string ServiceCode = null;

        public string Pvki = null;

        public string Pvv = null;

        public string Cvv = null;

        public string Pan = null;

        public string ProcessingCode = null;

        public string TransactionAmount = null;

        public string CardholderBillingAmount = null;

        public string TransmissionDatetime = null;

        public string SystemTraceAuditNumber = null;

        public string LocalTransactionTime = null;

        public string LocalTransactionDate = null;

        public string SettlementDate = null;

        public string CaptureDate = null;

        public string MerchantCategoryCode = null;

        public string PointOfServiceEntryMode = null;

        public string CardSequenceNumber = null;

        public string FunctionCode = null;

        public string NetworkInternationalIdentifier = null;

        public string MessageReasonCode = null;

        public string PointOfServiceConditionMode = null;

        public string TransactionFeeAmount = null;

        public string AcquiringInstitutionIdentificationCode = null;

        public string RetrievalReferenceNumber = null;

        public string ApproveCode = null;

        public string ResponseCode = null;

        public string TerminalId = null;

        public string MerchantId = null;

        public string BankName = null;

        public string City = null;

        public string State = null;

        public string Country = null;

        public string TransactionCountryCode = null;

        public string TransactionCurrencyCode = null;

        public string CardHolderBillingAmountCurrencyCode = null;

        public string PostCode = null;

        public string OriginMti = null;

        public string ReplacementAmount = null;

        public string SettlementInstitutionIdentificationCode = null;

        public string Pinblock = null;

        public string NcccBankCode = null;

        public string OriginRetrievalReferenceNumber = null;

        public string OriginTransactionDate = null;

        /* HHMMSSmm */
        public string OriginTransactionTime = null;

        public string OriginCaptureDate = null;

        public string AdditionalAmount = null;

        public string MessageAuthenticationCode = null;

        public string NetworkManagementInformationCode = null;

        /* add field for TWIN Card */

        public string NetworkInfoSubAcquirerID = null;

        public string NetworkInfoNetworkReferenceID = null;

        protected IDictionary<string, object> Others = new Dictionary<string, object>();

        public object GetOthersValByKey(string key)
        {
            object val;
            this.Others.TryGetValue(key, out val);
            return val;
        }

        public void SetOthersValByKey(string key, object val)
        {
            this.Others[key] = val;
        }

        public void PurgeOthersValByKey(string key)
        {
            this.Others.Remove(key);
        }

        public object GetFieldsByKey(FIELD key)
        {
            object val;
            this.FieldStorage.TryGetValue(key, out val);
            return val;
        }
    }
}

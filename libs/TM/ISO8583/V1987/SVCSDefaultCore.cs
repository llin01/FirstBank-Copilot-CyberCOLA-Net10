namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    #region DEPENDENCY

    using System.Collections.Generic;

    #endregion

    public class SVCSDefaultCore : Core
    {
        public bool NullSecondBitmap = false;

        public string ServiceCode = null;

        public string Pvv = null;

        public string Pan = null;

        public string ProcessingCode = null;

        public string TransactionAmount = null;

        public string TransmissionDatetime = null;

        public string SystemTraceAuditNumber = null;

        public string LocalTransactionTime = null;

        public string LocalTransactionDate = null;

        public string ExpirationDate = null;

        public string SettlementDate = null;

        public string AcquiringInstitutionIdentificationCode = null;

        public string RetrievalReferenceNumber = null;

        public string ResponseCode = null;

        public string TerminalId = null;

        public string MerchantId = null;

        public string F58SVCSChipRelatedData = null;

        public string NetworkManagementInformationCode = null;

        public string ActionDate = null;

        public string OriginMti = null;

        public string OriginTransactionDateTime = null;

        public string OriginSTAN = null;

        public string OriginAcquiringID = null;

        public string OriginReferenceNumber = null;

        public string FileUpdateCode = null;

        public string FileSecurityCode = null;

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

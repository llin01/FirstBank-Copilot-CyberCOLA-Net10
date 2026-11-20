namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    #region DEPENDENCY

    using System.Collections.Generic;

    #endregion

    public class ICASHChipDefaultCore : Core
    {
        public bool NullSecondBitmap = false;

        public string TransDate = null;

        public string StoreNo = null;

        public string RegID = null;

        public string SN = null;

        public string ACTDate = null;

        public string TransType = null;

        public string TxDateTime = null;

        public string ResponseCode = null;

        public string IcashCardNo = null;

        public string Amount = null;

        public string TSAM_OSN = null;

        public string TSAM_TSN = null;

        public string ICC_TSN = null;

        public string ICC_LSN = null;

        public string ICC_LA = null;

        public string ICC_DSN = null;

        public string ICC_DA = null;

        public string LSAM_ID = null;

        public string LSAM_CSN = null;

        public string LSAM_CA = null;

        public string LSAM_DSN = null;

        public string LSAM_DA = null;

        public string LSAM_DV = null;

        public string PSAM_ID = null;

        public string PSAM_CSN = null;

        public string PSAM_CA = null;

        public string PSAM_SBN = null;

        public string PSAM_NRS = null;

        public string PSAM_SV = null;

        public string CardExpirationDate = null;

        public string MAC = null;

        public string HSM_CODE = null;

        public string HSRW_Terminal_SN = null;

        public string MECM_ID = null;

        public string ICC_BL = null;

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

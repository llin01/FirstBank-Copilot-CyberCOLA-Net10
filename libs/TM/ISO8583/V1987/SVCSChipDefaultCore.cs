namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    #region DEPENDENCY

    using System.Collections.Generic;

    #endregion

    public class SVCSChipDefaultCore : Core
    {
        public bool NullSecondBitmap = false;

        public string TXCode = null;

        public string BatchNumber = null;

        public string ReloadFunction = null;

        public string HostTXDateTime = null;

        public string TerminalTXDateTime = null;

        public string TerminalTraceNo = null;

        public string CardHolderBirthday = null;

        public string ICCSNum = null;

        public string ICCTP = null;

        public string LSN = null;

        public string LRN = null;

        public string DSN = null;

        public string SAMID = null;

        public string SAMSN = null;

        public string SAMCRN = null;

        public string EasyCashBalanceBeforeTransaction = null;

        public string Loyalty1Balance = null;

        public string Loyalty2Balance = null;

        public string EasyCashValueAdd = null;

        public string EasyCashDeduction = null;

        public string EasyCashTransactionAmount = null;

        public string Loyalty1TransactionAmount = null;

        public string Loyalty2TransactionAmount = null;

        public string LTACLoadingCommand = null;

        public string DTAC = null;

        public string PTAC = null;

        public string TCCreditLoyalty1 = null;

        public string TCCreditLoyalty2 = null;

        public string TCUpdateLastOnlineDate = null;

        public string TCBlockCard = null;

        public string SourceTransactionTerminalTraceNo = null;

        public string SourceTransactionTXDateTime = null;

        public string SourceTransactionSNum = null;

        public string SourceTransactionEasyCashBalanceBeforeTransaction = null;




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

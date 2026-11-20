using Newtonsoft.Json;

namespace Com.Cybersoft4u.CommonLib.EAI
{
    public class FCB9GQ
    {
        public class FCB9GQ_REQ_HEADER
        {
            public string TxID { get; set; }
            public string SystemKey { get; set; }
            public string TxSeqNo { get; set; }
            public string MsgSeqNo { get; set; }
            public string CltTimeStamp { get; set; }
            public string CustID { get; set; }
            public string UserID { get; set; }
            public string AcctNo { get; set; }
            public string SvrTimeStamp { get; set; }
            public string MsgDirection { get; set; }
            public string System { get; set; }
            public string StatusCode { get; set; }
            public string StatusDesc { get; set; }
        }
        public class FCB9GQ_REQ_BODY
        {
            //public string CreditCardNo { get; set; }
            [JsonProperty("CreditCardNo")]
            public string CardNo { get; set; }
            public string MerPAN { get; set; }
            public string TxnSeqNo { get; set; }
            public string TxnAmtNTD { get; set; }
            public string MCC { get; set; }
            public string CurCode { get; set; }
            public string OriTxnAmt { get; set; }
            public string CountryCode { get; set; }
            public string MerName { get; set; }
            public string MerCity { get; set; }
            public string WalletAppInd { get; set; }
            public string ReserveField { get; set; }
            public string QRLen { get; set; }
            public string QRMsg { get; set; }
        }

        public class FCB9GQ_REQ
        {
            public string TXN_ID { get; set; }
            public string FN_CODE { get; set; }
            public string TERMINAL_ID { get; set; }
            public string USER_ID { get; set; }
            public FCB9GQ_REQ_HEADER Header { get; set; }
            public FCB9GQ_REQ_BODY BODY { get; set; }
        }

        public class FCB9GQ_RSP_BODY
        {
            public string TxnDate { get; set; }
            public string TxnTime { get; set; }
            public string CreditCardNo { get; set; }
            public string MerPAN { get; set; }
            public string QRTxnSeqNo { get; set; }
            public string TxnAmtNTD { get; set; }
            public string AuthorizationCode { get; set; }
        }

        public class FCB9GQ_RSP
        {
            public string FMH { get; set; }
            public string FN_CODE { get; set; }
            public string RESPONSE_CODE { get; set; }
            public string RESPONSE_DESCRIPTION { get; set; }
            public string TERMINAL_ID { get; set; }
            public string USER_ID { get; set; }
            public FCB9GQ_RSP_BODY BODY { get; set; }
        }
    }
}

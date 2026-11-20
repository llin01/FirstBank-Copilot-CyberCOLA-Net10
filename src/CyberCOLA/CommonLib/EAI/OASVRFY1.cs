namespace Com.Cybersoft4u.CommonLib.EAI
{
    public class OASVRFY1
    {
        public class OASVRFY1_REQ_BODY
        {
            public string CARD_NBR { get; set; }
            public string EXP_DTE { get; set; }
            public string CARD_CVV2 { get; set; }
            public string ID { get; set; }
            public string MCC_CODE { get; set; }
            public string CELL_PHONE { get; set; }
            public string CUST_BIRTHDAY { get; set; }
            public string MERCH_NO { get; set; }
            public string RTN_CODE { get; set; }
            public string AUTH_CODE { get; set; }

        }

        public class OASVRFY1_REQ
        {
            public string TXN_ID { get; set; }
            public string FN_CODE { get; set; }
            public string TERMINAL_ID { get; set; }
            public string USER_ID { get; set; }
            public OASVRFY1_REQ_BODY BODY { get; set; }
        }

        public class OASVRFY1_RSP_BODY
        {
            public string CARD_NBR { get; set; }
            public string EXP_DTE { get; set; }
            public string CARD_CVV2 { get; set; }
            public string ID { get; set; }
            public string MCC_CODE { get; set; }
            public string CELL_PHONE { get; set; }
            public string CUST_BIRTHDAY { get; set; }
            public string MERCH_NO { get; set; }
            public string RTN_CODE { get; set; }
            public string AUTH_CODE { get; set; }
            public string ERRORS { get; set; }

        }

        public class OASVRFY1_RSP
        {
            public string FMH { get; set; }
            public string FN_CODE { get; set; }
            public string RESPONSE_CODE { get; set; }
            public string RESPONSE_DESCRIPTION { get; set; }
            public string TERMINAL_ID { get; set; }
            public string USER_ID { get; set; }
            public OASVRFY1_RSP_BODY BODY { get; set; }
        }
    }
}

using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.MQ
{
    /// <summary>
    /// Contains XML serializable data models for Virtual Data Message Queue (VDMQ) request and response messages.
    /// </summary>
    public class VDMessage
    {
        /// <summary>
        /// Request message header containing transaction identification and routing information.
        /// </summary>
        [XmlRoot(ElementName = "Header")]
        public class RequestHeader
        {
            [XmlElement(ElementName = "TxID")]
            public string TxID { get; set; }
            [XmlElement(ElementName = "SystemKey")]
            public string SystemKey { get; set; }
            [XmlElement(ElementName = "TxSeqNo")]
            public string TxSeqNo { get; set; }
            [XmlElement(ElementName = "MsgSeqNo")]
            public string MsgSeqNo { get; set; }
            [XmlElement(ElementName = "CltTimeStamp")]
            public string CltTimeStamp { get; set; }
            [XmlElement(ElementName = "CustID")]
            public string CustID { get; set; }
            [XmlElement(ElementName = "UserID")]
            public string UserID { get; set; }
            [XmlElement(ElementName = "AcctNo")]
            public string AcctNo { get; set; }
            [XmlElement(ElementName = "MsgDirection")]
            public string MsgDirection { get; set; }
        }

        /// <summary>
        /// Response message header containing transaction identification, routing, and status information.
        /// </summary>
        [XmlRoot(ElementName = "Header")]
        public class ResponseHeader
        {
            [XmlElement(ElementName = "TxID")]
            public string TxID { get; set; }
            [XmlElement(ElementName = "SystemKey")]
            public string SystemKey { get; set; }
            [XmlElement(ElementName = "TxSeqNo")]
            public string TxSeqNo { get; set; }
            [XmlElement(ElementName = "MsgSeqNo")]
            public string MsgSeqNo { get; set; }
            [XmlElement(ElementName = "CltTimeStamp")]
            public string CltTimeStamp { get; set; }
            [XmlElement(ElementName = "CustID")]
            public string CustID { get; set; }
            [XmlElement(ElementName = "UserID")]
            public string UserID { get; set; }
            [XmlElement(ElementName = "AcctNo")]
            public string AcctNo { get; set; }
            [XmlElement(ElementName = "SvrTimeStamp")]
            public string SvrTimeStamp { get; set; }
            [XmlElement(ElementName = "MsgDirection")]
            public string MsgDirection { get; set; }
            [XmlElement(ElementName = "System")]
            public string System { get; set; }
            [XmlElement(ElementName = "StatusCode")]
            public string StatusCode { get; set; }
            [XmlElement(ElementName = "StatusDesc")]
            public string StatusDesc { get; set; }
        }

        /// <summary>
        /// Transaction request body containing account and transaction details.
        /// </summary>
        [XmlRoot(ElementName = "TxRq")]
        public class TxRq
        {
            [XmlElement(ElementName = "AcctNo")]
            public string AcctNo { get; set; }
            [XmlElement(ElementName = "FuncCode")]
            public string FuncCode { get; set; }
            [XmlElement(ElementName = "TxAmt")]
            public string TxAmt { get; set; }
            [XmlElement(ElementName = "CardNo")]
            public string CardNo { get; set; }
            [XmlElement(ElementName = "CreditNo")]
            public string CreditNo { get; set; }
            [XmlElement(ElementName = "AuthNo")]
            public string AuthNo { get; set; }
        }

        /// <summary>
        /// Transaction response body containing transaction results and account balance information.
        /// </summary>
        [XmlRoot(ElementName = "TxRs")]
        public class TxRs
        {
            [XmlElement(ElementName = "BusType")]
            public string BusType { get; set; }
            [XmlElement(ElementName = "WsID")]
            public string WsID { get; set; }
            [XmlElement(ElementName = "HostSeqNo")]
            public string HostSeqNo { get; set; }
            [XmlElement(ElementName = "TxID")]
            public string TxID { get; set; }
            [XmlElement(ElementName = "TxDate")]
            public string TxDate { get; set; }
            [XmlElement(ElementName = "TxTime")]
            public string TxTime { get; set; }
            [XmlElement(ElementName = "TxBrn")]
            public string TxBrn { get; set; }
            [XmlElement(ElementName = "AcctNo")]
            public string AcctNo { get; set; }
            [XmlElement(ElementName = "FuncCode")]
            public string FuncCode { get; set; }
            [XmlElement(ElementName = "TxnAmt")]
            public string TxnAmt { get; set; }
            [XmlElement(ElementName = "CardNo")]
            public string CardNo { get; set; }
            [XmlElement(ElementName = "CreditNo")]
            public string CreditNo { get; set; }
            [XmlElement(ElementName = "HoldBal")]
            public string HoldBal { get; set; }
            [XmlElement(ElementName = "AuthNo")]
            public string AuthNo { get; set; }
            [XmlElement(ElementName = "AvaiBalSign")]
            public string AvaiBalSign { get; set; }
            [XmlElement(ElementName = "AvaiBal")]
            public string AvaiBal { get; set; }
        }

        /// <summary>
        /// Complete request XML message structure containing header and transaction request.
        /// </summary>
        [XmlRoot(ElementName = "XML")]
        public class RequestXML
        {
            [XmlElement(ElementName = "Header")]
            public RequestHeader RequestHeader { get; set; }
            [XmlElement(ElementName = "TxRq")]
            public TxRq TxRq { get; set; }
        }

        /// <summary>
        /// Complete response XML message structure containing header and transaction response.
        /// </summary>
        [XmlRoot(ElementName = "XML")]
        public class ResponseXML
        {
            [XmlElement(ElementName = "Header")]
            public ResponseHeader ResponseHeader { get; set; }
            [XmlElement(ElementName = "TxRs")]
            public TxRs TxRs { get; set; }
        }
    }
}

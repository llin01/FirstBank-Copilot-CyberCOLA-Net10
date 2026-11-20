using System.Xml.Serialization;

namespace Com.Cybersoft4u.Interchange
{
    [XmlRoot(ElementName = "WebFileMaintDataXml")]
    public class WebFileMaintDataXml
    {
        [XmlElement(ElementName = "WebTxKeySeq")]
        public string WebTxKeySeq { get; set; }

        [XmlElement(ElementName = "WebIsoP2CardNbr")]
        public string WebIsoP2CardNbr { get; set; }

        [XmlElement(ElementName = "WebIsoP3ProcessCde")]
        public string WebIsoP3ProcessCde { get; set; }

        [XmlElement(ElementName = "WebIsoP7TxDteTime")]
        public string WebIsoP7TxDteTime { get; set; }

        [XmlElement(ElementName = "WebIsoP11TraceNo")]
        public string WebIsoP11TraceNo { get; set; }

        [XmlElement(ElementName = "WebIsoP13TransYear")]
        public string WebIsoP13TransYear { get; set; }

        [XmlElement(ElementName = "WebIsoP14ExpirDte")]
        public string WebIsoP14ExpirDte { get; set; }

        [XmlElement(ElementName = "WebIsoP33FwdIICode")]
        public string WebIsoP33FwdIICode { get; set; }

        [XmlElement(ElementName = "WebIsoP36NTID")]
        public string WebIsoP36NTID { get; set; }

        [XmlElement(ElementName = "WebIsoP37RefNbr")]
        public string WebIsoP37RefNbr { get; set; }

        [XmlElement(ElementName = "WebIsoP39Rsp")]
        public string WebIsoP39Rsp { get; set; }

        [XmlElement(ElementName = "WebIsoP48AdditionalData")]
        public string WebIsoP48AdditionalData { get; set; }

        [XmlElement(ElementName = "WebIsoP49TranCurCode")]
        public string WebIsoP49TranCurCode { get; set; }

        [XmlElement(ElementName = "WebIsoP58T05TxDte")]
        public string WebIsoP58T05TxDte { get; set; }

        [XmlElement(ElementName = "WebIsoP60FromHostMaintData")]
        public string WebIsoP60FromHostMaintData { get; set; }

        [XmlElement(ElementName = "WebIsoP63SystemID")]
        public string WebIsoP63SystemID { get; set; }

        [XmlElement(ElementName = "WebIsoS73ActDte")]
        public string WebIsoS73ActDte { get; set; }

        [XmlElement(ElementName = "WebIsoS91FileUpdCode")]
        public string WebIsoS91FileUpdCode { get; set; }

        [XmlElement(ElementName = "WebIsoS92FileSecurityCode")]
        public string WebIsoS92FileSecurityCode { get; set; }

        [XmlElement(ElementName = "WebIsoS96MessageSecurity")]
        public string WebIsoS96MessageSecurity { get; set; }

        [XmlElement(ElementName = "WebIsoS101FileName")]
        public string WebIsoS101FileName { get; set; }

        [XmlElement(ElementName = "WebIsoS115AdditionalTraceData")]
        public string WebIsoS115AdditionalTraceData { get; set; }

        [XmlElement(ElementName = "WebIsoS120FileUpd")]
        public string WebIsoS120FileUpd { get; set; }

        [XmlElement(ElementName = "WebIsoS122HostMaint")]
        public string WebIsoS122HostMaint { get; set; }

        [XmlElement(ElementName = "WebIsoS124TokenData")]
        public string WebIsoS124TokenData { get; set; }
    }
}

using System.Xml.Serialization;

namespace Com.Cybersoft4u.Interchange
{
    [XmlRoot(ElementName = "FileMaintData")]
    public class FileMaintData
    {
        [XmlElement(ElementName = "TxKeySeq")]
        public string TxKeySeq { get; set; }
        [XmlElement(ElementName = "IsoP2CardNbr")]
        public string IsoP2CardNbr { get; set; }
        [XmlElement(ElementName = "IsoP3ProcessCde")]
        public string IsoP3ProcessCde { get; set; }
        [XmlElement(ElementName = "IsoP7TxDteTime")]
        public string IsoP7TxDteTime { get; set; }
        [XmlElement(ElementName = "IsoP11TraceNo")]
        public string IsoP11TraceNo { get; set; }
        [XmlElement(ElementName = "IsoP13TransYear")]
        public string IsoP13TransYear { get; set; }
        [XmlElement(ElementName = "IsoP14ExpirDte")]
        public string IsoP14ExpirDte { get; set; }
        [XmlElement(ElementName = "IsoP32BinIca")]
        public string IsoP32BinIca { get; set; }
        [XmlElement(ElementName = "IsoP33FwdIICode")]
        public string IsoP33FwdIICode { get; set; }
        [XmlElement(ElementName = "IsoP36NTID")]
        public string IsoP36NTID { get; set; }
        [XmlElement(ElementName = "IsoP37RefNbr")]
        public string IsoP37RefNbr { get; set; }
        [XmlElement(ElementName = "IsoP39Rsp")]
        public string IsoP39Rsp { get; set; }
        [XmlElement(ElementName = "IsoP48AdditionalData")]
        public string IsoP48AdditionalData { get; set; }
        [XmlElement(ElementName = "IsoP49TranCurCode")]
        public string IsoP49TranCurCode { get; set; }
        [XmlElement(ElementName = "IsoP58T05TxDte")]
        public string IsoP58T05TxDte { get; set; }
        [XmlElement(ElementName = "IsoP60FromHostMaintData")]
        public string IsoP60FromHostMaintData { get; set; }
        [XmlElement(ElementName = "IsoP63SystemID")]
        public string IsoP63SystemID { get; set; }
        [XmlElement(ElementName = "IsoS73ActDte")]
        public string IsoS73ActDte { get; set; }
        [XmlElement(ElementName = "IsoS91FileUpdCode")]
        public string IsoS91FileUpdCode { get; set; }
        [XmlElement(ElementName = "IsoS92FileSecurityCode")]
        public string IsoS92FileSecurityCode { get; set; }
        [XmlElement(ElementName = "IsoS96MessageSecurity")]
        public string IsoS96MessageSecurity { get; set; }
        [XmlElement(ElementName = "IsoS101FileName")]
        public string IsoS101FileName { get; set; }
        [XmlElement(ElementName = "IsoS115AdditionalTraceData")]
        public string IsoS115AdditionalTraceData { get; set; }
        [XmlElement(ElementName = "IsoS120FileUpd")]
        public string IsoS120FileUpd { get; set; }
        [XmlElement(ElementName = "IsoS122HostMaint")]
        public string IsoS122HostMaint { get; set; }
        [XmlElement(ElementName = "IsoS124TokenData")]
        public string IsoS124TokenData { get; set; }
        [XmlAttribute(AttributeName = "DataType")]
        public string DataType { get; set; }
    }
}

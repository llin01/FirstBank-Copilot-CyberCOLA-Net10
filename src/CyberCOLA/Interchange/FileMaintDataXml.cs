using System.Xml.Serialization;

namespace Com.Cybersoft4u.Interchange
{
    [XmlRoot(ElementName = "TxKeySeq")]
    public class TxKeySeq
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP2CardNbr")]
    public class IsoP2CardNbr
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP3ProcessCde")]
    public class IsoP3ProcessCde
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP7TxDteTime")]
    public class IsoP7TxDteTime
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP11TraceNo")]
    public class IsoP11TraceNo
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP13TransYear")]
    public class IsoP13TransYear
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP14ExpirDte")]
    public class IsoP14ExpirDte
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP33FwdIICode")]
    public class IsoP33FwdIICode
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP36NTID")]
    public class IsoP36NTID
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP37RefNbr")]
    public class IsoP37RefNbr
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP39Rsp")]
    public class IsoP39Rsp
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP48AdditionalData")]
    public class IsoP48AdditionalData
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP49TranCurCode")]
    public class IsoP49TranCurCode
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP58T05TxDte")]
    public class IsoP58T05TxDte
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP60FromHostMaintData")]
    public class IsoP60FromHostMaintData
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoP63SystemID")]
    public class IsoP63SystemID
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS73ActDte")]
    public class IsoS73ActDte
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS91FileUpdCode")]
    public class IsoS91FileUpdCode
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS92FileSecurityCode")]
    public class IsoS92FileSecurityCode
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS96MessageSecurity")]
    public class IsoS96MessageSecurity
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS101FileName")]
    public class IsoS101FileName
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS115AdditionalTraceData")]
    public class IsoS115AdditionalTraceData
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS120FileUpd")]
    public class IsoS120FileUpd
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS122HostMaint")]
    public class IsoS122HostMaint
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "IsoS124TokenData")]
    public class IsoS124TokenData
    {
        [XmlAttribute(AttributeName = "Length")]
        public string Length { get; set; }
        [XmlAttribute(AttributeName = "Changed")]
        public string Changed { get; set; }
        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "FileMaintDataXml")]
    public class FileMaintDataXml
    {
        [XmlElement(ElementName = "TxKeySeq")]
        public TxKeySeq TxKeySeq { get; set; }
        [XmlElement(ElementName = "IsoP2CardNbr")]
        public IsoP2CardNbr IsoP2CardNbr { get; set; }
        [XmlElement(ElementName = "IsoP3ProcessCde")]
        public IsoP3ProcessCde IsoP3ProcessCde { get; set; }
        [XmlElement(ElementName = "IsoP7TxDteTime")]
        public IsoP7TxDteTime IsoP7TxDteTime { get; set; }
        [XmlElement(ElementName = "IsoP11TraceNo")]
        public IsoP11TraceNo IsoP11TraceNo { get; set; }
        [XmlElement(ElementName = "IsoP13LocalDate")]
        public IsoP13TransYear IsoP13TransYear { get; set; }
        [XmlElement(ElementName = "IsoP14ExpirDte")]
        public IsoP14ExpirDte IsoP14ExpirDte { get; set; }
        [XmlElement(ElementName = "IsoP33FwdIICode")]
        public IsoP33FwdIICode IsoP33FwdIICode { get; set; }
        [XmlElement(ElementName = "IsoP36NTID")]
        public IsoP36NTID IsoP36NTID { get; set; }
        [XmlElement(ElementName = "IsoP37RefNbr")]
        public IsoP37RefNbr IsoP37RefNbr { get; set; }
        [XmlElement(ElementName = "IsoP39Rsp")]
        public IsoP39Rsp IsoP39Rsp { get; set; }
        [XmlElement(ElementName = "IsoP48AdditionalData")]
        public IsoP48AdditionalData IsoP48AdditionalData { get; set; }
        [XmlElement(ElementName = "IsoP49TranCurCode")]
        public IsoP49TranCurCode IsoP49TranCurCode { get; set; }
        [XmlElement(ElementName = "IsoP58T05TxDte")]
        public IsoP58T05TxDte IsoP58T05TxDte { get; set; }
        [XmlElement(ElementName = "IsoP60FromHostMaintData")]
        public IsoP60FromHostMaintData IsoP60FromHostMaintData { get; set; }
        [XmlElement(ElementName = "IsoP63SystemID")]
        public IsoP63SystemID IsoP63SystemID { get; set; }
        [XmlElement(ElementName = "IsoS73ActDte")]
        public IsoS73ActDte IsoS73ActDte { get; set; }
        [XmlElement(ElementName = "IsoS91FileUpdCode")]
        public IsoS91FileUpdCode IsoS91FileUpdCode { get; set; }
        [XmlElement(ElementName = "IsoS92FileSecurityCode")]
        public IsoS92FileSecurityCode IsoS92FileSecurityCode { get; set; }
        [XmlElement(ElementName = "IsoS96MessageSecurity")]
        public IsoS96MessageSecurity IsoS96MessageSecurity { get; set; }
        [XmlElement(ElementName = "IsoS101FileName")]
        public IsoS101FileName IsoS101FileName { get; set; }
        [XmlElement(ElementName = "IsoS115AdditionalTraceData")]
        public IsoS115AdditionalTraceData IsoS115AdditionalTraceData { get; set; }
        [XmlElement(ElementName = "IsoS120FileUpd")]
        public IsoS120FileUpd IsoS120FileUpd { get; set; }
        [XmlElement(ElementName = "IsoS122HostMaint")]
        public IsoS122HostMaint IsoS122HostMaint { get; set; }
        [XmlElement(ElementName = "IsoS124TokenData")]
        public IsoS124TokenData IsoS124TokenData { get; set; }
        [XmlAttribute(AttributeName = "DataType")]
        public string DataType { get; set; }
    }

}

using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebISO8583
{
    [XmlRoot(ElementName = "BicField")]
    public class BicField
    {
        [XmlElement(ElementName = "BitNum")]
        public string BitNum { get; set; } = string.Empty;
        [XmlElement(ElementName = "Value")]
        public string Value { get; set; } = string.Empty;
        [XmlElement(ElementName = "FieldDataType")]
        public string FieldDataType { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "BicFieldDefaultValue")]
    public class BicFieldDefaultValue
    {
        [XmlElement(ElementName = "BicField")]
        public List<BicField> BicField { get; set; } = new();
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; } = string.Empty;
        [XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsd { get; set; } = string.Empty;
    }
}

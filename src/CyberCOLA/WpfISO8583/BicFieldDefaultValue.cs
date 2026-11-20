using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfISO8583
{
    [XmlRoot(ElementName = "BicField")]
    public class BicField
    {
        [XmlElement(ElementName = "BitNum")]
        public string BitNum { get; set; }
        [XmlElement(ElementName = "Value")]
        public string Value { get; set; }
        [XmlElement(ElementName = "FieldDataType")]
        public string FieldDataType { get; set; }
    }

    [XmlRoot(ElementName = "BicFieldDefaultValue")]
    public class BicFieldDefaultValue
    {
        [XmlElement(ElementName = "BicField")]
        public List<BicField> BicField { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsd { get; set; }
    }
}

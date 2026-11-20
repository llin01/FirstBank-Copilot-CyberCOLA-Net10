using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebMQTest
{
    public class CardInfo
    {
        [XmlElement(ElementName = "AcctNbr")]
        public string AcctNbr { get; set; } = string.Empty;
        [XmlElement(ElementName = "CardNbr")]
        public string CardNbr { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "CardArray")]
    public class CardArray
    {
        [XmlElement(ElementName = "CardInfo")]
        public List<CardInfo> CardInfo { get; set; } = new();
    }
}

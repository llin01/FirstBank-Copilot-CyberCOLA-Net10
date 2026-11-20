using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Com.Cybersoft4u.Test.Wpf.WpfMQTest
{
    internal class CardInfos
    {
        [XmlRoot(ElementName = "CardInfo")]
        public class CardInfo
        {
            [XmlElement(ElementName = "AcctNbr")]
            public string AcctNbr { get; set; }
            [XmlElement(ElementName = "CardNbr")]
            public string CardNbr { get; set; }
        }

        [XmlRoot(ElementName = "CardArray")]
        public class CardArray
        {
            [XmlElement(ElementName = "CardInfo")]
            public List<CardInfo> CardInfo { get; set; }
        }
    }
}

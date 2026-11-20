using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    [XmlRoot(ElementName = "Urls")]
    public class Urls
    {
        [XmlElement(ElementName = "Url")]
        public List<string> Url { get; set; }
    }

    [XmlRoot(ElementName = "SMSWebAPI")]
    public class SMSWebAPI
    {
        [XmlElement(ElementName = "Urls")]
        public Urls Urls { get; set; }
        [XmlElement(ElementName = "TimeoutMilliseconds")]
        public string TimeoutMilliseconds { get; set; }
        [XmlElement(ElementName = "RetryTimes")]
        public string RetryTimes { get; set; }
        [XmlElement(ElementName = "SmsProvider")]
        public string SmsProvider { get; set; }
    }

    [XmlRoot(ElementName = "DataInfo")]
    public class DataInfo
    {
        [XmlElement(ElementName = "SystemId")]
        public string SystemId { get; set; }
        [XmlElement(ElementName = "SourceId")]
        public string SourceId { get; set; }
    }

    [XmlRoot(ElementName = "SMSWebAPIInfo")]
    public class SMSWebAPIInfo
    {
        [XmlElement(ElementName = "SMSWebAPI")]
        public SMSWebAPI SMSWebAPI { get; set; }
        [XmlElement(ElementName = "DataInfo")]
        public DataInfo DataInfo { get; set; }
    }

}

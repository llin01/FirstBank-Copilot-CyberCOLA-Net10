using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    [XmlRoot(ElementName = "SMSServer")]
    public class SMSServer
    {
        [XmlElement(ElementName = "ServerName")]
        public string ServerName { get; set; }
        [XmlElement(ElementName = "ListenerPort")]
        public int ListenerPort { get; set; }
        [XmlElement(ElementName = "ConnectionTimeoutMilliseconds")]
        public int ConnectionTimeoutMilliseconds { get; set; }
        [XmlElement(ElementName = "SendReceiveTimeoutMilliseconds")]
        public int SendReceiveTimeoutMilliseconds { get; set; }
        [XmlElement(ElementName = "WaitForCloseMilliseconds")]
        public int WaitForCloseMilliseconds { get; set; }
    }

    [XmlRoot(ElementName = "SMSServers")]
    public class SMSServers
    {
        [XmlElement(ElementName = "SMSServer")]
        public List<SMSServer> SMSServer { get; set; }
    }

    [XmlRoot(ElementName = "AppDataInfo")]
    public class AppDataInfo
    {
        [XmlAttribute(AttributeName = "ThreadSleepTimeByMiliSec")]
        public int ThreadSleepTimeByMiliSec { get; set; }
        [XmlAttribute(AttributeName = "HaveNotice")]
        public bool HaveNotice { get; set; }
        [XmlAttribute(AttributeName = "MaxProcessCount")]
        public string MaxProcessCount { get; set; }
        [XmlAttribute(AttributeName = "MaxParallelTaskCount")]
        public string MaxParallelTaskCount { get; set; }
        [XmlAttribute(AttributeName = "StartKey")]
        public string StartKey { get; set; }
        [XmlAttribute(AttributeName = "HaveChangedStartKey")]
        public bool HaveChangedStartKey { get; set; }
        [XmlAttribute(AttributeName = "RetryCount")]
        public string RetryCount { get; set; }
    }

    [XmlRoot(ElementName = "SMSServerInfo")]
    public class SMSServerInfo
    {
        [XmlElement(ElementName = "SMSServers")]
        public SMSServers SMSServers { get; set; }
        [XmlElement(ElementName = "AppDataInfo")]
        public AppDataInfo AppDataInfo { get; set; }
    }

}

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.MQ
{
    [XmlRoot(ElementName = "MQServer")]
    public class MQServer
    {
        [XmlElement(ElementName = "MQUserName")]
        public string MQUserName { get; set; }
        [XmlElement(ElementName = "MQQueueManagerListener")]
        public string MQQueueManagerListener { get; set; }
        [XmlElement(ElementName = "MQChannelInfoListener")]
        public string MQChannelInfoListener { get; set; }
        [XmlElement(ElementName = "MQPutQueueName")]
        public string MQPutQueueName { get; set; }
        [XmlElement(ElementName = "MQGetQueueName")]
        public string MQGetQueueName { get; set; }
        [XmlElement(ElementName = "MQServerCCSID")]
        public string MQServerCCSID { get; set; }
        [XmlElement(ElementName = "MQClientCCSID")]
        public string MQClientCCSID { get; set; }
        [XmlElement(ElementName = "MQWaitIntervalMilliSec")]
        public string MQWaitIntervalMilliSec { get; set; }
        [XmlElement(ElementName = "MQDeleteFlag")]
        public string MQDeleteFlag { get; set; }
        [XmlElement(ElementName = "MQRetryMaxCount")]
        public string MQRetryMaxCount { get; set; }
    }

    [XmlRoot(ElementName = "MQServers")]
    public class MQServers
    {
        [XmlElement(ElementName = "MQServer")]
        public List<MQServer> MQServer { get; set; }
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
    }

    [XmlRoot(ElementName = "MQServerInfo")]
    public class MQServerInfo
    {
        [XmlElement(ElementName = "MQServers")]
        public MQServers MQServers { get; set; }
        [XmlElement(ElementName = "AppDataInfo")]
        public AppDataInfo AppDataInfo { get; set; }
    }

}

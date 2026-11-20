using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfMonitorService
{
    [XmlRoot(ElementName = "BankInfo")]
    public class BankInfo
    {
        [XmlElement(ElementName = "BankName")]
        public string BankName { get; set; }
        [XmlElement(ElementName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TCPClientServiceConfig")]
    public class TCPClientServiceConfig
    {
        [XmlElement(ElementName = "RemoteHostName")]
        public string RemoteHostName { get; set; }
        [XmlElement(ElementName = "RemoteIP")]
        public string RemoteIP { get; set; }
        [XmlElement(ElementName = "RemotePort")]
        public string RemotePort { get; set; }
        [XmlElement(ElementName = "LocalIP")]
        public string LocalIP { get; set; }
        [XmlElement(ElementName = "LocalPort")]
        public string LocalPort { get; set; }
        [XmlElement(ElementName = "AutoStartFlag")]
        public string AutoStartFlag { get; set; }
        [XmlElement(ElementName = "AutoEchoFlag")]
        public string AutoEchoFlag { get; set; }
        [XmlElement(ElementName = "ClientRetrievalTimeIntervalSec")]
        public string ClientRetrievalTimeIntervalSec { get; set; }
        [XmlElement(ElementName = "UDPServerName")]
        public string UDPServerName { get; set; }
        [XmlElement(ElementName = "UDPListenerPort")]
        public string UDPListenerPort { get; set; }
        [XmlElement(ElementName = "UDPAutoStartFlag")]
        public string UDPAutoStartFlag { get; set; }
    }

    [XmlRoot(ElementName = "DatabaseInfo")]
    public class DatabaseInfo
    {
        [XmlElement(ElementName = "ConnectionString")]
        public string ConnectionString { get; set; }
        [XmlElement(ElementName = "IsEncrypted")]
        public string IsEncrypted { get; set; }
        [XmlElement(ElementName = "EncryptedKey")]
        public string EncryptedKey { get; set; }
    }

    [XmlRoot(ElementName = "TCPClientServiceInfo")]
    public class TCPClientServiceInfo
    {
        [XmlElement(ElementName = "BankInfo")]
        public BankInfo BankInfo { get; set; }
        [XmlElement(ElementName = "TCPClientServiceConfig")]
        public TCPClientServiceConfig TCPClientServiceConfig { get; set; }
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; }
    }

}

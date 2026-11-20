using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebMonitorService
{
    [XmlRoot(ElementName = "BankInfo")]
    public class BankInfo
    {
        [XmlElement(ElementName = "BankName")]
        public string BankName { get; set; } = string.Empty;
        [XmlElement(ElementName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "TCPClientServiceConfig")]
    public class TCPClientServiceConfig
    {
        [XmlElement(ElementName = "RemoteHostName")]
        public string RemoteHostName { get; set; } = string.Empty;
        [XmlElement(ElementName = "RemoteIP")]
        public string RemoteIP { get; set; } = string.Empty;
        [XmlElement(ElementName = "RemotePort")]
        public string RemotePort { get; set; } = string.Empty;
        [XmlElement(ElementName = "LocalIP")]
        public string LocalIP { get; set; } = string.Empty;
        [XmlElement(ElementName = "LocalPort")]
        public string LocalPort { get; set; } = string.Empty;
        [XmlElement(ElementName = "AutoStartFlag")]
        public string AutoStartFlag { get; set; } = string.Empty;
        [XmlElement(ElementName = "AutoEchoFlag")]
        public string AutoEchoFlag { get; set; } = string.Empty;
        [XmlElement(ElementName = "ClientRetrievalTimeIntervalSec")]
        public string ClientRetrievalTimeIntervalSec { get; set; } = string.Empty;
        [XmlElement(ElementName = "UDPServerName")]
        public string UDPServerName { get; set; } = string.Empty;
        [XmlElement(ElementName = "UDPListenerPort")]
        public string UDPListenerPort { get; set; } = string.Empty;
        [XmlElement(ElementName = "UDPAutoStartFlag")]
        public string UDPAutoStartFlag { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "DatabaseInfo")]
    public class DatabaseInfo
    {
        [XmlElement(ElementName = "ConnectionString")]
        public string ConnectionString { get; set; } = string.Empty;
        [XmlElement(ElementName = "IsEncrypted")]
        public string IsEncrypted { get; set; } = string.Empty;
        [XmlElement(ElementName = "EncryptedKey")]
        public string EncryptedKey { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "TCPClientServiceInfo")]
    public class TCPClientServiceInfo
    {
        [XmlElement(ElementName = "BankInfo")]
        public BankInfo BankInfo { get; set; } = new();
        [XmlElement(ElementName = "TCPClientServiceConfig")]
        public TCPClientServiceConfig TCPClientServiceConfig { get; set; } = new();
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; } = new();
    }
}

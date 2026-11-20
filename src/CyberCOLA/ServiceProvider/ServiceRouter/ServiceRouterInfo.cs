using System.Xml.Serialization;

namespace Com.Cybersoft4u.ServiceProvider.ServiceRouter
{
    [XmlRoot(ElementName = "ServerServiceInfo")]
    public class ServerServiceInfo
    {
        [XmlElement(ElementName = "ServerLocalIP")]
        public string ServerLocalIP { get; set; }
        [XmlElement(ElementName = "ListenerPort")]
        public string ListenerPort { get; set; }
        [XmlElement(ElementName = "BackLog")]
        public string BackLog { get; set; }
        [XmlElement(ElementName = "ClientAuthServiceRemotePorts")]
        public string ClientAuthServiceRemotePorts { get; set; }
        [XmlElement(ElementName = "AutoStartFlag")]
        public string AutoStartFlag { get; set; }
    }

    [XmlRoot(ElementName = "ClientServiceInfo")]
    public class ClientServiceInfo
    {
        [XmlElement(ElementName = "ServerLocalIP")]
        public string ServerLocalIP { get; set; }
        [XmlElement(ElementName = "ListenerPort")]
        public string ListenerPort { get; set; }
        [XmlElement(ElementName = "BackLog")]
        public string BackLog { get; set; }
        [XmlElement(ElementName = "AutoStartFlag")]
        public string AutoStartFlag { get; set; }
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

    [XmlRoot(ElementName = "ServiceRouterInfo")]
    public class ServiceRouterInfo
    {
        [XmlElement(ElementName = "ServerServiceInfo")]
        public ServerServiceInfo ServerServiceInfo { get; set; }
        [XmlElement(ElementName = "ClientServiceInfo")]
        public ClientServiceInfo ClientServiceInfo { get; set; }
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; }
    }

}

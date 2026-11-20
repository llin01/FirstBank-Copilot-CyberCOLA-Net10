using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Interchange
{
    [XmlRoot(ElementName = "BankInfo")]
    public class BankInfo
    {
        [XmlElement(ElementName = "BankName")]
        public string BankName { get; set; }
        [XmlElement(ElementName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "Server")]
    public class Server
    {
        [XmlElement(ElementName = "GatewayType")]
        public string GatewayType { get; set; }
        [XmlElement(ElementName = "RemoteHostName")]
        public string RemoteHostName { get; set; }
        [XmlElement(ElementName = "RemoteIP")]
        public string RemoteIP { get; set; }
        [XmlElement(ElementName = "RemotePort")]
        public string RemotePort { get; set; }
    }

    [XmlRoot(ElementName = "Servers")]
    public class Servers
    {
        [XmlElement(ElementName = "Server")]
        public List<Server> Server { get; set; }
    }

    [XmlRoot(ElementName = "Client")]
    public class Client
    {
        [XmlElement(ElementName = "LocalHostName")]
        public string LocalHostName { get; set; }
        [XmlElement(ElementName = "LocalIP")]
        public string LocalIP { get; set; }
        [XmlElement(ElementName = "LocalPort")]
        public string LocalPort { get; set; }
        [XmlElement(ElementName = "DefaultServerIndex")]
        public string DefaultServerIndex { get; set; }
        [XmlElement(ElementName = "AutoStartFlag")]
        public string AutoStartFlag { get; set; }
        [XmlElement(ElementName = "AutoEchoFlag")]
        public string AutoEchoFlag { get; set; }
        [XmlElement(ElementName = "ClientRetrievalTimeIntervalSec")]
        public string ClientRetrievalTimeIntervalSec { get; set; }
    }

    [XmlRoot(ElementName = "UDPServer")]
    public class UDPServer
    {
        [XmlElement(ElementName = "UDPServerName")]
        public string UDPServerName { get; set; }
        [XmlElement(ElementName = "UDPListenerPort")]
        public string UDPListenerPort { get; set; }
        [XmlElement(ElementName = "UDPAutoStartFlag")]
        public string UDPAutoStartFlag { get; set; }
    }

    [XmlRoot(ElementName = "ClientAuthRouterServiceConfig")]
    public class ClientAuthRouterServiceConfig
    {
        [XmlElement(ElementName = "Servers")]
        public Servers Servers { get; set; }
        [XmlElement(ElementName = "Client")]
        public Client Client { get; set; }
        [XmlElement(ElementName = "UDPServer")]
        public UDPServer UDPServer { get; set; }
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

    [XmlRoot(ElementName = "TCPClientAuthRouterServiceInfo")]
    public class TCPClientAuthRouterServiceInfo
    {
        [XmlElement(ElementName = "BankInfo")]
        public BankInfo BankInfo { get; set; }
        [XmlElement(ElementName = "ClientAuthRouterServiceConfig")]
        public ClientAuthRouterServiceConfig ClientAuthRouterServiceConfig { get; set; }
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; }
    }
}

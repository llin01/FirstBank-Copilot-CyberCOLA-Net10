using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebServerMonitor
{
    [XmlRoot(ElementName = "BankInfo")]
    public class BankInfo
    {
        [XmlElement(ElementName = "BankName")]
        public string BankName { get; set; } = string.Empty;
        [XmlElement(ElementName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "ServiceInfo")]
    public class ServiceInfo
    {
        [XmlElement(ElementName = "ServerName")]
        public string ServerName { get; set; } = string.Empty;
        [XmlElement(ElementName = "ListenerPort")]
        public string ListenerPort { get; set; } = string.Empty;
        [XmlElement(ElementName = "BackLog")]
        public string BackLog { get; set; } = string.Empty;
        [XmlElement(ElementName = "ClientRetrievalTimeIntervalSec")]
        public string ClientRetrievalTimeIntervalSec { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "UDPServiceInfo")]
    public class UDPServiceInfo
    {
        [XmlElement(ElementName = "ServerName")]
        public string ServerName { get; set; } = string.Empty;
        [XmlElement(ElementName = "ListenerPort")]
        public string ListenerPort { get; set; } = string.Empty;
        [XmlElement(ElementName = "IsStart")]
        public string IsStart { get; set; } = string.Empty;
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

    [XmlRoot(ElementName = "Commands")]
    public class Commands
    {
        [XmlElement(ElementName = "Command")]
        public List<string> Command { get; set; } = new();
    }

    [XmlRoot(ElementName = "VISAService")]
    public class VISAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "MASTERCARDService")]
    public class MASTERCARDService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "MASTERCARDANRService")]
    public class MASTERCARDANRService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "COLAService")]
    public class COLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "NCCCService")]
    public class NCCCService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "Gateway")]
    public class Gateway
    {
        [XmlElement(ElementName = "VISAService")]
        public VISAService VISAService { get; set; } = new();
        [XmlElement(ElementName = "MASTERCARDService")]
        public MASTERCARDService MASTERCARDService { get; set; } = new();
        [XmlElement(ElementName = "MASTERCARDANRService")]
        public MASTERCARDANRService MASTERCARDANRService { get; set; } = new();
        [XmlElement(ElementName = "COLAService")]
        public COLAService COLAService { get; set; } = new();
        [XmlElement(ElementName = "NCCCService")]
        public NCCCService NCCCService { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "VISAClient")]
    public class VISAClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "MASTERCARDClient")]
    public class MASTERCARDClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "NCCCClient")]
    public class NCCCClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "ClientAuth")]
    public class ClientAuth
    {
        [XmlElement(ElementName = "VISAClient")]
        public VISAClient VISAClient { get; set; } = new();
        [XmlElement(ElementName = "MASTERCARDClient")]
        public MASTERCARDClient MASTERCARDClient { get; set; } = new();
        [XmlElement(ElementName = "NCCCClient")]
        public NCCCClient NCCCClient { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "CommandItemsInfo")]
    public class CommandItemsInfo
    {
        [XmlElement(ElementName = "Gateway")]
        public Gateway Gateway { get; set; } = new();
        [XmlElement(ElementName = "ClientAuth")]
        public ClientAuth ClientAuth { get; set; } = new();
    }

    [XmlRoot(ElementName = "ServiceMonitorCommandInfo")]
    public class ServiceMonitorCommandInfo
    {
        [XmlElement(ElementName = "ConnectionStatus")]
        public string ConnectionStatus { get; set; } = string.Empty;
        [XmlElement(ElementName = "ChangeKey")]
        public string ChangeKey { get; set; } = string.Empty;
        [XmlElement(ElementName = "Echo")]
        public string Echo { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "ServiceMonitorInfo")]
    public class ServiceMonitorInfo
    {
        [XmlElement(ElementName = "BankInfo")]
        public BankInfo BankInfo { get; set; } = new();
        [XmlElement(ElementName = "ServiceInfo")]
        public ServiceInfo ServiceInfo { get; set; } = new();
        [XmlElement(ElementName = "UDPServiceInfo")]
        public UDPServiceInfo UDPServiceInfo { get; set; } = new();
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; } = new();
        [XmlElement(ElementName = "CommandItemsInfo")]
        public CommandItemsInfo CommandItemsInfo { get; set; } = new();
        [XmlElement(ElementName = "ServiceMonitorCommandInfo")]
        public ServiceMonitorCommandInfo ServiceMonitorCommandInfo { get; set; } = new();
    }
}

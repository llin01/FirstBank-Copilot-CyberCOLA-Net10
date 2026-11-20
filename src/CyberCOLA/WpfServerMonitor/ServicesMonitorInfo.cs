using System.Xml.Serialization;
using System.Collections.Generic;
namespace Com.Cybersoft4u.Test.Wpf.WpfServerMonitor
{

    [XmlRoot(ElementName = "BankInfo")]
    public class BankInfo
    {
        [XmlElement(ElementName = "BankName")]
        public string BankName { get; set; }
        [XmlElement(ElementName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ServiceInfo")]
    public class ServiceInfo
    {
        [XmlElement(ElementName = "ServerName")]
        public string ServerName { get; set; }
        [XmlElement(ElementName = "ListenerPort")]
        public string ListenerPort { get; set; }
        [XmlElement(ElementName = "BackLog")]
        public string BackLog { get; set; }
        [XmlElement(ElementName = "ClientRetrievalTimeIntervalSec")]
        public string ClientRetrievalTimeIntervalSec { get; set; }
    }

    [XmlRoot(ElementName = "UDPServiceInfo")]
    public class UDPServiceInfo
    {
        [XmlElement(ElementName = "ServerName")]
        public string ServerName { get; set; }
        [XmlElement(ElementName = "ListenerPort")]
        public string ListenerPort { get; set; }
        [XmlElement(ElementName = "IsStart")]
        public string IsStart { get; set; }
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

    [XmlRoot(ElementName = "Commands")]
    public class Commands
    {
        [XmlElement(ElementName = "Command")]
        public List<string> Command { get; set; }
    }

    [XmlRoot(ElementName = "VISAService")]
    public class VISAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "MASTERCARDService")]
    public class MASTERCARDService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "MASTERCARDANRService")]
    public class MASTERCARDANRService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "COLAService")]
    public class COLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "NCCCService")]
    public class NCCCService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "Gateway")]
    public class Gateway
    {
        [XmlElement(ElementName = "VISAService")]
        public VISAService VISAService { get; set; }
        [XmlElement(ElementName = "MASTERCARDService")]
        public MASTERCARDService MASTERCARDService { get; set; }
        [XmlElement(ElementName = "MASTERCARDANRService")]
        public MASTERCARDANRService MASTERCARDANRService { get; set; }
        [XmlElement(ElementName = "COLAService")]
        public COLAService COLAService { get; set; }
        [XmlElement(ElementName = "NCCCService")]
        public NCCCService NCCCService { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "VISAClient")]
    public class VISAClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "MASTERCARDClient")]
    public class MASTERCARDClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "NCCCClient")]
    public class NCCCClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ClientAuth")]
    public class ClientAuth
    {
        [XmlElement(ElementName = "VISAClient")]
        public VISAClient VISAClient { get; set; }
        [XmlElement(ElementName = "MASTERCARDClient")]
        public MASTERCARDClient MASTERCARDClient { get; set; }
        [XmlElement(ElementName = "NCCCClient")]
        public NCCCClient NCCCClient { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "CommandItemsInfo")]
    public class CommandItemsInfo
    {
        [XmlElement(ElementName = "Gateway")]
        public Gateway Gateway { get; set; }
        [XmlElement(ElementName = "ClientAuth")]
        public ClientAuth ClientAuth { get; set; }
    }

    [XmlRoot(ElementName = "ServiceMonitorCommandInfo")]
    public class ServiceMonitorCommandInfo
    {
        [XmlElement(ElementName = "ConnectionStatus")]
        public string ConnectionStatus { get; set; }
        [XmlElement(ElementName = "ChangeKey")]
        public string ChangeKey { get; set; }
        [XmlElement(ElementName = "Echo")]
        public string Echo { get; set; }
    }

    [XmlRoot(ElementName = "ServiceMonitorInfo")]
    public class ServiceMonitorInfo
    {
        [XmlElement(ElementName = "BankInfo")]
        public BankInfo BankInfo { get; set; }
        [XmlElement(ElementName = "ServiceInfo")]
        public ServiceInfo ServiceInfo { get; set; }
        [XmlElement(ElementName = "UDPServiceInfo")]
        public UDPServiceInfo UDPServiceInfo { get; set; }
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; }
        [XmlElement(ElementName = "CommandItemsInfo")]
        public CommandItemsInfo CommandItemsInfo { get; set; }
        [XmlElement(ElementName = "ServiceMonitorCommandInfo")]
        public ServiceMonitorCommandInfo ServiceMonitorCommandInfo { get; set; }
    }

}

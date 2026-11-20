using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebTCPConnectionMonitor
{
    [XmlRoot(ElementName = "EndPoints")]
    public class EndPoints
    {
        [XmlElement(ElementName = "EndPoint")]
        public List<string> EndPoint { get; set; } = new();
    }

    [XmlRoot(ElementName = "Nccc2NcccService")]
    public class Nccc2NcccService
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "CloaServiceClientAuthService")]
    public class CloaServiceClientAuthService
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "RouterService2ClientMonitorService")]
    public class RouterService2ClientMonitorService
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "ClientAuthRouterService2TcpClient")]
    public class ClientAuthRouterService2TcpClient
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; } = new();
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    [XmlRoot(ElementName = "TCPConnectionMonitorConfig")]
    public class TCPConnectionMonitorConfig
    {
        [XmlElement(ElementName = "Nccc2NcccService")]
        public Nccc2NcccService Nccc2NcccService { get; set; } = new();
        [XmlElement(ElementName = "CloaServiceClientAuthService")]
        public CloaServiceClientAuthService CloaServiceClientAuthService { get; set; } = new();
        [XmlElement(ElementName = "RouterService2ClientMonitorService")]
        public RouterService2ClientMonitorService RouterService2ClientMonitorService { get; set; } = new();
        [XmlElement(ElementName = "ClientAuthRouterService2TcpClient")]
        public ClientAuthRouterService2TcpClient ClientAuthRouterService2TcpClient { get; set; } = new();
    }

    [XmlRoot(ElementName = "TCPConnectionMonitorInfo")]
    public class TCPConnectionMonitorInfo
    {
        [XmlElement(ElementName = "TCPConnectionMonitorConfig")]
        public TCPConnectionMonitorConfig TCPConnectionMonitorConfig { get; set; } = new();
    }
}

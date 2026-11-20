using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfTCPConnectionMonitor
{
    [XmlRoot(ElementName = "EndPoints")]
    public class EndPoints
    {
        [XmlElement(ElementName = "EndPoint")]
        public List<string> EndPoint { get; set; }
    }

    [XmlRoot(ElementName = "Nccc2NcccService")]
    public class Nccc2NcccService
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "CloaServiceClientAuthService")]
    public class CloaServiceClientAuthService
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "RouterService2ClientMonitorService")]
    public class RouterService2ClientMonitorService
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ClientAuthRouterService2TcpClient")]
    public class ClientAuthRouterService2TcpClient
    {
        [XmlElement(ElementName = "EndPoints")]
        public EndPoints EndPoints { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TCPConnectionMonitorConfig")]
    public class TCPConnectionMonitorConfig
    {
        [XmlElement(ElementName = "Nccc2NcccService")]
        public Nccc2NcccService Nccc2NcccService { get; set; }
        [XmlElement(ElementName = "CloaServiceClientAuthService")]
        public CloaServiceClientAuthService CloaServiceClientAuthService { get; set; }
        [XmlElement(ElementName = "RouterService2ClientMonitorService")]
        public RouterService2ClientMonitorService RouterService2ClientMonitorService { get; set; }
        [XmlElement(ElementName = "ClientAuthRouterService2TcpClient")]
        public ClientAuthRouterService2TcpClient ClientAuthRouterService2TcpClient { get; set; }
    }

    [XmlRoot(ElementName = "TCPConnectionMonitorInfo")]
    public class TCPConnectionMonitorInfo
    {
        [XmlElement(ElementName = "TCPConnectionMonitorConfig")]
        public TCPConnectionMonitorConfig TCPConnectionMonitorConfig { get; set; }
    }
}

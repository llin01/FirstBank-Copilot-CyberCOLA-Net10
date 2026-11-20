using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Interchange
{
    /// <summary>
    /// Represents a collection of command strings.
    /// </summary>
    [XmlRoot(ElementName = "Commands")]
    public class Commands
    {
        [XmlElement(ElementName = "Command")]
        public List<string> Command { get; set; }
    }

    /// <summary>
    /// VISA service configuration containing available commands and description.
    /// </summary>
    [XmlRoot(ElementName = "VISAService")]
    public class VISAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// MasterCard service configuration containing available commands and description.
    /// </summary>
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

    /// <summary>
    /// NCCC COLA service configuration containing available commands and description.
    /// </summary>
    [XmlRoot(ElementName = "NCCCCOLAService")]
    public class NCCCCOLAService
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

    [XmlRoot(ElementName = "NCPSCOLAService")]
    public class NCPSCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "NCPSService")]
    public class NCPSService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TWMPCOLAService")]
    public class TWMPCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TWMPService")]
    public class TWMPService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "STINCOLAService")]
    public class STINCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "STINService")]
    public class STINService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "FISCCOLAService")]
    public class FISCCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "FISCService")]
    public class FISCService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TWINCOLAService")]
    public class TWINCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TWINService")]
    public class TWINService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "SVCSCOLAService")]
    public class SVCSCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "SVCSService")]
    public class SVCSService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ICASHCOLAService")]
    public class ICASHCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ICASHService")]
    public class ICASHService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "IPASSCOLAService")]
    public class IPASSCOLAService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "IPASSService")]
    public class IPASSService
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "GatewayServiceCommand")]
    public class GatewayServiceCommand
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
        [XmlElement(ElementName = "NCCCCOLAService")]
        public NCCCCOLAService NCCCCOLAService { get; set; }
        [XmlElement(ElementName = "NCCCService")]
        public NCCCService NCCCService { get; set; }
        [XmlElement(ElementName = "NCPSCOLAService")]
        public NCPSCOLAService NCPSCOLAService { get; set; }
        [XmlElement(ElementName = "NCPSService")]
        public NCPSService NCPSService { get; set; }
        [XmlElement(ElementName = "TWMPCOLAService")]
        public TWMPCOLAService TWMPCOLAService { get; set; }
        [XmlElement(ElementName = "TWMPService")]
        public TWMPService TWMPService { get; set; }
        [XmlElement(ElementName = "STINCOLAService")]
        public STINCOLAService STINCOLAService { get; set; }
        [XmlElement(ElementName = "STINService")]
        public STINService STINService { get; set; }
        [XmlElement(ElementName = "FISCCOLAService")]
        public FISCCOLAService FISCCOLAService { get; set; }
        [XmlElement(ElementName = "FISCService")]
        public FISCService FISCService { get; set; }
        [XmlElement(ElementName = "TWINCOLAService")]
        public TWINCOLAService TWINCOLAService { get; set; }
        [XmlElement(ElementName = "TWINService")]
        public TWINService TWINService { get; set; }
        [XmlElement(ElementName = "SVCSCOLAService")]
        public SVCSCOLAService SVCSCOLAService { get; set; }
        [XmlElement(ElementName = "SVCSService")]
        public SVCSService SVCSService { get; set; }
        [XmlElement(ElementName = "ICASHCOLAService")]
        public ICASHCOLAService ICASHCOLAService { get; set; }
        [XmlElement(ElementName = "ICASHService")]
        public ICASHService ICASHService { get; set; }
        [XmlElement(ElementName = "IPASSCOLAService")]
        public IPASSCOLAService IPASSCOLAService { get; set; }
        [XmlElement(ElementName = "IPASSService")]
        public IPASSService IPASSService { get; set; }
        [XmlElement(ElementName = "GatewayServiceCommand")]
        public GatewayServiceCommand GatewayServiceCommand { get; set; }
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

    [XmlRoot(ElementName = "NCPSClient")]
    public class NCPSClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "STINClient")]
    public class STINClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "FISCClient")]
    public class FISCClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "TWINClient")]
    public class TWINClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "SVCSClient")]
    public class SVCSClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ICASHClient")]
    public class ICASHClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "IPASSClient")]
    public class IPASSClient
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "ClientAuthServiceCommand")]
    public class ClientAuthServiceCommand
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
        [XmlElement(ElementName = "NCPSClient")]
        public NCPSClient NCPSClient { get; set; }
        [XmlElement(ElementName = "STINClient")]
        public STINClient STINClient { get; set; }
        [XmlElement(ElementName = "FISCClient")]
        public FISCClient FISCClient { get; set; }
        [XmlElement(ElementName = "TWINClient")]
        public TWINClient TWINClient { get; set; }
        [XmlElement(ElementName = "SVCSClient")]
        public SVCSClient SVCSClient { get; set; }
        [XmlElement(ElementName = "ICASHClient")]
        public ICASHClient ICASHClient { get; set; }
        [XmlElement(ElementName = "IPASSClient")]
        public IPASSClient IPASSClient { get; set; }
        [XmlElement(ElementName = "ClientAuthServiceCommand")]
        public ClientAuthServiceCommand ClientAuthServiceCommand { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "RouterServiceCommand")]
    public class RouterServiceCommand
    {
        [XmlElement(ElementName = "Commands")]
        public Commands Commands { get; set; }
        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }
    }

    [XmlRoot(ElementName = "Router")]
    public class Router
    {
        [XmlElement(ElementName = "RouterServiceCommand")]
        public RouterServiceCommand RouterServiceCommand { get; set; }
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
        [XmlElement(ElementName = "Router")]
        public Router Router { get; set; }
    }

    [XmlRoot(ElementName = "InterchangeMessageCommandInfo")]
    public class InterchangeMessageCommandInfo
    {
        [XmlElement(ElementName = "CommandItemsInfo")]
        public CommandItemsInfo CommandItemsInfo { get; set; }
    }
}

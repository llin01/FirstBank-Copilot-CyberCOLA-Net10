using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    [XmlRoot(ElementName = "VDCardRange")]
    public class VDCardRange
    {
        [XmlElement(ElementName = "CardLowRange")]
        public string CardLowRange { get; set; }
        [XmlElement(ElementName = "CardHighRange")]
        public string CardHighRange { get; set; }
    }

    [XmlRoot(ElementName = "VDCardRangeInfo")]
    public class VDCardRangeInfo
    {
        [XmlElement(ElementName = "VDCardRange")]
        public List<VDCardRange> VDCardRange { get; set; }
    }

    [XmlRoot(ElementName = "MDCardRange")]
    public class MDCardRange
    {
        [XmlElement(ElementName = "CardLowRange")]
        public string CardLowRange { get; set; }
        [XmlElement(ElementName = "CardHighRange")]
        public string CardHighRange { get; set; }
    }

    [XmlRoot(ElementName = "MDCardRangeInfo")]
    public class MDCardRangeInfo
    {
        [XmlElement(ElementName = "MDCardRange")]
        public List<MDCardRange> MDCardRange { get; set; }
    }

    [XmlRoot(ElementName = "DebitInfo")]
    public class DebitInfo
    {
        [XmlElement(ElementName = "VDCardRangeInfo")]
        public VDCardRangeInfo VDCardRangeInfo { get; set; }
        [XmlElement(ElementName = "MDCardRangeInfo")]
        public MDCardRangeInfo MDCardRangeInfo { get; set; }
    }

    [XmlRoot(ElementName = "JCBCardRange")]
    public class JCBCardRange
    {
        [XmlElement(ElementName = "CardLowRange")]
        public string CardLowRange { get; set; }
        [XmlElement(ElementName = "CardHighRange")]
        public string CardHighRange { get; set; }
    }

    [XmlRoot(ElementName = "JCBCardRangeInfo")]
    public class JCBCardRangeInfo
    {
        [XmlElement(ElementName = "JCBCardRange")]
        public List<JCBCardRange> JCBCardRange { get; set; }
    }

    [XmlRoot(ElementName = "TPInfo")]
    public class TPInfo
    {
        [XmlElement(ElementName = "JCBCardRangeInfo")]
        public JCBCardRangeInfo JCBCardRangeInfo { get; set; }
    }

    [XmlRoot(ElementName = "BankInfo")]
    public class BankInfo
    {
        [XmlElement(ElementName = "BankName")]
        public string BankName { get; set; }
        [XmlElement(ElementName = "BankID")]
        public string BankID { get; set; }
        [XmlElement(ElementName = "BankCode")]
        public string BankCode { get; set; }
        [XmlElement(ElementName = "DebitInfo")]
        public DebitInfo DebitInfo { get; set; }
        [XmlElement(ElementName = "TPInfo")]
        public TPInfo TPInfo { get; set; }
    }

    [XmlRoot(ElementName = "DatabaseInfo")]
    public class DatabaseInfo
    {
        [XmlElement(ElementName = "ConnectionString")]
        public string ConnectionString { get; set; }
        [XmlElement(ElementName = "ConnectionSystemID")]
        public string ConnectionSystemID { get; set; }
        [XmlElement(ElementName = "IsEncrypted")]
        public string IsEncrypted { get; set; }
        [XmlElement(ElementName = "EncryptedKey")]
        public string EncryptedKey { get; set; }
    }

    [XmlRoot(ElementName = "FiscHSM")]
    public class FiscHSM
    {
        [XmlElement(ElementName = "IsARQCCheck")]
        public string IsARQCCheck { get; set; }
        [XmlElement(ElementName = "IsCVVCheck")]
        public string IsCVVCheck { get; set; }
        [XmlElement(ElementName = "IsCVV2Check")]
        public string IsCVV2Check { get; set; }
        [XmlElement(ElementName = "IsCAVVCheck")]
        public string IsCAVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVCheck")]
        public string IsPVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVDobCheck")]
        public string IsPVVDobCheck { get; set; }
    }

    [XmlRoot(ElementName = "IcashHSM")]
    public class IcashHSM
    {
        [XmlElement(ElementName = "IsARQCCheck")]
        public string IsARQCCheck { get; set; }
        [XmlElement(ElementName = "IsCVVCheck")]
        public string IsCVVCheck { get; set; }
        [XmlElement(ElementName = "IsCVV2Check")]
        public string IsCVV2Check { get; set; }
        [XmlElement(ElementName = "IsCAVVCheck")]
        public string IsCAVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVCheck")]
        public string IsPVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVDobCheck")]
        public string IsPVVDobCheck { get; set; }
    }

    [XmlRoot(ElementName = "IpassHSM")]
    public class IpassHSM
    {
        [XmlElement(ElementName = "IsARQCCheck")]
        public string IsARQCCheck { get; set; }
        [XmlElement(ElementName = "IsCVVCheck")]
        public string IsCVVCheck { get; set; }
        [XmlElement(ElementName = "IsCVV2Check")]
        public string IsCVV2Check { get; set; }
        [XmlElement(ElementName = "IsCAVVCheck")]
        public string IsCAVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVCheck")]
        public string IsPVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVDobCheck")]
        public string IsPVVDobCheck { get; set; }
    }

    [XmlRoot(ElementName = "NcccHSM")]
    public class NcccHSM
    {
        [XmlElement(ElementName = "IsARQCCheck")]
        public string IsARQCCheck { get; set; }
        [XmlElement(ElementName = "IsCVVCheck")]
        public string IsCVVCheck { get; set; }
        [XmlElement(ElementName = "IsCVV2Check")]
        public string IsCVV2Check { get; set; }
        [XmlElement(ElementName = "IsCAVVCheck")]
        public string IsCAVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVCheck")]
        public string IsPVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVDobCheck")]
        public string IsPVVDobCheck { get; set; }
    }

    [XmlRoot(ElementName = "SvcsHSM")]
    public class SvcsHSM
    {
        [XmlElement(ElementName = "IsARQCCheck")]
        public string IsARQCCheck { get; set; }
        [XmlElement(ElementName = "IsCVVCheck")]
        public string IsCVVCheck { get; set; }
        [XmlElement(ElementName = "IsCVV2Check")]
        public string IsCVV2Check { get; set; }
        [XmlElement(ElementName = "IsCAVVCheck")]
        public string IsCAVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVCheck")]
        public string IsPVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVDobCheck")]
        public string IsPVVDobCheck { get; set; }
    }

    [XmlRoot(ElementName = "TwinHSM")]
    public class TwinHSM
    {
        [XmlElement(ElementName = "IsARQCCheck")]
        public string IsARQCCheck { get; set; }
        [XmlElement(ElementName = "IsCVVCheck")]
        public string IsCVVCheck { get; set; }
        [XmlElement(ElementName = "IsCVV2Check")]
        public string IsCVV2Check { get; set; }
        [XmlElement(ElementName = "IsCAVVCheck")]
        public string IsCAVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVCheck")]
        public string IsPVVCheck { get; set; }
        [XmlElement(ElementName = "IsPVVDobCheck")]
        public string IsPVVDobCheck { get; set; }
    }

    [XmlRoot(ElementName = "HSMCheckFlags")]
    public class HSMCheckFlags
    {
        [XmlElement(ElementName = "FiscHSM")]
        public FiscHSM FiscHSM { get; set; }
        [XmlElement(ElementName = "IcashHSM")]
        public IcashHSM IcashHSM { get; set; }
        [XmlElement(ElementName = "IpassHSM")]
        public IpassHSM IpassHSM { get; set; }
        [XmlElement(ElementName = "NcccHSM")]
        public NcccHSM NcccHSM { get; set; }
        [XmlElement(ElementName = "SvcsHSM")]
        public SvcsHSM SvcsHSM { get; set; }
        [XmlElement(ElementName = "TwinHSM")]
        public TwinHSM TwinHSM { get; set; }
    }

    [XmlRoot(ElementName = "NCCCClientAuthRouterService")]
    public class NCCCClientAuthRouterService
    {
        [XmlElement(ElementName = "PrimaryServerName")]
        public string PrimaryServerName { get; set; }
        [XmlElement(ElementName = "BackupServerName")]
        public string BackupServerName { get; set; }
        [XmlElement(ElementName = "ListenerPortNumber")]
        public string ListenerPortNumber { get; set; }
        [XmlElement(ElementName = "ConnectionTimeOut")]
        public string ConnectionTimeOut { get; set; }
        [XmlElement(ElementName = "SendTimeOut")]
        public string SendTimeOut { get; set; }
        [XmlElement(ElementName = "ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }
        [XmlElement(ElementName = "RetryCount")]
        public string RetryCount { get; set; }
        [XmlElement(ElementName = "MaxSessionNumber")]
        public string MaxSessionNumber { get; set; }
    }

    [XmlRoot(ElementName = "FISCClientAuthRouterService")]
    public class FISCClientAuthRouterService
    {
        [XmlElement(ElementName = "PrimaryServerName")]
        public string PrimaryServerName { get; set; }
        [XmlElement(ElementName = "BackupServerName")]
        public string BackupServerName { get; set; }
        [XmlElement(ElementName = "ListenerPortNumber")]
        public string ListenerPortNumber { get; set; }
        [XmlElement(ElementName = "ConnectionTimeOut")]
        public string ConnectionTimeOut { get; set; }
        [XmlElement(ElementName = "SendTimeOut")]
        public string SendTimeOut { get; set; }
        [XmlElement(ElementName = "ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }
        [XmlElement(ElementName = "RetryCount")]
        public string RetryCount { get; set; }
        [XmlElement(ElementName = "MaxSessionNumber")]
        public string MaxSessionNumber { get; set; }
    }

    [XmlRoot(ElementName = "TWINClientAuthRouterService")]
    public class TWINClientAuthRouterService
    {
        [XmlElement(ElementName = "PrimaryServerName")]
        public string PrimaryServerName { get; set; }
        [XmlElement(ElementName = "BackupServerName")]
        public string BackupServerName { get; set; }
        [XmlElement(ElementName = "ListenerPortNumber")]
        public string ListenerPortNumber { get; set; }
        [XmlElement(ElementName = "ConnectionTimeOut")]
        public string ConnectionTimeOut { get; set; }
        [XmlElement(ElementName = "SendTimeOut")]
        public string SendTimeOut { get; set; }
        [XmlElement(ElementName = "ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }
        [XmlElement(ElementName = "RetryCount")]
        public string RetryCount { get; set; }
        [XmlElement(ElementName = "MaxSessionNumber")]
        public string MaxSessionNumber { get; set; }
    }

    [XmlRoot(ElementName = "SVCSClientAuthRouterService")]
    public class SVCSClientAuthRouterService
    {
        [XmlElement(ElementName = "PrimaryServerName")]
        public string PrimaryServerName { get; set; }
        [XmlElement(ElementName = "BackupServerName")]
        public string BackupServerName { get; set; }
        [XmlElement(ElementName = "ListenerPortNumber")]
        public string ListenerPortNumber { get; set; }
        [XmlElement(ElementName = "ConnectionTimeOut")]
        public string ConnectionTimeOut { get; set; }
        [XmlElement(ElementName = "SendTimeOut")]
        public string SendTimeOut { get; set; }
        [XmlElement(ElementName = "ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }
        [XmlElement(ElementName = "RetryCount")]
        public string RetryCount { get; set; }
        [XmlElement(ElementName = "MaxSessionNumber")]
        public string MaxSessionNumber { get; set; }
    }

    [XmlRoot(ElementName = "ICASHClientAuthRouterService")]
    public class ICASHClientAuthRouterService
    {
        [XmlElement(ElementName = "PrimaryServerName")]
        public string PrimaryServerName { get; set; }
        [XmlElement(ElementName = "BackupServerName")]
        public string BackupServerName { get; set; }
        [XmlElement(ElementName = "ListenerPortNumber")]
        public string ListenerPortNumber { get; set; }
        [XmlElement(ElementName = "ConnectionTimeOut")]
        public string ConnectionTimeOut { get; set; }
        [XmlElement(ElementName = "SendTimeOut")]
        public string SendTimeOut { get; set; }
        [XmlElement(ElementName = "ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }
        [XmlElement(ElementName = "RetryCount")]
        public string RetryCount { get; set; }
        [XmlElement(ElementName = "MaxSessionNumber")]
        public string MaxSessionNumber { get; set; }
    }

    [XmlRoot(ElementName = "IPASSClientAuthRouterService")]
    public class IPASSClientAuthRouterService
    {
        [XmlElement(ElementName = "PrimaryServerName")]
        public string PrimaryServerName { get; set; }
        [XmlElement(ElementName = "BackupServerName")]
        public string BackupServerName { get; set; }
        [XmlElement(ElementName = "ListenerPortNumber")]
        public string ListenerPortNumber { get; set; }
        [XmlElement(ElementName = "ConnectionTimeOut")]
        public string ConnectionTimeOut { get; set; }
        [XmlElement(ElementName = "SendTimeOut")]
        public string SendTimeOut { get; set; }
        [XmlElement(ElementName = "ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }
        [XmlElement(ElementName = "RetryCount")]
        public string RetryCount { get; set; }
        [XmlElement(ElementName = "MaxSessionNumber")]
        public string MaxSessionNumber { get; set; }
    }

    [XmlRoot(ElementName = "ClientAuthRouterServices")]
    public class ClientAuthRouterServices
    {
        [XmlElement(ElementName = "NCCCClientAuthRouterService")]
        public NCCCClientAuthRouterService NCCCClientAuthRouterService { get; set; }
        [XmlElement(ElementName = "FISCClientAuthRouterService")]
        public FISCClientAuthRouterService FISCClientAuthRouterService { get; set; }
        [XmlElement(ElementName = "TWINClientAuthRouterService")]
        public TWINClientAuthRouterService TWINClientAuthRouterService { get; set; }
        [XmlElement(ElementName = "SVCSClientAuthRouterService")]
        public SVCSClientAuthRouterService SVCSClientAuthRouterService { get; set; }
        [XmlElement(ElementName = "ICASHClientAuthRouterService")]
        public ICASHClientAuthRouterService ICASHClientAuthRouterService { get; set; }
        [XmlElement(ElementName = "IPASSClientAuthRouterService")]
        public IPASSClientAuthRouterService IPASSClientAuthRouterService { get; set; }
    }

    [XmlRoot(ElementName = "SystemParameterInfo")]
    public class SystemParameterInfo
    {
        [XmlElement(ElementName = "BankInfo")]
        public BankInfo BankInfo { get; set; }
        [XmlElement(ElementName = "DatabaseInfo")]
        public DatabaseInfo DatabaseInfo { get; set; }
        [XmlElement(ElementName = "HSMCheckFlags")]
        public HSMCheckFlags HSMCheckFlags { get; set; }
        [XmlElement(ElementName = "ClientAuthRouterServices")]
        public ClientAuthRouterServices ClientAuthRouterServices { get; set; }
    }

}

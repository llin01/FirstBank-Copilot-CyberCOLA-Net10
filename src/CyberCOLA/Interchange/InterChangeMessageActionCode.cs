namespace Com.Cybersoft4u.Interchange
{
    /// <summary>
    /// Defines message category constants for gateway, client auth, and router services.
    /// </summary>
    public class InterChangeMessageCategory
    {
        public const string GatewayVISAService = "G--V";
        public const string GatewayMASTERCARDService = "G--M";
        public const string GatewayMASTERCARDANRService = "G--MANR";
        public const string GatewayNCCCCOLAService = "G--NCCCCOLA";
        public const string GatewayNCPSCOLAService = "G--NCPSCOLA";
        public const string GatewayTWMPCOLAService = "G--TWMPNCOLA";
        public const string GatewayFISCCOLAService = "G--FISCCOLA";
        public const string GatewayTWINCOLAService = "G--TWINCOLA";
        public const string GatewaySTINCOLAService = "G--STINCOLA";
        public const string GatewaySVCSCOLAService = "G--SVCSCOLA";
        public const string GatewayICASHCOLAService = "G--ICASHCOLA";
        public const string GatewayIPASSCOLAService = "G--IPASSCOLA";
        public const string GatewayNCCCService = "G--NCCC";
        public const string GatewayNCPSService = "G--NCPS";
        public const string GatewayTWMPService = "G--TWMP";
        public const string GatewaySTINService = "G--STIN";
        public const string GatewayFISCService = "G--FISC";
        public const string GatewayTWINService = "G--TWIN";
        public const string GatewaySVCSService = "G--SVCS";
        public const string GatewayICASHService = "G--ICASH";
        public const string GatewayIPASSService = "G--IPASS";
        public const string GatewaySystemCommand = "G--S";
        public const string ClientAuthVISAClient = "C--V";
        public const string ClientAuthMASTERCARDClient = "C--M";
        public const string ClientAuthNCCCClient = "C--NCCC";
        public const string ClientAuthNCPSClient = "C--NCPS";
        public const string ClientAuthSTINClient = "C--STIN";
        public const string ClientAuthFISCClient = "C--FISC";
        public const string ClientAuthTWINClient = "C--TWIN";
        public const string ClientAuthSVCSClient = "C--SVCS";
        public const string ClientAuthICASHClient = "C--ICASH";
        public const string ClientAuthIPASSClient = "C--IPASS";
        public const string ClientAuthSystemCommand = "C--S";
        public const string RouterSystemCommand = "R--S";
    }
    /// <summary>
    /// Contains nested classes defining command code constants for various payment network services.
    /// </summary>
    public class InterChangeMessageCommandCode
    {

        /// <summary>
        /// VISA service command codes for sign-on, sign-off, and service control.
        /// </summary>
        public class VISAServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string STINSignOn = "SSI";
            public const string NormalSignOff = "NSO";
            public const string STINSignOff = "SSO";
            public const string SwitchAutoEchoFlag = "SEHO";
            public const string DisplayVISAConnectionInformation = "DISP";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }
        /// <summary>
        /// MasterCard service command codes for group and stage-based sign-on/sign-off operations.
        /// </summary>
        public class MASTERCARDServiceCommandCode
        {
            public const string GroupSignOn = "GSI";
            public const string GroupSignOff = "GSO";
            public const string Stage1SignOn = "S1SI";
            public const string Stage1SignOff = "S1SO";
            public const string Stage2SignOn = "S2SI";
            public const string Stage2SignOff = "S2SO";
            public const string SwitchGroupSignFlag = "CGSI";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }
        public class MASTERCARDANRServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }
        public class NCCCServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class NCPSServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class TWMPServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class FISCServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class TWINServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class SVCSServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class ICASHServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class IPASSServiceCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class NCCCCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class NCPSCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class TWMPCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class STINCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class FISCCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class TWINCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class SVCSCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class ICASHCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class IPASSCOLAServiceCommandCode
        {
            public const string StartService = "START";
            public const string StopService = "STOP";
        }

        public class VISAClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0300";
        }

        public class MASTERCARDClientCommandCode
        {
            public const string GroupSignOn = "GSI";
            public const string GroupSignOff = "GSO";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0300";
        }

        public class NCCCClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0300";
        }

        public class NCPSClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0300";
        }

        public class STINClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
        }

        public class FISCClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate0300 = "0300";
            public const string FileMaintUpdate0302 = "0302";
        }

        public class TWINClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string ChangeKey = "CKEY";
            public const string NewKey = "NKEY";
            public const string RepeatKey = "RKEY";
            public const string VerifyKey = "VKEY";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0300";
        }

        public class SVCSClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0302";
        }

        public class ICASHClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0302";
        }

        public class IPASSClientCommandCode
        {
            public const string NormalSignOn = "NSI";
            public const string NormalSignOff = "NSO";
            public const string EchoTest = "EHO";
            public const string FileMaintUpdate = "0302";
        }
        public class GatewaySYSTEMCommandCode
        {
            public const string GetConnectionStatus = "01";
            public const string GetChannelsStatus = "02";
            public const string Echo = "99";
        }
        public class ClientAuthSYSTEMCommandCode
        {
            public const string GetConnectionStatus = "01";
            public const string GetColaPasysInfo = "02";
            public const string ListReservedKeyData = "03";
            public const string DeleteReservedKeyData = "04";
            public const string ListReservedHCEKeyData = "05";
            public const string DeleteReservedHCEKeyData = "06";
            public const string ListReservedANRKeyData = "07";
            public const string DeleteReservedANRKeyData = "08";
            public const string SyncColaPasysInfo = "10";
            public const string Echo = "99";
        }
        public class RouterSYSTEMCommandCode
        {
            public const string GetConnectionStatus = "01";
        }
    }
    public enum InterChangeDataFormatType
    {
        Binary = 'B',
        JSON = 'J',
        Null = 'N',
        Object = 'O',
        String = 'S',
        XML = 'X'
    }
    public enum GatewayClientAuthServiceType
    {
        FISC = '0',
        ICASH = '1',
        IPASS = '2',
        NCCC = '3',
        NCPS = '4',
        SERVER = '5',
        SVCS = '6',
        TWIN = '7',
        NCCCQRCode = '8'
    }
}

#region << Version Note >>
/*
 * ==============================================================================
 *
 * Namespace/Filename: Com.Cybersoft4u.Test.WpfFunctionTest/XmleClass
 * CLR Version: 4.0.30319.42000
 * Description: 
 *
 * Version: 1.0
 * Created: 2020/10/27 上午 09:23:27
 * Compiler: Visual Studio 2019
 *
 * Author: CFLin
 * Company: CyberSoft Digital Services Corp.
 *
 *******************************************************************
 * CyberSoft Digital Services Corp. All rights reserved.
 *******************************************************************
 * ==============================================================================
*/
#endregion

#nullable disable

using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Wpf.WpfFunctionTest1
{
    [XmlRoot(ElementName = "TXN_INFO")]
    public class TXN_INFO
    {
        [XmlElement(ElementName = "NOTICE_NO")]
        public string NOTICE_NO { get; set; }
        [XmlElement(ElementName = "TXN_AMOUNT")]
        public string TXN_AMOUNT { get; set; }
    }

    [XmlRoot(ElementName = "CC")]
    public class CC
    {
        [XmlElement(ElementName = "CARD_NUMBER")]
        public string CARD_NUMBER { get; set; }
        [XmlElement(ElementName = "EXPIRE_DATE")]
        public string EXPIRE_DATE { get; set; }
        [XmlElement(ElementName = "CVV2")]
        public string CVV2 { get; set; }
        [XmlElement(ElementName = "ACQ_BIN")]
        public string ACQ_BIN { get; set; }
        [XmlElement(ElementName = "RRN")]
        public string RRN { get; set; }
        [XmlElement(ElementName = "TERMINAL_ID")]
        public string TERMINAL_ID { get; set; }
    }

    [XmlRoot(ElementName = "PAY_INFO")]
    public class PAY_INFO
    {
        [XmlElement(ElementName = "CC")]
        public CC CC { get; set; }
    }

    [XmlRoot(ElementName = "MESSAGE")]
    public class MESSAGE
    {
        [XmlElement(ElementName = "TXN_DATETIME")]
        public string TXN_DATETIME { get; set; }
        [XmlElement(ElementName = "API_TXN_NO")]
        public string API_TXN_NO { get; set; }
        [XmlElement(ElementName = "MTI")]
        public string MTI { get; set; }
        [XmlElement(ElementName = "PCODE")]
        public string PCODE { get; set; }
        [XmlElement(ElementName = "SRC_ID")]
        public string SRC_ID { get; set; }
        [XmlElement(ElementName = "SESSION_ID")]
        public string SESSION_ID { get; set; }
        [XmlElement(ElementName = "FEE_REF_ID")]
        public string FEE_REF_ID { get; set; }
        [XmlElement(ElementName = "HC")]
        public string HC { get; set; }
        [XmlElement(ElementName = "TXN_INFO")]
        public TXN_INFO TXN_INFO { get; set; }
        [XmlElement(ElementName = "PAY_INFO")]
        public PAY_INFO PAY_INFO { get; set; }
    }

    [XmlRoot(ElementName = "MAC_INFO")]
    public class MAC_INFO
    {
        [XmlElement(ElementName = "KEY_ID")]
        public string KEY_ID { get; set; }
        [XmlElement(ElementName = "DIV_DATA")]
        public string DIV_DATA { get; set; }
        [XmlElement(ElementName = "ICV")]
        public string ICV { get; set; }
        [XmlElement(ElementName = "MAC")]
        public string MAC { get; set; }
    }

    [XmlRoot(ElementName = "ENVELOPE")]
    public class ENVELOPE
    {
        [XmlElement(ElementName = "MESSAGE")]
        public MESSAGE MESSAGE { get; set; }
        [XmlElement(ElementName = "MAC_INFO")]
        public MAC_INFO MAC_INFO { get; set; }
    }
}

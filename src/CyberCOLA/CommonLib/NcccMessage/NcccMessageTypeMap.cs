// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NcccMessageTypeMap.cs" company="Cybersoft">
//   Author:
// </copyright>
// <summary>
//   Defines the Nccc message type map.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    [XmlRoot(ElementName = "Field")]
    [Serializable()]
    public class Field
    {
        [XmlAttribute(AttributeName = "Number")]
        public string Number { get; set; }
        [XmlAttribute(AttributeName = "DataCondition")]
        public string DataCondition { get; set; }
    }

    [XmlRoot(ElementName = "RequestField")]
    [Serializable()]
    public class RequestField
    {
        [XmlElement(ElementName = "Field")]
        public List<Field> Field { get; set; }
    }

    [XmlRoot(ElementName = "ResponseField")]
    [Serializable()]
    public class ResponseField
    {
        [XmlElement(ElementName = "Field")]
        public List<Field> Field { get; set; }
    }

    [XmlRoot(ElementName = "MessageType")]
    [Serializable()]
    public class MessageType
    {
        [XmlElement(ElementName = "RequestMessage")]
        public string RequestMessage { get; set; }
        [XmlElement(ElementName = "RequestField")]
        public RequestField RequestField { get; set; }
        [XmlElement(ElementName = "ResponseMessage")]
        public string ResponseMessage { get; set; }
        [XmlElement(ElementName = "ResponseField")]
        public ResponseField ResponseField { get; set; }
    }

    [XmlRoot(ElementName = "NcccMessageTypeInfo")]
    [Serializable()]
    public class NcccMessageTypeInfo
    {
        [XmlElement(ElementName = "MessageType")]
        public List<MessageType> MessageType { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsd { get; set; }
    }

}

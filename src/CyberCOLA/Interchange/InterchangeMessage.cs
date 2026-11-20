using System;

namespace Com.Cybersoft4u.Interchange
{
    /// <summary>
    /// Represents an interchange message containing routing information, command codes, and payload data.
    /// </summary>
    [Serializable]
    public class InterchangeMessage
    {
        public InterchangeMessage()
        {
            InterchangeDataFormat = InterChangeDataFormatType.Null;
        }
        public char StartingPoint { get; set; }
        public string MessageId { get; set; }
        public string SourceEndPoint { get; set; }
        public string DestinationEndPoint { get; set; }
        public string CreateDateTime { get; set; }
        public string ReturnDateTime { get; set; }
        public string Category { get; set; }
        public string CommandCode { get; set; }
        public InterChangeDataFormatType InterchangeDataFormat { get; set; }
        public byte[] InterchangeData { get; set; }
        public bool ReturnOption { get; set; }
        public string ReturnCode { get; set; }
    }
}

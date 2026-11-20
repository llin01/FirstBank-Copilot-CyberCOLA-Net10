namespace Com.Cybersoft4u.TM.ISO8583.TWIN
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public class TWINHeader : Iso8583Header
    {
        private const int PrefixEndIndex = 3;

        private const int MessageTypeEndIndex = 5;

        private const int ReleaseNumberEndIndex = 7;

        private const int StatusEndIndex = 10;

        private const int OriginatorCodeEndIndex = 11;

        private const int ResponderCodeEndIndex = 12;

        private string prefix;

        private string messageType;

        private string releaseNumber;

        private string status;

        private string originatorCode;

        private string responderCode;

        public TWINHeader()
        {
            this.prefix = "NCM";
            this.messageType = "00"; // For Network Management(00)/Authorization(03)/Blacklist(08)
            this.releaseNumber = "01";
            this.status = "000";
            //this.originatorCode = "7";   //Acquire
            this.originatorCode = "7";     //Interchange Switch Interface
            this.responderCode = "0";
        }

        public TWINHeader(string headerString)
        {
            this.Decode(headerString);
        }

        public string Encode()
        {
            return this.prefix + this.messageType + this.releaseNumber + this.status + this.originatorCode
                   + this.responderCode;
        }

        public Iso8583Header Decode(string header)
        {
            this.prefix = header.IndexSubstring(0, PrefixEndIndex);
            this.messageType = header.IndexSubstring(PrefixEndIndex, MessageTypeEndIndex);
            this.releaseNumber = header.IndexSubstring(MessageTypeEndIndex, ReleaseNumberEndIndex);
            this.status = header.IndexSubstring(ReleaseNumberEndIndex, StatusEndIndex);
            this.originatorCode = header.IndexSubstring(StatusEndIndex, OriginatorCodeEndIndex);
            this.responderCode = header.IndexSubstring(OriginatorCodeEndIndex, ResponderCodeEndIndex);
            return this;
        }

        public string Prefix()
        {
            return this.prefix;
        }

        public string MessageType()
        {
            return this.messageType;
        }

        public string ReleaseNumber()
        {
            return this.releaseNumber;
        }

        public string Status()
        {
            return this.status;
        }

        public string OriginatorCode()
        {
            return this.originatorCode;
        }

        public string ResponderCode()
        {
            return this.responderCode;
        }

        public TWINHeader Prefix(string prefix)
        {
            this.prefix = prefix;
            return this;
        }

        public TWINHeader ProductIndicator(string productIndicator)
        {
            this.messageType = productIndicator;
            return this;
        }

        public TWINHeader ReleaseNumber(string releaseNumber)
        {
            this.releaseNumber = releaseNumber;
            return this;
        }

        public TWINHeader Status(string status)
        {
            this.status = status;
            return this;
        }

        public TWINHeader OriginatorCode(string originatorCode)
        {
            this.originatorCode = originatorCode;
            return this;
        }

        public TWINHeader ResponderCode(string responderCode)
        {
            this.responderCode = responderCode;
            return this;
        }
    }
}

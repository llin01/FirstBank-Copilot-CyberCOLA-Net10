namespace Com.Cybersoft4u.TM.ISO8583.Nccc
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public class NcccHeader : Iso8583Header
    {
        private const int PrefixEndIndex = 3;

        private const int ProductIndicatorEndIndex = 5;

        private const int ReleaseNumberEndIndex = 7;

        private const int StatusEndIndex = 10;

        private const int OriginatorCodeEndIndex = 11;

        private const int ResponderCodeEndIndex = 12;

        private string prefix;

        private string productIndicator;

        private string releaseNumber;

        private string status;

        private string originatorCode;

        private string responderCode;

        public NcccHeader()
        {
            this.prefix = "ISO";
            //this.productIndicator = "02"; // For POS Module
            this.productIndicator = "00"; // For Network Management/Reconciliation Message
            this.releaseNumber = "60";
            this.status = "000";
            //this.originatorCode = "7";   //Acquire
            this.originatorCode = "6";     //Interchange Switch Interface
            this.responderCode = "0";
        }

        public NcccHeader(string headerString)
        {
            this.Decode(headerString);
        }

        public string Encode()
        {
            return this.prefix + this.productIndicator + this.releaseNumber + this.status + this.originatorCode
                   + this.responderCode;
        }

        public Iso8583Header Decode(string header)
        {
            this.prefix = header.IndexSubstring(0, PrefixEndIndex);
            this.productIndicator = header.IndexSubstring(PrefixEndIndex, ProductIndicatorEndIndex);
            this.releaseNumber = header.IndexSubstring(ProductIndicatorEndIndex, ReleaseNumberEndIndex);
            this.status = header.IndexSubstring(ReleaseNumberEndIndex, StatusEndIndex);
            this.originatorCode = header.IndexSubstring(StatusEndIndex, OriginatorCodeEndIndex);
            this.responderCode = header.IndexSubstring(OriginatorCodeEndIndex, ResponderCodeEndIndex);
            return this;
        }

        public string Prefix()
        {
            return this.prefix;
        }

        public string ProductIndicator()
        {
            return this.productIndicator;
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

        public NcccHeader Prefix(string prefix)
        {
            this.prefix = prefix;
            return this;
        }

        public NcccHeader ProductIndicator(string productIndicator)
        {
            this.productIndicator = productIndicator;
            return this;
        }

        public NcccHeader ReleaseNumber(string releaseNumber)
        {
            this.releaseNumber = releaseNumber;
            return this;
        }

        public NcccHeader Status(string status)
        {
            this.status = status;
            return this;
        }

        public NcccHeader OriginatorCode(string originatorCode)
        {
            this.originatorCode = originatorCode;
            return this;
        }

        public NcccHeader ResponderCode(string responderCode)
        {
            this.responderCode = responderCode;
            return this;
        }
    }
}

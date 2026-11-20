namespace Com.Cybersoft4u.TM.ISO8583.Edc
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public class EdcHeader : Iso8583Header
    {
        private const int TpduIdentiferEndIndex = 2;

        private const int TpduDestAddrEndIndex = 6;

        private const int TpduOrigAddrEndIndex = 10;

        private string tpduIdentifier;

        private string tpduDestAddr;

        private string tpduOrigAddr;

        public EdcHeader()
        {
            this.tpduIdentifier = "60";
            this.tpduDestAddr = "0297";
            this.tpduOrigAddr = "0000";
        }

        public EdcHeader(string headerString)
        {
            this.Decode(headerString);
        }

        public string Encode()
        {
            return this.tpduIdentifier + this.tpduDestAddr + this.tpduOrigAddr;
        }

        public Iso8583Header Decode(string header)
        {
            this.tpduIdentifier = header.IndexSubstring(0, TpduIdentiferEndIndex);
            this.tpduDestAddr = header.IndexSubstring(TpduIdentiferEndIndex, TpduDestAddrEndIndex);
            this.tpduOrigAddr = header.IndexSubstring(TpduDestAddrEndIndex, TpduOrigAddrEndIndex);
            return this;
        }

        public string TpduIdentifier()
        {
            return this.tpduIdentifier;
        }

        public string TpduDestAddr()
        {
            return this.tpduDestAddr;
        }

        public string TpduOrigAddr()
        {
            return this.tpduOrigAddr;
        }

        public EdcHeader TpduIdentifer(string tpduIdentifier)
        {
            this.tpduIdentifier = tpduIdentifier;
            return this;
        }

        public EdcHeader TpduDestAddr(string tpduDestAddr)
        {
            this.tpduDestAddr = tpduDestAddr;
            return this;
        }

        public EdcHeader TpduOrigAddr(string tpduOrigAddr)
        {
            this.tpduOrigAddr = tpduOrigAddr;
            return this;
        }
    }
}

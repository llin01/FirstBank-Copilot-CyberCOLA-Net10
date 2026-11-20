using Com.Cybersoft4u.CyberUtils.Utils;

namespace Com.Cybersoft4u.TM.ISO8583.ICASH
{
    #region DEPENDENCY



    #endregion

    public class ICASHHeader : Iso8583Header
    {
        private const int SrcIdEndIndex = 4;

        private const int DesIdEndIndex = 8;

        private string destId;

        private string srcId;

        public ICASHHeader()
        {
            this.srcId = IcashTransactionConst.MessageHeader.IcashIssuerID;   // Source ID
            this.destId = IcashTransactionConst.MessageHeader.IcashHostID;     // Destination ID
        }

        public ICASHHeader(string headerString)
        {
            this.Decode(headerString);
        }

        public string Encode()
        {
            return this.srcId + this.destId;
        }

        public Iso8583Header Decode(string header)
        {
            this.srcId = header.IndexSubstring(0, SrcIdEndIndex);
            this.destId = header.IndexSubstring(SrcIdEndIndex, DesIdEndIndex);
            return this;
        }

        public string SrcID()
        {
            return this.srcId;
        }
        public string DestID()
        {
            return this.destId;
        }
        public ICASHHeader DestId(string destId)
        {
            this.destId = destId;
            return this;
        }

        public ICASHHeader SrcId(string srcId)
        {
            this.srcId = srcId;
            return this;
        }
    }
}

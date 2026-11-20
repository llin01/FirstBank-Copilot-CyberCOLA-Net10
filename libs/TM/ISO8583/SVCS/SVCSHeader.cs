using Com.Cybersoft4u.CyberUtils.Utils;

namespace Com.Cybersoft4u.TM.ISO8583.SVCS
{
    #region DEPENDENCY



    #endregion

    public class SVCSHeader : Iso8583Header
    {
        private const int DesIdEndIndex = 4;

        private const int SrcIdEndIndex = 8;

        private string destId;

        private string srcId;

        public SVCSHeader()
        {
            this.destId = SvcsTransactionConst.MessageHeader.SvcsHostID;     // Destination ID
            this.srcId = SvcsTransactionConst.MessageHeader.SvcsIssuerID;   // Source ID
        }

        public SVCSHeader(string headerString)
        {
            this.Decode(headerString);
        }

        public string Encode()
        {
            return this.destId + this.srcId;
        }

        public Iso8583Header Decode(string header)
        {
            this.destId = header.IndexSubstring(0, DesIdEndIndex);
            this.srcId = header.IndexSubstring(DesIdEndIndex, SrcIdEndIndex);
            return this;
        }

        public string DestID()
        {
            return this.destId;
        }

        public string SrcID()
        {
            return this.srcId;
        }

        public SVCSHeader DestId(string destId)
        {
            this.destId = destId;
            return this;
        }

        public SVCSHeader SrcId(string srcId)
        {
            this.srcId = srcId;
            return this;
        }
    }
}

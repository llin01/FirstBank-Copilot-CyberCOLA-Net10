using Com.Cybersoft4u.TM.ISO8583.MCOLA;
using System;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class ReservedKeyData
    {
        public DateTime CreateDateTime { get; set; }
        public string TxKeySeq { get; set; }
    }

    public class ReservedHCEKeyData
    {
        public DateTime CreateDateTime { get; set; }
        public string F35 { get; set; }
        public string F39 { get; set; }
        public string F122 { get; set; }
    }

    public class ReservedANRKeyData
    {
        public DateTime CreateDateTime { get; set; }
        public MCOLA MCOLAMessage { get; set; }
    }
}

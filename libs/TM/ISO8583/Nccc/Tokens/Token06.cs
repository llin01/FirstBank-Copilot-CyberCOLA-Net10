// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Token06.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Token06 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class Token06 : Token
    {
        public Token06()
            : base("06")
        {
        }

        public string NewPinFrmt { get; set; }

        public string NewPinOfst { get; set; }

        public string PinCnt { get; set; }

        public string NewPinSize { get; set; }

        public string NewPin1 { get; set; }

        public string NewPin2 { get; set; }

        public string NcccStandInInd { get; set; }

        public string PvvOnCardFlg { get; set; }

        public override string DataEncode()
        {
            return this.NewPinFrmt + this.NewPinOfst + this.PinCnt + this.NewPinSize + this.NewPin1 + this.NewPin2
                   + this.NcccStandInInd + this.PvvOnCardFlg;
        }

        public override Token DataDecode(string data)
        {
            this.NewPinFrmt = data.Substring(0, 1);
            this.NewPinOfst = data.Substring(1, 16);
            this.PinCnt = data.Substring(17, 1);
            this.NewPinSize = data.Substring(18, 2);
            this.NewPin1 = data.Substring(20, 16);
            this.NewPin2 = data.Substring(36, 16);
            this.NcccStandInInd = data.Substring(52, 1);
            this.PvvOnCardFlg = data.Substring(53, 1);
            return this;
        }
    }
}

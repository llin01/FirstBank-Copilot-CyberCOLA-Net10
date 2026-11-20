// IAG-TM-TokenBY.cs
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenBY : Token
    {
        public TokenBY()
            : base("BY")
        {
        }

        public string CrdLvlProdIdVal { get; set; }

        public override string DataEncode()
        {
            return string.Empty.PadLeft(52, ' ') + this.CrdLvlProdIdVal + string.Empty.PadLeft(6, ' ');
        }

        public override Token DataDecode(string data)
        {
            this.CrdLvlProdIdVal = data.Substring(52, 2);
            return this;
        }
    }
}

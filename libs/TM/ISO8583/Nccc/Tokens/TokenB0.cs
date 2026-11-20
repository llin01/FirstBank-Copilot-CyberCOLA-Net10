// IAG-TM-TokenB0.cs
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenB0 : Token
    {
        public TokenB0()
            : base(NcccTransactionConst.PosAdditionalData.TokenB0.Id)
        {
            this.Fiid = string.Empty.PadRight(4, ' ');
            this.AdviceRsnCde = string.Empty.PadRight(3, ' ');
            this.OnBehalf = string.Empty.PadRight(30, ' ');
            this.AdvcDetlCde = string.Empty.PadRight(4, ' ');
            this.AuthAgentIdCde = string.Empty.PadRight(6, ' ');
        }

        public string Fiid { get; set; }

        public string AdviceRsnCde { get; set; }

        public string OnBehalf { get; set; }

        public string AdvcDetlCde { get; set; }

        public string AuthAgentIdCde { get; set; }


        public override string DataEncode()
        {
            return "220" + " " + this.Fiid + string.Empty.PadLeft(12, ' ') + this.AdviceRsnCde
                   + string.Empty.PadLeft(15, ' ') + this.OnBehalf
                   + string.Empty.PadLeft(17, ' ') + this.AdvcDetlCde + this.AuthAgentIdCde
                   + string.Empty.PadLeft(129, ' ');
        }

        public override Token DataDecode(string data)
        {
            this.Fiid = data.Substring(4, 4);
            this.AdviceRsnCde = data.Substring(20, 3);
            this.OnBehalf = data.Substring(38, 30);
            this.AdvcDetlCde = data.Substring(85, 4);
            this.AuthAgentIdCde = data.Substring(89, 6);
            return this;
        }
    }
}

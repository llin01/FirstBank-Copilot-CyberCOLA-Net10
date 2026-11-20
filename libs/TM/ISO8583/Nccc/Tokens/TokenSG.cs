// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenSG.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenSG type.
//   Update:C.F 20180903 
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenSG : Token
    {
        public TokenSG()
            : base("SG")
        {
        }

        public string FrmtCde { get; set; }

        public string Score { get; set; }

        public string RsnCde { get; set; }

        public string RuleAdjScore { get; set; }

        public string RuleRsnCde1 { get; set; }

        public string RuleRsnCde2 { get; set; }

        public string NumSrvcs { get; set; }

        public string Ind { get; set; }

        public string Data { get; set; }

        public string InfoFiller { get; set; }

        public string AcctDataCmprmsInfo { get; set; }

        public string Filler { get; set; }

        public override string DataEncode()
        {
            return this.FrmtCde + this.Score + this.RsnCde + this.RuleAdjScore + this.RuleRsnCde1 + this.RuleRsnCde2
                 + this.NumSrvcs + this.Ind + this.Data + this.InfoFiller + this.AcctDataCmprmsInfo + this.Filler;

        }

        public override Token DataDecode(string data)
        {
            this.FrmtCde = data.Substring(0, 2);
            //FraudData(2 - 13)
            this.Score = data.Substring(2, 3);
            this.RsnCde = data.Substring(5, 2);
            this.RuleAdjScore = data.Substring(7, 3);
            this.RuleRsnCde1 = data.Substring(10, 2);
            this.RuleRsnCde2 = data.Substring(12, 2);
            //SecSrvc(14 - 111)
            this.NumSrvcs = data.Substring(14, 2);
            //Info(16 - 111)
            this.Ind = data.Substring(16, 3);
            this.Data = data.Substring(19, 3);
            this.InfoFiller = data.Substring(22, 90);

            this.AcctDataCmprmsInfo = data.Substring(112, 24);
            this.Filler = data.Substring(136, 4);

            return this;
        }
    }
}

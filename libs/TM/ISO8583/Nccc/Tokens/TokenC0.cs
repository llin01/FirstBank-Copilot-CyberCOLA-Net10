// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenC0.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenC0 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenC0 : Token
    {
        public TokenC0()
            : base(NcccTransactionConst.PosAdditionalData.TokenC0.Id)
        {
            this.CvdFld = string.Empty.PadRight(4, ' ');
            this.MotoOrECFlag = NcccTransactionConst.PosAdditionalData.TokenC0.MoToEcFlag.NotApplicable;
            this.CvdFldPresent = NcccTransactionConst.PosAdditionalData.TokenC0.CvdFldPresent.Cvv2NotProvided;
            this.UcafAuthnCollInd = string.Empty.PadRight(1, ' ');
            this.CavvAavResults = string.Empty.PadRight(1, ' ');
            this.Visa3DSInd = string.Empty.PadRight(1, ' ');
        }

        public string CvdFld { get; set; }

        public string MotoOrECFlag { get; set; }

        public string CvdFldPresent { get; set; }

        public string UcafAuthnCollInd { get; set; }

        public string CavvAavResults { get; set; }

        public string Visa3DSInd { get; set; }

        public override string DataEncode()
        {
            return this.CvdFld + string.Empty.PadLeft(14, ' ') + this.MotoOrECFlag + "  " + this.CvdFldPresent + this.Visa3DSInd
                   + this.UcafAuthnCollInd + " " + this.CavvAavResults;
        }

        public override Token DataDecode(string data)
        {
            this.CvdFld = data.Substring(0, 4);
            this.MotoOrECFlag = data.Substring(18, 1);
            this.CvdFldPresent = data.Substring(21, 1);
            this.Visa3DSInd = data.Substring(22, 1);
            this.UcafAuthnCollInd = data.Substring(23, 1);
            this.CavvAavResults = data.Substring(25, 1);
            return this;
        }
    }
}

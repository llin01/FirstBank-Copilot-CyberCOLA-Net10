// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenFA.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenFA type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenFA : Token
    {
        public TokenFA()
            : base("FA")
        {
        }

        public string PmntFacilitatorId { get; set; }

        public string IdpntSalesOrgId { get; set; }

        public string SubMrchId { get; set; }

        public override string DataEncode()
        {
            return this.PmntFacilitatorId + this.IdpntSalesOrgId + this.SubMrchId + string.Empty.PadLeft(13, ' ');
        }

        public override Token DataDecode(string data)
        {
            this.PmntFacilitatorId = data.Substring(0, 11);
            this.IdpntSalesOrgId = data.Substring(11, 11);
            this.SubMrchId = data.Substring(22, 15);
            return this;
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenFJ.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenFJ type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenFJ : Token
    {
        public TokenFJ()
            : base("FJ")
        {
        }

        public string FrmtCde { get; set; }

        public string LengthOfData { get; set; }

        public string MerchantId { get; set; }

        public override string DataEncode()
        {
            return this.FrmtCde + this.LengthOfData + this.MerchantId;
        }

        public override Token DataDecode(string data)
        {
            this.FrmtCde = data.Substring(0, 2);
            this.LengthOfData = data.Substring(2, 3);
            if (int.TryParse(LengthOfData, out var infoBufferLength))
            {
                this.MerchantId = data.Substring(5, infoBufferLength);
            }
            else
            {
                this.MerchantId = string.Empty;
            }
            return this;
        }
    }
}

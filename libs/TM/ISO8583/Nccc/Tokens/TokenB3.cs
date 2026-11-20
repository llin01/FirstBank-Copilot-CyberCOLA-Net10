// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenB3.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenB3 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenB3 : Token
    {
        public TokenB3()
            : base(NcccTransactionConst.PosAdditionalData.TokenB3.Id)
        {
            this.Bitmap = string.Empty.PadRight(4, ' ');
            this.TermSerNum = string.Empty.PadRight(8, '0');
            this.TermCap = string.Empty.PadRight(6, '0');
            this.UsrFld1 = string.Empty.PadRight(6, ' ');
            this.UsFld2 = string.Empty.PadRight(8, ' ');
            this.EmvTermTyp = string.Empty.PadRight(2, '0');
            this.ApplVerNum = string.Empty.PadRight(4, ' ');
            this.CvmRslts = string.Empty.PadRight(6, '0');
            this.DfNameLgth = string.Empty.PadRight(4, '0');
            this.DfName = string.Empty.PadRight(32, '0');
        }

        public string Bitmap { get; set; }

        public string TermSerNum { get; set; }

        public string TermCap { get; set; }

        public string UsrFld1 { get; set; }

        public string UsFld2 { get; set; }

        public string EmvTermTyp { get; set; }

        public string ApplVerNum { get; set; }

        public string CvmRslts { get; set; }

        public string DfNameLgth { get; set; }

        public string DfName { get; set; }

        public override string DataEncode()
        {
            return this.Bitmap + this.TermSerNum + this.TermCap + this.UsrFld1 + this.UsFld2 + this.EmvTermTyp
                   + this.ApplVerNum + this.CvmRslts + this.DfNameLgth + this.DfName;
        }

        public override Token DataDecode(string data)
        {
            this.Bitmap = data.Substring(0, 4);
            this.TermSerNum = data.Substring(4, 8);
            this.TermCap = data.Substring(12, 6);
            this.UsrFld1 = data.Substring(18, 6);
            this.UsFld2 = data.Substring(24, 8);
            this.EmvTermTyp = data.Substring(32, 2);
            this.ApplVerNum = data.Substring(34, 4);
            this.CvmRslts = data.Substring(38, 6);
            this.DfNameLgth = data.Substring(44, 4);
            this.DfName = data.Substring(48, 32);
            return this;
        }
    }
}

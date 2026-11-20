// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenS4.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenS4 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    #region DEPENDENCY

    using System;

    #endregion

    public class TokenS4 : Token
    {
        public TokenS4()
            : base("S4")
        {
        }

        public string DatasetId { get; set; }

        public string DatasetLength { get; set; }

        public string SupplData { get; set; }

        public override string DataEncode()
        {
            return this.DatasetId + this.DatasetLength + this.SupplData;
        }

        public override Token DataDecode(string data)
        {
            this.DatasetId = data.Substring(0, 2);
            this.DatasetLength = data.Substring(2, 4);
            //SupplData Unpack Format
            this.SupplData = data.Substring(6, Convert.ToUInt16(DatasetLength) * 2);
            return this;
        }
    }
}

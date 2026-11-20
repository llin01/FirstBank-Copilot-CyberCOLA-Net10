// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenQR.cs">
//   Author: C.F
// </copyright>
// <summary>
//   Defines the TokenQR type.
//   Update:C.F 20190617 
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenQR : Token
    {
        public TokenQR()
            : base("QR")
        {
        }

        public string TokenQRData { get; set; }

        public override string DataEncode()
        {
            return TokenQRData;
        }

        public override Token DataDecode(string data)
        {
            this.TokenQRData = data;

            return this;
        }

        public Dictionary<string, string> GetSubTokenData()
        {
            if (string.IsNullOrEmpty(this.TokenQRData))
                return null;
            try
            {
                var subTokenDictionary = new Dictionary<string, string>();
                int startPosition = 0;
                int totalLength = this.TokenQRData.Length;
                while (startPosition < totalLength)
                {
                    var offset = 2;
                    var subtoken = TokenQRData.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 3;
                    var subtokenLength = Convert.ToInt16(TokenQRData.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subtokenLength;
                    var subtokenData = TokenQRData.Substring(startPosition, offset);
                    startPosition += offset;
                    subTokenDictionary[subtoken] = subtokenData;
                }
                return subTokenDictionary;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}

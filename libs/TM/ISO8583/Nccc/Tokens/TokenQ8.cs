// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenQ8.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenQ8 type.
//   Update:C.F 20181121
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenQ8 : Token
    {
        public TokenQ8()
            : base("Q8")
        {
        }

        public string TokenQ8Data { get; set; }

        public override string DataEncode()
        {
            return TokenQ8Data;
        }

        public override Token DataDecode(string data)
        {
            this.TokenQ8Data = data;

            return this;
        }

        public Dictionary<string, string> GetSubTokenData()
        {
            if (string.IsNullOrEmpty(this.TokenQ8Data))
                return null;
            try
            {
                var subTokenDictionary = new Dictionary<string, string>();
                int startPosition = 0;
                int totalLength = this.TokenQ8Data.Length;
                while (startPosition < totalLength)
                {
                    var offset = 2;
                    if (startPosition + offset > totalLength) break;
                    var subtoken = TokenQ8Data.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 3;
                    if (startPosition + offset > totalLength) break;
                    var subtokenLength = Convert.ToInt16(TokenQ8Data.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subtokenLength;
                    if (startPosition + offset > totalLength) break;
                    var subtokenData = TokenQ8Data.Substring(startPosition, offset);
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

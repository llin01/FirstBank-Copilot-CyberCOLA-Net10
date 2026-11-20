// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenQA.cs">
//   Author: C.F
// </copyright>
// <summary>
//   Defines the TokenQA type.
//   Update:C.F 20190617
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens
{
    public class TokenQA : Token
    {
        public TokenQA()
            : base("QA")
        {
        }

        public string TokenQAData { get; set; }

        public override string DataEncode()
        {
            return TokenQAData;
        }

        public override Token DataDecode(string data)
        {
            this.TokenQAData = data;

            return this;
        }

        public Dictionary<string, string> GetSubTokenData()
        {
            if (string.IsNullOrEmpty(this.TokenQAData))
                return null;
            try
            {
                var subTokenDictionary = new Dictionary<string, string>();
                int startPosition = 0;
                int totalLength = this.TokenQAData.Length;
                while (startPosition < totalLength)
                {
                    var offset = 2;
                    var subtoken = TokenQAData.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 3;
                    var subtokenLength = Convert.ToInt16(TokenQAData.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subtokenLength;
                    var subtokenData = TokenQAData.Substring(startPosition, offset);
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

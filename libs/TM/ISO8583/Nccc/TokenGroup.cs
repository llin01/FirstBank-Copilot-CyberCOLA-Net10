// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TokenGroup.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TokenGroup type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Com.Cybersoft4u.CyberUtils.Utils;

namespace Com.Cybersoft4u.TM.ISO8583.Nccc
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Com.Cybersoft4u.Utils;

    #endregion

    public class TokenGroup : IFieldCodec<string, string, TokenGroup>
    {
        public const string EyeCatcher = "&";

        public Dictionary<string, Token> Tokens = new Dictionary<string, Token>();

        public TokenGroup()
        {
        }

        public Token GetToken(string id)
        {
            if (this.Tokens.ContainsKey(id))
            {
                return this.Tokens[id];
            }
            else
            {
                return null;
            }
        }

        public TokenGroup(string data)
        {
            this.Decode(data);
        }

        public string Encode()
        {
            var tokensData = this.Tokens.Aggregate(
                string.Empty,
                (current, tokenPair) => current + tokenPair.Value.Encode());

            return EyeCatcher + " " + (this.Tokens.Count + 1).ToString().PadLeft(5, '0')
                   + (tokensData.Length + 12).ToString().PadLeft(5, '0') + tokensData;
        }

        public TokenGroup Decode(string data)
        {
            if (data.Length <= 12)
            {
                return this;
            }

            data = data.Substring(12);
            while (data.Length > 10)
            {
                var tokenDataLength = Convert.ToUInt16(data.Substring(4, 5));
                var tokenData = data.Substring(0, 10 + tokenDataLength);
                data = data.Substring(10 + tokenDataLength);
                var token = Token.Generate(tokenData);
                if (ObjectUtils.IsNull(token))
                {
                    LoggerAdapter.GetLogger(this.GetType().FullName).Warn($"Token Generate Error[{tokenData}]");
                    continue;
                }
                this.Tokens[token.Id] = token;
            }

            return this;
        }

        public void ShowTokens()
        {
            foreach (var token in this.Tokens)
            {
                LoggerAdapter.GetLogger(this.GetType().FullName).Info($"[Token{token.Key}] = [{token.Value.Encode()}]");
            }
        }
    }
}

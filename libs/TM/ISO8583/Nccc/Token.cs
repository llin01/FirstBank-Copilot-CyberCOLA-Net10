// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Token.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Token type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;

    #endregion

    public abstract class Token : IFieldCodec<string, string, Token>
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public const string EyeCatcher = "!";
        private static readonly Dictionary<string, Type> TokenTypes = new Dictionary<string, Type>();

        static Token()
        {
            Init();
        }

        protected Token(string id)
        {
            this.Id = id;
        }

        public string Id { get; protected set; }

        public static Token Generate(string data)
        {
            var id = data.Substring(2, 2);
            Type type;
            if (!TokenTypes.TryGetValue(id, out type))
            {
                return null;
            }

            //Depend on C5 data length and decide what type(NCCC=120,144)
            if (id.Equals("C5"))
            {
                var dataLength = Convert.ToUInt16(data.Substring(4, 5));
                //type = Convert.ToUInt16(data.Substring(4, 5)) == 120 ?
                //     typeof(TokenNCCCRoyaltyC5) : typeof(TokenNCCCInstallmentC5);
                try
                {
                    var txIndicator = Convert.ToChar(data[100]);
                    switch (dataLength)
                    {
                        case 120:
                        case 144:
                            type = (txIndicator == 'E' || txIndicator == 'I')
                                 ? typeof(TokenNCCCInstallmentC5)
                                 : typeof(TokenNCCCRoyaltyC5);
                            break;
                        default:
                            return null;
                    }
                }
                catch (System.Exception)
                {
                    // Ignore
                }
            }
            var token = Expression.Lambda<Func<Token>>(Expression.New(type)).Compile()();
            token.Decode(data);
            return token;
        }

        public static void Init()
        {
            //if (TokenTypes.Count > 0) return; //fixed System.TypeInitializationException error
            //AssemblyUtils.GetAllAssemblies()
            //    .ToList()
            //    .ForEach(
            //        assembly =>
            //            {
            //                assembly.GetTypes()
            //                    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Token)))
            //                    .ToList()
            //                    .ForEach(type => TokenTypes[((Token)Activator.CreateInstance(type)).Id] = type);
            //            });
            //
            try
            {
                var assemblies = AssemblyUtils.GetAllAssemblies().ToList();

                foreach (var assembly in assemblies)
                {
                    var types = assembly.GetTypes()
                               .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Token)))
                               .ToList();
                    foreach (var type in types)
                    {
                        TokenTypes[((Token)Activator.CreateInstance(type)).Id] = type;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string Encode()
        {
            var data = this.DataEncode();
            return EyeCatcher + " " + this.Id + data.Length.ToString().PadLeft(5, '0') + " " + data;
        }

        public Token Decode(string data)
        {
            //Console.WriteLine("decode [{0}]", data);
            // This class logs through NLog directly (not the adapter), so mask here: tokens such as S8 carry the PAN.
            logger.Debug("Token Decode Data [{0}]", Com.Cybersoft4u.Utils.CardDataMasker.Mask(data));
            this.Id = data.Substring(2, 2);
            data = data.Substring(10);
            return this.DataDecode(data);
        }

        public abstract string DataEncode();

        public abstract Token DataDecode(string data);
    }
}

using System.Linq;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.TM.ISO8583.Category.CardType
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;

    #endregion

    public abstract class CardType
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        public static readonly DefaultCardType Default = new DefaultCardType();

        private static readonly Dictionary<Type, CardType> TypeMaps = new Dictionary<Type, CardType>();

        private static readonly Dictionary<string, CardType> IdMaps = new Dictionary<string, CardType>();

        static CardType()
        {
            Init();
        }

        public static TCardType Get<TCardType>() where TCardType : CardType
        {
            return (TCardType)TypeMaps[typeof(TCardType)];
        }

        public static CardType Get(string id)
        {
            CardType rtn;
            return !IdMaps.TryGetValue(id, out rtn) ? null : rtn;
        }

        public abstract string Id();

        public abstract string Name();

        private static void Init()
        {
            //if (TypeMaps.Count > 0) return;   //fixed System.TypeInitializationException error
            //AssemblyUtils.GetAllAssemblies()
            //.ToList()
            //.ForEach(
            //    assembly =>
            //        {
            //            assembly.GetTypes()
            //                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CardType)))
            //                .ToList()
            //                .ForEach(
            //                    type =>
            //                        {
            //                            var instance = (CardType)Activator.CreateInstance(type);
            //                            TypeMaps[type] = instance;
            //                            IdMaps[instance.Id()] = instance;
            //                        });
            //        });
            //
            try
            {
                var assemblies = AssemblyUtils.GetAllAssemblies().ToList();
                foreach (var assembly in assemblies)
                {
                    if (!assembly.GetName().Name.Equals("TM"))
                    {
                        continue;
                    }
                    var types = assembly.GetTypes()
                               .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CardType)))
                               .ToList();
                    foreach (var type in types)
                    {
                        CardType instance = (CardType)Activator.CreateInstance(type);
                        TypeMaps[type] = instance;
                        IdMaps[instance.Id()] = instance;
                    }
                }
            }
            catch (Exception ex)
            {
                // ignored
                Log.Error($"CardType Init Error: {ex.Message}");
            }
        }
    }
}

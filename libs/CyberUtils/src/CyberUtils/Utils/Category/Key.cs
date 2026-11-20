namespace Com.Cybersoft4u.CyberUtils.Utils.Category
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    using Com.Cybersoft4u.CyberUtils.Exception;

    #endregion

    public abstract class Key : Category
    {
        protected static ConcurrentDictionary<Type, Key> Keys = new ConcurrentDictionary<Type, Key>();

        protected Key()
        {
        }

        public static T Get<T>() where T : Key
        {
            Key rtn;
            var type = typeof(T);

            if (Keys.TryGetValue(type, out rtn))
            {
                return (T)rtn;
            }

            try
            {
                Monitor.Enter(Keys);
                if (Keys.TryGetValue(type, out rtn))
                {
                    Monitor.Exit(Keys);
                    return (T)rtn;
                }

                var key = (Key)Activator.CreateInstance(type);
                Keys.TryAdd(type, key);
                Monitor.Exit(Keys);
                return (T)key;
            }
            catch (Exception)
            {
                throw new InvalidTypeException();
            }
        }

        public string Name()
        {
            return this.GetType().Name;
        }
    }
}

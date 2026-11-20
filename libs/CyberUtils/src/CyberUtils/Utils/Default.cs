namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System.Collections.Concurrent;

    #endregion

    public class Default
    {
        private static readonly ConcurrentDictionary<string, string> Defaults = new ConcurrentDictionary<string, string>();

        public static string Val(string key)
        {
            string rtn;
            return Defaults.TryGetValue(key, out rtn) ? rtn : null;
        }

        public static void Val(string key, string value)
        {
            Defaults[key] = value;
        }
    }
}

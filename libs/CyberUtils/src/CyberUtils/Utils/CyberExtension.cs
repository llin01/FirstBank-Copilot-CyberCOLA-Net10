namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;

    #endregion

    public static class CyberExtension
    {
        public static string IndexSubstring(this string s, int start, int end)
        {
            return s.Substring(start, end - start);
        }

        public static string GetIndexSubstring(this string s, int start, int end)
        {
            try
            {
                return IndexSubstring(s, start, end);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetIndexSubstring(this string s, int start, int end, string defaultString)
        {
            try
            {
                return IndexSubstring(s, start, end);
            }
            catch (Exception)
            {
                return defaultString;
            }
        }

        public static string GetWithDefault(this string s, string defaultString)
        {
            try
            {
                return s ?? defaultString;
            }
            catch (Exception)
            {
                return defaultString;
            }
        }
    }
}

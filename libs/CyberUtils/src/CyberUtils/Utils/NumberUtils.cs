namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;
    using System.Text.RegularExpressions;

    using Com.Cybersoft4u.CyberUtils.Exception;

    #endregion

    public static class NumberUtils
    {
        public static bool IsDigits(string val)
        {
            return Regex.IsMatch(val, @"^\d+$");
        }

        public static void CheckDigits(string val)
        {
            if (!IsDigits(val))
            {
                throw new InvalidTypeException("Not digits");
            }
        }

        public static bool IsInteger(string val)
        {
            try
            {
                int test;
                int.TryParse(val, out test);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsLong(string val)
        {
            try
            {
                long test;
                long.TryParse(val, out test);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

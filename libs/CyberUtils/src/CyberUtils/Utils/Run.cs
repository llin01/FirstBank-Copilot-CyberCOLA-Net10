namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;

    #endregion

    public class Run
    {
        public delegate void Function();

        public delegate string StringReturnFunction();

        public static void IgnoreException(Function function)
        {
            try
            {
                function();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static string IgnoreException(StringReturnFunction function)
        {
            try
            {
                return function();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void WhenTrue(bool fact, Function function)
        {
            if (fact)
            {
                function();
            }
        }

        public static void WhenFalse(bool fact, Function function)
        {
            if (!fact)
            {
                function();
            }
        }
    }
}

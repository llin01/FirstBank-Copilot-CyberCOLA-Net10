namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;
    using System.Threading;

    #endregion

    public class TimeUtils
    {
        public static long CurrentSecond()
        {
            return Convert.ToInt64(Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds));
        }

        public static long CurrentMillisecond()
        {
            return Convert.ToInt64(Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds));
        }

        public static string CurrentIso8583CCYY()
        {
            return DateTime.Now.ToString("yyyy");
        }

        public static string CurrentIso8583Date()
        {
            return DateTime.Now.ToString("MMdd");
        }

        public static string CurrentIso8583Time()
        {
            return DateTime.Now.ToString("HHmmss");
        }
        public static string CurrentIso8583UtcDatetime()
        {
            return DateTime.Now.ToUniversalTime().ToString("MMddHHmmss");
        }
        public static string CurrentIso8583Datetime()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }

        public static string CurrentYddd()
        {
            var now = DateTime.Now;
            return now.ToString("yy")[1] + now.DayOfYear.ToString().PadLeft(3, '0');
        }

        /// <summary>
        /// CCYYMMddHHmmss
        /// </summary>
        /// <returns></returns>
        public static string CurrentIso8583CCYYDatetime()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public static void Sleep(int millisecond)
        {
            Thread.Sleep(millisecond);
        }

        public static string NextDate(string yyyyMMdd)
        {
            return
                DateTime.Parse(yyyyMMdd.Substring(0, 4) + "-" + yyyyMMdd.Substring(4, 2) + "-" + yyyyMMdd.Substring(6))
                    .AddDays(1)
                    .ToString("MMdd");
        }
    }
}

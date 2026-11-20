namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System;
    using System.Security.Cryptography;
    using System.Threading;

    #endregion

    public class SerialGenerator
    {
        private static int randomSeed = Environment.TickCount;

        private static int stanNumber = Convert.ToInt32($"{DateTime.Now:ffff}");

        private static long stanNumberV2003 = Convert.ToInt32($"{DateTime.Now:fffff}");

        private static long hexKeyCounter = Convert.ToInt32($"{DateTime.Now:fffff}");

        //public static string Stan()
        //{
        //    if (stanNumber > 899998)
        //    {
        //        stanNumber = 0;
        //    }
        //
        //    return Convert.ToString(Interlocked.Increment(ref stanNumber)).PadLeft(6, '0');
        //}

        public static string Stan()
        {
            //ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref randomSeed)));
            //return Convert.ToString(random.Value.Next(1, 899999)).PadLeft(6, '0');
            int randomNumber = GetRandomNumber(900000);
            if (randomNumber == 0) randomNumber = 1;
            return (Convert.ToString(randomNumber).PadLeft(6, '0'));
        }

        public static string Stan12()
        {
            if (stanNumberV2003 > 999999999998)
            {
                stanNumberV2003 = 0;
            }

            return Convert.ToString(Interlocked.Increment(ref stanNumberV2003)).PadLeft(12, '0');
        }
        public static long HexKeyCounter()
        {
            if (hexKeyCounter > 999999999998)
            {
                hexKeyCounter = 0;
            }

            return Interlocked.Increment(ref hexKeyCounter);
        }
        public static int GetRandomNumber(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomNumber = new byte[4];
                rng.GetBytes(randomNumber);

                int result = BitConverter.ToInt32(randomNumber, 0) % maxExclusive;
                result = result < 0 ? -result : result; // Ensure non-negative
                if (result >= maxExclusive) result = 0; // Ensure less than maxExclusive
                return result;
            }
        }
    }
}

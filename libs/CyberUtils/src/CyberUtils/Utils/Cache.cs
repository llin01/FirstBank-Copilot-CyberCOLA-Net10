namespace Com.Cybersoft4u.CyberUtils.Utils
{
    public class Cache
    {
        private static readonly object Locker = new object();

        private static Cache instanceCache;

        public static Cache Instance()
        {
            if (instanceCache == null)
            {
                CreateInstance();
            }

            return instanceCache;
        }

        private static void CreateInstance()
        {
            lock (Locker)
            {
                if (instanceCache == null)
                {
                    instanceCache = new Cache();
                }
            }
        }
    }
}

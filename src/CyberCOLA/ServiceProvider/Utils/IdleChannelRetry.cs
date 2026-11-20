namespace Com.Cybersoft4u.ServiceProvider.Utils
{
    #region DEPENDENCY

    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Lets a COLA service hold a request briefly when every channel to the Auth is busy, instead of answering
    /// RC 96 at once. The retry is scheduled on a timer so no thread-pool thread blocks while waiting: the Auth
    /// responses that free channels also run on the thread pool, and blocking it starved them.
    /// </summary>
    public static class IdleChannelRetry
    {
        // Used when "cola-service-idle-channel-wait-ms" is not configured.
        public const int DefaultMaxWaitMilliseconds = 1000;

        private const int RetryDelayMilliseconds = 20;

        // Keyed by request instance; an entry goes away with its request, so nothing has to be removed.
        private static readonly ConditionalWeakTable<object, StrongBox<long>> WaitStarts = new ConditionalWeakTable<object, StrongBox<long>>();

        /// <summary>
        /// Schedules the request to be queued again shortly.
        /// </summary>
        /// <returns>true when a retry was scheduled; false once the request has waited maxWaitMilliseconds.</returns>
        public static bool TrySchedule<T>(T item, Action<T> requeue, int maxWaitMilliseconds, out long waitedMilliseconds) where T : class
        {
            var now = Environment.TickCount64;
            waitedMilliseconds = now - WaitStarts.GetValue(item, _ => new StrongBox<long>(now)).Value;
            if (waitedMilliseconds >= maxWaitMilliseconds)
            {
                return false;
            }

            Task.Delay(RetryDelayMilliseconds).ContinueWith(_ => requeue(item));
            return true;
        }

        /// <summary>
        /// True when the request is being dispatched again after waiting for a channel; callers use it to skip
        /// per-attempt logging, since one request can be retried dozens of times.
        /// </summary>
        public static bool IsRetry(object item)
        {
            return WaitStarts.TryGetValue(item, out _);
        }
    }
}

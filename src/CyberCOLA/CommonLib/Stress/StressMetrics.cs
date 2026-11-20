using System.Threading;

namespace Com.Cybersoft4u.CommonLib.Stress
{
    /// <summary>
    /// Thread-safe high-performance telemetry metrics accumulator.
    /// Utilizes hardware-level CPU Interlocked operations for zero lock contention.
    /// </summary>
    public class StressMetrics
    {
        private long _activeConnections;
        private long _successCount;
        private long _errorCount;

        /// <summary>
        /// Gets the current number of active client connections.
        /// </summary>
        public int ActiveConnections => (int)Interlocked.Read(ref _activeConnections);

        /// <summary>
        /// Gets the total number of successfully processed transactions.
        /// </summary>
        public long SuccessCount => Interlocked.Read(ref _successCount);

        /// <summary>
        /// Gets the total number of network or protocol errors.
        /// </summary>
        public long ErrorCount => Interlocked.Read(ref _errorCount);

        public void IncrementConnections()
        {
            Interlocked.Increment(ref _activeConnections);
        }

        public void DecrementConnections()
        {
            Interlocked.Decrement(ref _activeConnections);
        }

        public void IncrementSuccess()
        {
            Interlocked.Increment(ref _successCount);
        }

        public void IncrementErrors()
        {
            Interlocked.Increment(ref _errorCount);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _activeConnections, 0);
            Interlocked.Exchange(ref _successCount, 0);
            Interlocked.Exchange(ref _errorCount, 0);
        }
    }
}

using System.Diagnostics;
using System.Threading;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Per-transaction timing for the Auth services: how long one request spent in stored procedure calls, in
    /// SqlCommandBuilder.DeriveParameters round trips and in the HSM. Log timestamps cannot answer this because the
    /// logger stamps lines when its background thread writes them, in batches.
    /// An Auth request runs synchronously on one worker thread from ProcessTransactionFlow to the response, so a
    /// thread-static accumulator attributes the time to the transaction that spent it. Outside a Begin/End scope
    /// (other services, tools) every Stop call is a no-op.
    /// </summary>
    public static class AuthTiming
    {
        [ThreadStatic]
        private static Scope? _current;

        // Set by MarkDequeued on the pool thread that picked up a read, consumed by the next Begin on that thread.
        [ThreadStatic]
        private static long _queuedAt;
        [ThreadStatic]
        private static long _dequeuedAt;

        public sealed class Scope
        {
            internal readonly long StartTicks = Stopwatch.GetTimestamp();
            // Time the read waited for a thread-pool thread, and the time from that thread starting to Begin
            // (frame split, logging and decoding). Only known when the read came through MarkDequeued.
            internal bool HasQueue;
            internal long QueueTicks;
            internal long PreTicks;
            internal int PoolThreads;
            internal long PoolPending;
            public long DbExecTicks;
            public int DbExecCalls;
            public long DbDeriveTicks;
            public int DbDeriveCalls;
            public long HsmTicks;
            public int HsmCalls;

            /// <summary>One line for the Auth log: total, DB execute, DB parameter lookup, HSM and the rest.</summary>
            public string Summary(string mti)
            {
                long total = Stopwatch.GetTimestamp() - StartTicks;
                long other = total - DbExecTicks - DbDeriveTicks - HsmTicks;
                var line = $"AuthTiming MTI[{mti}] Total[{Ms(total)}ms] DbExec[{Ms(DbExecTicks)}ms x{DbExecCalls}] " +
                           $"DbDerive[{Ms(DbDeriveTicks)}ms x{DbDeriveCalls}] Hsm[{Ms(HsmTicks)}ms x{HsmCalls}] Other[{Ms(other)}ms]";
                // Appended after the existing fields so parsers of the original line keep working.
                return HasQueue
                    ? line + $" Queue[{Ms(QueueTicks)}ms] Pre[{Ms(PreTicks)}ms] Pool[{PoolThreads}/{PoolPending}]"
                    : line;
            }
        }

        /// <summary>
        /// Called first on the thread-pool thread that runs a read handed over at <paramref name="queuedAt"/>, so the
        /// next Begin on this thread can report how long the read queued and how long it took to reach Begin.
        /// </summary>
        public static void MarkDequeued(long queuedAt)
        {
            _queuedAt = queuedAt;
            _dequeuedAt = Stopwatch.GetTimestamp();
        }

        /// <summary>Drops an unconsumed mark so a later Begin on this pool thread cannot pick it up.</summary>
        public static void ClearDequeued() => _queuedAt = _dequeuedAt = 0;

        public static Scope Begin()
        {
            var scope = new Scope();
            if (_dequeuedAt != 0)
            {
                scope.HasQueue = true;
                scope.QueueTicks = _dequeuedAt - _queuedAt;
                scope.PreTicks = scope.StartTicks - _dequeuedAt;
                // Pool size and backlog when the SP work starts: low thread counts with a backlog mean starvation.
                scope.PoolThreads = ThreadPool.ThreadCount;
                scope.PoolPending = ThreadPool.PendingWorkItemCount;
                ClearDequeued();
            }
            return _current = scope;
        }

        public static void End() => _current = null;

        public static long Start() => Stopwatch.GetTimestamp();

        public static void StopDbExec(long start)
        {
            var scope = _current;
            if (scope == null) return;
            scope.DbExecTicks += Stopwatch.GetTimestamp() - start;
            scope.DbExecCalls++;
        }

        public static void StopDbDerive(long start)
        {
            var scope = _current;
            if (scope == null) return;
            scope.DbDeriveTicks += Stopwatch.GetTimestamp() - start;
            scope.DbDeriveCalls++;
        }

        public static void StopHsm(long start)
        {
            var scope = _current;
            if (scope == null) return;
            scope.HsmTicks += Stopwatch.GetTimestamp() - start;
            scope.HsmCalls++;
        }

        private static long Ms(long ticks) => ticks * 1000 / Stopwatch.Frequency;
    }
}

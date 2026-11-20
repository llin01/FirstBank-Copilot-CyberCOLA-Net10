using System.Runtime.InteropServices;

namespace Com.Cybersoft4u.Yetty.Utils.Concurrent
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Management;
    using System.Threading;

    using Com.Cybersoft4u.Yetty.Exception;

    #endregion

    /// <summary>
    /// Provides a custom thread pool implementation for managing and executing concurrent tasks with configurable thread limits.
    /// </summary>
    public class ThreadPool
    {
        public static readonly ThreadPool Default = new ThreadPool();

        public static int CpuCounts;

        private const int TaskTakeWaitTime = 100;

        private readonly BlockingCollection<object> queuedTasks =
            new BlockingCollection<object>();

        private readonly ConcurrentDictionary<ThreadWorker, bool> idleWorkers = new ConcurrentDictionary<ThreadWorker, bool>();

        private readonly ConcurrentDictionary<ThreadWorker, bool> activeWorkers = new ConcurrentDictionary<ThreadWorker, bool>();

        private readonly ConcurrentDictionary<ThreadWorker, bool> totalWorkers = new ConcurrentDictionary<ThreadWorker, bool>();

        private readonly Thread invoker;

        private readonly WorkerFactory workerFactory;

        private bool continued = true;

        private bool stop;

        static ThreadPool()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
                {
                    CpuCounts += int.Parse(item["NumberOfCores"].ToString());
                }

                if (CpuCounts == 0)
                {
                    CpuCounts = 1;
                }
            }
            else
            {
                CpuCounts = Environment.ProcessorCount;

            }
        }

        /// <summary>
        /// Initializes a new instance of the ThreadPool class with specified thread pool parameters.
        /// </summary>
        /// <param name="maxThreads">Maximum number of threads (defaults to CPU core count if 0 or less).</param>
        /// <param name="minThreads">Minimum number of threads (defaults to 1 if 0 or less).</param>
        /// <param name="timeToLiveMs">Thread time-to-live in milliseconds before idle threads are terminated.</param>
        /// <param name="workerFactory">Optional custom worker factory (defaults to DefaultWorkerFactory if null).</param>
        public ThreadPool(
            int maxThreads = 0,
            int minThreads = 0,
            int timeToLiveMs = 60000,
            WorkerFactory workerFactory = null)
        {
            if (minThreads > maxThreads)
            {
                throw new InvalidValueException("minThreads > maxThreads");
            }

            this.MaxThreads = maxThreads <= 0 ? CpuCounts : maxThreads;
            this.MinThreads = minThreads <= 0 ? 1 : minThreads;
            this.TimeToLiveMs = timeToLiveMs;

            this.workerFactory = workerFactory ?? new DefaultWorkerFactory(this, timeToLiveMs);

            this.InitThreadPool();

            this.invoker = new Thread(this.InnerRun);
            this.invoker.Start();
        }

        public delegate object ThreadTask();

        public int MaxThreads { get; private set; }

        public int MinThreads { get; private set; }

        public int TimeToLiveMs { get; private set; }

        public int CurrentThreads
        {
            get
            {
                return this.totalWorkers.Count;
            }
        }

        public void Shutdown(bool gracefully = false)
        {
            if (!this.continued)
            {
                throw new Com.Cybersoft4u.Yetty.Exception.InvalidOperationException("ThreadPool has been shutdowned.");
            }

            this.continued = false;

            if (!gracefully)
            {
                this.queuedTasks.Dispose();

                this.idleWorkers
                    .Keys
                    .ToList()
                    .ForEach(worker => worker.Destroy());
                this.idleWorkers.Clear();

                this.activeWorkers
                    .Keys
                    .ToList()
                    .ForEach(worker => worker.Destroy());
                this.activeWorkers.Clear();

                this.stop = true;
                return;
            }

            while (this.queuedTasks.Count > 0 || this.activeWorkers.Count > 0)
            {
                Thread.Sleep(10);
            }

            this.stop = true;
        }

        public TaskFuture Execute(ThreadTask task)
        {
            if (!this.continued)
            {
                throw new Com.Cybersoft4u.Yetty.Exception.InvalidOperationException("ThreadPool has been shutdowned.");
            }

            var future = new TaskFuture();

            future.SetupTask(() => task());
            while (!this.queuedTasks.TryAdd(future))
            {
            }

            return future;
        }

        public void RecycleWorker(ThreadWorker threadWorker)
        {
            bool tmp;
            this.activeWorkers.TryRemove(threadWorker, out tmp);
            this.idleWorkers.TryAdd(threadWorker, true);
        }

        public void RemoveWorker(ThreadWorker threadWorker)
        {
            bool tmp;
            this.activeWorkers.TryRemove(threadWorker, out tmp);
            this.idleWorkers.TryRemove(threadWorker, out tmp);
            this.totalWorkers.TryRemove(threadWorker, out tmp);
        }

        private void AddNewWorker(ThreadWorker threadWorker)
        {
            this.idleWorkers.TryAdd(threadWorker, true);
            this.totalWorkers.TryAdd(threadWorker, true);
        }

        private void MoveIdleWorkerToActive(ThreadWorker threadWorker)
        {
            bool tmp;
            this.idleWorkers.TryRemove(threadWorker, out tmp);
            this.activeWorkers.TryAdd(threadWorker, true);
        }

        private void InnerRun()
        {
            while (!this.stop)
            {
                object future;
                if (!this.queuedTasks.TryTake(out future, TaskTakeWaitTime))
                {
                    continue;
                }

                try
                {
                    var worker = this.idleWorkers.First().Key;
                    this.MoveIdleWorkerToActive(worker);
                    ((TaskFuture)future).SetupWorker(worker);
                    ((TaskFuture)future).Run();
                    continue;
                }
                catch (Exception)
                {
                    // ignored
                }

                while (!this.queuedTasks.TryAdd(future))
                {
                }

                if (this.totalWorkers.Count >= this.MaxThreads)
                {
                    continue;
                }

                var newWorker = this.workerFactory.NewWorker();
                newWorker.Start();
                this.AddNewWorker(newWorker);
            }
        }

        private void InitThreadPool()
        {
            for (var i = 0; i < this.MinThreads; i++)
            {
                var worker = this.workerFactory.NewWorker();
                worker.TimeToLiveMs = 0;
                worker.Start();
                this.idleWorkers.TryAdd(worker, true);
                this.totalWorkers.TryAdd(worker, true);
            }
        }
    }
}

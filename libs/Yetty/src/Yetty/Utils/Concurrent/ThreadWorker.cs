namespace Com.Cybersoft4u.Yetty.Utils.Concurrent
{
    #region DEPENDENCY

    using System;
    using System.Threading;

    using Com.Cybersoft4u.Utils;

    #endregion

    /// <summary>
    /// ThreadWorker , did not throw exception, only log.
    /// </summary>
    public class ThreadWorker
    {
        protected internal int TimeToLiveMs;

        private static readonly Logger Logger = LoggerAdapter.GetLogger(typeof(ThreadWorker));

        private readonly Concurrent.ThreadPool parent;

        private readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        private Thread thread;

        private volatile bool stop;

        private volatile WorkerTask task;

        /// <summary>
        /// <code>timeToLiveMs</code> = 0 for infinity run
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="timeToLiveMs"></param>
        public ThreadWorker(Concurrent.ThreadPool parent = null, int timeToLiveMs = 60000)
        {
            this.parent = parent ?? Concurrent.ThreadPool.Default;
            this.TimeToLiveMs = parent == null ? -1 : timeToLiveMs;
        }

        public delegate void WorkerTask();

        public virtual void Start()
        {
            this.resetEvent.Reset();
            this.thread = new Thread(this.InnerLoop);
            this.thread.Start();
        }

        public virtual bool Execute(WorkerTask task)
        {
            if (this.task != null)
            {
                return false;
            }

            this.task = task;
            this.resetEvent.Set();
            return true;
        }

        public virtual void Destroy(bool gracefully = false)
        {
            this.stop = true;
            if (!this.thread.IsAlive)
            {
                return;
            }

            if (gracefully)
            {
                return;
            }

            try
            {
                this.thread.Interrupt();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void InnerLoop()
        {
            while (!this.stop)
            {
                try
                {
                    if (this.TimeToLiveMs <= 0)
                    {
                        this.resetEvent.WaitOne();
                    }
                    else
                    {
                        if (!this.resetEvent.WaitOne(this.TimeToLiveMs))
                        {
                            this.stop = true;
                            this.parent.RemoveWorker(this);
                            this.resetEvent.Set();
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    this.resetEvent.Set();
                }

                this.resetEvent.Reset();
                if (this.task != null)
                {
                    try
                    {
                        this.task();
                    }
                    catch (Exception exception)
                    {
                        Logger.Error("ThreadWorker Exception Throwed.", exception);
                    }
                }

                if (this.TimeToLiveMs < 0)
                {
                    this.parent.RemoveWorker(this);
                    break;
                }

                this.task = null;
                this.parent.RecycleWorker(this);
            }
        }
    }
}

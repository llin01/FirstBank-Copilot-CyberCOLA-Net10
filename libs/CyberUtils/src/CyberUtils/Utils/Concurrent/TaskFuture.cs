namespace Com.Cybersoft4u.CyberUtils.Utils.Concurrent
{
    #region DEPENDENCY

    #endregion

    public class TaskFuture : Future, Recyclable
    {
        private FutureTask task;

        private bool cancel;

        private bool done;

        private object result;

        private volatile RecyclableManualResetEvent recyclableManualResetEvent = new RecyclableManualResetEvent();

        private volatile ThreadWorker threadWorker;

        public delegate object FutureTask();

        public bool Cancel()
        {
            if (this.cancel)
            {
                return false;
            }

            this.cancel = true;
            this.threadWorker.Destroy();
            this.recyclableManualResetEvent.Set();
            return true;
        }

        public object Get()
        {
            this.recyclableManualResetEvent.Wait();
            return this.result;
        }

        /// <summary>
        /// <code>Not Support Now</code>
        /// </summary>
        /// <param name="millisecond"></param>
        /// <returns></returns>
        public object Get(long millisecond)
        {
            this.recyclableManualResetEvent.Wait((int)millisecond);
            return this.result;
        }

        public bool Canceled()
        {
            return this.cancel;
        }

        public bool Done()
        {
            return this.done;
        }

        public void Recycle()
        {
            this.cancel = false;
            this.done = false;
            this.result = null;
            this.recyclableManualResetEvent.Reset();
            this.threadWorker = null;
        }

        internal TaskFuture SetupTask(FutureTask task)
        {
            this.task = task;
            this.recyclableManualResetEvent.Reset();

            return this;
        }

        internal TaskFuture SetupWorker(ThreadWorker threadWorker)
        {
            this.threadWorker = threadWorker;
            return this;
        }

        internal TaskFuture Run()
        {
            this.threadWorker.Execute(
                () =>
                    {
                        try
                        {
                            this.result = this.task();
                        }
                        finally
                        {
                            this.done = true;
                            this.recyclableManualResetEvent.Set();
                        }
                    });

            return this;
        }
    }
}

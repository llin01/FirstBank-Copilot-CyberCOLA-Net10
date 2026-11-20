namespace Com.Cybersoft4u.Yetty.Utils.Concurrent
{
    public class DefaultWorkerFactory : WorkerFactory
    {
        private readonly ThreadPool threadPool;

        private readonly int timeToLiveMs;

        public DefaultWorkerFactory(ThreadPool threadPool, int timeToLiveMs = 60000)
        {
            this.threadPool = threadPool;
            this.timeToLiveMs = timeToLiveMs;
        }

        public ThreadWorker NewWorker()
        {
            return new ThreadWorker(this.threadPool, this.timeToLiveMs);
        }
    }
}

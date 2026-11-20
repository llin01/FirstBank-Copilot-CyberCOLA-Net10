namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Threading;

    #endregion

    public class AsyncFuture
    {
        private readonly ManualResetEvent Event = new ManualResetEvent(false);

        private readonly IChannel channel;

        public AsyncFuture(IChannel channel)
        {
            this.channel = channel;
            this.Reset();
        }

        public AsyncFuture Sync()
        {
            this.Event.WaitOne();
            return this;
        }

        public IChannel Channel()
        {
            return this.channel;
        }

        protected internal AsyncFuture Set()
        {
            this.Event.Set();
            return this;
        }

        protected internal AsyncFuture Reset()
        {
            this.Event.Reset();
            return this;
        }
    }
}

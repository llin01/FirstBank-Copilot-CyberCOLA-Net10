namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System;
    using System.Threading;

    #endregion

    public abstract class EntryPoint
    {
        private readonly ManualResetEvent loop = new ManualResetEvent(false);

        protected EntryPoint()
        {
            this.Terminated = false;
        }

        public bool Terminated { get; protected set; }

        public EntryPoint Start()
        {
            this.loop.Reset();
            this.StartTask();
            return this;
        }

        public EntryPoint Stop()
        {
            this.Terminated = true;
            this.StopTask();
            this.loop.Set();
            return this;
        }

        public void CommandTrap()
        {
            while (!this.Terminated)
            {
                this.CommandTask(Console.ReadLine());
            }
        }

        public EntryPoint Sync()
        {
            this.loop.WaitOne();
            return this;
        }

        protected abstract void StartTask();

        protected abstract void StopTask();

        protected abstract void CommandTask(string command);
    }
}

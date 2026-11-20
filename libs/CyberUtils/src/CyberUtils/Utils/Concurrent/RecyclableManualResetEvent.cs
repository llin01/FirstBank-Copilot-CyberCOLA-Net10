namespace Com.Cybersoft4u.CyberUtils.Utils.Concurrent
{
    using System.Threading;

    public class RecyclableManualResetEvent : Recyclable
    {
        private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public void Reset()
        {
            this.manualResetEvent.Reset();
        }

        public void Set()
        {
            this.manualResetEvent.Set();
        }

        public void Wait()
        {
            this.manualResetEvent.WaitOne();
        }

        public void Wait(int milliseconds)
        {
            this.manualResetEvent.WaitOne(milliseconds);
        }

        public void Recycle()
        {
            this.Reset();
        }
    }
}

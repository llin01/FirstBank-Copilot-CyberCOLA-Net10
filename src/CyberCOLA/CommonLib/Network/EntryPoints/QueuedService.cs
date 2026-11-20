// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QueuedService.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the QueuedService type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.CommonLib.Network.EntryPoints
{
    #region DEPENDENCY
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.Utils;
    using Com.Cybersoft4u.Yetty.Channel;


    #endregion

    public abstract class QueuedService<T> : EntryPoint
    {
        protected BlockingCollection<T> Queue;

        private Logger logger;

        protected QueuedService()
        {
            this.Terminated = false;
            this.QueueWaitTime = 1000;
            this.Queue = new BlockingCollection<T>();
        }

        public int Count
        {
            get
            {
                return this.Queue.Count;
            }
        }

        protected Logger Logger
        {
            get
            {
                return this.logger ?? (this.logger = LoggerAdapter.GetLogger(this.GetType().FullName));
            }

            set
            {
                this.logger = value;
            }
        }

        protected int QueueWaitTime { get; set; }

        public void Dispose()
        {
        }

        public void Put(T item)
        {
            if (this.Terminated)
            {
                throw new InvalidOperationException("EntryPoint Terminated.");
            }

            this.Queue.Add(item);
        }

        protected override void StartTask()
        {
            this.PreStartTask();
            ThreadPool.QueueUserWorkItem(s => this.WorkerTask());
        }

        protected override void StopTask()
        {
            this.Terminated = true;
            this.Dispose();
        }

        /// <summary>
        ///     Get item form queue and dispatch it.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>false for put item back to queue, and true means success.</returns>
        protected abstract bool DispatchTask(T item);

        protected virtual void WorkerTask()
        {
            while (true)
            {
                T item;
                if (!this.Queue.TryTake(out item, this.QueueWaitTime))
                {
                    break;
                }

                ThreadPool.QueueUserWorkItem(
                    s => Run.IgnoreException(
                        () =>
                            {
                                if (!this.DispatchTask(item))
                                {
                                    this.Queue.Add(item);
                                }
                            }));
            }

            if (!this.Terminated)
            {
                ThreadPool.QueueUserWorkItem(s => this.WorkerTask());
            }
        }

        protected abstract void PreStartTask();

        protected override void CommandTask(string command)
        {
            throw new NotImplementedException();
        }
    }
}

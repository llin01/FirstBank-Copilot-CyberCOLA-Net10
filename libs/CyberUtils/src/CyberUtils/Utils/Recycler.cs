namespace Com.Cybersoft4u.CyberUtils.Utils
{
    using System;
    using System.Collections.Concurrent;

    public class Recycler
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentQueue<Recyclable>> TypeList = new ConcurrentDictionary<Type, ConcurrentQueue<Recyclable>>();

        private static readonly object Monitor = new object();

        protected Recycler()
        {
        }

        public static T Get<T>() where T : Recyclable
        {
            Recyclable rtn;
            GetTypeQueue(typeof(T)).TryDequeue(out rtn);
            return (T)rtn;
        }

        public static Recyclable Get(Recyclable recyclable)
        {
            Recyclable rtn;
            GetTypeQueue(recyclable.GetType()).TryDequeue(out rtn);
            return rtn;
        }

        public static TRecyclable GetOrNew<TRecyclable>() where TRecyclable : Recyclable, new()
        {
            Recyclable rtn;
            if (!GetTypeQueue(typeof(TRecyclable)).TryDequeue(out rtn))
            {
                rtn = new TRecyclable();
            }

            return (TRecyclable)rtn;
        }

        public static void Recycle(Recyclable obj)
        {
            obj.Recycle();
            GetTypeQueue(obj.GetType()).Enqueue(obj);
        }

        private static ConcurrentQueue<Recyclable> GetTypeQueue(Type type)
        {
            ConcurrentQueue<Recyclable> queue;
            if (TypeList.TryGetValue(type, out queue))
            {
                return queue;
            }

            System.Threading.Monitor.Enter(Monitor);
            if (!TypeList.TryGetValue(type, out queue))
            {
                queue = new ConcurrentQueue<Recyclable>();
                TypeList.TryAdd(type, queue);
            }

            System.Threading.Monitor.Exit(Monitor);

            return queue;
        }
    }
}

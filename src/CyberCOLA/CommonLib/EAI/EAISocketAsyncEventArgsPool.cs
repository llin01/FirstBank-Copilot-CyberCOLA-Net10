using System.Net.Sockets;

namespace Com.Cybersoft4u.CommonLib.EAI
{
    /// <summary>
    /// Based on example from http://msdn2.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.socketasynceventargs.aspx
    /// Represents a collection of reusable SocketAsyncEventArgs objects.  
    /// </summary>
    public class EAISocketAsyncEventArgsPool
    {
        /// <summary>
        /// Pool of SocketAsyncEventArgs.
        /// </summary>
        private Stack<SocketAsyncEventArgs> pool = default!;

        /// <summary>
        /// Initializes the object pool to the specified size.
        /// </summary>
        /// <param name="capacity">Maximum number of SocketAsyncEventArgs objects the pool can hold.</param>
        public EAISocketAsyncEventArgsPool(int capacity)
        {
            pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// Removes a SocketAsyncEventArgs instance from the pool.
        /// </summary>
        /// <returns>SocketAsyncEventArgs removed from the pool.</returns>
        public SocketAsyncEventArgs? Pop()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    return pool.Pop();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Add a SocketAsyncEventArg instance to the pool. 
        /// </summary>
        /// <param name="item">SocketAsyncEventArgs instance to add to the pool.</param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException($"Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (pool)
            {
                pool.Push(item);
            }
        }
    }
}

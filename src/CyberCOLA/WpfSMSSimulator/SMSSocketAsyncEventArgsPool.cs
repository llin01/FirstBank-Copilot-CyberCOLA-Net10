using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Com.Cybersoft4u.Simulator.Wpf.WpfSMSSimulator
{
    /// <summary>
    /// Based on example from http://msdn2.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.socketasynceventargs.aspx
    /// Represents a collection of reusable SocketAsyncEventArgs objects.  
    /// </summary>
    public class SMSSocketAsyncEventArgsPool
    {
        /// <summary>
        /// Pool of SocketAsyncEventArgs.
        /// </summary>
        Stack<SocketAsyncEventArgs> pool;

        /// <summary>
        /// Initializes the object pool to the specified size.
        /// </summary>
        /// <param name="capacity">Maximum number of SocketAsyncEventArgs objects the pool can hold.</param>
        internal SMSSocketAsyncEventArgsPool(int capacity)
        {
            pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// Removes a SocketAsyncEventArgs instance from the pool.
        /// </summary>
        /// <returns>SocketAsyncEventArgs removed from the pool.</returns>
        internal SocketAsyncEventArgs Pop()
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
        internal void Push(SocketAsyncEventArgs item)
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

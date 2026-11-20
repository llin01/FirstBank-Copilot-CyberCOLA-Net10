#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Com.Cybersoft4u.ServiceProvider.ClientAuth
{
    /// <summary>
    /// Hands a host response to the thread waiting for it, keyed by the transaction key. Replaces polling a
    /// dictionary with Thread.Sleep: the waiter is woken the moment the response arrives instead of up to one
    /// sleep interval later, and a response nobody waits for any more is dropped instead of kept forever.
    /// The wait still blocks its thread, because the callers (SocketAsyncEventArgs handlers) are synchronous.
    /// </summary>
    public sealed class PendingResponses<T> where T : class
    {
        private sealed class Waiter
        {
            public T? Response;

            public bool Completed;

            // Set under the lock when the waiter stops waiting, so a response racing the timeout is
            // reported as dropped rather than as delivered to no one.
            public bool Abandoned;
        }

        private readonly ConcurrentDictionary<string, Waiter> _pending = new ConcurrentDictionary<string, Waiter>();

        public int Count => _pending.Count;

        /// <summary>
        /// Registers a waiter for <paramref name="key"/>. Call before sending the request, so a response that
        /// arrives before <see cref="Wait"/> starts is not lost. Returns false if the key is already pending.
        /// </summary>
        public bool Register(string key)
        {
            return _pending.TryAdd(key, new Waiter());
        }

        /// <summary>Removes a registered waiter whose request was never sent.</summary>
        public void Cancel(string key)
        {
            if (_pending.TryRemove(key, out var waiter))
            {
                lock (waiter)
                {
                    waiter.Abandoned = true;
                }
            }
        }

        /// <summary>
        /// Delivers <paramref name="response"/> to the waiter for <paramref name="key"/>. Returns false when no
        /// one waits for it (never registered, already answered, or the waiter timed out).
        /// </summary>
        public bool TryComplete(string key, T response)
        {
            // Left in the map for Wait to remove: removing it here made a Wait that had not started yet
            // find no waiter and lose a response that had already arrived.
            if (!_pending.TryGetValue(key, out var waiter))
            {
                return false;
            }

            lock (waiter)
            {
                if (waiter.Completed || waiter.Abandoned)
                {
                    return false;
                }

                waiter.Response = response;
                waiter.Completed = true;
                Monitor.PulseAll(waiter);
            }

            return true;
        }

        /// <summary>
        /// Blocks until the response for a registered <paramref name="key"/> arrives or the timeout passes.
        /// Returns null on timeout or when the key was not registered. The key is no longer pending afterwards.
        /// </summary>
        public T? Wait(string key, int timeoutMilliseconds)
        {
            if (!_pending.TryGetValue(key, out var waiter))
            {
                return null;
            }

            // Monitor rather than an event object: nothing to dispose, so a TryComplete racing a timeout
            // can never signal a disposed handle.
            var deadline = Environment.TickCount64 + timeoutMilliseconds;
            T? response;
            lock (waiter)
            {
                while (!waiter.Completed)
                {
                    var remaining = deadline - Environment.TickCount64;
                    if (remaining <= 0 || !Monitor.Wait(waiter, (int)remaining))
                    {
                        break;
                    }
                }

                waiter.Abandoned = !waiter.Completed;
                response = waiter.Completed ? waiter.Response : null;
            }

            // Remove only this waiter: a key reused by a later request must keep its own registration.
            _pending.TryRemove(new KeyValuePair<string, Waiter>(key, waiter));
            return response;
        }
    }
}

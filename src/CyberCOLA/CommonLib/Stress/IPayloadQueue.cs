using System;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.CommonLib.Stress
{
    /// <summary>
    /// Defines high-performance abstract queue operations for packet payload storage and retrieval.
    /// Supports lock-free memory channels and MSMQ compatibility.
    /// </summary>
    public interface IPayloadQueue
    {
        /// <summary>
        /// Gets the logical path or name of the queue.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Asynchronously sends a binary message payload to the queue.
        /// </summary>
        Task SendAsync(byte[] payload);

        /// <summary>
        /// Asynchronously receives a binary message payload from the queue.
        /// Returns null if the queue is empty or timed out.
        /// </summary>
        Task<byte[]?> ReceiveAsync(CancellationToken ct);

        /// <summary>
        /// Synchronously receives a binary message payload from the queue with zero timeout.
        /// Useful for quick polling or compatibility with blocking legacy models.
        /// </summary>
        byte[]? Receive(TimeSpan timeout);

        /// <summary>
        /// Gets the current number of pending messages in the queue.
        /// </summary>
        int GetCount();

        /// <summary>
        /// Clears all pending message payloads inside the queue.
        /// </summary>
        void Purge();
    }
}

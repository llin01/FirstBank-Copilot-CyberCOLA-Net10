using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Com.Cybersoft4u.CommonLib.Stress
{
    /// <summary>
    /// A high-performance lock-free memory queue utilizing System.Threading.Channels.
    /// Completely bypasses OS-level context switches and filesystem latency.
    /// </summary>
    public class ChannelPayloadQueue : IPayloadQueue
    {
        private readonly Channel<byte[]> _channel;
        private readonly string _path;

        public string Path => _path;

        public ChannelPayloadQueue(string path)
        {
            _path = path;
            var options = new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = false
            };
            _channel = Channel.CreateUnbounded<byte[]>(options);
        }

        public async Task SendAsync(byte[] payload)
        {
            if (payload == null) return;
            await _channel.Writer.WriteAsync(payload);
        }

        public async Task<byte[]?> ReceiveAsync(CancellationToken ct)
        {
            try
            {
                if (await _channel.Reader.WaitToReadAsync(ct))
                {
                    if (_channel.Reader.TryRead(out var payload))
                    {
                        return payload;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, exit gracefully
            }
            return null;
        }

        public byte[]? Receive(TimeSpan timeout)
        {
            if (_channel.Reader.TryRead(out var payload))
            {
                return payload;
            }

            if (timeout == TimeSpan.Zero)
            {
                return null;
            }

            try
            {
                using var cts = new CancellationTokenSource(timeout);
                var waitTask = _channel.Reader.WaitToReadAsync(cts.Token).AsTask();
                if (waitTask.Result)
                {
                    if (_channel.Reader.TryRead(out payload))
                    {
                        return payload;
                    }
                }
            }
            catch
            {
                // Timeout or canceled
            }
            return null;
        }

        public int GetCount()
        {
            return _channel.Reader.Count;
        }

        public void Purge()
        {
            while (_channel.Reader.TryRead(out _))
            {
                // Empty the channel reader cache
            }
        }
    }
}

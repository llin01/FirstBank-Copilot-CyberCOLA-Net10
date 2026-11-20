using System;
using System.Threading;
using System.Threading.Tasks;
using Experimental.System.Messaging;

namespace Com.Cybersoft4u.CommonLib.Stress
{
    /// <summary>
    /// Legacy MSMQ wrapper implementing IPayloadQueue.
    /// Provides strict backward compatibility with CyberCOLA's system routers and MSMQ deployment configurations.
    /// </summary>
    public class MsmqPayloadQueue : IPayloadQueue, IDisposable
    {
        private readonly MessageQueue _messageQueue;
        private readonly string _path;

        public string Path => _path;

        public MsmqPayloadQueue(string queueName)
        {
            _path = queueName;

            string absolutePath = queueName.Contains(@"Private$\", StringComparison.OrdinalIgnoreCase)
                ? queueName
                : $@".\Private$\" + queueName;

            if (MessageQueue.Exists(absolutePath))
            {
                _messageQueue = new MessageQueue(absolutePath);
            }
            else
            {
                _messageQueue = MessageQueue.Create(absolutePath);
            }

            _messageQueue.Formatter = new XmlMessageFormatter(new[] { typeof(byte[]) });
        }

        public Task SendAsync(byte[] payload)
        {
            if (payload == null) return Task.CompletedTask;

            return Task.Run(() =>
            {
                var message = new Message(payload, new XmlMessageFormatter(new[] { typeof(byte[]) }))
                {
                    Label = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}_{Guid.NewGuid().ToString().Substring(0, 8)}"
                };
                _messageQueue.Send(message);
            });
        }

        public Task<byte[]?> ReceiveAsync(CancellationToken ct)
        {
            return Task.Run<byte[]?>(() =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            var message = _messageQueue.Receive(TimeSpan.FromMilliseconds(200));
                            if (message?.Body is byte[] body)
                            {
                                return body;
                            }
                        }
                        catch (MessageQueueException mqex) when (mqex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                        {
                            // Timeout, continue loop to check cancellation
                        }
                    }
                }
                catch
                {
                    // Catch unexpected queue exceptions gracefully
                }
                return null;
            }, ct);
        }

        public byte[]? Receive(TimeSpan timeout)
        {
            try
            {
                var message = _messageQueue.Receive(timeout);
                return message?.Body as byte[];
            }
            catch
            {
                return null;
            }
        }

        public int GetCount()
        {
            int messageCount = 0;
            try
            {
                using var messageEnumerator = _messageQueue.GetMessageEnumerator2();
                while (messageEnumerator.MoveNext())
                {
                    messageCount++;
                }
            }
            catch
            {
                // Return 0 if queue is inaccessible
            }
            return messageCount;
        }

        public void Purge()
        {
            try
            {
                _messageQueue.Purge();
            }
            catch
            {
                // Graceful fail
            }
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

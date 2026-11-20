using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Com.Cybersoft4u.CommonLib.DBUtils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.ServiceProvider.ServiceRouter
{
    /// <summary>
    /// Thread-safe, lock-free log queue using System.Threading.Channels.
    /// Safely buffers and writes ServiceRouter logs to the database asynchronously,
    /// avoiding blocking network event loops during transaction processing.
    /// </summary>
    public static class ServiceRouterLogQueue
    {
        private static readonly Channel<InterchangeMessage> LogChannel = Channel.CreateUnbounded<InterchangeMessage>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });

        private static readonly Logger Logger = LoggerAdapter.GetLogger("LoggerHandler[ServiceRouterLogQueue]");

        static ServiceRouterLogQueue()
        {
            // Start background consumer thread to write logs asynchronously
            Task.Run(async () => await ProcessQueueAsync());
        }

        /// <summary>
        /// Enqueues a message to be written to the database asynchronously.
        /// Returns immediately without blocking the caller.
        /// </summary>
        /// <param name="message">The interchange message to log.</param>
        public static void Enqueue(InterchangeMessage message)
        {
            if (message != null)
            {
                LogChannel.Writer.TryWrite(message);
            }
        }

        private static async Task ProcessQueueAsync()
        {
            var reader = LogChannel.Reader;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var msg))
                {
                    try
                    {
                        var spRetnCode = StoreProcUtility.InsertServiceRouterLog(msg);
                        if (spRetnCode != 0)
                        {
                            Logger.Fatal($"Insert COLA_SERVICE_ROUTER_LOG Error, Return Code = [{spRetnCode}]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal($"ProcessQueueAsync DB Write Exception: {ex.Message}", ex);
                    }
                }
            }
        }
    }
}

using System.Collections.Concurrent;
using Com.Cybersoft4u.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Messages are written by a background worker; a service exiting right after logging its stop sequence lost the
    // tail. Flush (also run on process exit) waits for the worker to write everything queued so far.
    [TestFixture]
    public class LoggerFlushTest
    {
        private sealed class SlowLogger : Logger
        {
            public readonly ConcurrentQueue<string> Written = new();

            public SlowLogger() : base("slow")
            {
            }

            protected override bool LoggerDebugEnabled() => true;
            protected override bool LoggerInfoEnabled() => true;
            protected override bool LoggerWarnEnabled() => true;
            protected override bool LoggerErrorEnabled() => true;
            protected override bool LoggerFatalEnabled() => true;

            protected override void WriteMessage(LogMessage logMessage)
            {
                Thread.Sleep(2);
                Written.Enqueue(logMessage.Message.ToString()!);
            }

            protected override void WriteMessageException(LogMessage logMessage) => WriteMessage(logMessage);

            protected override void WriteMessageTask(LogMessage logMessage) => WriteMessage(logMessage);
        }

        [Test]
        public void Flush_MessagesStillQueued_ReturnsAfterAllAreWritten()
        {
            const int count = 200;
            var logger = new SlowLogger();
            for (var i = 0; i < count; i++)
            {
                logger.Info($"message {i}");
            }

            Assert.That(Logger.Flush(TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(logger.Written, Has.Count.EqualTo(count));
        }

        [Test]
        public void Flush_TimeoutShorterThanBacklog_ReturnsFalse()
        {
            var logger = new SlowLogger();
            for (var i = 0; i < 500; i++)
            {
                logger.Info($"message {i}");
            }

            Assert.That(Logger.Flush(TimeSpan.FromMilliseconds(50)), Is.False);
            Assert.That(Logger.Flush(TimeSpan.FromSeconds(10)), Is.True);
        }
    }
}

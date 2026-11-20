using System.Collections.Concurrent;
using Com.Cybersoft4u.ServiceProvider.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    [TestFixture]
    public class IdleChannelRetryTest
    {
        private const int MaxWait = 300;

        [Test]
        public void TrySchedule_FirstAttempt_RequeuesSameItemLater()
        {
            var item = new object();
            var requeued = new BlockingCollection<object>();

            var scheduled = IdleChannelRetry.TrySchedule(item, requeued.Add, MaxWait, out var waited);

            Assert.That(scheduled, Is.True);
            Assert.That(waited, Is.EqualTo(0));
            Assert.That(requeued.TryTake(out var back, TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(back, Is.SameAs(item));
        }

        [Test]
        public void TrySchedule_AfterMaxWait_ReturnsFalseWithoutRequeue()
        {
            var item = new object();
            var requeued = new BlockingCollection<object>();
            IdleChannelRetry.TrySchedule(item, requeued.Add, MaxWait, out _);
            requeued.TryTake(out _, TimeSpan.FromSeconds(2));

            Thread.Sleep(MaxWait + 50);
            var scheduled = IdleChannelRetry.TrySchedule(item, requeued.Add, MaxWait, out var waited);

            Assert.That(scheduled, Is.False);
            Assert.That(waited, Is.GreaterThanOrEqualTo(MaxWait));
            Assert.That(requeued.TryTake(out _, TimeSpan.FromMilliseconds(200)), Is.False);
        }

        [Test]
        public void IsRetry_OnlyAfterFirstSchedule_ReturnsTrue()
        {
            var item = new object();
            var before = IdleChannelRetry.IsRetry(item);

            IdleChannelRetry.TrySchedule(item, _ => { }, MaxWait, out _);

            Assert.That(before, Is.False);
            Assert.That(IdleChannelRetry.IsRetry(item), Is.True);
        }
    }
}

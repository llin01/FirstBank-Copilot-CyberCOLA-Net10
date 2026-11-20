using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    [TestFixture]
    public class PendingResponsesTest
    {
        private sealed class Response
        {
            public Response(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }

        [Test]
        public void Wait_ResponseArrivesBeforeWaitStarts_ReturnsResponseAtOnce()
        {
            var pending = new PendingResponses<Response>();
            Assert.IsTrue(pending.Register("K1"));
            Assert.IsTrue(pending.TryComplete("K1", new Response("R1")));
            Assert.IsFalse(pending.TryComplete("K1", new Response("duplicate")), "A second response must not replace the first.");

            var watch = Stopwatch.StartNew();
            var response = pending.Wait("K1", 5000);

            Assert.AreEqual("R1", response?.Id);
            Assert.Less(watch.ElapsedMilliseconds, 1000);
            Assert.AreEqual(0, pending.Count);
        }

        [Test]
        public void Wait_ResponseArrivesWhileWaiting_WakesWithoutPollingDelay()
        {
            var pending = new PendingResponses<Response>();
            pending.Register("K1");
            var completer = Task.Run(() =>
            {
                Thread.Sleep(200);
                pending.TryComplete("K1", new Response("R1"));
            });

            var watch = Stopwatch.StartNew();
            var response = pending.Wait("K1", 10000);
            watch.Stop();
            completer.GetAwaiter().GetResult();

            Assert.AreEqual("R1", response?.Id);
            // Woken by the delivery itself: far below the 10 s timeout, with generous slack for slow CI hosts.
            Assert.Less(watch.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void Wait_NoResponse_ReturnsNullAfterTimeoutAndLateResponseIsDropped()
        {
            var pending = new PendingResponses<Response>();
            pending.Register("K1");

            var watch = Stopwatch.StartNew();
            var response = pending.Wait("K1", 300);
            watch.Stop();

            Assert.IsNull(response);
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 250);
            Assert.AreEqual(0, pending.Count, "A timed-out waiter must not stay registered.");
            Assert.IsFalse(pending.TryComplete("K1", new Response("late")), "A late response must not be stored.");
            Assert.AreEqual(0, pending.Count);
        }

        [Test]
        public void Register_KeyAlreadyPending_ReturnsFalse()
        {
            var pending = new PendingResponses<Response>();
            Assert.IsTrue(pending.Register("K1"));
            Assert.IsFalse(pending.Register("K1"));
        }

        [Test]
        public void Wait_KeyNotRegistered_ReturnsNullImmediately()
        {
            var pending = new PendingResponses<Response>();
            var watch = Stopwatch.StartNew();
            Assert.IsNull(pending.Wait("missing", 5000));
            Assert.Less(watch.ElapsedMilliseconds, 1000);
        }

        [Test]
        public void Cancel_RegisteredKey_LaterResponseIsDropped()
        {
            var pending = new PendingResponses<Response>();
            pending.Register("K1");
            pending.Cancel("K1");

            Assert.AreEqual(0, pending.Count);
            Assert.IsFalse(pending.TryComplete("K1", new Response("R1")));
        }

        [Test]
        public void Wait_ManyConcurrentKeys_EachWaiterGetsItsOwnResponse()
        {
            const int count = 500;
            var pending = new PendingResponses<Response>();
            var keys = Enumerable.Range(0, count).Select(i => $"K{i}").ToArray();
            foreach (var key in keys)
            {
                Assert.IsTrue(pending.Register(key));
            }

            // Dedicated threads: 500 blocked waiters on the thread pool would only test its injection rate.
            var results = new string?[count];
            var waiters = Enumerable.Range(0, count)
                .Select(i => new Thread(() => results[i] = pending.Wait(keys[i], 10000)?.Id) { IsBackground = true })
                .ToList();
            waiters.ForEach(t => t.Start());

            Parallel.ForEach(keys.Reverse(), key => Assert.IsTrue(pending.TryComplete(key, new Response($"R-{key}"))));

            Assert.IsTrue(waiters.All(t => t.Join(TimeSpan.FromSeconds(15))), "A waiter was never woken.");
            for (var i = 0; i < count; i++)
            {
                Assert.AreEqual($"R-{keys[i]}", results[i]);
            }

            Assert.AreEqual(0, pending.Count);
        }
    }
}

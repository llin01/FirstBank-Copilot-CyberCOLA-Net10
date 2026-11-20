using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.Yetty.Test
{
    [TestFixture]
    public class TcpChannelFlushTest
    {
        private class EmptyInitializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
            }
        }

        private static IChannel ConnectClient(EndPoint remote)
        {
            var clientChannel = new Bootstrap()
                .ChannelType(typeof(TcpSocketChannel))
                .Handler(new EmptyInitializer())
                .Connect(remote)
                .Sync()
                .Channel();
            Assert.IsTrue(clientChannel.IsConnected());
            return clientChannel;
        }

        // Dedicated background threads: a hung Flush() must neither starve the thread pool
        // nor keep the test host process alive.
        private static Thread StartBackground(ThreadStart body)
        {
            var thread = new Thread(body) { IsBackground = true };
            thread.Start();
            return thread;
        }

        [Test]
        public void WriteAndFlush_ConcurrentWritersInBursts_AllCallsReturn()
        {
            // A caller that loses the wakeup is only stuck for good when no later write
            // completes, so each round ends with a quiet period (all writers must return).
            const int rounds = 300;
            const int writerCount = 8;

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var clientChannel = ConnectClient(listener.LocalEndpoint);
            using var peer = listener.AcceptSocket();
            StartBackground(() =>
            {
                var sink = new byte[64 * 1024];
                try
                {
                    while (peer.Receive(sink) > 0)
                    {
                    }
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            });

            var payload = new byte[64];
            for (var round = 0; round < rounds; round++)
            {
                using var start = new Barrier(writerCount);
                var writers = Enumerable.Range(0, writerCount)
                    .Select(_ => StartBackground(() =>
                    {
                        start.SignalAndWait();
                        clientChannel.WriteAndFlush(ByteBuf.From(payload, 0, payload.Length));
                    }))
                    .ToList();

                var stuck = writers.Count(t => !t.Join(TimeSpan.FromSeconds(5)));
                Assert.AreEqual(0, stuck, $"Round {round}: {stuck} of {writerCount} WriteAndFlush calls never returned.");
            }

            clientChannel.Disconnect();
            listener.Stop();
        }

        [Test]
        public void WriteAndFlush_PeerResetsConnection_CallReturns()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var clientChannel = ConnectClient(listener.LocalEndpoint);
            var peer = listener.AcceptSocket();

            // Linger 0 makes Close() send RST, so subsequent client sends fail.
            peer.LingerState = new LingerOption(true, 0);
            peer.Close();
            listener.Stop();

            var payload = new byte[64];
            for (var attempt = 0; attempt < 50 && !clientChannel.IsClosed(); attempt++)
            {
                var writer = StartBackground(() =>
                {
                    try
                    {
                        clientChannel.WriteAndFlush(ByteBuf.From(payload, 0, payload.Length));
                    }
                    catch (System.Exception)
                    {
                        // Throwing is acceptable; only blocking forever is a failure.
                    }
                });

                Assert.IsTrue(writer.Join(TimeSpan.FromSeconds(5)), $"Attempt {attempt}: WriteAndFlush hung after the peer reset the connection.");
                Thread.Sleep(20);
            }

            // IsConnected() is never cleared on disconnect, so check Closed instead.
            Assert.IsTrue(clientChannel.IsClosed(), "Send failures never closed the channel; the error path was not exercised.");
        }
    }
}
